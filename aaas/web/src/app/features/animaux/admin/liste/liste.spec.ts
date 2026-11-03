import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AdminAnimauxService } from '../../../../core/admin-animaux.service';
import { AnimalCarteDto, CollectionDto } from '../../../../core/api/animal.dto';
import { caramel, carteAnimal, collection, rex } from '../../../../testing/animaux';
import { ADMIN_ANIMAUX_ROUTES } from '../admin-animaux.routes';

describe('Admin · Animaux (/admin/animaux)', () => {
  const simba = carteAnimal({
    id: 21,
    nom: 'Simba',
    sexe: 'Male',
    age: 'Adulte',
    statut: 'Adopte',
  });
  const messageRefus = 'Rex a des réservations : il ne peut pas être supprimé.';

  let lister: ReturnType<typeof vi.fn<() => Promise<CollectionDto<AnimalCarteDto>>>>;
  let supprimer: ReturnType<typeof vi.fn<(id: number) => Promise<void>>>;
  let harnais: RouterTestingHarness;
  let page: HTMLElement;

  beforeEach(() => {
    lister = vi.fn<() => Promise<CollectionDto<AnimalCarteDto>>>();
    lister.mockResolvedValue(collection([caramel, rex, simba]));
    supprimer = vi.fn<(id: number) => Promise<void>>();
    supprimer.mockResolvedValue(undefined);
  });

  afterEach(() => vi.restoreAllMocks());

  async function stabiliser(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve));
    await harnais.fixture.whenStable();
    harnais.detectChanges();
  }

  async function ouvrir(): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'admin/animaux', children: ADMIN_ANIMAUX_ROUTES }]),
        { provide: AdminAnimauxService, useValue: { lister, supprimer } },
      ],
    });
    harnais = await RouterTestingHarness.create();
    await harnais.navigateByUrl('/admin/animaux');
    await stabiliser();
    page = harnais.routeNativeElement!;
  }

  const lignes = () => Array.from(page.querySelectorAll<HTMLElement>('li.ligne'));
  const noms = () => lignes().map((ligne) => ligne.querySelector('.nom')?.textContent?.trim());
  const boutonSupprimer = (nom: string) =>
    page.querySelector<HTMLButtonElement>(`li.ligne button[aria-label="Supprimer ${nom}"]`)!;
  const dialogue = () => page.querySelector<HTMLElement>('[role="dialog"]');
  const boutonDialogue = (libelle: string) =>
    Array.from(dialogue()!.querySelectorAll('button')).find(
      (bouton) => bouton.textContent?.trim() === libelle,
    )!;
  const alerte = () => page.querySelector('[role="alert"]');

  async function demanderSuppressionDeRex(): Promise<void> {
    const bouton = boutonSupprimer('Rex');
    bouton.focus();
    bouton.click();
    await stabiliser();
  }

  it('titre « Animaux », onglet « Admin · Animaux · AAAS », « Ajouter un animal » en tête', async () => {
    await ouvrir();

    expect(page.querySelector('h1')?.textContent?.trim()).toBe('Animaux');
    expect(TestBed.inject(Title).getTitle()).toBe('Admin · Animaux · AAAS');
    const ajouter = page.querySelector('.en-tete-page a');
    expect(ajouter?.textContent?.trim()).toBe('Ajouter un animal');
    expect(ajouter?.getAttribute('href')).toBe('/admin/animaux/nouveau');
  });

  it('une ligne par animal, tous statuts, dans l’ordre de l’API, chacune vers son formulaire', async () => {
    await ouvrir();

    expect(lister).toHaveBeenCalledTimes(1);
    expect(noms()).toEqual(['Caramel', 'Rex', 'Simba']);
    expect(
      lignes().map((ligne) => ligne.querySelector('app-pastille-statut')?.textContent?.trim()),
    ).toEqual(['Disponible', 'Réservé', 'Adopté']);
    expect(lignes().map((ligne) => ligne.querySelector('.meta')?.textContent?.trim())).toEqual([
      'Chat · Jeune',
      'Chien · Adulte',
      'Chat · Adulte',
    ]);
    expect(lignes().map((ligne) => ligne.querySelector('a')?.getAttribute('href'))).toEqual([
      '/admin/animaux/7',
      '/admin/animaux/12',
      '/admin/animaux/21',
    ]);
    expect(lignes().map((ligne) => ligne.querySelector('button')?.textContent?.trim())).toEqual([
      'Supprimer',
      'Supprimer',
      'Supprimer',
    ]);
    expect(boutonSupprimer('Rex').classList).toContain('bouton-contour-danger');
    expect(page.querySelector('.etat-vide')).toBeNull();
    expect(alerte()).toBeNull();
    expect(dialogue()).toBeNull();
  });

  it('vignette : photo principale, sinon dégradé de remplacement sans image', async () => {
    await ouvrir();

    const [ligneCaramel, ligneRex] = lignes();
    expect(ligneCaramel.querySelector('.vignette')).not.toBeNull();
    expect(ligneCaramel.querySelector('.vignette img')).toBeNull();
    expect(ligneRex.querySelector('.vignette img')?.getAttribute('src')).toBe('/fichiers/rex.jpg');
  });

  it('aucun animal : phrase courte et « Ajouter un animal »', async () => {
    lister.mockResolvedValue(collection([]));

    await ouvrir();

    expect(lignes()).toHaveLength(0);
    expect(page.querySelector('.etat-vide')?.textContent).toContain('Aucun animal pour l');
    expect(page.querySelector('a[href="/admin/animaux/nouveau"]')?.textContent?.trim()).toBe(
      'Ajouter un animal',
    );
    expect(alerte()).toBeNull();
  });

  it('chargement en échec : alerte d’erreur, erreur journalisée', async () => {
    const journal = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    lister.mockRejectedValue(new HttpErrorResponse({ status: 500 }));

    await ouvrir();

    expect(lignes()).toHaveLength(0);
    expect(alerte()?.textContent).toContain("La liste des animaux n'a pas pu être chargée.");
    expect(alerte()?.classList).toContain('alerte-erreur');
    expect(journal).toHaveBeenCalledWith(
      expect.stringContaining('/api/admin/animaux'),
      expect.any(HttpErrorResponse),
    );
  });

  it('« Supprimer » sur Rex : dialogue nommant Rex, focus sur « Retour »', async () => {
    await ouvrir();

    await demanderSuppressionDeRex();

    expect(dialogue()?.textContent).toContain('Êtes-vous sûr ?');
    expect(dialogue()?.textContent).toContain(
      'Supprimer Rex ? Sa fiche et ses photos seront effacées.',
    );
    expect(document.activeElement).toBe(boutonDialogue('Retour'));
    expect(supprimer).not.toHaveBeenCalled();
  });

  it.each([
    ['« Retour »', () => boutonDialogue('Retour').click()],
    [
      'Échap',
      () =>
        boutonDialogue('Retour').dispatchEvent(
          new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }),
        ),
    ],
  ])('%s : dialogue fermé, aucun appel, focus sur « Supprimer » de Rex', async (_, fermer) => {
    await ouvrir();
    await demanderSuppressionDeRex();

    fermer();
    await stabiliser();

    expect(dialogue()).toBeNull();
    expect(supprimer).not.toHaveBeenCalled();
    expect(noms()).toEqual(['Caramel', 'Rex', 'Simba']);
    expect(document.activeElement).toBe(boutonSupprimer('Rex'));
  });

  it('piège du focus : Tab depuis le dernier bouton, Maj+Tab depuis le premier restent dans le dialogue', async () => {
    await ouvrir();
    await demanderSuppressionDeRex();

    const tab = (cible: HTMLElement, shiftKey: boolean) =>
      cible.dispatchEvent(
        new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true }),
      );
    tab(boutonDialogue('Retour'), false);
    expect(document.activeElement).toBe(boutonDialogue('Supprimer'));
    tab(boutonDialogue('Supprimer'), true);
    expect(document.activeElement).toBe(boutonDialogue('Retour'));
  });

  it('confirmer, 204 : DELETE de Rex, dialogue fermé, ligne retirée sans recharger', async () => {
    await ouvrir();
    await demanderSuppressionDeRex();

    boutonDialogue('Supprimer').click();
    await stabiliser();

    expect(supprimer).toHaveBeenCalledExactlyOnceWith(12);
    expect(dialogue()).toBeNull();
    expect(noms()).toEqual(['Caramel', 'Simba']);
    expect(lister).toHaveBeenCalledTimes(1);
    expect(alerte()).toBeNull();
  });

  it('suppression en cours : un second clic n’envoie pas un second DELETE', async () => {
    let terminer!: () => void;
    supprimer.mockReturnValue(new Promise<void>((resolve) => (terminer = resolve)));
    await ouvrir();
    await demanderSuppressionDeRex();

    boutonDialogue('Supprimer').click();
    await stabiliser();
    expect(boutonDialogue('Supprimer').getAttribute('aria-disabled')).toBe('true');
    boutonDialogue('Supprimer').click();
    terminer();
    await stabiliser();

    expect(supprimer).toHaveBeenCalledTimes(1);
    expect(noms()).toEqual(['Caramel', 'Simba']);
  });

  it('409 animal-a-des-reservations : dialogue fermé, alerte avec le message de l’API, Rex conservé', async () => {
    supprimer.mockRejectedValue(
      new HttpErrorResponse({
        status: 409,
        error: { error: messageRefus, code: 'animal-a-des-reservations' },
      }),
    );
    await ouvrir();
    await demanderSuppressionDeRex();

    boutonDialogue('Supprimer').click();
    await stabiliser();

    expect(dialogue()).toBeNull();
    expect(alerte()?.textContent?.trim()).toBe(messageRefus);
    expect(noms()).toEqual(['Caramel', 'Rex', 'Simba']);
    // L'alerte est au-dessus de la liste.
    expect(
      alerte()!.compareDocumentPosition(page.querySelector('ul.liste')!) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it('404 : dialogue fermé, liste rechargée', async () => {
    supprimer.mockRejectedValue(
      new HttpErrorResponse({
        status: 404,
        error: { error: 'La ressource demandée est introuvable.' },
      }),
    );
    await ouvrir();
    lister.mockResolvedValue(collection([caramel, simba]));
    await demanderSuppressionDeRex();

    boutonDialogue('Supprimer').click();
    await stabiliser();

    expect(dialogue()).toBeNull();
    expect(lister).toHaveBeenCalledTimes(2);
    expect(noms()).toEqual(['Caramel', 'Simba']);
    expect(alerte()).toBeNull();
  });

  it('404 puis rechargement en échec : liste périmée retirée, alerte de chargement', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    supprimer.mockRejectedValue(new HttpErrorResponse({ status: 404 }));
    await ouvrir();
    lister.mockRejectedValue(new HttpErrorResponse({ status: 500 }));
    await demanderSuppressionDeRex();

    boutonDialogue('Supprimer').click();
    await stabiliser();

    expect(lignes()).toHaveLength(0);
    expect(page.querySelector('.etat-vide')).toBeNull();
    expect(alerte()?.textContent).toContain("La liste des animaux n'a pas pu être chargée.");
  });

  it('autre échec : alerte générique, erreur journalisée, Rex conservé', async () => {
    const journal = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    supprimer.mockRejectedValue(new HttpErrorResponse({ status: 500 }));
    await ouvrir();
    await demanderSuppressionDeRex();

    boutonDialogue('Supprimer').click();
    await stabiliser();

    expect(dialogue()).toBeNull();
    expect(alerte()?.textContent).toContain("La suppression n'a pas abouti.");
    expect(noms()).toEqual(['Caramel', 'Rex', 'Simba']);
    expect(journal).toHaveBeenCalledWith(
      expect.stringContaining('/api/admin/animaux'),
      expect.any(HttpErrorResponse),
    );
  });

  it('une nouvelle suppression efface l’alerte précédente', async () => {
    supprimer.mockRejectedValueOnce(
      new HttpErrorResponse({
        status: 409,
        error: { error: messageRefus, code: 'animal-a-des-reservations' },
      }),
    );
    await ouvrir();
    await demanderSuppressionDeRex();
    boutonDialogue('Supprimer').click();
    await stabiliser();
    expect(alerte()).not.toBeNull();

    boutonSupprimer('Caramel').click();
    await stabiliser();
    boutonDialogue('Supprimer').click();
    await stabiliser();

    expect(alerte()).toBeNull();
    expect(noms()).toEqual(['Rex', 'Simba']);
  });
});
