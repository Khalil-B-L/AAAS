using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Aaas.Api.Common.Email;
using Aaas.Api.Tests.Outils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Tests;

/// <summary>Envoi d'emails (story 4) : une couverture par ligne de la matrice, sauf « Smtp OK » (test réel, voir <see cref="EmailReelTests"/>).</summary>
[Collection(ApiCollection.Name)]
public class EmailTests(AaasFactory factory)
{
    private const string MotDePasseSmtp = "mot-de-passe-application-secret";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- Journal --------------------------------------------------------------------------------------

    [Fact]
    public async Task Journal_ecrit_destinataire_sujet_corps_et_reponse_en_Information()
    {
        var sender = factory.Services.GetRequiredService<IEmailSender>();
        var sujet = $"Sujet {Guid.NewGuid():N}";

        await sender.EnvoyerAsync(new MessageEmail(sujet, "Corps accentué\nligne 2", "visiteur@exemple.fr"), Ct);

        Assert.IsType<JournalEmailSender>(sender);
        var entree = Assert.Single(factory.Journal.Entrees, e => e.Message.Contains(sujet));
        Assert.Equal(LogLevel.Information, entree.Niveau);
        Assert.Equal(typeof(JournalEmailSender).FullName, entree.Categorie);
        Assert.Contains(AaasFactory.NotificationEmail, entree.Message);
        Assert.Contains("Corps accentué\nligne 2", entree.Message);
        Assert.Contains("visiteur@exemple.fr", entree.Message);
    }

    [Fact]
    public async Task Journal_sans_reponse_a_reste_valide()
    {
        var sender = factory.Services.GetRequiredService<IEmailSender>();
        var sujet = $"Sujet {Guid.NewGuid():N}";

        await sender.EnvoyerAsync(new MessageEmail(sujet, "Corps"), Ct);

        var entree = Assert.Single(factory.Journal.Entrees, e => e.Message.Contains(sujet));
        Assert.Contains("(aucun)", entree.Message);
    }

    // --- Choix de l'implémentation ----------------------------------------------------------------------

