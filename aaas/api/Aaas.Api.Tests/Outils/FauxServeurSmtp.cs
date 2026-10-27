using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Aaas.Api.Tests.Outils;

/// <summary>
/// Serveur SMTP local d'une seule session : STARTTLS (certificat auto-signé), AUTH PLAIN, accepte le message,
/// puis coupe la connexion sans répondre au QUIT. <see cref="Session"/> rend le contenu reçu après DATA.
/// </summary>
public sealed class FauxServeurSmtp : IDisposable
{
    private readonly TcpListener _ecoute = new(IPAddress.Loopback, 0);
    private readonly X509Certificate2 _certificat = CreerCertificat();

    public FauxServeurSmtp()
    {
        _ecoute.Start();
        Port = ((IPEndPoint)_ecoute.LocalEndpoint).Port;
        Session = Task.Run(ServirAsync);
    }

    public int Port { get; }

    public Task<string> Session { get; }

    public bool QuitRecu { get; private set; }

    public void Dispose()
    {
        _ecoute.Stop();
        _certificat.Dispose();
    }

    private async Task<string> ServirAsync()
    {
        using var tcp = await _ecoute.AcceptTcpClientAsync();
        Stream flux = tcp.GetStream();
        var (lecteur, ecrivain) = Ouvrir(flux);
        var tls = false;
        var donnees = new StringBuilder();
        await ecrivain.WriteLineAsync("220 faux ESMTP");
        while (await lecteur.ReadLineAsync() is { } ligne)
        {
            var commande = ligne.ToUpperInvariant();
            if (commande.StartsWith("EHLO", StringComparison.Ordinal))
            {
                await ecrivain.WriteLineAsync("250-faux");
                await ecrivain.WriteLineAsync(tls ? "250 AUTH PLAIN" : "250 STARTTLS");
            }
            else if (commande == "STARTTLS")
            {
                await ecrivain.WriteLineAsync("220 pret");
                var ssl = new SslStream(flux);
                await ssl.AuthenticateAsServerAsync(_certificat);
                flux = ssl;
                (lecteur, ecrivain) = Ouvrir(flux);
                tls = true;
            }
            else if (commande.StartsWith("AUTH PLAIN", StringComparison.Ordinal))
            {
                if (commande.Trim() == "AUTH PLAIN")
                {
                    await ecrivain.WriteLineAsync("334 ");
                    await lecteur.ReadLineAsync();
                }
                await ecrivain.WriteLineAsync("235 ok");
            }
            else if (commande.StartsWith("MAIL", StringComparison.Ordinal) || commande.StartsWith("RCPT", StringComparison.Ordinal))
            {
                await ecrivain.WriteLineAsync("250 ok");
            }
            else if (commande == "DATA")
            {
                await ecrivain.WriteLineAsync("354 fin par .");
                while (await lecteur.ReadLineAsync() is { } contenu && contenu != ".")
                    donnees.AppendLine(contenu);
                await ecrivain.WriteLineAsync("250 accepte");
            }
            else if (commande == "QUIT")
            {
                QuitRecu = true;
                break; // connexion coupée sans réponse
            }
            else
            {
                await ecrivain.WriteLineAsync("502 inconnu");
            }
        }
        return donnees.ToString();
    }

    private static (StreamReader, StreamWriter) Ouvrir(Stream flux) =>
        (new StreamReader(flux, Encoding.UTF8, false, 1024, leaveOpen: true),
         new StreamWriter(flux, new UTF8Encoding(false), 1024, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true });

    private static X509Certificate2 CreerCertificat()
    {
        using var rsa = RSA.Create(2048);
        var demande = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificat = demande.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        // Rechargé depuis un PFX : SslStream côté serveur exige une clé privée utilisable sous Windows.
        return X509CertificateLoader.LoadPkcs12(certificat.Export(X509ContentType.Pfx), null);
    }
}
