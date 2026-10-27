using Microsoft.Extensions.Options;

namespace Aaas.Api.Tests.Outils;

/// <summary>Gardes de démarrage : retrouver l'<see cref="OptionsValidationException"/> levée par un hôte mal configuré.</summary>
public static class ValidationDemarrage
{
    /// <summary>
    /// Démarre un hôte avec la configuration en mémoire donnée et renvoie l'<see cref="OptionsValidationException"/> levée
    /// au démarrage (<c>null</c> s'il a démarré ou échoué autrement).
    /// </summary>
    public static OptionsValidationException? DemarrerAvecConfigurationInvalide(this AaasFactory factory,
        Dictionary<string, string?> configuration, string? environnement = null)
    {
        using var invalide = factory.Configurer(configuration, environnement);
        return TrouverValidation(Record.Exception(() => invalide.Services));
    }

    public static OptionsValidationException? TrouverValidation(Exception? exception) => exception switch
    {
        null => null,
        OptionsValidationException ove => ove,
        AggregateException ae => ae.InnerExceptions.Select(TrouverValidation).FirstOrDefault(e => e is not null),
        _ => TrouverValidation(exception.InnerException),
    };
}