    [Fact]
    public void Testing_force_Journal_meme_en_mode_Smtp_avec_mot_de_passe()
    {
        using var hote = factory.Redemarrer(environnement: "Testing", configuration: new()
        {
            ["Email:Mode"] = "Smtp",
            ["Smtp:Password"] = MotDePasseSmtp,
        });

        Assert.IsType<JournalEmailSender>(hote.Services.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Smtp_sans_mot_de_passe_en_Development_demarre_en_Journal_avec_avertissement()
    {
        var avant = factory.Journal.Entrees.Count(EstAvertissementRepli);

        // "" écrase un éventuel Smtp:Password des user-secrets du poste.
        using var hote = factory.Redemarrer(environnement: "Development", configuration: new()
        {
            ["Email:Mode"] = "Smtp",
            ["Smtp:Password"] = "",
        });

        Assert.IsType<JournalEmailSender>(hote.Services.GetRequiredService<IEmailSender>());
        Assert.True(factory.Journal.Entrees.Count(EstAvertissementRepli) > avant);
    }

    [Fact]
    public void Smtp_avec_mot_de_passe_en_Development_resout_SmtpEmailSender()
    {
        using var hote = factory.Redemarrer(environnement: "Development", configuration: new()
        {
            ["Email:Mode"] = "Smtp",
            ["Smtp:Password"] = MotDePasseSmtp,
            ["Smtp:User"] = "expediteur@exemple.fr",
        });

        Assert.IsType<SmtpEmailSender>(hote.Services.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Journal_demande_en_Development_resout_Journal_sans_avertissement()
    {
        var avant = factory.Journal.Entrees.Count(EstAvertissementRepli);

        // Sans mot de passe : seul le mode Smtp demandé déclencherait l'avertissement de repli.
        using var hote = factory.Redemarrer(environnement: "Development", configuration: new()
        {
            ["Email:Mode"] = "Journal",
            ["Smtp:Password"] = "",
        });

        Assert.IsType<JournalEmailSender>(hote.Services.GetRequiredService<IEmailSender>());
        Assert.Equal(avant, factory.Journal.Entrees.Count(EstAvertissementRepli));
    }

    [Theory]
    [InlineData("Smtp:User", "pas-un-email")]
    [InlineData("Smtp:Port", "abc")]             // non numérique : le binder ne doit pas être appelé en mode Journal
    [InlineData("Smtp:TimeoutSecondes", "dix")]
    public void Journal_demande_demarre_meme_avec_une_section_Smtp_invalide(string cle, string valeur)
    {
        using var hote = factory.Redemarrer(environnement: "Development", configuration: new()
        {
            ["Email:Mode"] = "Journal",
            ["Smtp:Password"] = MotDePasseSmtp,
            [cle] = valeur,
        });

        Assert.IsType<JournalEmailSender>(hote.Services.GetRequiredService<IEmailSender>());
    }

    // --- Répondre-à mal formé --------------------------------------------------------------------------

    public static TheoryData<string> RepondreAInvalides => ["pas-un-email", "a@b", "Nom <a@b.fr>", "a@b@c.fr"];

    [Theory]
    [MemberData(nameof(RepondreAInvalides))]
    public async Task Journal_refuse_un_RepondreA_mal_forme(string repondreA)
    {
        var sender = factory.Services.GetRequiredService<IEmailSender>();
        var sujet = $"Sujet {Guid.NewGuid():N}";

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => sender.EnvoyerAsync(new MessageEmail(sujet, "Corps", repondreA), Ct));

        Assert.Contains(nameof(MessageEmail.RepondreA), exception.ParamName);
        Assert.DoesNotContain(factory.Journal.Entrees, e => e.Message.Contains(sujet));
    }

    [Theory]
    [MemberData(nameof(RepondreAInvalides))]
    public async Task Smtp_refuse_un_RepondreA_mal_forme_avant_toute_connexion(string repondreA)
    {
        var journal = new JournalDeTest();
        var connexions = 0;
        var sender = CreerSmtp(PortFerme(), timeoutSecondes: 5, journal, () =>
        {
            connexions++;
            return new MailKit.Net.Smtp.SmtpClient();
        });

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => sender.EnvoyerAsync(new MessageEmail("Sujet", "Corps", repondreA), Ct));

        Assert.Contains(nameof(MessageEmail.RepondreA), exception.ParamName);
        Assert.Equal(0, connexions);
        Assert.Empty(journal.Entrees);
    }

    // --- Smtp : envoi accepté puis QUIT en échec -------------------------------------------------------

    [Fact]
    public async Task Smtp_envoi_accepte_puis_QUIT_en_echec_reste_un_succes_avec_avertissement()
    {
        using var serveur = new FauxServeurSmtp();
        var journal = new JournalDeTest();
        var sender = CreerSmtp(serveur.Port, timeoutSecondes: 10, journal, () => new ClientQuitEnEchec());

        await sender.EnvoyerAsync(new MessageEmail("Réservation de Câline", "Corps accentué é", "visiteur@exemple.fr"), Ct);

        var donnees = await serveur.Session.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.Contains("From: expediteur@exemple.fr", donnees);
        Assert.Contains($"To: {AaasFactory.NotificationEmail}", donnees);
        Assert.Contains("Reply-To: visiteur@exemple.fr", donnees);
        Assert.Contains("charset=utf-8", donnees);
        Assert.True(serveur.QuitRecu);
        Assert.DoesNotContain(journal.Entrees, e => e.Niveau >= LogLevel.Error);
        var avertissement = Assert.Single(journal.Entrees, e => e.Niveau == LogLevel.Warning);
        Assert.Contains("Réservation de Câline", avertissement.Message);
        Assert.IsType<IOException>(avertissement.Exception);
    }

    /// <summary>Client MailKit qui accepte le certificat auto-signé et dont la déconnexion échoue (connexion coupée sur QUIT).</summary>
    private sealed class ClientQuitEnEchec : MailKit.Net.Smtp.SmtpClient
    {
        public ClientQuitEnEchec() => ServerCertificateValidationCallback = (_, _, _, _) => true;

        public override async Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
        {
            await base.DisconnectAsync(quit, cancellationToken);
            throw new IOException("Connexion coupée par le serveur pendant le QUIT.");
        }
    }

    // --- Configuration invalide ------------------------------------------------------------------------

    [Theory]
    [InlineData("Testing", "Admin:NotificationEmail", "", "Admin:NotificationEmail")]
    [InlineData("Testing", "Admin:NotificationEmail", "pas-un-email", "Admin:NotificationEmail")]
    [InlineData("Testing", "Email:Mode", "", "Email:Mode")]
    [InlineData("Testing", "Email:Mode", "Sms", "Email:Mode")]
    [InlineData("Testing", "Email:Mode", "smtp", "Email:Mode")]
    [InlineData("Development", "Smtp:User", "pas-un-email", "Smtp:User")]
    [InlineData("Development", "Smtp:TimeoutSecondes", "0", "Smtp:TimeoutSecondes")]
    public void Demarrage_refuse_avec_configuration_email_invalide(string environnement, string cle, string valeur, string attendu)
    {
        var validation = factory.DemarrerAvecConfigurationInvalide(new()
        {
            ["Email:Mode"] = "Smtp",
            ["Smtp:Password"] = MotDePasseSmtp,
            [cle] = valeur,
        }, environnement);

        Assert.NotNull(validation);
        Assert.Contains(attendu, validation.Message);
        Assert.DoesNotContain(MotDePasseSmtp, validation.Message);
    }

    // --- Smtp en échec ---------------------------------------------------------------------------------

    [Fact]
    public async Task Smtp_vers_un_port_ferme_leve_et_journalise_Error_sans_mot_de_passe()
    {
        var journal = new JournalDeTest();
        var sender = CreerSmtp(PortFerme(), timeoutSecondes: 5, journal);
        var chrono = Stopwatch.StartNew();

        var exception = await Record.ExceptionAsync(() => sender.EnvoyerAsync(new MessageEmail("Sujet en échec", "Corps"), Ct));

        Assert.NotNull(exception);
        Assert.True(chrono.Elapsed < TimeSpan.FromSeconds(5) + Marge, $"Échec après {chrono.Elapsed}.");
        AssertErreurJournaliseeSansMotDePasse(journal, exception, "Sujet en échec");
    }

    [Fact]
    public async Task Smtp_vers_un_serveur_muet_leve_au_plus_tard_apres_le_delai()
    {
        // Le serveur accepte la connexion mais n'envoie jamais le message d'accueil SMTP.
        using var muet = new TcpListener(IPAddress.Loopback, 0);
        muet.Start();
        var port = ((IPEndPoint)muet.LocalEndpoint).Port;
        var journal = new JournalDeTest();
        var sender = CreerSmtp(port, timeoutSecondes: 1, journal);
        var chrono = Stopwatch.StartNew();

        var exception = await Record.ExceptionAsync(() => sender.EnvoyerAsync(new MessageEmail("Sujet muet", "Corps"), Ct));

        Assert.NotNull(exception);
        Assert.True(chrono.Elapsed < TimeSpan.FromSeconds(1) + Marge, $"Échec après {chrono.Elapsed}.");
        Assert.IsNotType<OperationCanceledException>(exception, exactMatch: false);
        AssertErreurJournaliseeSansMotDePasse(journal, exception, "Sujet muet");
    }

    // --- Outils ----------------------------------------------------------------------------------------

    private static readonly TimeSpan Marge = TimeSpan.FromSeconds(3);

    private static bool EstAvertissementRepli(JournalDeTest.Entree e) =>
        e.Niveau == LogLevel.Warning && e.Categorie == typeof(EmailSetup).FullName && e.Message.Contains("Smtp:Password");

    private static SmtpEmailSender CreerSmtp(int port, int timeoutSecondes, JournalDeTest journal,
        Func<MailKit.Net.Smtp.SmtpClient>? creerClient = null)
    {
        var loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(journal));
        return new SmtpEmailSender(
            Options.Create(new SmtpOptions
            {
                Host = "127.0.0.1",
                Port = port,
                User = "expediteur@exemple.fr",
                Password = MotDePasseSmtp,
                TimeoutSecondes = timeoutSecondes,
            }),
            Options.Create(new NotificationOptions { NotificationEmail = AaasFactory.NotificationEmail }),
            loggerFactory.CreateLogger<SmtpEmailSender>())
        {
            CreerClient = creerClient ?? (() => new MailKit.Net.Smtp.SmtpClient()),
        };
    }

    /// <summary>Port local libéré juste après réservation : plus personne n'y écoute.</summary>
    private static int PortFerme()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void AssertErreurJournaliseeSansMotDePasse(JournalDeTest journal, Exception exception, string sujet)
    {
        var erreur = Assert.Single(journal.Entrees, e => e.Niveau == LogLevel.Error);
        Assert.Equal(typeof(SmtpEmailSender).FullName, erreur.Categorie);
        Assert.Contains(sujet, erreur.Message);
        Assert.Contains(AaasFactory.NotificationEmail, erreur.Message);
        Assert.NotNull(erreur.Exception);
        Assert.All(journal.Entrees, e =>
        {
            Assert.DoesNotContain(MotDePasseSmtp, e.Message);
            Assert.DoesNotContain(MotDePasseSmtp, e.Exception?.ToString() ?? string.Empty);
        });
        Assert.DoesNotContain(MotDePasseSmtp, exception.ToString());
    }
}
