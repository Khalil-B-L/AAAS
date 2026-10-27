namespace Aaas.Api.Features.Auth.Contrats;

/// <summary>Réponse d'inscription et de connexion (<c>contrats-api.md</c> §6).</summary>
public sealed record AuthReponseDto(string Jeton, DateTimeOffset ExpireLe, UtilisateurDto Utilisateur);
