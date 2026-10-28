using System.Linq.Expressions;

namespace Aaas.Api.Common;

/// <summary>
/// Composition d'expressions sans <see cref="InvocationExpression"/> : le corps de la première est inséré à la place
/// du second paramètre de la seconde, pour que EF Core traduise le tout en SQL (ex. statut effectif dans une projection).
/// </summary>
public static class ExpressionComposition
{
    /// <summary>
    /// <c>x =&gt; suite(x, valeur(x))</c> : la règle <paramref name="valeur"/> reste écrite à un seul endroit
    /// et peut être réutilisée dans un filtre comme dans une projection.
    /// </summary>
    public static Expression<Func<T, TResultat>> Composer<T, TValeur, TResultat>(
        this Expression<Func<T, TValeur>> valeur,
        Expression<Func<T, TValeur, TResultat>> suite)
    {
        var parametre = suite.Parameters[0];
        var corpsValeur = new Remplacement(valeur.Parameters[0], parametre).Visit(valeur.Body);
        var corps = new Remplacement(suite.Parameters[1], corpsValeur).Visit(suite.Body);
        return Expression.Lambda<Func<T, TResultat>>(corps, parametre);
    }

    private sealed class Remplacement(ParameterExpression cible, Expression remplacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == cible ? remplacement : base.VisitParameter(node);
    }
}
