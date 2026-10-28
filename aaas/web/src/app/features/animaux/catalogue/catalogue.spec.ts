import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AnimalCarteDto, CollectionDto, FiltresCatalogue } from '../../../core/api/animal.dto';
import { AnimauxService } from '../../../core/animaux.service';
import { caramel, carteAnimal, collection, rex } from '../../../testing/animaux';
import { CATALOGUE_ROUTES } from '../animaux.routes';

describe('Catalogue (/adopter)', () => {
  const mimi = carteAnimal({ id: 3, nom: 'Mimi', age: 'Bebe' });
  let lister: ReturnType<
    typeof vi.fn<(filtres?: FiltresCatalogue) => Promise<CollectionDto<AnimalCarteDto>>>
  >;
  let harnais: RouterTestingHarness;

  beforeEach(() => {
    lister = vi.fn<(filtres?: FiltresCatalogue) => Promise<CollectionDto<AnimalCarteDto>>>();
    lister.mockResolvedValue(collection([caramel, mimi]));
  });

  afterEach(() => vi.restoreAllMocks());

  async function stabiliser(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    await harnais.fixture.whenStable();
    harnais.detectChanges();
  }

  async function ouvrir(url: string): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'adopter', children: CATALOGUE_ROUTES }]),
        { provide: AnimauxService, useValue: { lister } },
      ],
    });
    harnais = await RouterTestingHarness.create();
    await harnais.navigateByUrl(url);
    await stabiliser();
    return harnais.routeNativeElement!;
  }

  function puce(page: HTMLElement, filtre: string): HTMLButtonElement {
    const bouton = page.querySelector<HTMLButtonElement>(`button[data-filtre="${filtre}"]`);
    if (!bouton) {
      throw new Error(`Puce ${filtre} introuvable`);
    }
    return bouton;
  }

  function pucesActives(page: HTMLElement): string[] {
    return Array.from(page.querySelectorAll('button.puce[aria-pressed="true"]')).map((bouton) =>
      bouton.getAttribute('data-filtre')!,
    );
  }

  function effacer(page: HTMLElement): HTMLButtonElement | undefined {
    return Array.from(page.querySelectorAll<HTMLButtonElement>('button')).find(
      (bouton) => bouton.textContent?.trim() === 'Effacer les filtres',
    );
  }

  function noms(page: HTMLElement): string[] {
    return Array.from(page.querySelectorAll('app-grille-animaux app-carte-animal h3')).map(
      (titre) => titre.textContent!.trim(),
    );
  }

  const compteur = (page: HTMLElement) => page.querySelector('.compteur')?.textContent?.trim();
  const url = () => TestBed.inject(Router).url;

  it('titre de page, puces par groupe en boutons à état, lien vers Ils ont trouvé une famille', async () => {
    const page = await ouvrir('/adopter');

    expect(page.querySelector('h1')?.textContent?.trim()).toBe('Adopter');
    expect(TestBed.inject(Title).getTitle()).toBe('Adopter · AAAS');
    const groupes = Array.from(page.querySelectorAll('[role="group"]'));
    expect(
      groupes.map((groupe) => {
        const libelle = page.querySelector(`#${groupe.getAttribute('aria-labelledby')}`);
        return libelle?.childNodes[0].textContent?.trim();
      }),
    ).toEqual(['Espèce', 'Âge', 'Taille', 'Sexe', 'Ententes']);
    expect(
      groupes.map((groupe) =>
        Array.from(groupe.querySelectorAll('button.puce')).map((b) => b.textContent?.trim()),
      ),
    ).toEqual([
      ['Chat', 'Chien'],
      ['Bébé', 'Jeune', 'Adulte', 'Senior'],
      ['Petite', 'Moyenne', 'Grande'],
      ['Mâle', 'Femelle'],
      ['Enfants', 'Chiens', 'Chats'],
    ]);
    expect(page.querySelector('#filtre-ententes')?.textContent).toContain('(Oui ou À voir)');
    for (const bouton of Array.from(page.querySelectorAll('button.puce'))) {
      expect(bouton.getAttribute('type')).toBe('button');
      expect(bouton.getAttribute('aria-pressed')).toBe('false');
    }
    const adoptes = page.querySelector<HTMLAnchorElement>('.adoptes a');
    expect(adoptes?.textContent?.trim()).toBe('Ils ont trouvé une famille →');
    expect(adoptes?.getAttribute('href')).toBe('/ils-ont-trouve-une-famille');
  });

  it('sans filtre : tous les animaux, compteur annoncé, pas de « Effacer les filtres »', async () => {
    const page = await ouvrir('/adopter');

    expect(lister).toHaveBeenCalledExactlyOnceWith({});
    expect(noms(page)).toEqual(['Caramel', 'Mimi']);
    expect(page.querySelector('.compteur')?.getAttribute('role')).toBe('status');
    expect(compteur(page)).toBe('2 animaux');
    expect(effacer(page)).toBeUndefined();
  });

  it('arrivée depuis Miaou ! (?espece=Chat) : puce Chat active, liste filtrée, compteur', async () => {
    const page = await ouvrir('/adopter?espece=Chat');

    expect(lister).toHaveBeenCalledExactlyOnceWith({ espece: 'Chat' });
    expect(pucesActives(page)).toEqual(['espece=Chat']);
    expect(compteur(page)).toBe('2 animaux');
    expect(effacer(page)).toBeDefined();
  });

  it('clic sur une puce : URL mise à jour, liste rechargée, compteur à jour', async () => {
    const page = await ouvrir('/adopter?espece=Chat');
    lister.mockResolvedValue(collection([mimi]));

    puce(page, 'age=Bebe').click();
    await stabiliser();

    expect(url()).toBe('/adopter?espece=Chat&age=Bebe');
    expect(lister).toHaveBeenLastCalledWith({ espece: 'Chat', age: 'Bebe' });
    expect(lister).toHaveBeenCalledTimes(2);
    expect(pucesActives(page)).toEqual(['espece=Chat', 'age=Bebe']);
    expect(noms(page)).toEqual(['Mimi']);
    expect(compteur(page)).toBe('1 animal');
  });

  it('clic sur Taille Grande sans filtre : « Effacer les filtres » apparaît', async () => {
    const page = await ouvrir('/adopter');
    lister.mockResolvedValue(collection([rex]));

    puce(page, 'taille=Grande').click();
    await stabiliser();

    expect(url()).toBe('/adopter?taille=Grande');
    expect(lister).toHaveBeenLastCalledWith({ taille: 'Grande' });
    expect(compteur(page)).toBe('1 animal');
    expect(effacer(page)).toBeDefined();
  });

  it('une valeur par paramètre : une autre puce du groupe remplace, la puce active se désactive', async () => {
    const page = await ouvrir('/adopter?espece=Chat&taille=Petite');

    puce(page, 'espece=Chien').click();
    await stabiliser();
    expect(url()).toBe('/adopter?espece=Chien&taille=Petite');
    expect(pucesActives(page)).toEqual(['espece=Chien', 'taille=Petite']);

    puce(page, 'taille=Petite').click();
    await stabiliser();
    expect(url()).toBe('/adopter?espece=Chien');
    expect(lister).toHaveBeenLastCalledWith({ espece: 'Chien' });
  });

  it('puces d’entente : paramètre à Oui', async () => {
    const page = await ouvrir('/adopter');

    puce(page, 'enfants=Oui').click();
    await stabiliser();
    puce(page, 'chats=Oui').click();
    await stabiliser();

    expect(url()).toBe('/adopter?enfants=Oui&chats=Oui');
    expect(lister).toHaveBeenLastCalledWith({ enfants: 'Oui', chats: 'Oui' });
    expect(pucesActives(page)).toEqual(['enfants=Oui', 'chats=Oui']);
  });

  it('« Effacer les filtres » : URL /adopter, tous les animaux', async () => {
    const page = await ouvrir('/adopter?espece=Chat&age=Bebe&chiens=Oui');

    effacer(page)!.click();
    await stabiliser();

    expect(url()).toBe('/adopter');
    expect(lister).toHaveBeenLastCalledWith({});
    expect(pucesActives(page)).toEqual([]);
    expect(effacer(page)).toBeUndefined();
  });

  it('aucun résultat : phrase rappelant les filtres actifs et « Effacer les filtres »', async () => {
    lister.mockResolvedValue(collection([]));

    const page = await ouvrir('/adopter?espece=Chien&taille=Grande&enfants=Oui');

    expect(page.querySelector('app-grille-animaux')).toBeNull();
    expect(page.querySelector('app-catalogue-vide')).toBeNull();
    expect(page.querySelector('.aucun-resultat')?.textContent?.trim()).toBe(
      'Aucun animal ne correspond à vos filtres : Chien, taille Grande, entente avec les enfants.',
    );
    expect(compteur(page)).toBe('0 animal');
    expect(effacer(page)).toBeDefined();
  });

  it('catalogue vide sans filtre : « Tous nos protégés… » et lien', async () => {
    lister.mockResolvedValue(collection([]));

    const page = await ouvrir('/adopter');

    expect(page.querySelector('app-grille-animaux')).toBeNull();
    expect(page.querySelector('.aucun-resultat')).toBeNull();
    expect(page.querySelector('app-catalogue-vide .etat-vide')?.textContent).toContain(
      "Tous nos protégés ont trouvé une famille pour l'instant !",
    );
    expect(page.querySelector('app-catalogue-vide a')?.getAttribute('href')).toBe(
      '/ils-ont-trouve-une-famille',
    );
  });

  it('API en échec : état « Catalogue vide », erreur journalisée, pas de compteur', async () => {
    const erreur = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    lister.mockRejectedValue(new Error('502'));

    const page = await ouvrir('/adopter');

    expect(page.querySelector('app-catalogue-vide')).not.toBeNull();
    expect(page.querySelector('app-grille-animaux')).toBeNull();
    expect(compteur(page)).toBe('');
    expect(erreur).toHaveBeenCalledOnce();
  });

  it('valeur inconnue dans l’URL : ignorée, jamais transmise à l’API', async () => {
    const page = await ouvrir('/adopter?espece=Lapin&age=jeune&enfants=AVoir&limite=2');

    expect(lister).toHaveBeenCalledExactlyOnceWith({});
    expect(pucesActives(page)).toEqual([]);
  });
});
