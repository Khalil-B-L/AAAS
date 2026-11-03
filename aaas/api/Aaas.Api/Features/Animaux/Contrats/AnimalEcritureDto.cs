namespace Aaas.Api.Features.Animaux.Contrats;

/// <summary>
/// Corps de <c>POST</c> et <c>PUT /api/admin/animaux</c> (<c>contrats-api.md</c> §6) : exactement les 12 champs
/// d'édition, jamais de statut (un champ <c>statut</c> envoyé est ignoré). Tout est nullable et validé par le handler
/// (<see cref="Valider"/>) ; une valeur d'énumération inconnue ou de casse différente est refusée en 400 par le
/// convertisseur global avant le handler.
/// </summary>
public sealed record AnimalEcritureDto(
    string? Nom,
    Espece? Espece,
    Sexe? Sexe,
    TrancheAge? Age,
    Taille? Taille,
    string? Description,
    bool? Vaccine,
    bool? Sterilise,
    bool? Puce,
    Entente? EntenteEnfants,
    Entente? EntenteChiens,
    Entente? EntenteChats)
{
    public const string MessageChoix = "Choisissez une valeur.";

    /// <summary>
    /// Validation partagée création / modification : erreurs par champ (clés JSON camelCase), vide si valide.
    /// <c>nom</c> et <c>description</c> trimés et requis, énumérations et booléens requis.
    /// </summary>
    public IReadOnlyDictionary<string, string> Valider()
    {
        var errors = new Dictionary<string, string>();

        var nom = NomTrime;
        if (nom.Length == 0)
            errors["nom"] = "Indiquez le nom de l'animal.";
        else if (nom.Length > Animal.NomMax)
            errors["nom"] = $"Nom trop long ({Animal.NomMax} caractères maximum).";

        var description = DescriptionTrimee;
        if (description.Length == 0)
            errors["description"] = "Indiquez la description de l'animal.";
        else if (description.Length > Animal.DescriptionMax)
            errors["description"] = $"Description trop longue ({Animal.DescriptionMax} caractères maximum).";

        Requis(Espece, "espece", errors);
        Requis(Sexe, "sexe", errors);
        Requis(Age, "age", errors);
        Requis(Taille, "taille", errors);
        Requis(Vaccine, "vaccine", errors);
        Requis(Sterilise, "sterilise", errors);
        Requis(Puce, "puce", errors);
        Requis(EntenteEnfants, "ententeEnfants", errors);
        Requis(EntenteChiens, "ententeChiens", errors);
        Requis(EntenteChats, "ententeChats", errors);

        return errors;
    }

    /// <summary>Copie les champs d'édition sur <paramref name="animal"/> ; à n'appeler qu'après un <see cref="Valider"/> sans erreur.</summary>
    public void Appliquer(Animal animal)
    {
        animal.Nom = NomTrime;
        animal.Espece = Espece!.Value;
        animal.Sexe = Sexe!.Value;
        animal.Age = Age!.Value;
        animal.Taille = Taille!.Value;
        animal.Description = DescriptionTrimee;
        animal.Vaccine = Vaccine!.Value;
        animal.Sterilise = Sterilise!.Value;
        animal.Puce = Puce!.Value;
        animal.EntenteEnfants = EntenteEnfants!.Value;
        animal.EntenteChiens = EntenteChiens!.Value;
        animal.EntenteChats = EntenteChats!.Value;
    }

    private string NomTrime => (Nom ?? string.Empty).Trim();

    private string DescriptionTrimee => (Description ?? string.Empty).Trim();

    private static void Requis<T>(T? valeur, string champ, Dictionary<string, string> errors) where T : struct
    {
        if (valeur is null)
            errors[champ] = MessageChoix;
    }
}
