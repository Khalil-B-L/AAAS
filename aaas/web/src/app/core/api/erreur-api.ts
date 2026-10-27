import { HttpErrorResponse } from '@angular/common/http';
import { ErreurApi } from './auth.dto';

/** Corps `ErreurApi` d'une réponse HTTP en échec, ou `null` (réseau coupé, corps inattendu). */
export function lireErreurApi(erreur: unknown): ErreurApi | null {
  if (!(erreur instanceof HttpErrorResponse)) {
    return null;
  }
  const corps: unknown = erreur.error;
  return typeof corps === 'object' &&
    corps !== null &&
    typeof (corps as { error?: unknown }).error === 'string'
    ? (corps as ErreurApi)
    : null;
}
