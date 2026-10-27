import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { connecterEnTantQue } from '../../testing/session';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

@Component({ template: '' })
class PageTest {}

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let auth: AuthService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: '**', component: PageTest }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  function appeler(url: string): Promise<unknown> {
    return firstValueFrom(client.get(url)).catch((erreur: unknown) => erreur);
  }

  it('401 tardif sur un ancien jeton : la nouvelle session est gardée, pas de navigation', async () => {
    await connecterEnTantQue('Membre', 'jeton-A');
    await router.navigateByUrl('/mes-reservations');

    const appel = appeler('/api/reservations');
    const requete = http.expectOne('/api/reservations');
    expect(requete.request.headers.get('Authorization')).toBe('Bearer jeton-A');
    await connecterEnTantQue('Membre', 'jeton-B');
    requete.flush(
      { error: 'Connectez-vous pour continuer.' },
      { status: 401, statusText: 'Unauthorized' },
    );
    expect(await appel).toMatchObject({ status: 401 });
    await new Promise((resolve) => setTimeout(resolve));

    expect(auth.estConnecte()).toBe(true);
    expect(auth.jeton).toBe('jeton-B');
    expect(router.url).toBe('/mes-reservations');
  });

  it('ajoute Authorization: Bearer sur les appels /api', async () => {
    await connecterEnTantQue('Membre');

    const appel = appeler('/api/reservations');
    const requete = http.expectOne('/api/reservations');
    expect(requete.request.headers.get('Authorization')).toBe('Bearer jeton-1');
    requete.flush({ items: [], serverNow: '2026-10-15T10:00:00Z' });
    await appel;
  });

  it("n'ajoute rien sans jeton ni hors /api", async () => {
    const verifierSansEnTete = async (url: string) => {
      const appel = appeler(url);
      const requete = http.expectOne(url);
      expect(requete.request.headers.has('Authorization')).toBe(false);
      requete.flush({});
      await appel;
    };

    await verifierSansEnTete('/api/site');
    await connecterEnTantQue('Membre');
    await verifierSansEnTete('/fichiers/abc.jpg');
    await verifierSansEnTete('https://exemple.fr/api/x');
  });

  it('401 en cours de route : déconnexion locale puis Connexion avec la page courante', async () => {
    await connecterEnTantQue('Membre');
    await router.navigateByUrl('/mes-reservations?onglet=1');

    const appel = appeler('/api/reservations');
    http
      .expectOne('/api/reservations')
      .flush(
        { error: 'Connectez-vous pour continuer.' },
        { status: 401, statusText: 'Unauthorized' },
      );
    const erreur = await appel;
    await vi.waitFor(() =>
      expect(router.url).toBe('/connexion?returnUrl=%2Fmes-reservations%3Fonglet%3D1'),
    );

    expect(erreur).toMatchObject({ status: 401 });
    expect(auth.estConnecte()).toBe(false);
    expect(auth.jeton).toBeNull();
  });

  it('400 de /api/auth/connexion : aucun effet sur la session ni la page', async () => {
    await connecterEnTantQue('Membre');
    await router.navigateByUrl('/connexion');

    const appel = firstValueFrom(
      client.post('/api/auth/connexion', { email: 'x', motDePasse: 'faux' }),
    ).catch((erreur: unknown) => erreur);
    http
      .expectOne('/api/auth/connexion')
      .flush(
        { error: 'Email ou mot de passe incorrect.', code: 'identifiants-invalides' },
        { status: 400, statusText: 'Bad Request' },
      );
    await appel;

    expect(auth.estConnecte()).toBe(true);
    expect(router.url).toBe('/connexion');
  });

  it('401 sur /api/auth/* : pas de redirection', async () => {
    await router.navigateByUrl('/accueil');

    const appel = appeler('/api/auth/moi');
    http
      .expectOne('/api/auth/moi')
      .flush(
        { error: 'Connectez-vous pour continuer.' },
        { status: 401, statusText: 'Unauthorized' },
      );
    expect(await appel).toMatchObject({ status: 401 });
    await new Promise((resolve) => setTimeout(resolve));

    expect(router.url).toBe('/accueil');
  });

  it('403 → /acces-refuse', async () => {
    await connecterEnTantQue('Membre');
    await router.navigateByUrl('/adopter');

    const appel = appeler('/api/admin/animaux');
    http
      .expectOne('/api/admin/animaux')
      .flush(
        { error: "Cette page est réservée à l'association." },
        { status: 403, statusText: 'Forbidden' },
      );
    expect(await appel).toMatchObject({ status: 403 });
    await vi.waitFor(() => expect(router.url).toBe('/acces-refuse'));

    expect(auth.estConnecte()).toBe(true);
  });
});
