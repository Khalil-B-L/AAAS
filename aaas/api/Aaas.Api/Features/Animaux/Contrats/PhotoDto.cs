namespace Aaas.Api.Features.Animaux.Contrats;

/// <summary>Photo d'un animal (<c>contrats-api.md</c> §6) : <c>url</c> relative (<c>/fichiers/&lt;clé&gt;</c>).</summary>
public sealed record PhotoDto(int Id, string Url, int Ordre);
