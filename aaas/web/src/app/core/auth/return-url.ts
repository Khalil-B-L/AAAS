/**
 * `returnUrl` sûr (AD-21) : seulement un chemin interne qui commence par `/`
 * (ni `//`, ni `/\`, qui mèneraient hors du site) ; sinon l'accueil.
 */
export function returnUrlSur(valeur: string | null | undefined): string {
  if (typeof valeur !== 'string' || !valeur.startsWith('/')) {
    return '/';
  }
  const second = valeur.charAt(1);
  return second === '/' || second === '\\' ? '/' : valeur;
}
