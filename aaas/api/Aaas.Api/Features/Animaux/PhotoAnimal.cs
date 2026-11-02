namespace Aaas.Api.Features.Animaux;

/// <summary>
/// Photo d'un animal (AD-12) : la base ne stocke que la clé du fichier générée par le serveur, jamais un chemin ni le nom
/// envoyé par le client. <see cref="Ordre"/> = max + 1 attribué à l'ajout, jamais renuméroté ; la photo principale est
/// celle d'ordre minimal.
/// </summary>
public class PhotoAnimal
{
    public const int CleMax = 64;

    /// <summary>Pour EF Core.</summary>
    private PhotoAnimal()
    {
    }

    public PhotoAnimal(int animalId, string cle, int ordre)
    {
        AnimalId = animalId;
        Cle = cle;
        Ordre = ordre;
    }

    public int Id { get; private set; }

    public int AnimalId { get; private set; }

    public string Cle { get; private set; } = string.Empty;

    public int Ordre { get; private set; }
}
