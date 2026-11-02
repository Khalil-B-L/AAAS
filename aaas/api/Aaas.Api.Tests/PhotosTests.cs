using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Tests.Outils;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using static Aaas.Api.Tests.Outils.AssertionsErreur;
using static Aaas.Api.Tests.Outils.Comptes;

namespace Aaas.Api.Tests;

/// <summary>Stockage des photos (AD-12) : une couverture par ligne de la matrice de la story « Stockage des photos ».</summary>
[Collection(ApiCollection.Name)]
public partial class PhotosTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    [GeneratedRegex("^/fichiers/[0-9a-f]{32}\\.(jpg|png|webp)$")]
    private static partial Regex FormatUrl();

    private Task<string> JetonAdminAsync() => factory.ConnecterAsync(AaasFactory.AdminEmail, AaasFactory.AdminPassword);

    private async Task<int> AnimalAsync(string nom = "Caramel")
    {
        var animal = Animaux.Nouveau(nom);
        await factory.InsererAsync(animal);
        return animal.Id;
    }

    private static async Task<(int Id, string Url, int Ordre)> LirePhotoAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        Assert.Equal(["id", "url", "ordre"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        return (doc.RootElement.GetProperty("id").GetInt32(), doc.RootElement.GetProperty("url").GetString()!,
            doc.RootElement.GetProperty("ordre").GetInt32());
    }

    private async Task<(int Id, string Url, int Ordre)> AjouterAsync(int animalId, string jeton, byte[]? contenu = null,
        string typeMime = "image/jpeg") =>
        await LirePhotoAsync(await factory.EnvoyerPhotoAsync(animalId, jeton, contenu ?? Fichiers.Jpeg(1024), typeMime));

    private async Task<string?> PhotoPrincipaleAsync(int animalId)
    {
        var response = await factory.CreateClient().GetAsync("/api/animaux", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        var carte = doc.RootElement.GetProperty("items").EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == animalId);
        var url = carte.GetProperty("photoPrincipaleUrl");
        return url.ValueKind == JsonValueKind.Null ? null : url.GetString();
    }

    private async Task<int[]> OrdresAsync(int animalId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<PhotoAnimal>()
            .Where(p => p.AnimalId == animalId).OrderBy(p => p.Ordre).Select(p => p.Ordre).ToArrayAsync(Ct);
    }

    private static void AssertRefusFichier(Dictionary<string, string> errors, string message)
    {
        Assert.Equal([RegleFichier.Champ], errors.Keys);
        Assert.Equal(message, errors[RegleFichier.Champ]);
    }

    // --- Upload valide --------------------------------------------------------------------------------

    [Fact]
    public async Task Upload_JPEG_valide_renvoie_201_et_l_URL_sert_le_fichier()
    {
        var animalId = await AnimalAsync();
        var contenu = Fichiers.Jpeg(200 * 1024);

        var (id, url, ordre) = await LirePhotoAsync(await factory.EnvoyerPhotoAsync(animalId, await JetonAdminAsync(), contenu));

        Assert.True(id > 0);
        Assert.Matches(FormatUrl(), url);
        Assert.EndsWith(".jpg", url);
        Assert.Equal(1, ordre);

        var servi = await factory.CreateClient().GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, servi.StatusCode);
        Assert.Equal("image/jpeg", servi.Content.Headers.ContentType?.MediaType);
        Assert.Equal(contenu, await servi.Content.ReadAsByteArrayAsync(Ct));
        Assert.Equal("nosniff", servi.Headers.GetValues("X-Content-Type-Options").Single());

        // La base ne stocke que la clé générée : ni chemin, ni nom de fichier du client.
        using var scope = factory.Services.CreateScope();
        var cle = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<PhotoAnimal>()
            .Where(p => p.Id == id).Select(p => p.Cle).SingleAsync(Ct);
        Assert.Equal(url["/fichiers/".Length..], cle);
        Assert.Contains(cle, factory.FichiersStockes());
    }

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("webp", "image/webp")]
    public async Task Upload_PNG_et_WebP_valides_gardent_leur_type(string extension, string typeMime)
    {
        var animalId = await AnimalAsync();
        var contenu = extension == "png" ? Fichiers.Png() : Fichiers.WebP();

        var (_, url, _) = await AjouterAsync(animalId, await JetonAdminAsync(), contenu, typeMime);

        Assert.EndsWith("." + extension, url);
        var servi = await factory.CreateClient().GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, servi.StatusCode);
        Assert.Equal(typeMime, servi.Content.Headers.ContentType?.MediaType);
        Assert.Equal(contenu, await servi.Content.ReadAsByteArrayAsync(Ct));
    }

    [Fact]
    public async Task Upload_de_exactement_5_Mo_est_accepte()
    {
        var animalId = await AnimalAsync();

        var response = await factory.EnvoyerPhotoAsync(animalId, await JetonAdminAsync(), Fichiers.Jpeg(Fichiers.CinqMo));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // --- Ordre et photo principale --------------------------------------------------------------------

    [Fact]
    public async Task Ordres_successifs_et_photo_principale_d_ordre_minimal()
    {
        var animalId = await AnimalAsync();
        var sansPhoto = await AnimalAsync("Rex");
        var jeton = await JetonAdminAsync();

        var premiere = await AjouterAsync(animalId, jeton);
        var deuxieme = await AjouterAsync(animalId, jeton, Fichiers.Png(), "image/png");
        var troisieme = await AjouterAsync(animalId, jeton, Fichiers.WebP(), "image/webp");

        Assert.Equal([1, 2, 3], new[] { premiere.Ordre, deuxieme.Ordre, troisieme.Ordre });
        Assert.Equal(premiere.Url, await PhotoPrincipaleAsync(animalId));
        Assert.Null(await PhotoPrincipaleAsync(sansPhoto));
    }

    [Fact]
    public async Task Les_ordres_sont_propres_a_chaque_animal()
    {
        var a = await AnimalAsync();
        var b = await AnimalAsync("Rex");
        var jeton = await JetonAdminAsync();

        await AjouterAsync(a, jeton);
        await AjouterAsync(a, jeton);

        Assert.Equal(1, (await AjouterAsync(b, jeton)).Ordre);
    }

    // --- Refus du fichier -----------------------------------------------------------------------------

    public static TheoryData<string, string> TypesRefuses => new()
    {
        { "gif", "image/gif" },
        { "pdf", "application/pdf" },
        { "png", "image/jpeg" },   // JPEG déclaré, signature PNG
        { "texte", "image/jpeg" }, // JPEG déclaré, texte
        { "jpeg", "image/png" },   // PNG déclaré, signature JPEG
        { "jpeg", "application/octet-stream" },
    };

    [Theory]
    [MemberData(nameof(TypesRefuses))]
    public async Task Type_refuse_renvoie_400_errors_fichier_sans_ecrire_de_fichier(string contenu, string typeMime)
    {
        var animalId = await AnimalAsync();
        var avant = factory.FichiersStockes();
        var octets = contenu switch
        {
            "gif" => Fichiers.Gif(),
            "pdf" => Fichiers.Pdf(),
            "png" => Fichiers.Png(),
            "texte" => Fichiers.Texte(),
            _ => Fichiers.Jpeg(1024),
        };

        var response = await factory.EnvoyerPhotoAsync(animalId, await JetonAdminAsync(), octets, typeMime);

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageType);
        Assert.Equal(avant, factory.FichiersStockes());
        Assert.Empty(await OrdresAsync(animalId));
    }

    [Fact]
    public async Task Fichier_de_5_Mo_plus_1_octet_renvoie_400()
    {
        var animalId = await AnimalAsync();
        var avant = factory.FichiersStockes();

        var response = await factory.EnvoyerPhotoAsync(animalId, await JetonAdminAsync(), Fichiers.Jpeg(Fichiers.CinqMo + 1));

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageTaille);
        Assert.Equal(avant, factory.FichiersStockes());
    }

    [Fact]
    public async Task Onzieme_photo_renvoie_400_et_n_ecrit_aucun_fichier()
    {
        var animalId = await AnimalAsync();
        var jeton = await JetonAdminAsync();
        for (var i = 0; i < RegleFichier.PhotosMaxParAnimal; i++)
            await AjouterAsync(animalId, jeton, Fichiers.Png(), "image/png");
        var avant = factory.FichiersStockes();

        var response = await factory.EnvoyerPhotoAsync(animalId, jeton, Fichiers.Png(), "image/png");

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageNombre);
        Assert.Equal(avant, factory.FichiersStockes());
        Assert.Equal(Enumerable.Range(1, 10), await OrdresAsync(animalId));
    }

    [Fact]
    public async Task Champ_fichier_absent_renvoie_400()
    {
        var animalId = await AnimalAsync();

        var response = await factory.EnvoyerAsync(animalId, await JetonAdminAsync(),
            Fichiers.Formulaire("photo", (Fichiers.Jpeg(1024), "image/jpeg")));

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageAbsent);
    }

    [Fact]
    public async Task Fichier_vide_renvoie_400()
    {
        var animalId = await AnimalAsync();

        var response = await factory.EnvoyerPhotoAsync(animalId, await JetonAdminAsync(), []);

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageAbsent);
    }

    [Fact]
    public async Task Corps_non_multipart_renvoie_400()
    {
        var animalId = await AnimalAsync();

        var response = await factory.EnvoyerAsync(animalId, await JetonAdminAsync(), RequetesApi.JsonContent("{}"));

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageAbsent);
    }

    [Fact]
    public async Task Deux_fichiers_dans_une_requete_renvoient_400()
    {
        var animalId = await AnimalAsync();

        var response = await factory.EnvoyerAsync(animalId, await JetonAdminAsync(),
            Fichiers.Formulaire("fichier", (Fichiers.Jpeg(1024), "image/jpeg"), (Fichiers.Png(), "image/png")));

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessagePlusieurs);
        Assert.Empty(await OrdresAsync(animalId));
    }

    [Fact]
    public async Task Multipart_sans_boundary_renvoie_400()
    {
        var animalId = await AnimalAsync();
        var corps = new ByteArrayContent(Fichiers.Jpeg(1024));
        corps.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data");

        var response = await factory.EnvoyerAsync(animalId, await JetonAdminAsync(), corps);

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageIllisible);
        Assert.Empty(await OrdresAsync(animalId));
    }

    [Fact]
    public async Task Multipart_tronque_renvoie_400()
    {
        var animalId = await AnimalAsync();
        var formulaire = Fichiers.Formulaire("fichier", (Fichiers.Jpeg(1024), "image/jpeg"));
        var complet = await formulaire.ReadAsByteArrayAsync(Ct);
        // Même en-tête (boundary), corps coupé en plein fichier : la boundary de fin n'arrive jamais.
        var corps = new ByteArrayContent(complet[..(complet.Length / 2)]);
        corps.Headers.ContentType = formulaire.Headers.ContentType;

        var response = await factory.EnvoyerAsync(animalId, await JetonAdminAsync(), corps);

        AssertRefusFichier(await AssertValidationAsync(response), RegleFichier.MessageIllisible);
        Assert.Empty(await OrdresAsync(animalId));
    }

    // --- Dossier de stockage pas encore créé ----------------------------------------------------------

    [Fact]
    public async Task Supprimer_sur_un_dossier_pas_encore_cree_ne_fait_rien_et_ne_journalise_aucune_erreur()
    {
        var dossier = Path.Combine(Path.GetTempPath(), $"AaasTests_photos_absent_{Guid.NewGuid():N}");
        using var hote = factory.Configurer(new() { ["Storage:PhotosPath"] = dossier });
        var stockage = hote.Services.GetRequiredService<IFileStorage>();
        var erreursAvant = factory.Journal.Entrees.Count(e => e.Niveau >= Microsoft.Extensions.Logging.LogLevel.Error);

        stockage.Supprimer(CleFichier.Nouvelle(TypeFichier.Jpeg));

        // Refus après la validation du fichier (animal inconnu) : le nettoyage appelle Supprimer sur le dossier absent.
        var client = hote.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await JetonAdminAsync());
        var response = await client.PostAsync("/api/admin/animaux/999999/photos",
            Fichiers.Formulaire("fichier", (Fichiers.Jpeg(1024), "image/jpeg")), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(Directory.Exists(dossier));
        Assert.Equal(erreursAvant, factory.Journal.Entrees.Count(e => e.Niveau >= Microsoft.Extensions.Logging.LogLevel.Error));
    }

    // --- Animal inconnu, accès ------------------------------------------------------------------------

    [Fact]
    public async Task Animal_inconnu_renvoie_404_sans_ecrire_de_fichier()
    {
        var avant = factory.FichiersStockes();

        var response = await factory.EnvoyerPhotoAsync(999_999, await JetonAdminAsync(), Fichiers.Jpeg(1024));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, Common.ApiErrors.MessageIntrouvable);
        Assert.Equal(avant, factory.FichiersStockes());
    }

    [Fact]
    public async Task Membre_recoit_403_et_anonyme_401()
    {
        var animalId = await AnimalAsync();
        var photo = await AjouterAsync(animalId, await JetonAdminAsync());
        var membre = await factory.InscrireAsync(NouvelEmail());

        var envoiMembre = await factory.EnvoyerPhotoAsync(animalId, membre, Fichiers.Jpeg(1024));
        var envoiAnonyme = await factory.EnvoyerPhotoAsync(animalId, null, Fichiers.Jpeg(1024));
        var suppressionMembre = await factory.SupprimerPhotoAsync(animalId, photo.Id, membre);
        var suppressionAnonyme = await factory.SupprimerPhotoAsync(animalId, photo.Id, null);

        Assert.Equal(HttpStatusCode.Forbidden, envoiMembre.StatusCode);
        await AssertErreurAsync(envoiMembre, Common.ApiErrors.MessageInterdit);
        Assert.Equal(HttpStatusCode.Unauthorized, envoiAnonyme.StatusCode);
        await AssertErreurAsync(envoiAnonyme, Common.ApiErrors.MessageNonConnecte);
        Assert.Equal(HttpStatusCode.Forbidden, suppressionMembre.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, suppressionAnonyme.StatusCode);
        Assert.Equal(new[] { 1 }, await OrdresAsync(animalId));
    }

    // --- Suppression ----------------------------------------------------------------------------------

    [Fact]
    public async Task Suppression_efface_le_fichier_sans_renumeroter_et_change_la_principale()
    {
        var animalId = await AnimalAsync();
        var jeton = await JetonAdminAsync();
        var premiere = await AjouterAsync(animalId, jeton);
        var deuxieme = await AjouterAsync(animalId, jeton);
        await AjouterAsync(animalId, jeton);

        var response = await factory.SupprimerPhotoAsync(animalId, premiere.Id, jeton);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(premiere.Url["/fichiers/".Length..], factory.FichiersStockes());
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync(premiere.Url, Ct)).StatusCode);
        Assert.Equal(new[] { 2, 3 }, await OrdresAsync(animalId));
        Assert.Equal(deuxieme.Url, await PhotoPrincipaleAsync(animalId));

        // Ordre suivant : max + 1 des photos restantes.
        Assert.Equal(4, (await AjouterAsync(animalId, jeton)).Ordre);
    }

    [Fact]
    public async Task Supprimer_la_photo_d_un_autre_animal_renvoie_404_et_garde_le_fichier()
    {
        var a = await AnimalAsync();
        var b = await AnimalAsync("Rex");
        var jeton = await JetonAdminAsync();
        var photo = await AjouterAsync(a, jeton);

        var autreAnimal = await factory.SupprimerPhotoAsync(b, photo.Id, jeton);
        var inconnue = await factory.SupprimerPhotoAsync(a, 999_999, jeton);

        Assert.Equal(HttpStatusCode.NotFound, autreAnimal.StatusCode);
        await AssertErreurAsync(autreAnimal, Common.ApiErrors.MessageIntrouvable);
        Assert.Equal(HttpStatusCode.NotFound, inconnue.StatusCode);
        Assert.Equal(new[] { 1 }, await OrdresAsync(a));
        Assert.Contains(photo.Url["/fichiers/".Length..], factory.FichiersStockes());
    }

    [Fact]
    public async Task Supprimer_un_animal_supprime_ses_photos_en_cascade()
    {
        var animalId = await AnimalAsync();
        await AjouterAsync(animalId, await JetonAdminAsync());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Set<Animal>().Where(a => a.Id == animalId).ExecuteDeleteAsync(Ct);

        Assert.Empty(await OrdresAsync(animalId));
    }

    // --- GET /fichiers/:cle ---------------------------------------------------------------------------

    [Theory]
    [InlineData("/fichiers/abc")]
    [InlineData("/fichiers/../appsettings.json")]
    [InlineData("/fichiers/..%2Fappsettings.json")]
    [InlineData("/fichiers/..%5Cappsettings.json")]
    [InlineData("/fichiers/appsettings.json")]
    [InlineData("/fichiers/0123456789abcdef0123456789abcdef.gif")]
    [InlineData("/fichiers/0123456789ABCDEF0123456789ABCDEF.jpg")]
    [InlineData("/fichiers/0123456789abcdef0123456789abcdef.jpg")] // bon format, fichier absent
    public async Task Cle_invalide_ou_absente_renvoie_404(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, Common.ApiErrors.MessageIntrouvable);
    }

    // --- Commit en échec ------------------------------------------------------------------------------

    [Fact]
    public async Task Commit_en_echec_supprime_le_fichier_ecrit_et_propage_l_erreur()
    {
        var animalId = await AnimalAsync();
        var jeton = await JetonAdminAsync();
        var avant = factory.FichiersStockes();
        var intercepteur = new EchecSurPhoto(factory.PhotosPath);
        using var hote = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(intercepteur))));
        var client = hote.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jeton);

        var response = await client.PostAsync($"/api/admin/animaux/{animalId}/photos",
            Fichiers.Formulaire("fichier", (Fichiers.Jpeg(1024), "image/jpeg")), Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertErreurAsync(response, Common.ApiErrors.MessageInterne);
        Assert.True(intercepteur.FichierPresentAuSaveChanges, "Le fichier doit être écrit avant le commit.");
        Assert.Equal(avant, factory.FichiersStockes());
        Assert.Empty(await OrdresAsync(animalId));
    }

    /// <summary>Fait échouer le <c>SaveChanges</c> qui insère une photo, après avoir constaté que son fichier est déjà écrit.</summary>
    private sealed class EchecSurPhoto(string dossier) : SaveChangesInterceptor
    {
        public bool FichierPresentAuSaveChanges { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var photo = eventData.Context!.ChangeTracker.Entries<PhotoAnimal>().FirstOrDefault(e => e.State == EntityState.Added);
            if (photo is null)
                return base.SavingChangesAsync(eventData, result, cancellationToken);

            FichierPresentAuSaveChanges = File.Exists(Path.Combine(dossier, photo.Entity.Cle));
            throw new DbUpdateException("Échec simulé du commit.");
        }
    }

    // --- Démarrage ------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Storage_PhotosPath_vide_empeche_le_demarrage(string valeur)
    {
        var validation = factory.DemarrerAvecConfigurationInvalide(new() { ["Storage:PhotosPath"] = valeur });

        Assert.NotNull(validation);
        Assert.Contains("Storage:PhotosPath", validation.Message);
    }
}
