import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AnimalCarteDto, CollectionDto } from '../../../core/api/animal.dto';
import { AnimauxService } from '../../../core/animaux.service';
import { carteAnimal, collection } from '../../../testing/animaux';
import { ADOPTES_ROUTES } from '../animaux.routes';

describe('Ils ont trouvé une famille (/ils-ont-trouve-une-famille)', () => {
  const simba = carteAnimal({
    id: 21,
    nom: 'Simba',
    sexe: 'Male',
    age: 'Adulte',
    statut: 'Adopte',
  });
  const kira = carteAnimal({
    id: 22,
    nom: 'Kira',
    espece: 'Chien',
    age: 'Senior',
    statut: 'Adopte',
  });
  let listerAdoptes: ReturnType<typeof vi.fn<() => Promise<CollectionDto<AnimalCarteDto>>>>;
  let harnais: RouterTestingHarness;

  beforeEach(() => {
    listerAdoptes = vi.fn<() => Promise<CollectionDto<AnimalCarteDto>>>();
    listerAdoptes.mockResolvedValue(collection([kira, simba]));
  });

  afterEach(() => vi.restoreAllMocks());

  async function ouvrir(): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'ils-ont-trouve-une-famille', children: ADOPTES_ROUTES }]),
        { provide: AnimauxService, useValue: { listerAdoptes } },
      ],
    });
    harnais = await RouterTestingHarness.create();
    await harnais.navigateByUrl('/ils-ont-trouve-une-famille');
    await new Promise((resolve) => setTimeout(resolve));
    await harnais.fixture.whenStable();
    harnais.detectChanges();
    return harnais.routeNativeElement!;
  }

  const cartes = (page: HTMLElement) =>
    Array.from(page.querySelectorAll('app-grille-animaux app-carte-animal'));

  it('titre de page et titre d’onglet', async () => {
    const page = await ouvrir();

    expect(page.querySelector('h1')?.textContent?.trim()).toBe('Ils ont trouvé une famille');
    expect(TestBed.inject(Title).getTitle()).toBe('Ils ont trouvé une famille · AAAS');
  });

  it('une carte par adopté dans l’ordre de l’API, pastille Adopté, chaque carte ouvre la fiche', async () => {
    const page = await ouvrir();

    expect(listerAdoptes).toHaveBeenCalledTimes(1);
    expect(cartes(page).map((carte) => carte.querySelector('h3')?.textContent?.trim())).toEqual([
      'Kira',
      'Simba',
    ]);
    expect(
      cartes(page).map((carte) => carte.querySelector('app-pastille-statut')?.textContent?.trim()),
    ).toEqual(['Adopté', 'Adopté']);
    expect(cartes(page).map((carte) => carte.querySelector('a')?.getAttribute('href'))).toEqual([
      '/animaux/22',
      '/animaux/21',
    ]);
    expect(page.querySelector('.etat-vide')).toBeNull();
  });

  it('pas de filtre ni de pagination', async () => {
    const page = await ouvrir();

    expect(page.querySelector('[role="group"]')).toBeNull();
    expect(page.querySelector('button')).toBeNull();
  });

  it('aucun adopté : phrase courte et lien vers Adopter, pas l’état « Catalogue vide »', async () => {
    listerAdoptes.mockResolvedValue(collection([]));

    const page = await ouvrir();

    expect(cartes(page)).toHaveLength(0);
    expect(page.querySelector('.etat-vide')?.textContent).toContain(
      'Aucune adoption à afficher pour le moment.',
    );
    const lien = page.querySelector('.etat-vide a');
    expect(lien?.textContent?.trim()).toBe('Voir les animaux à adopter');
    expect(lien?.getAttribute('href')).toBe('/adopter');
    expect(page.querySelector('app-catalogue-vide')).toBeNull();
    expect(page.textContent).not.toContain('Tous nos protégés');
  });

  it('API en échec : même état vide, erreur journalisée', async () => {
    const erreur = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    listerAdoptes.mockRejectedValue(new Error('réseau'));

    const page = await ouvrir();

    expect(cartes(page)).toHaveLength(0);
    expect(page.querySelector('.etat-vide a')?.getAttribute('href')).toBe('/adopter');
    expect(erreur).toHaveBeenCalledWith(
      expect.stringContaining('/api/animaux/adoptes'),
      expect.any(Error),
    );
  });
});
