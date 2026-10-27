namespace Aaas.Api.Features.Auth.Contrats;

/// <summary>Corps de <c>POST /api/auth/connexion</c> (<c>contrats-api.md</c> §6).</summary>
public sealed record ConnexionDto(string? Email, string? MotDePasse);
