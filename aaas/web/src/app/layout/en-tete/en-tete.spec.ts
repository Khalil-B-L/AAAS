import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Role } from '../../core/api/auth.dto';
import { AuthService, CLE_JETON } from '../../core/auth/auth.service';
import { connecterEnTantQue } from '../../testing/session';
import { EnTete } from './en-tete';

@Component({ template: '' })
class PageTest {}

const fournisseurs = [
  provideRouter([{ path: '**', component: PageTest }]),
  provideHttpClient(),
  provideHttpClientTesting(),
];

describe('EnTete', () => {
  let fixture: ComponentFixture<EnTete>;
  let racine: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [EnTete],
      providers: fournisseurs,
    });
    fixture = TestBed.createComponent(EnTete);
    racine = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  const burger = () => racine.querySelector<HTMLButtonElement>('button.burger')!;

  async function ouvrir(): Promise<void> {
    burger().click();
    await fixture.whenStable();
    expect(burger().getAttribute('aria-expanded')).toBe('true');
  }

  it('expose un burger nommé « Menu », fermé par défaut', () => {
    expect(burger().getAttribute('aria-label')).toBe('Menu');
    expect(burger().getAttribute('aria-expanded')).toBe('false');
    expect(burger().getAttribute('aria-controls')).toBe('menu-principal');
    expect(racine.querySelector('#menu-principal')?.classList).not.toContain('menu-ouvert');
  });

  it('bascule aria-expanded au toucher', async () => {
    await ouvrir();
    expect(racine.querySelector('#menu-principal')?.classList).toContain('menu-ouvert');

    burger().click();
    await fixture.whenStable();
    expect(burger().getAttribute('aria-expanded')).toBe('false');
  });

  it('se ferme après une navigation', async () => {
    await ouvrir();

    await TestBed.inject(Router).navigateByUrl('/adopter');
    await fixture.whenStable();

    expect(burger().getAttribute('aria-expanded')).toBe('false');
  });

  it('se ferme au toucher d’un lien, même vers la page courante', async () => {
    await ouvrir();

    racine.querySelector<HTMLAnchorElement>('nav a')!.click();
    await fixture.whenStable();

    expect(burger().getAttribute('aria-expanded')).toBe('false');
  });

  it('ignore un Échap venu du reste de la page', async () => {
    await ouvrir();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    await fixture.whenStable();

    expect(burger().getAttribute('aria-expanded')).toBe('true');
  });

  it('se ferme à Échap et rend le focus au burger', async () => {
    await ouvrir();

    burger().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();

    expect(burger().getAttribute('aria-expanded')).toBe('false');
    expect(document.activeElement).toBe(burger());
  });

  it("liste les liens dans l'ordre et mène Connexion à /connexion", () => {
    const liens = Array.from(racine.querySelectorAll<HTMLAnchorElement>('nav a'));
    expect(liens.map((lien) => lien.textContent?.trim())).toEqual([
      'Accueil',
      'Adopter',
      'Actualités',
      'Nous aider',
      'Contact',
      'Connexion',
    ]);
    expect(liens.at(-1)?.getAttribute('href')).toBe('/connexion');
  });

  it('marque le lien de la page courante', async () => {
    await TestBed.inject(Router).navigateByUrl('/nous-aider/benevolat?x=1');
    await fixture.whenStable();

    const actifs = Array.from(racine.querySelectorAll('nav a[aria-current="page"]'));
    expect(actifs.map((lien) => lien.textContent?.trim())).toEqual(['Nous aider']);
    expect(actifs[0].classList).toContain('lien-actif');
  });
});

