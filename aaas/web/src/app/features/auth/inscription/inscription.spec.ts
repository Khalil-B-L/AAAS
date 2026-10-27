import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService, CLE_JETON } from '../../../core/auth/auth.service';
import * as formulaires from '../../../testing/formulaires';
import { membre, reponseAuth, saisieInscription } from '../../../testing/utilisateurs';
import { Inscription } from './inscription';

const saisie = saisieInscription;

const ids: Record<keyof typeof saisie, string> = {
  prenom: 'inscription-prenom',
  nom: 'inscription-nom',
  email: 'inscription-email',
  telephone: 'inscription-telephone',
  motDePasse: 'inscription-mot-de-passe',
};

describe('Inscription', () => {
  let fixture: ComponentFixture<Inscription>;
  let racine: HTMLElement;
  let http: HttpTestingController;
  let router: Router;

  async function rendre(returnUrl?: string): Promise<void> {
    ({ fixture, racine, http, router } = await formulaires.rendreFormulaire(
      Inscription,
      returnUrl,
    ));
  }

  const stable = () => formulaires.stable(fixture);

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  const champ = (id: string) => formulaires.champ(racine, id);

  const saisirEtEnvoyer = (valeurs: typeof saisie) =>
    formulaires.saisirEtEnvoyer(
      fixture,
      Object.fromEntries(
        Object.entries(valeurs).map(([cle, valeur]) => [ids[cle as keyof typeof saisie], valeur]),
      ),
    );

  it('demande prénom, nom, email, téléphone avec son aide et son exemple, mot de passe', async () => {
    await rendre();

    expect(racine.querySelector('h1')?.textContent?.trim()).toBe('Créer mon compte');
    const libelles = Array.from(racine.querySelectorAll('label')).map((l) => l.textContent?.trim());
    expect(libelles).toEqual(['Prénom', 'Nom', 'Email', 'Téléphone', 'Mot de passe']);

    const telephone = champ('inscription-telephone');
    expect(telephone.type).toBe('tel');
    expect(telephone.placeholder).toBe('72 000 000');
    const aide = racine.querySelector(`#${telephone.getAttribute('aria-describedby')}`);
    expect(aide?.textContent?.trim()).toBe("Votre numéro permet à l'association de vous rappeler.");
  });

  it('inscription OK : jeton stocké, connecté, retour à returnUrl', async () => {
    await rendre('/animaux/7');

    await saisirEtEnvoyer(saisie);
    const requete = http.expectOne('/api/auth/inscription');
    expect(requete.request.body).toEqual(saisie);
    requete.flush(reponseAuth(membre), { status: 201, statusText: 'Created' });

    await vi.waitFor(() => expect(router.url).toBe('/animaux/7'));
    expect(TestBed.inject(AuthService).utilisateur()?.prenom).toBe('Sonia');
    expect(localStorage.getItem(CLE_JETON)).toBe('jeton-1');
  });

  it('inscription refusée : message sous chaque champ concerné, saisie conservée', async () => {
    await rendre();

    await saisirEtEnvoyer({ ...saisie, telephone: '' });
    http.expectOne('/api/auth/inscription').flush(
      {
        error: 'Certains champs sont invalides.',
        errors: {
          telephone: 'Indiquez votre numéro de téléphone.',
          email: 'Un compte existe déjà avec cet email.',
        },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await stable();

    const telephone = champ('inscription-telephone');
    expect(telephone.getAttribute('aria-invalid')).toBe('true');
    expect(telephone.getAttribute('aria-describedby')).toBe(
      'inscription-telephone-aide inscription-telephone-erreur',
    );
    expect(racine.querySelector('#inscription-telephone-erreur')?.textContent?.trim()).toBe(
      'Indiquez votre numéro de téléphone.',
    );

    const email = champ('inscription-email');
    expect(email.getAttribute('aria-describedby')).toBe('inscription-email-erreur');
    expect(racine.querySelector('#inscription-email-erreur')?.textContent?.trim()).toBe(
      'Un compte existe déjà avec cet email.',
    );

    expect(champ('inscription-prenom').hasAttribute('aria-invalid')).toBe(false);
    expect(champ('inscription-prenom').value).toBe('Sonia');
    expect(email.value).toBe('sonia@exemple.tn');
    expect(TestBed.inject(AuthService).estConnecte()).toBe(false);
    expect(document.activeElement).toBe(email);
  });

  it('erreur sur un champ inconnu du formulaire : alerte générique', async () => {
    await rendre();

    await saisirEtEnvoyer(saisie);
    http
      .expectOne('/api/auth/inscription')
      .flush({ error: 'x', errors: { autre: 'y' } }, { status: 400, statusText: 'Bad Request' });
    await stable();

    expect(racine.querySelector('[role="alert"]')?.textContent?.trim()).toBe(
      'Certains champs sont à corriger.',
    );
  });

  it('« Connexion » garde returnUrl', async () => {
    await rendre('/mes-favoris');

    const lien = Array.from(racine.querySelectorAll('a')).find(
      (a) => a.textContent?.trim() === 'Connexion',
    );
    expect(lien?.getAttribute('href')).toBe('/connexion?returnUrl=%2Fmes-favoris');
  });
});
