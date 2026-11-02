import { HttpErrorResponse } from '@angular/common/http';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { AnimalDetailDto } from '../../../core/api/animal.dto';
import { AnimauxService } from '../../../core/animaux.service';
import { ficheAnimal, ficheCaramel } from '../../../testing/animaux';
import { FICHE_ROUTES } from '../animaux.routes';

registerLocaleData(localeFr);

describe('Fiche animal (/animaux/:id)', () => {
  let obtenir: ReturnType<typeof vi.fn<(id: number) => Promise<AnimalDetailDto>>>;
  let harnais: RouterTestingHarness;

  beforeEach(() => {
    obtenir = vi.fn<(id: number) => Promise<AnimalDetailDto>>();
    obtenir.mockResolvedValue(ficheCaramel);
  });

  afterEach(() => vi.restoreAllMocks());

  async function ouvrir(url: string): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'animaux', children: FICHE_ROUTES }]),
        { provide: AnimauxService, useValue: { obtenir } },
        { provide: LOCALE_ID, useValue: 'fr-FR' },
      ],
    });
    harnais = await RouterTestingHarness.create();
    await harnais.navigateByUrl(url);
    await new Promise((resolve) => setTimeout(resolve));
    await harnais.fixture.whenStable();
    harnais.detectChanges();
    return harnais.routeNativeElement!;
  }

  const texte = (element: Element | null | undefined) =>
    element?.textContent?.replace(/\s+/g, ' ').trim();
  const textes = (page: HTMLElement, selecteur: string) =>
    Array.from(page.querySelectorAll(selecteur)).map((element) => texte(element));
  const titre = () => TestBed.inject(Title).getTitle();

  it('Disponible : nom, galerie « 1 / 3 », puces, description, santé, ententes, emplacement d’action vide', async () => {
    const page = await ouvrir('/animaux/7');

    expect(obtenir).toHaveBeenCalledWith(7);
    expect(texte(page.querySelector('h1'))).toBe('Caramel');
    expect(titre()).toBe('Caramel · AAAS');
    expect(texte(page.querySelector('app-galerie .compteur'))).toBe('1 / 3');
    expect(texte(page.querySelector('app-galerie app-pastille-statut'))).toBe('Disponible');
    expect(textes(page, '.fait')).toEqual(['Chat', 'Jeune', 'Femelle', 'Taille moyenne']);
    expect(texte(page.querySelector('.description'))).toBe(ficheCaramel.description);
    expect(textes(page, 'h2')).toEqual(['Son caractère', 'Santé', "Elle s'entend avec"]);

    // Santé et ententes lisibles sans la couleur : libellé écrit, accordé au sexe.
    expect(textes(page, '.case-sante')).toEqual(['✓ Vaccinée', '— Non stérilisée', '✓ Pucée']);
    expect(page.querySelectorAll('.case-sante.absent').length).toBe(1);
    expect(textes(page, '.entente > span:first-child')).toEqual([
      '👧 Les enfants',
      '🐶 Les chiens',
      '🐱 Les chats',
    ]);
    expect(textes(page, '.valeur-entente')).toEqual(['Oui', 'À voir', 'Non']);
    expect(page.querySelector('.sante')?.getAttribute('aria-labelledby')).toBe('titre-sante');
    expect(page.querySelector('.ententes')?.getAttribute('aria-labelledby')).toBe('titre-ententes');

    // Emplacement d'action vide : ni bouton « Je veux l'adopter », ni invitation à se connecter.
    const action = page.querySelector('.action');
    expect(action).not.toBeNull();
    expect(action?.querySelector('button, a')).toBeNull();
    expect(texte(action)).toBe('');
    expect(page.querySelector('.mention-adopte')).toBeNull();
  });

  it('mâle : santé et titre des ententes accordés (« Vacciné », « Non stérilisé », « Il s’entend avec »)', async () => {
    obtenir.mockResolvedValue(
      ficheAnimal({ nom: 'Filou', sexe: 'Male', vaccine: false, sterilise: true, puce: false }),
    );

    const page = await ouvrir('/animaux/7');

    expect(textes(page, '.case-sante')).toEqual(['— Non vacciné', '✓ Stérilisé', '— Non pucé']);
    expect(textes(page, 'h2')).toContain("Il s'entend avec");
    expect(textes(page, '.fait')).toContain('Mâle');
  });

  it('Réservé : pastille Réservé et bouton « Déjà réservé » désactivé', async () => {
    obtenir.mockResolvedValue(ficheAnimal({ statut: 'Reserve' }));

    const page = await ouvrir('/animaux/7');

    expect(texte(page.querySelector('app-galerie app-pastille-statut'))).toBe('Réservé');
    const boutons = Array.from(page.querySelectorAll<HTMLButtonElement>('.action button'));
    expect(boutons.map((bouton) => texte(bouton))).toEqual(['Déjà réservé']);
    expect(boutons[0].disabled).toBe(true);
  });

  it('Adopté : pastille Adopté, mention « A trouvé une famille » datée, aucun bouton', async () => {
    obtenir.mockResolvedValue(ficheAnimal({ statut: 'Adopte', adopteLe: '2026-10-12T08:00:00Z' }));

    const page = await ouvrir('/animaux/7');

    expect(texte(page.querySelector('app-galerie app-pastille-statut'))).toBe('Adopté');
    expect(texte(page.querySelector('.mention-adopte'))).toBe(
      'A trouvé une famille le 12 octobre 2026',
    );
    expect(page.querySelector('.action button')).toBeNull();
  });

  it('sans photo : dégradé de remplacement, pas de compteur', async () => {
    obtenir.mockResolvedValue(ficheAnimal({ photos: [], photoPrincipaleUrl: null }));

    const page = await ouvrir('/animaux/7');

    expect(page.querySelector('app-galerie .cadre')).not.toBeNull();
    expect(page.querySelector('app-galerie img')).toBeNull();
    expect(page.querySelector('app-galerie .compteur')).toBeNull();
  });

  it('chien : lien « ← Les chiens » vers /adopter?espece=Chien', async () => {
    obtenir.mockResolvedValue(ficheAnimal({ nom: 'Rex', espece: 'Chien', sexe: 'Male' }));

    const page = await ouvrir('/animaux/12');

    const retour = page.querySelector('a.retour');
    expect(texte(retour)).toBe('← Les chiens');
    expect(retour?.getAttribute('href')).toBe('/adopter?espece=Chien');
  });

  it('chat : lien « ← Les chats » vers /adopter?espece=Chat', async () => {
    const page = await ouvrir('/animaux/7');

    const retour = page.querySelector('a.retour');
    expect(texte(retour)).toBe('← Les chats');
    expect(retour?.getAttribute('href')).toBe('/adopter?espece=Chat');
  });

  it('id inconnu (404) : « Animal introuvable » et lien vers Adopter', async () => {
    obtenir.mockRejectedValue(
      new HttpErrorResponse({
        status: 404,
        error: { error: 'La ressource demandée est introuvable.' },
      }),
    );

    const page = await ouvrir('/animaux/999999');

    expect(obtenir).toHaveBeenCalledWith(999999);
    expect(texte(page.querySelector('h1'))).toBe('Animal introuvable');
    expect(titre()).toBe('Animal introuvable · AAAS');
    expect(page.querySelector('a[href="/adopter"]')).not.toBeNull();
    expect(page.querySelector('app-galerie')).toBeNull();
  });

  it.each(['abc', '0', '-3', '1.5', '99999999999'])(
    'id invalide « %s » : introuvable sans appel à l’API',
    async (id) => {
      const page = await ouvrir(`/animaux/${id}`);

      expect(obtenir).not.toHaveBeenCalled();
      expect(texte(page.querySelector('h1'))).toBe('Animal introuvable');
      expect(page.querySelector('a[href="/adopter"]')).not.toBeNull();
    },
  );

  it('API muette : message d’erreur et lien vers Adopter', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    obtenir.mockRejectedValue(new HttpErrorResponse({ status: 0 }));

    const page = await ouvrir('/animaux/7');

    expect(texte(page.querySelector('h1'))).toBe('Fiche indisponible');
    expect(page.querySelector('[role="alert"]')).not.toBeNull();
    expect(page.querySelector('a[href="/adopter"]')).not.toBeNull();
  });

  it('passer à une autre fiche recharge l’animal et le titre', async () => {
    await ouvrir('/animaux/7');
    obtenir.mockResolvedValue(ficheAnimal({ id: 12, nom: 'Rex', espece: 'Chien' }));

    await harnais.navigateByUrl('/animaux/12');
    await new Promise((resolve) => setTimeout(resolve));
    harnais.detectChanges();

    expect(obtenir).toHaveBeenLastCalledWith(12);
    expect(texte(harnais.routeNativeElement?.querySelector('h1'))).toBe('Rex');
    expect(titre()).toBe('Rex · AAAS');
  });
});
