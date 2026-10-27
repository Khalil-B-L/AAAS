namespace Aaas.Api.Common;

/// <summary>Cas d'usage qui modifie l'état (AD-2, AD-3), appelé directement par l'endpoint.</summary>
public interface ICommandHandler<in TCommand, TReponse>
{
    Task<Result<TReponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Cas d'usage qui modifie l'état sans renvoyer de valeur (succès → 204).</summary>
public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Cas d'usage en lecture seule (AD-3), appelé directement par l'endpoint.</summary>
public interface IQueryHandler<in TQuery, TReponse>
{
    Task<Result<TReponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

public static class HandlersServiceCollectionExtensions
{
    private static readonly Type[] InterfacesHandler =
    [
        typeof(ICommandHandler<,>),
        typeof(ICommandHandler<>),
        typeof(IQueryHandler<,>),
    ];

    /// <summary>Enregistre en scoped toutes les implémentations de handlers de l'assembly de l'API.</summary>
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        var implementations = typeof(HandlersServiceCollectionExtensions).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var implementation in implementations)
        {
            foreach (var contrat in implementation.GetInterfaces()
                         .Where(i => i.IsGenericType && InterfacesHandler.Contains(i.GetGenericTypeDefinition())))
            {
                services.AddScoped(contrat, implementation);
            }
        }
        return services;
    }
}
