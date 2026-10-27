import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AnimalCarteDto, CollectionDto } from '../../core/api/animal.dto';
import { AnimauxService } from '../../core/animaux.service';
import { collection, derniersPensionnaires } from '../../testing/animaux';
import { Accueil } from './accueil';

describe('Accueil', () => {
  let lister: ReturnType<typeof vi.fn<(limite?: number) => Promise<CollectionDto<AnimalCarteDto>>>>;

  beforeEach(() => {
    lister = vi.fn<(limite?: number) => Promise<CollectionDto<AnimalCarteDto>>>();
    lister.mockResolvedValue(collection(derniersPensionnaires));
  });

  afterEach(() => vi.restoreAllMocks());

  async function rendre(): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      imports: [Accueil],
      providers: [provideRouter([]), { provide: AnimauxService, useValue: { lister } }],
    });
    const fixture = TestBed.createComponent(Accueil);
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it("affiche l'accroche et les boutons Miaou ! / Wouf ! pré-filtrés", async () => {
    const accueil = await rendre();

    expect(accueil.querySelector('h1')?.textContent?.trim()).toBe(
      'Une truffe vous attend au Cap Bon !',
    );
    const especes = Array.from(accueil.querySelectorAll<HTMLAnchorElement>('.especes a'));
    expect(especes.map((lien) => lien.getAttribute('href'))).toEqual([
      '/adopter?espece=Chat',
      '/adopter?espece=Chien',
    ]);
    expect(especes.map((lien) => lien.querySelector('[aria-hidden="true"]')?.textContent)).toEqual([
      '🐱 ',
      '🐶 ',
    ]);
  });

  it('prévoit les derniers pensionnaires et « Voir tous les animaux → »', async () => {
    const accueil = await rendre();

    expect(accueil.querySelector('#titre-pensionnaires')?.textContent).toBe(
      'Nos derniers pensionnaires',
    );
    const voirTous = accueil.querySelector('.voir-tous a');
    expect(voirTous?.textContent?.trim()).toBe('Voir tous les animaux →');
    expect(voirTous?.getAttribute('href')).toBe('/adopter');
  });

  it('charge les 4 derniers pensionnaires et affiche une carte par animal, dans l’ordre', async () => {
    const accueil = await rendre();

    expect(lister).toHaveBeenCalledExactlyOnceWith(4);
    const cartes = Array.from(accueil.querySelectorAll('.grille-animaux app-carte-animal'));
    expect(cartes.map((carte) => carte.querySelector('h3')?.textContent?.trim())).toEqual([
      'Caramel',
      'Rex',
      'Mimi',
      'Filou',
    ]);
    expect(cartes.map((carte) => carte.querySelector('a')?.getAttribute('href'))).toEqual([
      '/animaux/7',
      '/animaux/12',
      '/animaux/3',
      '/animaux/4',
    ]);
    expect(cartes[1].querySelector('.photo-reserve')).not.toBeNull();
    expect(accueil.querySelector('.etat-vide')).toBeNull();
  });

  it('aucun animal : message d’état vide, « Voir tous les animaux → » conservé', async () => {
    lister.mockResolvedValue(collection([]));

    const accueil = await rendre();

    expect(accueil.querySelector('.grille-animaux')).toBeNull();
    expect(accueil.querySelector('.etat-vide')?.textContent).toContain(
      "Tous nos protégés ont trouvé une famille pour l'instant !",
    );
    expect(accueil.querySelector('.etat-vide a')?.getAttribute('href')).toBe(
      '/ils-ont-trouve-une-famille',
    );
    expect(accueil.querySelector('.voir-tous a')?.getAttribute('href')).toBe('/adopter');
  });

  it('API en échec : même état vide et erreur journalisée', async () => {
    const erreur = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    lister.mockRejectedValue(new Error('502'));

    const accueil = await rendre();

    expect(accueil.querySelector('.grille-animaux')).toBeNull();
    expect(accueil.querySelector('.etat-vide')).not.toBeNull();
    expect(accueil.querySelector('.voir-tous a')).not.toBeNull();
    expect(erreur).toHaveBeenCalledOnce();
  });

  it("ne rend pas la section Actualités tant qu'il n'y a pas de données", async () => {
    const accueil = await rendre();

    expect(accueil.querySelector('#titre-actualites')).toBeNull();
    expect(accueil.textContent).not.toContain('Actualités');
  });
});
