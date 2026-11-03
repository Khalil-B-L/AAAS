using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Aaas.Api.Tests.Outils;

/// <summary>
/// Avant le <c>SaveChanges</c> qui modifie ou supprime un animal, change sa <c>RowVersion</c> (écriture concurrente
/// simulée) : depuis une autre connexion pour une modification (<c>ReadCommitted</c>), depuis la transaction du
/// handler pour une suppression (<c>Serializable</c>, dont les verrous bloqueraient une autre connexion).
/// </summary>
public sealed class ModificationConcurrente(IServiceProvider services) : SaveChangesInterceptor
{
    public bool Declenche { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var entree = eventData.Context!.ChangeTracker.Entries<Animal>()
            .FirstOrDefault(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (entree is not null && !Declenche)
        {
            Declenche = true;
            var id = entree.Entity.Id;
            if (entree.State == EntityState.Deleted)
            {
                // Suppression en Serializable : une autre connexion attendrait le commit, lui-même en attente ici.
                await eventData.Context.Database.ExecuteSqlAsync(
                    $"UPDATE Animaux SET Nom = Nom WHERE Id = {id}", cancellationToken);
            }
            else
            {
                using var scope = services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Animal>()
                    .Where(a => a.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.Nom, a => a.Nom), cancellationToken);
            }
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
