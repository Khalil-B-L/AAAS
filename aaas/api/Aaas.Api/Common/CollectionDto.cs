namespace Aaas.Api.Common;

/// <summary>Enveloppe de toute collection renvoyée par l'API (AD-11, AD-22) : <c>{ items, serverNow }</c>, même vide.</summary>
public sealed record CollectionDto<T>(IReadOnlyList<T> Items, DateTimeOffset ServerNow);
