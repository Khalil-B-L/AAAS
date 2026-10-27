namespace Aaas.Api.Common;

/// <summary>
/// Seule source des liens vers le front construits par l'API (AD-21), alignée sur
/// <c>contrats-api.md</c> §7. Chemins relatifs : l'email les rend absolus avec <c>Site:BaseUrl</c>.
/// </summary>
public static class LiensFront
{
    public const string Accueil = "/";
    public const string Adopter = "/adopter";
    public const string IlsOntTrouveUneFamille = "/ils-ont-trouve-une-famille";
    public const string Actualites = "/actualites";
    public const string NousAiderDons = "/nous-aider/dons";
    public const string NousAiderBenevolat = "/nous-aider/benevolat";
    public const string NousAiderFamilleAccueil = "/nous-aider/famille-accueil";
    public const string Contact = "/contact";
    public const string Connexion = "/connexion";
    public const string Inscription = "/inscription";
    public const string MesReservations = "/mes-reservations";
    public const string MesFavoris = "/mes-favoris";
    public const string AdminReservations = "/admin/reservations";
    public const string AdminAnimaux = "/admin/animaux";
    public const string AdminAnimalNouveau = "/admin/animaux/nouveau";
    public const string AdminActualites = "/admin/actualites";
    public const string AccesRefuse = "/acces-refuse";

    /// <summary>Nom du paramètre de retour après connexion ou inscription.</summary>
    public const string ParametreRetour = "returnUrl";

    /// <summary>Nom du paramètre d'onglet de l'admin des réservations.</summary>
    public const string ParametreStatut = "statut";

    public static string FicheAnimal(int id) => $"/animaux/{id}";

    public static string ReserverAnimal(int id) => $"/animaux/{id}/reserver";

    public static string ReservationEnvoyee(int id) => $"/reservations/{id}/envoyee";

    public static string AdminAnimal(int id) => $"/admin/animaux/{id}";

    /// <summary><c>/admin/reservations?statut=…</c> (valeur de <c>StatutReservation</c>, même casse).</summary>
    public static string AdminReservationsParStatut(string statut) =>
        $"{AdminReservations}?{ParametreStatut}={Uri.EscapeDataString(statut)}";

    public static string ConnexionAvecRetour(string returnUrl) =>
        $"{Connexion}?{ParametreRetour}={Uri.EscapeDataString(returnUrl)}";

    public static string InscriptionAvecRetour(string returnUrl) =>
        $"{Inscription}?{ParametreRetour}={Uri.EscapeDataString(returnUrl)}";
}
