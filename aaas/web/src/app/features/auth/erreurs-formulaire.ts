import { lireErreurApi } from '../../core/api/erreur-api';

export const MESSAGE_IDENTIFIANTS_INVALIDES = 'Email ou mot de passe incorrect.';
export const MESSAGE_ENVOI_IMPOSSIBLE =
  "L'envoi n'a pas abouti. Vérifiez votre connexion et réessayez.";
export const MESSAGE_CHAMPS_A_CORRIGER = 'Certains champs sont à corriger.';

/** Ce que le formulaire affiche après un échec : messages sous les champs et/ou alerte. */
export interface ErreursFormulaire<K extends string> {
  readonly champs: Partial<Record<K, string>>;
  readonly alerte: string | null;
}

/**
 * Traduit un échec d'appel en affichage, selon `code` puis `errors` (AD-10), jamais selon le texte `error`.
 * Un `errors` sur un champ inconnu du formulaire donne l'alerte générique. Sans `code` connu ni `errors`,
 * l'alerte reprend le message `error` de l'API ; sans corps API lisible (réseau), le message d'envoi impossible.
 */
export function interpreterErreur<K extends string>(
  erreur: unknown,
  champsConnus: readonly K[],
): ErreursFormulaire<K> {
  const api = lireErreurApi(erreur);
  if (api?.code === 'identifiants-invalides') {
    return { champs: {}, alerte: MESSAGE_IDENTIFIANTS_INVALIDES };
  }
  if (api?.errors) {
    const champs: Partial<Record<K, string>> = {};
    let inconnu = false;
    for (const [cle, message] of Object.entries(api.errors)) {
      if ((champsConnus as readonly string[]).includes(cle)) {
        champs[cle as K] = message;
      } else {
        inconnu = true;
      }
    }
    return { champs, alerte: inconnu ? MESSAGE_CHAMPS_A_CORRIGER : null };
  }
  if (api) {
    // Ni `code` connu ni `errors` (500, 503, 429, corps invalide…) : le message français de l'API.
    return { champs: {}, alerte: api.error };
  }
  return { champs: {}, alerte: MESSAGE_ENVOI_IMPOSSIBLE };
}
