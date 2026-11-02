using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Tests.Outils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aaas.Api.Tests;

/// <summary>Photos de démonstration (entrée 7) : une couverture par ligne de la matrice, et chaque illustration valide.</summary>
[Collection(ApiCollection.Name)]
public class DemoPhotosTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Development charge les user-secrets du poste : un Smtp:Password vide force le mode Journal, quel que soit le poste.
    private static readonly Dictionary<string, string?> ConfigurationDevelopment = new() { ["Smtp:Password"] = "" };

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    public static TheoryData<string> NomsDemo => new(DemoAnimaux.Noms);

    // --- Illustrations versionnées --------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(NomsDemo))]
    public async Task Chaque_illustration_copiee_en_sortie_est_un_PNG_accepte_par_RegleFichier(string nom)
    {
        var chemin = DemoPhotos.Fichier(DemoPhotos.DossierParDefaut, nom);
        Assert.True(File.Exists(chemin), $"Illustration absente de la sortie de build : {chemin}");
        var contenu = await File.ReadAllBytesAsync(chemin, Ct);
        await using var flux = new MemoryStream(contenu);
        var fichier = new FormFile(flux, 0, contenu.Length, RegleFichier.Champ, Path.GetFileName(chemin))
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };

        var (valide, refus) = await RegleFichier.ValiderAsync([fichier], Ct);

        Assert.Null(refus);
        Assert.Equal(TypeFichier.Png, valide!.Type);
        Assert.InRange(contenu.Length, 1, RegleFichier.TailleMax);
    }

    [Fact]
    public void Une_illustration_par_animal_de_demonstration()
    {
        var attendus = DemoAnimaux.Noms.Select(n => Path.GetFileName(DemoPhotos.Fichier(DemoPhotos.DossierParDefaut, n))).Order();
        var presents = Directory.GetFiles(DemoPhotos.DossierParDefaut).Select(f => Path.GetFileName(f)).Order();
        Assert.Equal(attendus, presents);
    }

    // --- Matrice ------------------------------------------------------------------------------------------

    [Fact]
    public async Task Premier_demarrage_Development_chaque_animal_de_demo_recoit_une_photo_servie()
    {
        using (factory.Redemarrer(ConfigurationDevelopment, "Development"))
        {
        }

        var photos = await PhotosParAnimalAsync();
        Assert.Equal(DemoAnimaux.Noms.Order(), photos.Keys.Order());
        Assert.All(photos.Values, ordres => Assert.Equal([1], ordres));

        // Cartes (non adoptés) : photo principale renseignée et servie en PNG.
        var cartes = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/animaux", Ct);
        var urls = cartes.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("photoPrincipaleUrl").GetString()).ToList();
        Assert.NotEmpty(urls);
        foreach (var url in urls)
        {
            Assert.NotNull(url);
            Assert.StartsWith("/fichiers/", url);
            var servi = await factory.CreateClient().GetAsync(url, Ct);
            Assert.Equal(HttpStatusCode.OK, servi.StatusCode);
            Assert.Equal("image/png", servi.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Redemarrage_n_ajoute_aucune_photo()
    {
        using (factory.Redemarrer(ConfigurationDevelopment, "Development"))
        {
        }
        var stockesAvant = factory.FichiersStockes();

        using (factory.Redemarrer(ConfigurationDevelopment, "Development"))
        {
        }

        var photos = await PhotosParAnimalAsync();
        Assert.Equal(DemoAnimaux.Nombre, photos.Count);
        Assert.All(photos.Values, ordres => Assert.Equal([1], ordres));
        Assert.Equal(stockesAvant, factory.FichiersStockes());
    }

    [Fact]
    public async Task Animal_de_demo_avec_photo_admin_ne_recoit_rien_et_animal_hors_demo_non_plus()
    {
        var caramel = Animaux.Nouveau("Caramel");
        var rex = Animaux.Nouveau("Rex", espece: Espece.Chien);
        var horsDemo = Animaux.Nouveau("Existant");
        await factory.InsererAsync(caramel, rex, horsDemo);
        var jeton = await factory.ConnecterAsync(AaasFactory.AdminEmail, AaasFactory.AdminPassword);
        var envoi = await factory.EnvoyerPhotoAsync(caramel.Id, jeton, Fichiers.Jpeg(1024));
        Assert.Equal(HttpStatusCode.Created, envoi.StatusCode);
        var cleAdmin = await ClesAsync(caramel.Id);

        using (factory.Redemarrer(ConfigurationDevelopment, "Development"))
        {
        }

        // Des animaux existent : DemoAnimaux ne crée rien ; seul Rex (démo, sans photo) reçoit son illustration.
        var photos = await PhotosParAnimalAsync();
        Assert.Equal(["Caramel", "Rex"], photos.Keys.Order());
        Assert.Equal(cleAdmin, await ClesAsync(caramel.Id));
        Assert.EndsWith(".jpg", Assert.Single(cleAdmin));
        Assert.EndsWith(".png", Assert.Single(await ClesAsync(rex.Id)));
        Assert.Empty(await ClesAsync(horsDemo.Id));
    }

    [Fact]
    public async Task En_Testing_aucune_photo_de_demonstration()
    {
        await factory.InsererAsync(Animaux.Nouveau("Caramel"));

        using (factory.Redemarrer([]))
        {
        }

        Assert.Empty(await PhotosParAnimalAsync());
    }

    [Fact]
    public async Task Illustration_refusee_saute_l_animal_journalise_et_continue()
    {
        var caramel = Animaux.Nouveau("Caramel");
        var rex = Animaux.Nouveau("Rex", espece: Espece.Chien);
        await factory.InsererAsync(caramel, rex);
        var dossier = Directory.CreateTempSubdirectory("AaasTests_illustrations_").FullName;
        try
        {
            // Caramel : faux PNG (texte) ; Rex : la vraie illustration.
            await File.WriteAllTextAsync(DemoPhotos.Fichier(dossier, "Caramel"), "Ceci n'est pas une photo.", Ct);
            File.Copy(DemoPhotos.Fichier(DemoPhotos.DossierParDefaut, "Rex"), DemoPhotos.Fichier(dossier, "Rex"));

            await DemoPhotos.AttacherAsync(factory.Services, dossier, Ct);
        }
        finally
        {
            Directory.Delete(dossier, recursive: true);
        }

        Assert.Empty(await ClesAsync(caramel.Id));
        Assert.Single(await ClesAsync(rex.Id));
        Assert.Contains(factory.Journal.Entrees, e =>
            e.Niveau == LogLevel.Warning && e.Categorie == typeof(DemoPhotos).FullName
            && e.Message.Contains("Caramel") && e.Message.Contains(RegleFichier.MessageType));
    }

    [Fact]
    public async Task Animal_portant_un_nom_de_demo_mais_d_une_autre_espece_ne_recoit_rien()
    {
        var rexChat = Animaux.Nouveau("Rex", espece: Espece.Chat);
        await factory.InsererAsync(rexChat);

        using (factory.Redemarrer(ConfigurationDevelopment, "Development"))
        {
        }

        Assert.Empty(await ClesAsync(rexChat.Id));
    }

    [Fact]
    public async Task Illustration_absente_saute_l_animal_journalise_et_continue()
    {
        var caramel = Animaux.Nouveau("Caramel");
        var rex = Animaux.Nouveau("Rex", espece: Espece.Chien);
        await factory.InsererAsync(caramel, rex);
        var dossier = Directory.CreateTempSubdirectory("AaasTests_illustrations_").FullName;
        try
        {
            // Pas de caramel.png ; Rex : la vraie illustration.
            File.Copy(DemoPhotos.Fichier(DemoPhotos.DossierParDefaut, "Rex"), DemoPhotos.Fichier(dossier, "Rex"));

            await DemoPhotos.AttacherAsync(factory.Services, dossier, Ct);
        }
        finally
        {
            Directory.Delete(dossier, recursive: true);
        }

        Assert.Empty(await ClesAsync(caramel.Id));
        Assert.Single(await ClesAsync(rex.Id));
        Assert.Contains(factory.Journal.Entrees, e =>
            e.Niveau == LogLevel.Warning && e.Categorie == typeof(DemoPhotos).FullName
            && e.Message.Contains("absente") && e.Message.Contains("Caramel"));
    }

    // --- Outils -------------------------------------------------------------------------------------------

    /// <summary>Ordres des photos, par nom d'animal (seulement les animaux ayant au moins une photo).</summary>
    private async Task<Dictionary<string, int[]>> PhotosParAnimalAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lignes = await db.Set<PhotoAnimal>()
            .Join(db.Set<Animal>(), p => p.AnimalId, a => a.Id, (p, a) => new { a.Nom, p.Ordre })
            .ToListAsync(Ct);
        return lignes.GroupBy(l => l.Nom).ToDictionary(g => g.Key, g => g.Select(l => l.Ordre).Order().ToArray());
    }

    private async Task<string[]> ClesAsync(int animalId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<PhotoAnimal>()
            .Where(p => p.AnimalId == animalId).Select(p => p.Cle).ToArrayAsync(Ct);
    }
}
