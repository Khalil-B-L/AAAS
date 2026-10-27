/** Date ISO 8601 avec heure et fuseau explicite (`Z` ou décalage `±hh:mm`). */
const ISO_AVEC_FUSEAU =
  /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,7})?)?(?:Z|[+-]\d{2}:\d{2})$/i;

/**
 * Seul parseur de date du front (AD-11) : l'API écrit toujours l'UTC avec `Z`.
 * Une date sans fuseau serait lue en heure locale ; elle est donc rejetée.
 */
export function parseUtc(valeur: string): Date {
  if (!ISO_AVEC_FUSEAU.test(valeur)) {
    throw new Error(`Date sans fuseau ou mal formée : « ${valeur} ».`);
  }
  const date = new Date(valeur);
  if (Number.isNaN(date.getTime())) {
    throw new Error(`Date invalide : « ${valeur} ».`);
  }
  return date;
}
