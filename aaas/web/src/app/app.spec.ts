import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { App } from './app';
import { appConfig } from './app.config';
import { SiteService } from './core/site.service';
import { saisirEtEnvoyer, stable } from './testing/formulaires';
import { stockerJeton } from './testing/session';
import { membre, motDePasse, reponseAuth } from './testing/utilisateurs';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });

  async function chargerSite(
    reponse: (req: ReturnType<HttpTestingController['expectOne']>) => void,
  ) {
    const chargement = TestBed.inject(SiteService).charger();
    reponse(http.expectOne('/api/site'));
    await chargement;
  }

  it("rend l'en-tête, le contenu, les pattes masquées et le pied de page", async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const racine = fixture.nativeElement as HTMLElement;
    expect(racine.querySelector('app-en-tete header')).not.toBeNull();
    expect(racine.querySelector('main router-outlet')).not.toBeNull();
    expect(racine.querySelector('main .pattes')?.getAttribute('aria-hidden')).toBe('true');
    expect(racine.querySelector('app-pied-de-page footer')).not.toBeNull();
  });

  it('affiche le numéro du site dans le pied de page', async () => {
    await chargerSite((req) =>
      req.flush({
        telephone: '+21672000000',
        facebook: 'https://www.facebook.com/aaas-test',
        instagram: 'https://www.instagram.com/aaas-test',
      }),
    );

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const pied = (fixture.nativeElement as HTMLElement).querySelector('footer');
    expect(pied?.querySelector('.telephone')?.textContent?.trim()).toBe('+216 72 000 000');
  });

  it("rend le pied de page sans numéro si l'API est injoignable", async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    await chargerSite((req) => req.flush(null, { status: 504, statusText: 'Gateway Timeout' }));

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const pied = (fixture.nativeElement as HTMLElement).querySelector('footer');
    expect(pied).not.toBeNull();
    expect(pied?.querySelector('.telephone')).toBeNull();
  });
});

describe('App avec appConfig', () => {
  it('charge /api/site au démarrage et affiche le numéro dans le pied de page', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [...appConfig.providers, provideHttpClientTesting()],
    }).compileComponents();
    const http = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(App);
    http.expectOne('/api/site').flush({
      telephone: '+21672000000',
      facebook: 'https://www.facebook.com/aaas-test',
      instagram: 'https://www.instagram.com/aaas-test',
    });
    await fixture.whenStable();

    const pied = (fixture.nativeElement as HTMLElement).querySelector('footer');
    expect(pied?.querySelector('.telephone')?.textContent?.trim()).toBe('+216 72 000 000');
    expect(pied?.querySelector('a[href^="tel:"]')?.getAttribute('href')).toBe('tel:+21672000000');
    http.verify();
  });
});

/** Chaîne de démarrage auth réelle (F8) : intercepteur, restauration par `/moi`, liaison de `returnUrl`. */
describe('App avec appConfig · session', () => {
  const site = {
    telephone: '+21672000000',
    facebook: 'https://www.facebook.com/aaas-test',
    instagram: 'https://www.instagram.com/aaas-test',
  };
  let http: HttpTestingController;

  async function demarrer(): Promise<ComponentFixture<App>> {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [...appConfig.providers, provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(App);
    http.expectOne('/api/site').flush(site);
    return fixture;
  }

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('restaure la session : /api/auth/moi part avec Bearer <jeton> et l’en-tête affiche le prénom', async () => {
    stockerJeton('jeton-stocke');
    const fixture = await demarrer();

    const moi = http.expectOne('/api/auth/moi');
    expect(moi.request.headers.get('Authorization')).toBe('Bearer jeton-stocke');
    moi.flush(membre);
    await stable(fixture);

    const compte = (fixture.nativeElement as HTMLElement).querySelector('header .bouton-compte');
    expect(compte?.textContent).toContain('Sonia');
  });

  it('connexion depuis /connexion?returnUrl=%2Fmes-favoris : retour à /mes-favoris', async () => {
    const fixture = await demarrer();
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/connexion?returnUrl=%2Fmes-favoris');
    await stable(fixture);

    // Seul formulaire de l'application : celui de app-connexion.
    await saisirEtEnvoyer(fixture, {
      'connexion-email': membre.email,
      'connexion-mot-de-passe': motDePasse,
    });
    http.expectOne('/api/auth/connexion').flush(reponseAuth(membre));

    await vi.waitFor(() => expect(router.url).toBe('/mes-favoris'));
  });
});

describe('App · pattes en filigrane', () => {
  @Component({ template: '' })
  class PageTest {}

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([{ path: '**', component: PageTest }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
  });

  it.each([
    ['/adopter', true],
    ['/admin/reservations?statut=EnAttente', false],
  ])('%s → pattes affichées : %s', async (url, attendu) => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl(url);
    await fixture.whenStable();

    const pattes = (fixture.nativeElement as HTMLElement).querySelector('main .pattes');
    expect(pattes !== null).toBe(attendu);
  });
});
