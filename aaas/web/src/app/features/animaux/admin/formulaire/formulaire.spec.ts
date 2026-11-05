import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AdminAnimauxService } from '../../../../core/admin-animaux.service';
import { AnimalDetailDto, AnimalEcritureDto, PhotoDto } from '../../../../core/api/animal.dto';
import { collection, ficheAnimal, ficheCaramel } from '../../../../testing/animaux';
import { ADMIN_ANIMAUX_ROUTES } from '../admin-animaux.routes';

/** Promesse résolue ou rejetée à la main, pour vérifier l'ordre des envois. */
function differe<T>() {
  let resoudre!: (valeur: T) => void;
  let rejeter!: (erreur: unknown) => void;
  const promesse = new Promise<T>((ok, ko) => {
    resoudre = ok;
    rejeter = ko;
  });
  return { promesse, resoudre, rejeter };
}

function erreurHttp(status: number, error: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error });
}

describe('Admin · formulaire animal (/admin/animaux/nouveau, /admin/animaux/:id)', () => {
  const ficheLuna: AnimalDetailDto = ficheAnimal({
    id: 30,
    nom: 'Luna',
    espece: 'Chien',
    sexe: 'Femelle',
    age: 'Jeune',
    taille: 'Moyenne',
    description: 'Trouvée à Kélibia.',
    vaccine: true,
    sterilise: false,
    puce: false,
    ententeEnfants: 'Oui',
    ententeChiens: 'Oui',
    ententeChats: 'AVoir',
    photoPrincipaleUrl: null,
    photos: [],
  });
  const corpsLuna: AnimalEcritureDto = {
    nom: 'Luna',
    espece: 'Chien',
    sexe: 'Femelle',
    age: 'Jeune',
    taille: 'Moyenne',
    description: 'Trouvée à Kélibia.',
    vaccine: true,
    sterilise: false,
    puce: false,
    ententeEnfants: 'Oui',
    ententeChiens: 'Oui',
    ententeChats: 'AVoir',
  };
  const photo = (id: number, ordre: number): PhotoDto => ({
    id,
    url: `/fichiers/photo-${id}.jpg`,
    ordre,
  });
  const fichiers = ['luna-1.jpg', 'luna-2.jpg', 'luna-3.jpg'].map(
    (nom) => new File(['x'], nom, { type: 'image/jpeg' }),
  );

  let service: {
    lister: ReturnType<typeof vi.fn>;
    obtenir: ReturnType<typeof vi.fn>;
    creer: ReturnType<typeof vi.fn>;
    modifier: ReturnType<typeof vi.fn>;
    supprimer: ReturnType<typeof vi.fn>;
    ajouterPhoto: ReturnType<typeof vi.fn>;
    supprimerPhoto: ReturnType<typeof vi.fn>;
    forcerStatut: ReturnType<typeof vi.fn>;
  };
  let harnais: RouterTestingHarness;
  let page: HTMLElement;
  let revoquer: ReturnType<typeof vi.fn<(url: string) => void>>;
  const urlOriginale = { creer: URL.createObjectURL, revoquer: URL.revokeObjectURL };

  beforeEach(() => {
    service = {
      lister: vi.fn().mockResolvedValue(collection([])),
      obtenir: vi.fn().mockResolvedValue(ficheCaramel),
      creer: vi.fn().mockResolvedValue(ficheLuna),
      modifier: vi.fn(),
      supprimer: vi.fn().mockResolvedValue(undefined),
      ajouterPhoto: vi.fn(),
      supprimerPhoto: vi.fn().mockResolvedValue(undefined),
      forcerStatut: vi.fn(),
    };
    // jsdom n'implémente pas les URL `blob:` des aperçus.
    URL.createObjectURL = vi.fn((fichier: Blob) => `blob:${(fichier as File).name}`);
    revoquer = vi.fn<(url: string) => void>();
    URL.revokeObjectURL = revoquer;
  });

  afterEach(() => {
    URL.createObjectURL = urlOriginale.creer;
    URL.revokeObjectURL = urlOriginale.revoquer;
    vi.restoreAllMocks();
  });

  async function stabiliser(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    await harnais.fixture.whenStable();
    harnais.detectChanges();
  }

  async function ouvrir(url: string): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'admin/animaux', children: ADMIN_ANIMAUX_ROUTES }]),
        { provide: AdminAnimauxService, useValue: service },
      ],
    });
    harnais = await RouterTestingHarness.create();
    await harnais.navigateByUrl(url);
    await stabiliser();
    page = harnais.routeNativeElement!;
  }

  const url = () => TestBed.inject(Router).url;
  const titreOnglet = () => TestBed.inject(Title).getTitle();
  const h1 = () => page.querySelector('h1')?.textContent?.trim();
  const champ = <T extends HTMLElement = HTMLInputElement>(id: string) =>
    page.querySelector<T>(`#formulaire-animal-${id}`)!;
  const erreur = (champ: string) =>
    page.querySelector(`#formulaire-animal-${champ}-erreur`)?.textContent?.trim();
  const radios = (nom: string) =>
    Array.from(page.querySelectorAll<HTMLInputElement>(`input[type="radio"][name="${nom}"]`));
  const cases = () => Array.from(page.querySelectorAll<HTMLInputElement>('input[type="checkbox"]'));
  const images = () => Array.from(page.querySelectorAll<HTMLImageElement>('.photos img'));
  const vignettes = () => Array.from(page.querySelectorAll<HTMLElement>('.photos li'));
  const bouton = (libelle: string, racine: ParentNode = page) =>
    Array.from(racine.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => (b.getAttribute('aria-label') ?? b.textContent?.trim()) === libelle,
    );
  const dialogue = () => page.querySelector<HTMLElement>('[role="dialog"]');
  const alerte = () => page.querySelector('[role="alert"]')?.textContent?.trim();

  function saisir(id: string, valeur: string): void {
    const element = champ<HTMLInputElement | HTMLTextAreaElement>(id);
    element.value = valeur;
    element.dispatchEvent(new Event('input'));
  }

  function choisirDansListe(id: string, index: number): void {
    const liste = champ<HTMLSelectElement>(id);
    liste.selectedIndex = index;
    liste.dispatchEvent(new Event('change'));
  }

  async function choisirFichiers(liste: File[]): Promise<void> {
    const entree = page.querySelector<HTMLInputElement>('input[type="file"]')!;
    Object.defineProperty(entree, 'files', { value: liste, configurable: true });
    entree.dispatchEvent(new Event('change'));
    await stabiliser();
  }

  async function envoyer(): Promise<void> {
    page.querySelector('form')!.dispatchEvent(new Event('submit'));
    await stabiliser();
  }

  /** Remplit Luna : chienne jeune, taille moyenne, vaccinée, ententes Oui / Oui / À voir. */
  function remplirLuna(sauf: readonly string[] = []): void {
    if (!sauf.includes('espece')) radios('espece')[1].click();
    if (!sauf.includes('nom')) saisir('nom', 'Luna');
    if (!sauf.includes('age')) choisirDansListe('age', 2);
    if (!sauf.includes('taille')) choisirDansListe('taille', 2);
    if (!sauf.includes('sexe')) radios('sexe')[1].click();
    cases()[0].click();
    radios('ententeEnfants')[0].click();
    radios('ententeChiens')[0].click();
    radios('ententeChats')[1].click();
    if (!sauf.includes('description')) saisir('description', 'Trouvée à Kélibia.');
    harnais.detectChanges();
  }

  describe('ajout', () => {
    it('titre « Ajouter un animal », onglet, lien « ← Animaux », « Statut : Disponible » sans choix', async () => {
      await ouvrir('/admin/animaux/nouveau');

      expect(h1()).toBe('Ajouter un animal');
      expect(titreOnglet()).toBe('Admin · Ajouter un animal · AAAS');
      const retour = page.querySelector('a.retour')!;
      expect(retour.textContent?.replace(/\s+/g, ' ').trim()).toBe('← Animaux');
      expect(retour.getAttribute('href')).toBe('/admin/animaux');
      expect(Array.from(page.querySelectorAll('legend')).map((l) => l.textContent?.trim())).toEqual(
        ['Espèce', 'Sexe', 'Santé', 'Ententes', '👧 Enfants', '🐶 Chiens', '🐱 Chats'],
      );
      expect(radios('ententeChats').map((r) => r.parentElement?.textContent?.trim())).toEqual([
        'Oui',
        'À voir',
        'Non',
      ]);
      expect(radios('sexe').map((r) => r.parentElement?.textContent?.trim())).toEqual([
        'Mâle',
        'Femelle',
      ]);
      expect(cases().map((c) => c.parentElement?.textContent?.trim())).toEqual([
        'Vacciné(e)',
        'Stérilisé(e)',
        'Pucé(e)',
      ]);
      expect(page.textContent).toContain('Statut : Disponible');
      expect(radios('statut')).toEqual([]);
      expect(bouton('Changer le statut')).toBeUndefined();
      expect(bouton("Enregistrer l'animal")?.type).toBe('submit');
      expect(bouton("Supprimer l'animal")).toBeUndefined();
      expect(service.obtenir).not.toHaveBeenCalled();
    });

    it('zone photo : sélection multiple, types acceptés, sans capture', async () => {
      await ouvrir('/admin/animaux/nouveau');

      const entree = page.querySelector<HTMLInputElement>('input[type="file"]')!;
      expect(entree.accept).toBe('image/jpeg,image/png,image/webp');
      expect(entree.multiple).toBe(true);
      expect(entree.hasAttribute('capture')).toBe(false);
      expect(entree.closest('label')?.textContent).toContain('Prendre / choisir des photos');
    });

    it('santé accordée au sexe choisi', async () => {
      await ouvrir('/admin/animaux/nouveau');

      radios('sexe')[1].click();
      harnais.detectChanges();

      expect(cases().map((c) => c.parentElement?.textContent?.trim())).toEqual([
        'Vaccinée',
        'Stérilisée',
        'Pucée',
      ]);
    });

    it('création : POST puis 3 envois successifs dans l’ordre, adresse /admin/animaux/:id, 1re Principale', async () => {
      const envois = [differe<PhotoDto>(), differe<PhotoDto>(), differe<PhotoDto>()];
      let appel = 0;
      service.ajouterPhoto.mockImplementation(() => envois[appel++].promesse);
      await ouvrir('/admin/animaux/nouveau');

      remplirLuna();
      await choisirFichiers(fichiers);
      expect(images().map((i) => i.alt)).toEqual([
        'Photo 1 de Luna',
        'Photo 2 de Luna',
        'Photo 3 de Luna',
      ]);
      expect(images()[0].getAttribute('src')).toBe('blob:luna-1.jpg');
      expect(vignettes()[0].querySelector('.principale')?.textContent).toBe('Principale');
      expect(vignettes()[1].querySelector('.principale')).toBeNull();
      expect(service.ajouterPhoto).not.toHaveBeenCalled();

      await envoyer();

      expect(service.creer).toHaveBeenCalledExactlyOnceWith(corpsLuna);
      expect(url()).toBe('/admin/animaux/30');
      expect(service.ajouterPhoto).toHaveBeenCalledExactlyOnceWith(30, fichiers[0]);

      envois[0].resoudre(photo(301, 1));
      await stabiliser();
      expect(service.ajouterPhoto).toHaveBeenCalledTimes(2);
      expect(service.ajouterPhoto).toHaveBeenLastCalledWith(30, fichiers[1]);

      envois[1].resoudre(photo(302, 2));
      await stabiliser();
      expect(service.ajouterPhoto).toHaveBeenLastCalledWith(30, fichiers[2]);

      envois[2].resoudre(photo(303, 3));
      await stabiliser();
      expect(service.ajouterPhoto).toHaveBeenCalledTimes(3);
      expect(images().map((i) => i.getAttribute('src'))).toEqual([
        '/fichiers/photo-301.jpg',
        '/fichiers/photo-302.jpg',
        '/fichiers/photo-303.jpg',
      ]);
      expect(vignettes()[0].querySelector('.principale')?.textContent).toBe('Principale');
      expect(revoquer).toHaveBeenCalledTimes(3);
      // Même formulaire, passé en modification sans rechargement.
      expect(service.obtenir).not.toHaveBeenCalled();
      expect(h1()).toBe('Modifier Luna');
      expect(titreOnglet()).toBe('Admin · Modifier Luna · AAAS');
      expect(page.querySelector('[role="status"]')?.textContent?.trim()).toBe('Animal enregistré.');
      expect(bouton("Supprimer l'animal")).toBeDefined();
    });

    it('champs manquants : aucun appel, messages sous Nom et Sexe, focus sur Nom', async () => {
      await ouvrir('/admin/animaux/nouveau');

      remplirLuna(['nom', 'sexe']);
      await envoyer();

      expect(service.creer).not.toHaveBeenCalled();
      expect(erreur('nom')).toBe("Indiquez le nom de l'animal.");
      expect(erreur('sexe')).toBe('Choisissez une valeur.');
      expect(page.querySelectorAll('.champ-erreur')).toHaveLength(2);
      expect(champ('nom').getAttribute('aria-invalid')).toBe('true');
      expect(champ('nom').getAttribute('aria-describedby')).toBe('formulaire-animal-nom-erreur');
      expect(radios('sexe').every((r) => r.getAttribute('aria-invalid') === 'true')).toBe(true);
      expect(document.activeElement).toBe(champ('nom'));
    });

    it('formulaire vide : chaque choix requis', async () => {
      await ouvrir('/admin/animaux/nouveau');

      await envoyer();

      expect(service.creer).not.toHaveBeenCalled();
      for (const nom of [
        'espece',
        'sexe',
        'age',
        'taille',
        'ententeEnfants',
        'ententeChiens',
        'ententeChats',
      ]) {
        expect(erreur(nom)).toBe('Choisissez une valeur.');
      }
      expect(erreur('description')).toBe("Indiquez la description de l'animal.");
    });

    it('nom de 51 caractères et description de 2001 : refusés avant envoi', async () => {
      await ouvrir('/admin/animaux/nouveau');

      remplirLuna();
      saisir('nom', 'x'.repeat(51));
      saisir('description', 'x'.repeat(2001));
      await envoyer();

      expect(service.creer).not.toHaveBeenCalled();
      expect(erreur('nom')).toBe('Nom trop long (50 caractères maximum).');
      expect(erreur('description')).toBe('Description trop longue (2000 caractères maximum).');
    });

    it('erreur API : 400 errors.description affiché sous Description, photos gardées en attente', async () => {
      service.creer.mockRejectedValue(
        erreurHttp(400, {
          error: 'Données invalides.',
          errors: { description: 'Indiquez la description de l’animal.' },
        }),
      );
      await ouvrir('/admin/animaux/nouveau');

      remplirLuna();
      await choisirFichiers([fichiers[0]]);
      await envoyer();

      expect(erreur('description')).toBe('Indiquez la description de l’animal.');
      expect(alerte()).toBeUndefined();
      expect(url()).toBe('/admin/animaux/nouveau');
      expect(service.ajouterPhoto).not.toHaveBeenCalled();
      expect(images()).toHaveLength(1);
      expect(bouton("Enregistrer l'animal")?.disabled).toBe(false);
    });

    it('photo refusée : 1re et 3e envoyées, 2e en échec avec le message ; « Renvoyer » ne renvoie qu’elle', async () => {
      service.ajouterPhoto
        .mockResolvedValueOnce(photo(301, 1))
        .mockRejectedValueOnce(
          erreurHttp(400, {
            error: 'Données invalides.',
            errors: { fichier: 'Format non accepté (JPEG, PNG ou WebP).' },
          }),
        )
        .mockResolvedValueOnce(photo(303, 2));
      await ouvrir('/admin/animaux/nouveau');

      remplirLuna();
      await choisirFichiers(fichiers);
      await envoyer();
      await stabiliser();

      expect(service.ajouterPhoto.mock.calls).toEqual([
        [30, fichiers[0]],
        [30, fichiers[1]],
        [30, fichiers[2]],
      ]);
      expect(images().map((i) => i.getAttribute('src'))).toEqual([
        '/fichiers/photo-301.jpg',
        '/fichiers/photo-303.jpg',
        'blob:luna-2.jpg',
      ]);
      expect(vignettes()[2].classList).toContain('photo-echec');
      expect(page.querySelector('.echecs')?.textContent?.replace(/\s+/g, ' ').trim()).toContain(
        'Photo 3 de Luna : Format non accepté (JPEG, PNG ou WebP).',
      );

      service.ajouterPhoto.mockResolvedValueOnce(photo(304, 3));
      bouton('Renvoyer la photo 3 de Luna')!.click();
      await stabiliser();

      expect(service.ajouterPhoto).toHaveBeenCalledTimes(4);
      expect(service.ajouterPhoto).toHaveBeenLastCalledWith(30, fichiers[1]);
      expect(page.querySelector('.echecs')).toBeNull();
      expect(images()).toHaveLength(3);
    });

    it('× sur une photo en attente : retirée localement, sans dialogue ni appel', async () => {
      await ouvrir('/admin/animaux/nouveau');

      await choisirFichiers(fichiers.slice(0, 2));
      bouton("Retirer la photo 1 de l'animal")!.click();
      await stabiliser();

      expect(dialogue()).toBeNull();
      expect(images().map((i) => i.getAttribute('src'))).toEqual(['blob:luna-2.jpg']);
      expect(revoquer).toHaveBeenCalledWith('blob:luna-1.jpg');
      expect(document.activeElement).toBe(page.querySelector('input[type="file"]'));
      expect(service.supprimerPhoto).not.toHaveBeenCalled();
    });
  });

  describe('modification', () => {
    it('GET puis formulaire rempli, titre « Modifier [prénom] »', async () => {
      await ouvrir('/admin/animaux/7');

      expect(service.obtenir).toHaveBeenCalledExactlyOnceWith(7);
      expect(h1()).toBe('Modifier Caramel');
      expect(titreOnglet()).toBe('Admin · Modifier Caramel · AAAS');
      expect(champ('nom').value).toBe('Caramel');
      expect(radios('espece').map((r) => r.checked)).toEqual([true, false]);
      expect(radios('ententeChiens').map((r) => r.checked)).toEqual([false, true, false]);
      expect(cases().map((c) => c.checked)).toEqual([true, false, true]);
      expect(images().map((i) => i.alt)).toEqual([
        'Photo 1 de Caramel',
        'Photo 2 de Caramel',
        'Photo 3 de Caramel',
      ]);
      expect(vignettes()[0].querySelector('.principale')).not.toBeNull();
    });

    it('changement du nom : PUT avec les 12 champs, « Modifications enregistrées. »', async () => {
      service.modifier.mockResolvedValue(ficheAnimal({ nom: 'Caramelle' }));
      await ouvrir('/admin/animaux/7');

      saisir('nom', 'Caramelle');
      await envoyer();

      expect(service.modifier).toHaveBeenCalledExactlyOnceWith(7, {
        nom: 'Caramelle',
        espece: 'Chat',
        sexe: 'Femelle',
        age: 'Jeune',
        taille: 'Moyenne',
        description: 'Caramel a été trouvée près du port de Nabeul.',
        vaccine: true,
        sterilise: false,
        puce: true,
        ententeEnfants: 'Oui',
        ententeChiens: 'AVoir',
        ententeChats: 'Non',
      });
      expect(page.querySelector('[role="status"]')?.textContent?.trim()).toBe(
        'Modifications enregistrées.',
      );
      expect(h1()).toBe('Modifier Caramelle');
      expect(titreOnglet()).toBe('Admin · Modifier Caramelle · AAAS');
    });

    it('2 photos choisies : envoyées une par une dès leur choix', async () => {
      const envois = [differe<PhotoDto>(), differe<PhotoDto>()];
      let appel = 0;
      service.ajouterPhoto.mockImplementation(() => envois[appel++].promesse);
      await ouvrir('/admin/animaux/7');

      await choisirFichiers(fichiers.slice(0, 2));

      expect(service.ajouterPhoto).toHaveBeenCalledExactlyOnceWith(7, fichiers[0]);
      expect(vignettes()[3].textContent).toContain('Envoi…');
      expect(bouton('Retirer la photo 4 de Caramel')).toBeUndefined();
      const statutEnvoi = () => page.querySelector('.envoi-photos');
      expect(statutEnvoi()?.getAttribute('role')).toBe('status');
      expect(statutEnvoi()?.textContent?.replace(/\s+/g, ' ').trim()).toBe(
        'Envoi des photos : 1 sur 2… Restez sur cette page.',
      );

      envois[0].resoudre(photo(74, 4));
      await stabiliser();
      expect(service.ajouterPhoto).toHaveBeenLastCalledWith(7, fichiers[1]);
      expect(statutEnvoi()?.textContent?.replace(/\s+/g, ' ').trim()).toBe(
        'Envoi des photos : 2 sur 2… Restez sur cette page.',
      );

      envois[1].resoudre(photo(75, 5));
      await stabiliser();
      expect(service.ajouterPhoto).toHaveBeenCalledTimes(2);
      expect(statutEnvoi()).toBeNull();
      expect(images().map((i) => i.alt)).toEqual([
        'Photo 1 de Caramel',
        'Photo 2 de Caramel',
        'Photo 3 de Caramel',
        'Photo 4 de Caramel',
        'Photo 5 de Caramel',
      ]);
    });

    it('conflit : PUT → 409 modifie-ailleurs, alerte et formulaire rechargé depuis l’API', async () => {
      service.modifier.mockRejectedValue(
        erreurHttp(409, {
          error: "L'animal a été modifié entre-temps. Rechargez la fiche puis recommencez.",
          code: 'modifie-ailleurs',
        }),
      );
      await ouvrir('/admin/animaux/7');
      service.obtenir.mockResolvedValue(ficheAnimal({ nom: 'Caramel la douce' }));

      saisir('nom', 'Caramelle');
      await envoyer();

      expect(service.obtenir).toHaveBeenCalledTimes(2);
      expect(alerte()).toContain('modifié entre-temps');
      expect(champ('nom').value).toBe('Caramel la douce');
      expect(h1()).toBe('Modifier Caramel la douce');
    });

    it('animal inconnu : GET → 404, « Animal introuvable » et lien vers la liste', async () => {
      service.obtenir.mockRejectedValue(erreurHttp(404, { error: 'Introuvable.' }));
      await ouvrir('/admin/animaux/999');

      expect(h1()).toBe('Animal introuvable');
      expect(page.querySelector('form')).toBeNull();
      const lien = Array.from(page.querySelectorAll('a')).find(
        (a) => a.textContent?.trim() === 'Voir la liste des animaux',
      );
      expect(lien?.getAttribute('href')).toBe('/admin/animaux');
      expect(titreOnglet()).toBe('Admin · Animal introuvable · AAAS');
    });

    it('id invalide : introuvable sans appel', async () => {
      await ouvrir('/admin/animaux/abc');

      expect(service.obtenir).not.toHaveBeenCalled();
      expect(h1()).toBe('Animal introuvable');
    });

    it('chargement en échec (500) : alerte, erreur journalisée', async () => {
      const journal = vi.spyOn(console, 'error').mockImplementation(() => undefined);
      service.obtenir.mockRejectedValue(erreurHttp(500, null));
      await ouvrir('/admin/animaux/7');

      expect(alerte()).toContain("n'a pas pu être chargée");
      expect(page.querySelector('form')).toBeNull();
      expect(journal).toHaveBeenCalled();
    });

    it('× sur une photo enregistrée : dialogue « Retirer la photo 2 de Caramel ? » puis DELETE', async () => {
      await ouvrir('/admin/animaux/7');

      bouton('Retirer la photo 2 de Caramel')!.click();
      await stabiliser();

      expect(dialogue()?.textContent).toContain('Retirer la photo 2 de Caramel ?');
      expect(service.supprimerPhoto).not.toHaveBeenCalled();

      bouton('Retirer', dialogue()!)!.click();
      await stabiliser();

      expect(service.supprimerPhoto).toHaveBeenCalledExactlyOnceWith(7, 72);
      expect(document.activeElement).toBe(page.querySelector('input[type="file"]'));
      expect(dialogue()).toBeNull();
      expect(images().map((i) => i.getAttribute('src'))).toEqual([
        '/fichiers/caramel-1.jpg',
        '/fichiers/caramel-3.jpg',
      ]);
    });

    it('× sur une photo enregistrée puis « Retour » : rien n’est retiré', async () => {
      await ouvrir('/admin/animaux/7');

      bouton('Retirer la photo 1 de Caramel')!.click();
      await stabiliser();
      bouton('Retour', dialogue()!)!.click();
      await stabiliser();

      expect(dialogue()).toBeNull();
      expect(service.supprimerPhoto).not.toHaveBeenCalled();
      expect(images()).toHaveLength(3);
    });
  });

  describe('statut forcé', () => {
    const ficheAdoptee = ficheAnimal({ statut: 'Adopte', adopteLe: '2026-11-04T10:00:00Z' });
    const coches = () => radios('statut').map((r) => r.checked);
    const pastille = () => page.querySelector('app-pastille-statut')?.textContent?.trim();
    const changer = () => bouton('Changer le statut')!;
    const statut = () => page.querySelector('.statut [role="status"]')?.textContent?.trim();
    const alerteStatut = () => page.querySelector('.statut [role="alert"]')?.textContent?.trim();

    async function choisir(index: number): Promise<void> {
      radios('statut')[index].click();
      await stabiliser();
    }

    it('choix Disponible / Réservé / Adopté présélectionné, Réservé désactivé, bouton inactif', async () => {
      await ouvrir('/admin/animaux/7');

      expect(radios('statut').map((r) => r.parentElement?.textContent?.trim())).toEqual([
        'Disponible',
        'Réservé',
        'Adopté',
      ]);
      expect(coches()).toEqual([true, false, false]);
      expect(radios('statut').map((r) => r.disabled)).toEqual([false, true, false]);
      expect(pastille()).toBe('Disponible');
      expect(changer().type).toBe('button');
      expect(changer().disabled).toBe(true);
      changer().click();
      await stabiliser();
      expect(service.forcerStatut).not.toHaveBeenCalled();
    });

    it('adopter : POST { Adopte, Disponible }, statut et pastille à jour, champs saisis conservés', async () => {
      service.forcerStatut.mockResolvedValue(ficheAdoptee);
      await ouvrir('/admin/animaux/7');
      saisir('nom', 'Caramelle');

      await choisir(2);
      expect(service.forcerStatut).not.toHaveBeenCalled();
      expect(changer().disabled).toBe(false);
      changer().click();
      await stabiliser();

      expect(service.forcerStatut).toHaveBeenCalledExactlyOnceWith(7, 'Adopte', 'Disponible');
      expect(service.modifier).not.toHaveBeenCalled();
      expect(statut()).toBe('Statut enregistré : Adopté.');
      expect(pastille()).toBe('Adopté');
      expect(coches()).toEqual([false, false, true]);
      expect(changer().disabled).toBe(true);
      expect(h1()).toBe('Modifier Caramel');
      expect(titreOnglet()).toBe('Admin · Modifier Caramel · AAAS');
      expect(champ('nom').value).toBe('Caramelle');
    });

    it('remettre disponible : POST { Disponible, Adopte }', async () => {
      service.obtenir.mockResolvedValue(ficheAdoptee);
      service.forcerStatut.mockResolvedValue(ficheCaramel);
      await ouvrir('/admin/animaux/7');
      expect(coches()).toEqual([false, false, true]);

      await choisir(0);
      changer().click();
      await stabiliser();

      expect(service.forcerStatut).toHaveBeenCalledExactlyOnceWith(7, 'Disponible', 'Adopte');
      expect(pastille()).toBe('Disponible');
      expect(statut()).toBe('Statut enregistré : Disponible.');
    });

    it('sélection revenue sur le statut chargé : bouton inactif, aucun appel', async () => {
      await ouvrir('/admin/animaux/7');

      await choisir(2);
      await choisir(0);

      expect(changer().disabled).toBe(true);
      expect(service.forcerStatut).not.toHaveBeenCalled();
    });

    it('clavier : la flèche change la sélection sans rien appeler', async () => {
      await ouvrir('/admin/animaux/7');

      // Ce que fait le navigateur sur une flèche dans le groupe : coche l'option voisine et émet `change`.
      const adopte = radios('statut')[2];
      adopte.checked = true;
      adopte.dispatchEvent(new Event('change'));
      await stabiliser();

      expect(coches()).toEqual([false, false, true]);
      expect(changer().disabled).toBe(false);
      expect(service.forcerStatut).not.toHaveBeenCalled();
    });

    it('animal Réservé : trois options désactivées, phrase vers Admin · Réservations, bouton inactif', async () => {
      service.obtenir.mockResolvedValue(ficheAnimal({ statut: 'Reserve' }));
      await ouvrir('/admin/animaux/7');

      expect(coches()).toEqual([false, true, false]);
      expect(radios('statut').every((r) => r.disabled)).toBe(true);
      const phrase = page.querySelector('#formulaire-animal-statut-reserve')!;
      expect(phrase.textContent?.replace(/\s+/g, ' ').trim()).toBe(
        "Réservé : traitez d'abord sa réservation dans Admin · Réservations.",
      );
      expect(phrase.querySelector('a')?.getAttribute('href')).toBe('/admin/reservations');
      expect(radios('statut')[0].getAttribute('aria-describedby')).toBe(
        'formulaire-animal-statut-reserve',
      );
      expect(pastille()).toBe('Réservé');
      expect(changer().disabled).toBe(true);
    });

    for (const code of ['reservation-active', 'modifie-ailleurs']) {
      it(`409 ${code} : GET rejoué, formulaire rechargé, alerte avec le message de l’API`, async () => {
        const message = `Message de l’API (${code}).`;
        service.forcerStatut.mockRejectedValue(erreurHttp(409, { error: message, code }));
        await ouvrir('/admin/animaux/7');
        service.obtenir.mockResolvedValue(
          ficheAnimal({ nom: 'Caramel la douce', statut: 'Reserve' }),
        );
        saisir('nom', 'Caramelle');

        await choisir(2);
        changer().click();
        await stabiliser();

        expect(service.obtenir).toHaveBeenCalledTimes(2);
        expect(alerteStatut()).toBe(message);
        expect(champ('nom').value).toBe('Caramel la douce');
        expect(pastille()).toBe('Réservé');
        expect(coches()).toEqual([false, true, false]);
        expect(changer().disabled).toBe(true);
      });
    }

    it('404 : « Animal introuvable »', async () => {
      service.forcerStatut.mockRejectedValue(erreurHttp(404, { error: 'Introuvable.' }));
      await ouvrir('/admin/animaux/7');

      await choisir(2);
      changer().click();
      await stabiliser();

      expect(h1()).toBe('Animal introuvable');
    });

    it('échec 500 : alerte générique, sélection remise sur le statut chargé, erreur journalisée', async () => {
      const journal = vi.spyOn(console, 'error').mockImplementation(() => undefined);
      service.forcerStatut.mockRejectedValue(erreurHttp(500, null));
      await ouvrir('/admin/animaux/7');

      await choisir(2);
      changer().click();
      await stabiliser();

      expect(alerteStatut()).toBe(
        "Le changement de statut n'a pas abouti. Vérifiez votre connexion et réessayez.",
      );
      expect(coches()).toEqual([true, false, false]);
      expect(pastille()).toBe('Disponible');
      expect(service.obtenir).toHaveBeenCalledTimes(1);
      expect(journal).toHaveBeenCalled();
    });

    it('appel en cours : bouton « Envoi… » désactivé, pas de double envoi', async () => {
      const reponse = differe<AnimalDetailDto>();
      service.forcerStatut.mockReturnValue(reponse.promesse);
      await ouvrir('/admin/animaux/7');

      await choisir(2);
      changer().click();
      await stabiliser();
      const occupe = bouton('Envoi…', page.querySelector('.statut')!)!;
      expect(occupe.disabled).toBe(true);
      occupe.click();
      await stabiliser();
      expect(service.forcerStatut).toHaveBeenCalledTimes(1);

      reponse.resoudre(ficheAdoptee);
      await stabiliser();
      expect(changer().disabled).toBe(true);
      expect(pastille()).toBe('Adopté');
    });

    it('statut en cours : « Enregistrer l’animal » désactivé, aucun PUT', async () => {
      const reponse = differe<AnimalDetailDto>();
      service.forcerStatut.mockReturnValue(reponse.promesse);
      await ouvrir('/admin/animaux/7');

      await choisir(2);
      changer().click();
      await stabiliser();
      expect(bouton("Enregistrer l'animal")!.disabled).toBe(true);
      await envoyer();
      expect(service.modifier).not.toHaveBeenCalled();

      reponse.resoudre(ficheAdoptee);
      await stabiliser();
      expect(bouton("Enregistrer l'animal")!.disabled).toBe(false);
    });

    it('PUT en cours : « Changer le statut » désactivé, aucun POST', async () => {
      const reponse = differe<AnimalDetailDto>();
      service.modifier.mockReturnValue(reponse.promesse);
      await ouvrir('/admin/animaux/7');

      await choisir(2);
      await envoyer();
      expect(service.modifier).toHaveBeenCalledTimes(1);
      expect(changer().disabled).toBe(true);
      changer().click();
      await stabiliser();
      expect(service.forcerStatut).not.toHaveBeenCalled();

      reponse.resoudre(ficheCaramel);
      await stabiliser();
      expect(service.forcerStatut).not.toHaveBeenCalled();
    });

    it('PUT réussi après un statut : le message de statut disparaît', async () => {
      service.forcerStatut.mockResolvedValue(ficheAdoptee);
      service.modifier.mockResolvedValue(ficheAnimal({ statut: 'Adopte', nom: 'Caramelle' }));
      await ouvrir('/admin/animaux/7');

      await choisir(2);
      changer().click();
      await stabiliser();
      expect(statut()).toBe('Statut enregistré : Adopté.');

      saisir('nom', 'Caramelle');
      await envoyer();

      expect(statut()).toBeUndefined();
      expect(alerteStatut()).toBeUndefined();
    });
  });

  describe("suppression de l'animal", () => {
    async function demanderSuppression(): Promise<void> {
      await ouvrir('/admin/animaux/7');
      bouton("Supprimer l'animal")!.click();
      await stabiliser();
    }

    it('dialogue nommant l’animal ; confirmer → DELETE, 204 → liste', async () => {
      await demanderSuppression();

      expect(dialogue()?.textContent).toContain(
        'Supprimer Caramel ? Sa fiche et ses photos seront effacées.',
      );

      bouton('Supprimer', dialogue()!)!.click();
      await stabiliser();

      expect(service.supprimer).toHaveBeenCalledExactlyOnceWith(7);
      expect(url()).toBe('/admin/animaux');
    });

    it('409 animal-a-des-reservations : alerte avec le message de l’API, formulaire conservé', async () => {
      const message = 'Caramel a des réservations : elle ne peut pas être supprimée.';
      service.supprimer.mockRejectedValue(
        erreurHttp(409, { error: message, code: 'animal-a-des-reservations' }),
      );
      await demanderSuppression();

      bouton('Supprimer', dialogue()!)!.click();
      await stabiliser();

      expect(dialogue()).toBeNull();
      expect(alerte()).toBe(message);
      expect(url()).toBe('/admin/animaux/7');
    });
  });
});
