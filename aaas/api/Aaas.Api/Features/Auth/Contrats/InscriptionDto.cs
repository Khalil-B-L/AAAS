namespace Aaas.Api.Features.Auth.Contrats;

/// <summary>Corps de <c>POST /api/auth/inscription</c> (<c>contrats-api.md</c> §6). Tout est validé par le handler.</summary>
public sealed record InscriptionDto(string? Prenom, string? Nom, string? Email, string? Telephone, string? MotDePasse);
