using System.Data;
using Aaas.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Common.Data;

/// <summary>
/// Seule façon d'ouvrir une transaction (AD-6) : transaction, travail, <c>SaveChanges</c> + commit seulement
/// si le travail réussit, rollback sinon ; nouvel essai borné sur interblocage SQL 1205 avec
/// <c>ChangeTracker.Clear()</c> entre deux essais. Le travail ne doit ni ouvrir de transaction ni commiter.
/// </summary>
public sealed class TransactionRunner(AppDbContext db)
{
    public const int EssaisMax = 3;

    public Task<Result<T>> RunAsync<T>(
        IsolationLevel isolation,
        Func<CancellationToken, Task<Result<T>>> travail,
        CancellationToken cancellationToken = default) =>
        ExecuterAsync(isolation, travail, r => r.EstSucces, cancellationToken);

    /// <summary>Variante pour un travail sans valeur (succès → 204).</summary>
    public Task<Result> RunAsync(
        IsolationLevel isolation,
        Func<CancellationToken, Task<Result>> travail,
        CancellationToken cancellationToken = default) =>
        ExecuterAsync(isolation, travail, r => r.EstSucces, cancellationToken);

    private async Task<TResult> ExecuterAsync<TResult>(
        IsolationLevel isolation,
        Func<CancellationToken, Task<TResult>> travail,
        Func<TResult, bool> estSucces,
        CancellationToken cancellationToken)
    {
        for (var essai = 1; ; essai++)
        {
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(isolation, cancellationToken);

                var resultat = await travail(cancellationToken);
                if (!estSucces(resultat))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    // Les modifications suivies du travail refusé ne doivent pas fuiter dans un SaveChanges ultérieur.
                    db.ChangeTracker.Clear();
                    return resultat;
                }

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return resultat;
            }
            catch (Exception ex) when (SqlErreurs.EstInterblocage(ex) && essai < EssaisMax)
            {
                db.ChangeTracker.Clear();
            }
            catch
            {
                // Toute autre exception (ex. unicité 2601/2627 traitée par le handler) : les entités refusées
                // ne doivent pas être réinsérées par un SaveChanges ultérieur dans la même portée.
                db.ChangeTracker.Clear();
                throw;
            }
        }
    }
}

/// <summary>Reconnaissance des erreurs SQL Server utiles aux handlers (exception directe ou interne).</summary>
public static class SqlErreurs
{
    /// <summary>Interblocage : erreur SQL 1205.</summary>
    public static bool EstInterblocage(Exception exception) => ContientErreur(exception, 1205);

    /// <summary>Violation d'index ou de contrainte unique : erreurs SQL 2601 et 2627.</summary>
    public static bool EstViolationUnicite(Exception exception) => ContientErreur(exception, 2601, 2627);

    private static bool ContientErreur(Exception? exception, params int[] numeros)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is SqlException sql && numeros.Contains(sql.Number))
                return true;
        }
        return false;
    }
}
