import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { stockerJeton } from '../../testing/session';
import {
  admin,
  membre,
  motDePasse,
  reponseAuth,
  saisieInscription,
} from '../../testing/utilisateurs';
import { AuthService, CLE_JETON, DELAI_RESTAURATION_MS } from './auth.service';

describe('AuthService', () => {
  let http: HttpTestingController;

  function creer(jetonStocke?: string): AuthService {
    stockerJeton(jetonStocke);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  afterEach(() => {
    http.verify();
    localStorage.clear();
    vi.useRealTimers();
  });

  it('démarre en visiteur et ne restaure rien sans jeton', async () => {
    const auth = creer();

    await auth.restaurer();

    http.expectNone('/api/auth/moi');
    expect(auth.utilisateur()).toBeNull();
    expect(auth.estConnecte()).toBe(false);
    expect(auth.jeton).toBeNull();
  });

  it('inscription OK : jeton stocké, utilisateur connecté', async () => {
    const auth = creer();
    const corps = saisieInscription;

    const inscription = auth.inscrire(corps);
    const requete = http.expectOne('/api/auth/inscription');
    expect(requete.request.method).toBe('POST');
    expect(requete.request.body).toEqual(corps);
    requete.flush(reponseAuth(membre), { status: 201, statusText: 'Created' });
    await inscription;

    expect(localStorage.getItem(CLE_JETON)).toBe('jeton-1');
    expect(auth.jeton).toBe('jeton-1');
    expect(auth.utilisateur()).toEqual(membre);
    expect(auth.estConnecte()).toBe(true);
    expect(auth.estAdmin()).toBe(false);
  });

  it('connexion OK : le rôle vient de UtilisateurDto', async () => {
    const auth = creer();

    const connexion = auth.connecter({ email: 'admin@aaas.tn', motDePasse: 'secret' });
    http.expectOne('/api/auth/connexion').flush(reponseAuth(admin, 'jeton-admin'));
    await connexion;

    expect(auth.estAdmin()).toBe(true);
    expect(localStorage.getItem(CLE_JETON)).toBe('jeton-admin');
  });

  it('identifiants faux : rejette sans toucher à la session', async () => {
    const auth = creer();
    const premiere = auth.connecter({ email: membre.email, motDePasse });
    http.expectOne('/api/auth/connexion').flush(reponseAuth(membre));
    await premiere;

    const seconde = auth.connecter({ email: membre.email, motDePasse: 'faux' });
    http
      .expectOne('/api/auth/connexion')
      .flush(
        { error: 'Email ou mot de passe incorrect.', code: 'identifiants-invalides' },
        { status: 400, statusText: 'Bad Request' },
      );

    await expect(seconde).rejects.toMatchObject({ status: 400 });
    expect(auth.utilisateur()).toEqual(membre);
    expect(localStorage.getItem(CLE_JETON)).toBe('jeton-1');
  });

  it('rechargement : /moi restaure l’utilisateur, une seule fois', async () => {
    const auth = creer('jeton-stocke');

    const restauration = auth.restaurer();
    expect(auth.restaurer()).toBe(restauration);
    http.expectOne('/api/auth/moi').flush(membre);
    await restauration;

    expect(auth.utilisateur()).toEqual(membre);
    expect(auth.jeton).toBe('jeton-stocke');
  });

  it('rechargement : 401 sur /moi efface le jeton (visiteur)', async () => {
    const auth = creer('jeton-perime');

    const restauration = auth.restaurer();
    http
      .expectOne('/api/auth/moi')
      .flush(
        { error: 'Connectez-vous pour continuer.' },
        { status: 401, statusText: 'Unauthorized' },
      );
    await restauration;

    expect(auth.utilisateur()).toBeNull();
    expect(auth.jeton).toBeNull();
    expect(localStorage.getItem(CLE_JETON)).toBeNull();
  });

  it('rechargement : 404 sur /moi (compte disparu) efface le jeton (visiteur)', async () => {
    const auth = creer('jeton-compte-supprime');

    const restauration = auth.restaurer();
    http
      .expectOne('/api/auth/moi')
      .flush({ error: 'Ce compte est introuvable.' }, { status: 404, statusText: 'Not Found' });
    await restauration;

    expect(auth.utilisateur()).toBeNull();
    expect(auth.jeton).toBeNull();
    expect(localStorage.getItem(CLE_JETON)).toBeNull();
  });

  it('rechargement : API en panne → visiteur, jeton gardé', async () => {
    const auth = creer('jeton-stocke');

    const restauration = auth.restaurer();
    http.expectOne('/api/auth/moi').flush(null, { status: 502, statusText: 'Bad Gateway' });
    await restauration;

    expect(auth.utilisateur()).toBeNull();
    expect(localStorage.getItem(CLE_JETON)).toBe('jeton-stocke');
  });

  it(`rechargement : abandonne /moi après ${DELAI_RESTAURATION_MS} ms`, async () => {
    vi.useFakeTimers();
    const auth = creer('jeton-stocke');

    const restauration = auth.restaurer();
    const requete = http.expectOne('/api/auth/moi');
    await vi.advanceTimersByTimeAsync(DELAI_RESTAURATION_MS);
    await restauration;

    expect(requete.cancelled).toBe(true);
    expect(auth.utilisateur()).toBeNull();
  });

  it('déconnexion : jeton effacé, visiteur', async () => {
    const auth = creer();
    const connexion = auth.connecter({ email: membre.email, motDePasse });
    http.expectOne('/api/auth/connexion').flush(reponseAuth(membre));
    await connexion;

    auth.deconnecter();

    expect(auth.utilisateur()).toBeNull();
    expect(auth.jeton).toBeNull();
    expect(localStorage.getItem(CLE_JETON)).toBeNull();
  });
});
