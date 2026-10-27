import { HttpErrorResponse } from '@angular/common/http';
import {
  MESSAGE_CHAMPS_A_CORRIGER,
  MESSAGE_ENVOI_IMPOSSIBLE,
  MESSAGE_IDENTIFIANTS_INVALIDES,
  interpreterErreur,
} from './erreurs-formulaire';

const CHAMPS = ['email', 'motDePasse'] as const;

function reponse(status: number, corps: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: corps });
}

describe('interpreterErreur', () => {
  it('code identifiants-invalides : alerte dédiée, quel que soit le texte', () => {
    expect(
      interpreterErreur(
        reponse(400, { error: 'texte libre', code: 'identifiants-invalides' }),
        CHAMPS,
      ),
    ).toEqual({ champs: {}, alerte: MESSAGE_IDENTIFIANTS_INVALIDES });
  });

  it('errors : messages sous les champs connus, alerte générique pour un champ inconnu', () => {
    expect(
      interpreterErreur(
        reponse(400, {
          error: 'x',
          errors: { email: 'Indiquez votre adresse email.', autre: 'y' },
        }),
        CHAMPS,
      ),
    ).toEqual({
      champs: { email: 'Indiquez votre adresse email.' },
      alerte: MESSAGE_CHAMPS_A_CORRIGER,
    });
  });

  it.each([
    [500, 'Une erreur inattendue est survenue. Veuillez réessayer plus tard.'],
    [503, 'Le service est momentanément indisponible. Veuillez réessayer plus tard.'],
    [400, 'La requête est invalide.'],
  ])('%s sans code ni errors : alerte = message error de l’API', (status, message) => {
    expect(interpreterErreur(reponse(status, { error: message }), CHAMPS)).toEqual({
      champs: {},
      alerte: message,
    });
  });

  it('code inconnu sans errors : alerte = message error de l’API', () => {
    expect(
      interpreterErreur(reponse(409, { error: 'Conflit.', code: 'autre-code' }), CHAMPS).alerte,
    ).toBe('Conflit.');
  });

  it.each([
    ['réseau coupé', new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') })],
    ['corps vide', reponse(502, null)],
    ['corps non API', reponse(500, '<html>Erreur</html>')],
    ['autre erreur', new Error('boum')],
  ])('%s : message réseau', (_cas, erreur) => {
    expect(interpreterErreur(erreur, CHAMPS)).toEqual({
      champs: {},
      alerte: MESSAGE_ENVOI_IMPOSSIBLE,
    });
  });
});
