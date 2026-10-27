import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService, CLE_JETON } from '../../../core/auth/auth.service';
import * as formulaires from '../../../testing/formulaires';
import { membre, motDePasse, reponseAuth } from '../../../testing/utilisateurs';
import { Connexion } from './connexion';

const reponseOk = reponseAuth(membre);

describe('Connexion', () => {
  let fixture: ComponentFixture<Connexion>;
  let racine: HTMLElement;
  let http: HttpTestingController;
  let router: Router;

  async function rendre(returnUrl?: string): Promise<void> {
    ({ fixture, racine, http, router } = await formulaires.rendreFormulaire(Connexion, returnUrl));
  }

  const stable = () => formulaires.stable(fixture);

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  const champ = (id: string) => formulaires.champ(racine, id);
  const bouton = () => racine.querySelector<HTMLButtonElement>('button[type="submit"]')!;

  const saisirEtEnvoyer = (email: string, motDePasseSaisi: string) =>
    formulaires.saisirEtEnvoyer(fixture, {
      'connexion-email': email,
      'connexion-mot-de-passe': motDePasseSaisi,
    });

  it('affiche le titre, les champs libellés et « Créer mon compte » qui garde returnUrl', async () => {
    await rendre('/mes-favoris');

    expect(racine.querySelector('h1')?.textContent?.trim()).toBe('Connexion');
    expect(racine.querySelector('label[for="connexion-email"]')?.textContent?.trim()).toBe('Email');
    expect(champ('connexion-mot-de-passe').type).toBe('password');
    const creer = Array.from(racine.querySelectorAll('a')).find(
      (lien) => lien.textContent?.trim() === 'Créer mon compte',
    );
    expect(creer?.getAttribute('href')).toBe('/inscription?returnUrl=%2Fmes-favoris');
  });

  it('connexion OK : envoie la saisie puis revient à returnUrl', async () => {
    await rendre('/mes-reservations');

    await saisirEtEnvoyer(membre.email, motDePasse);
    expect(bouton().disabled).toBe(true);
    expect(bouton().textContent?.trim()).toBe('Envoi…');
    const requete = http.expectOne('/api/auth/connexion');
    expect(requete.request.body).toEqual({ email: membre.email, motDePasse });
    requete.flush(reponseOk);

    await vi.waitFor(() => expect(router.url).toBe('/mes-reservations'));
    expect(TestBed.inject(AuthService).estConnecte()).toBe(true);
  });

  it.each([undefined, 'https://evil.fr', '//evil.fr'])(
    'returnUrl %s → retour à /',
    async (returnUrl) => {
      await rendre(returnUrl);
      await router.navigateByUrl('/connexion');

      await saisirEtEnvoyer(membre.email, motDePasse);
      http.expectOne('/api/auth/connexion').flush(reponseOk);

      await vi.waitFor(() => expect(router.url).toBe('/'));
    },
  );

  it('identifiants faux : message sur le formulaire, saisie et page conservées', async () => {
    await rendre('/mes-favoris');
    await router.navigateByUrl('/connexion?returnUrl=%2Fmes-favoris');

    await saisirEtEnvoyer(membre.email, 'faux');
    http
      .expectOne('/api/auth/connexion')
      .flush(
        { error: 'texte libre du serveur', code: 'identifiants-invalides' },
        { status: 400, statusText: 'Bad Request' },
      );
    await stable();

    const alerte = racine.querySelector('[role="alert"]');
    expect(alerte?.textContent?.trim()).toBe('Email ou mot de passe incorrect.');
    expect(racine.querySelector('.champ-erreur')).toBeNull();
    expect(champ('connexion-email').value).toBe(membre.email);
    expect(bouton().disabled).toBe(false);
    expect(router.url).toBe('/connexion?returnUrl=%2Fmes-favoris');
    expect(TestBed.inject(AuthService).estConnecte()).toBe(false);
    expect(localStorage.getItem(CLE_JETON)).toBeNull();
  });

  it('erreur de champ : message sous le champ relié par aria-describedby', async () => {
    await rendre();

    await saisirEtEnvoyer('', motDePasse);
    http.expectOne('/api/auth/connexion').flush(
      {
        error: 'Certains champs sont invalides.',
        errors: { email: 'Indiquez votre adresse email.' },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await stable();

    const email = champ('connexion-email');
    expect(email.getAttribute('aria-invalid')).toBe('true');
    const message = racine.querySelector(`#${email.getAttribute('aria-describedby')}`);
    expect(message?.textContent?.trim()).toBe('Indiquez votre adresse email.');
    expect(champ('connexion-mot-de-passe').hasAttribute('aria-invalid')).toBe(false);
    expect(racine.querySelector('[role="alert"]')).toBeNull();
    expect(document.activeElement).toBe(email);
  });

  it('erreur serveur avec corps API : alerte = message error de l’API', async () => {
    await rendre();

    await saisirEtEnvoyer(membre.email, motDePasse);
    http
      .expectOne('/api/auth/connexion')
      .flush(
        { error: 'Une erreur inattendue est survenue. Veuillez réessayer plus tard.' },
        { status: 500, statusText: 'Internal Server Error' },
      );
    await stable();

    expect(racine.querySelector('[role="alert"]')?.textContent?.trim()).toBe(
      'Une erreur inattendue est survenue. Veuillez réessayer plus tard.',
    );
    expect(bouton().disabled).toBe(false);
  });

  it('réseau coupé : alerte d’envoi, bouton actif pour réessayer', async () => {
    await rendre();

    await saisirEtEnvoyer(membre.email, motDePasse);
    http.expectOne('/api/auth/connexion').error(new ProgressEvent('error'));
    await stable();

    expect(racine.querySelector('[role="alert"]')?.textContent?.trim()).toBe(
      "L'envoi n'a pas abouti. Vérifiez votre connexion et réessayez.",
    );
    expect(bouton().disabled).toBe(false);
    expect(bouton().textContent?.trim()).toBe('Connexion');
  });
});
