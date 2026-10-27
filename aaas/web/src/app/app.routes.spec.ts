import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
import { collection } from './testing/animaux';
import { connecterEnTantQue } from './testing/session';

describe('routes (contrats-api.md §7)', () => {
  let harnais: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harnais = await RouterTestingHarness.create();
  });

  afterEach(() => {
    // L'accueil charge ses derniers pensionnaires : réponse vide pour toute navigation qui y mène.
    http.match('/api/animaux?limite=4').forEach((requete) => requete.flush(collection([])));
    http.verify();
    localStorage.clear();
  });

  const pages: readonly (readonly [string, string])[] = [
    ['/adopter', 'Adopter'],
    ['/animaux/7', 'Fiche animal'],
    ['/animaux/7/reserver', 'Réserver'],
    ['/reservations/7/envoyee', 'Réservation envoyée'],
    ['/ils-ont-trouve-une-famille', 'Ils ont trouvé une famille'],
    ['/actualites', 'Actualités'],
    ['/nous-aider/dons', 'Dons'],
    ['/nous-aider/benevolat', 'Bénévolat'],
    ['/nous-aider/famille-accueil', "Famille d'accueil"],
    ['/contact', 'Contact'],
    ['/connexion?returnUrl=%2Fmes-favoris', 'Connexion'],
    ['/inscription', 'Créer mon compte'],
    ['/mes-reservations', 'Mes réservations'],
    ['/mes-favoris', 'Mes favoris'],
    ['/admin/reservations?statut=EnAttente', 'Admin · Réservations'],
    ['/admin/animaux', 'Admin · Animaux'],
    ['/admin/animaux/nouveau', 'Admin · Ajouter un animal'],
    ['/admin/animaux/7', 'Admin · Modifier un animal'],
    ['/admin/actualites', 'Admin · Actualités'],
    ['/acces-refuse', "Cette page est réservée à l'association."],
  ];

  it.each(pages)('%s affiche la page « %s » (session Admin)', async (url, titre) => {
    await connecterEnTantQue('Admin');

    await harnais.navigateByUrl(url);

    expect(TestBed.inject(Router).url).toBe(url);
    expect(harnais.routeNativeElement?.querySelector('h1')?.textContent?.trim()).toBe(titre);
    expect(TestBed.inject(Title).getTitle()).toBe(`${titre} · AAAS`);
  });

  it("/ affiche l'accueil", async () => {
    await harnais.navigateByUrl('/');

    expect(harnais.routeNativeElement?.querySelector('h1')?.textContent).toContain(
      'Une truffe vous attend',
    );
    expect(TestBed.inject(Title).getTitle()).toBe('AAAS · Adoption');
  });

  it.each([
    '/nimporte-quoi',
    '/admin/inconnue',
    '/nous-aider',
    '/nous-aider/inconnue',
    '/animaux',
    '/reservations',
    '/reservations/7',
  ])('%s redirige vers /', async (url) => {
    await harnais.navigateByUrl(url);

    expect(TestBed.inject(Router).url).toBe('/');
  });

  it.each([
    ['/mes-reservations', '/connexion?returnUrl=%2Fmes-reservations'],
    ['/mes-favoris', '/connexion?returnUrl=%2Fmes-favoris'],
    ['/animaux/7/reserver', '/connexion?returnUrl=%2Fanimaux%2F7%2Freserver'],
    ['/reservations/7/envoyee', '/connexion?returnUrl=%2Freservations%2F7%2Fenvoyee'],
    ['/admin/animaux', '/connexion?returnUrl=%2Fadmin%2Fanimaux'],
    ['/admin', '/connexion?returnUrl=%2Fadmin%2Freservations'],
  ])('visiteur sur %s → %s', async (url, attendu) => {
    await harnais.navigateByUrl(url);

    expect(TestBed.inject(Router).url).toBe(attendu);
    expect(harnais.routeNativeElement?.querySelector('h1')?.textContent?.trim()).toBe('Connexion');
  });

  it.each(['/mes-reservations', '/mes-favoris', '/animaux/7/reserver'])(
    'membre sur %s → page affichée',
    async (url) => {
      await connecterEnTantQue('Membre');

      await harnais.navigateByUrl(url);

      expect(TestBed.inject(Router).url).toBe(url);
    },
  );

  it.each(['/admin/animaux', '/admin/reservations', '/admin'])(
    'membre sur %s → /acces-refuse',
    async (url) => {
      await connecterEnTantQue('Membre');

      await harnais.navigateByUrl(url);

      expect(TestBed.inject(Router).url).toBe('/acces-refuse');
      expect(harnais.routeNativeElement?.querySelector('h1')?.textContent?.trim()).toBe(
        "Cette page est réservée à l'association.",
      );
      expect(harnais.routeNativeElement?.querySelector('a')?.getAttribute('href')).toBe('/');
    },
  );

  it('admin sur /admin → /admin/reservations', async () => {
    await connecterEnTantQue('Admin');

    await harnais.navigateByUrl('/admin');

    expect(TestBed.inject(Router).url).toBe('/admin/reservations');
    expect(harnais.routeNativeElement?.querySelector('h1')?.textContent?.trim()).toBe(
      'Admin · Réservations',
    );
  });
});