describe('EnTete · session', () => {
  let fixture: ComponentFixture<EnTete>;
  let racine: HTMLElement;
  let http: HttpTestingController;

  async function rendreEnTant(role: Role): Promise<void> {
    TestBed.configureTestingModule({ imports: [EnTete], providers: fournisseurs });
    http = TestBed.inject(HttpTestingController);
    await connecterEnTantQue(role);
    fixture = TestBed.createComponent(EnTete);
    racine = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  }

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  const boutonCompte = () => racine.querySelector<HTMLButtonElement>('button.bouton-compte')!;
  const libellesMenu = () =>
    Array.from(racine.querySelectorAll('#menu-membre a, #menu-membre button')).map((e) =>
      e.textContent?.trim(),
    );

  async function ouvrirCompte(): Promise<void> {
    boutonCompte().click();
    await fixture.whenStable();
    expect(boutonCompte().getAttribute('aria-expanded')).toBe('true');
  }

  it('membre : le bouton Connexion devient le menu au prénom, fermé par défaut', async () => {
    await rendreEnTant('Membre');

    expect(racine.querySelector('a.connexion')).toBeNull();
    expect(boutonCompte().textContent).toContain('Sonia');
    expect(boutonCompte().getAttribute('aria-expanded')).toBe('false');
    expect(boutonCompte().getAttribute('aria-controls')).toBe('menu-membre');
    expect(racine.querySelector('#menu-membre')?.classList).not.toContain('menu-membre-ouvert');
  });

  it('membre : Mes réservations, Mes favoris, Déconnexion', async () => {
    await rendreEnTant('Membre');
    await ouvrirCompte();

    expect(racine.querySelector('#menu-membre')?.classList).toContain('menu-membre-ouvert');
    expect(libellesMenu()).toEqual(['Mes réservations', 'Mes favoris', 'Déconnexion']);
    expect(racine.querySelector('#menu-membre a')?.getAttribute('href')).toBe('/mes-reservations');
  });

  it('admin : en plus Réservations, Animaux, Actualités', async () => {
    await rendreEnTant('Admin');
    await ouvrirCompte();

    expect(libellesMenu()).toEqual([
      'Mes réservations',
      'Mes favoris',
      'Réservations',
      'Animaux',
      'Actualités',
      'Déconnexion',
    ]);
    const hrefs = Array.from(racine.querySelectorAll('.liens-admin a')).map((a) =>
      a.getAttribute('href'),
    );
    expect(hrefs).toEqual(['/admin/reservations', '/admin/animaux', '/admin/actualites']);
  });

  it('pastille « Admin » après le sigle sur les routes /admin/** seulement', async () => {
    await rendreEnTant('Admin');
    const router = TestBed.inject(Router);
    const pastille = () => racine.querySelector('.identite .pastille-admin');

    expect(pastille()).toBeNull();

    await router.navigateByUrl('/admin/reservations?statut=EnAttente');
    await fixture.whenStable();
    expect(pastille()?.textContent?.trim()).toBe('Admin');

    await router.navigateByUrl('/administration');
    await fixture.whenStable();
    expect(pastille()).toBeNull();
  });

  it('Échap ferme le menu membre et rend le focus à son bouton, puis ferme le burger', async () => {
    await rendreEnTant('Membre');
    const burger = racine.querySelector<HTMLButtonElement>('button.burger')!;
    burger.click();
    await fixture.whenStable();
    await ouvrirCompte();

    boutonCompte().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();

    expect(boutonCompte().getAttribute('aria-expanded')).toBe('false');
    expect(burger.getAttribute('aria-expanded')).toBe('true');
    expect(document.activeElement).toBe(boutonCompte());

    boutonCompte().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();

    expect(burger.getAttribute('aria-expanded')).toBe('false');
    expect(document.activeElement).toBe(burger);
  });

  it('un toucher ailleurs ferme le menu membre', async () => {
    await rendreEnTant('Membre');
    await ouvrirCompte();

    document.body.click();
    await fixture.whenStable();

    expect(boutonCompte().getAttribute('aria-expanded')).toBe('false');
  });

  it('Déconnexion : jeton effacé, Connexion revient, accueil', async () => {
    await rendreEnTant('Membre');
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/mes-favoris');
    await fixture.whenStable();
    await ouvrirCompte();

    racine.querySelector<HTMLButtonElement>('#menu-membre button.deconnexion')!.click();
    await fixture.whenStable();

    expect(localStorage.getItem(CLE_JETON)).toBeNull();
    expect(TestBed.inject(AuthService).estConnecte()).toBe(false);
    expect(racine.querySelector('a.connexion')?.textContent?.trim()).toBe('Connexion');
    expect(racine.querySelector('button.bouton-compte')).toBeNull();
    await vi.waitFor(() => expect(router.url).toBe('/'));
  });
});
