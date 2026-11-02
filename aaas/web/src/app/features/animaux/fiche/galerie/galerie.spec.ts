import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PhotoDto, StatutAnimal } from '../../../../core/api/animal.dto';
import { ficheCaramel } from '../../../../testing/animaux';
import { Galerie } from './galerie';

describe('Galerie', () => {
  let fixture: ComponentFixture<Galerie>;

  async function rendre(
    photos: readonly PhotoDto[] = ficheCaramel.photos,
    statut: StatutAnimal = 'Disponible',
  ): Promise<HTMLElement> {
    TestBed.configureTestingModule({ imports: [Galerie] });
    fixture = TestBed.createComponent(Galerie);
    fixture.componentRef.setInput('photos', photos);
    fixture.componentRef.setInput('nom', 'Caramel');
    fixture.componentRef.setInput('statut', statut);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  async function stabiliser(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const photo = (galerie: HTMLElement) => galerie.querySelector<HTMLImageElement>('.cadre img');
  const compteur = (galerie: HTMLElement) =>
    galerie.querySelector('.compteur')?.textContent?.replace(/\s+/g, ' ').trim();
  const points = (galerie: HTMLElement) =>
    Array.from(galerie.querySelectorAll<HTMLButtonElement>('button.point'));
  const vignettes = (galerie: HTMLElement) =>
    Array.from(galerie.querySelectorAll<HTMLButtonElement>('button.vignette'));
  const actif = (boutons: HTMLButtonElement[]) =>
    boutons.findIndex((bouton) => bouton.getAttribute('aria-current') === 'true');

  function balayer(galerie: HTMLElement, debut: number, fin: number, finY = 0): void {
    const cadre = galerie.querySelector('.cadre')!;
    for (const [type, x, y] of [
      ['touchstart', debut, 0],
      ['touchend', fin, finY],
    ] as const) {
      const evenement = new Event(type);
      Object.defineProperty(evenement, 'changedTouches', { value: [{ clientX: x, clientY: y }] });
      cadre.dispatchEvent(evenement);
    }
  }

  it('affiche la 1re photo, le compteur « 1 / 3 », la pastille et des textes alternatifs « Photo n de Caramel »', async () => {
    const galerie = await rendre();

    expect(photo(galerie)?.getAttribute('src')).toBe('/fichiers/caramel-1.jpg');
    expect(photo(galerie)?.getAttribute('alt')).toBe('Photo 1 de Caramel');
    expect(compteur(galerie)).toBe('1 / 3');
    expect(galerie.querySelector('app-pastille-statut')?.textContent?.trim()).toBe('Disponible');
    expect(vignettes(galerie).map((v) => v.querySelector('img')?.getAttribute('alt'))).toEqual([
      'Photo 1 de Caramel',
      'Photo 2 de Caramel',
      'Photo 3 de Caramel',
    ]);
    expect(points(galerie).map((p) => p.getAttribute('aria-label'))).toEqual([
      'Photo 1 de Caramel',
      'Photo 2 de Caramel',
      'Photo 3 de Caramel',
    ]);
    expect(actif(vignettes(galerie))).toBe(0);
    expect(actif(points(galerie))).toBe(0);
  });

  it('points et vignettes sont des boutons (actionnables au clavier)', async () => {
    const galerie = await rendre();

    for (const bouton of [...points(galerie), ...vignettes(galerie)]) {
      expect(bouton.tagName).toBe('BUTTON');
      expect(bouton.type).toBe('button');
    }
  });

  it('clic sur la 3e vignette : photo, compteur et vignette active à jour', async () => {
    const galerie = await rendre();

    vignettes(galerie)[2].click();
    await stabiliser();

    expect(photo(galerie)?.getAttribute('src')).toBe('/fichiers/caramel-3.jpg');
    expect(photo(galerie)?.getAttribute('alt')).toBe('Photo 3 de Caramel');
    expect(compteur(galerie)).toBe('3 / 3');
    expect(actif(vignettes(galerie))).toBe(2);
    expect(actif(points(galerie))).toBe(2);
  });

  it('clic sur le point 2 : photo 2, compteur « 2 / 3 »', async () => {
    const galerie = await rendre();

    points(galerie)[1].click();
    await stabiliser();

    expect(photo(galerie)?.getAttribute('src')).toBe('/fichiers/caramel-2.jpg');
    expect(compteur(galerie)).toBe('2 / 3');
    expect(actif(vignettes(galerie))).toBe(1);
  });

  it('balayage : vers la gauche photo suivante, vers la droite précédente, sans boucler ; trop court ignoré', async () => {
    const galerie = await rendre();

    balayer(galerie, 200, 100);
    await stabiliser();
    expect(compteur(galerie)).toBe('2 / 3');

    balayer(galerie, 200, 190);
    await stabiliser();
    expect(compteur(galerie)).toBe('2 / 3');

    balayer(galerie, 200, 100);
    balayer(galerie, 200, 100);
    await stabiliser();
    expect(compteur(galerie)).toBe('3 / 3');

    balayer(galerie, 100, 200);
    await stabiliser();
    expect(compteur(galerie)).toBe('2 / 3');

    balayer(galerie, 100, 200);
    balayer(galerie, 100, 200);
    await stabiliser();
    expect(compteur(galerie)).toBe('1 / 3');
  });

  it('geste surtout vertical (défilement) avec une dérive horizontale au-dessus du seuil : photo inchangée', async () => {
    const galerie = await rendre();

    balayer(galerie, 200, 140, 200);
    await stabiliser();

    expect(compteur(galerie)).toBe('1 / 3');
  });

  it('sans photo : dégradé de remplacement, ni image, ni compteur, ni points, ni vignettes', async () => {
    const galerie = await rendre([]);

    expect(galerie.querySelector('.cadre')).not.toBeNull();
    expect(galerie.querySelector('img')).toBeNull();
    expect(galerie.querySelector('.compteur')).toBeNull();
    expect(points(galerie)).toEqual([]);
    expect(vignettes(galerie)).toEqual([]);
  });

  it('une seule photo : compteur « 1 / 1 », ni points ni vignettes', async () => {
    const galerie = await rendre([ficheCaramel.photos[0]]);

    expect(compteur(galerie)).toBe('1 / 1');
    expect(points(galerie)).toEqual([]);
    expect(vignettes(galerie)).toEqual([]);
  });

  it('revient à la 1re photo quand les photos changent', async () => {
    const galerie = await rendre();
    vignettes(galerie)[2].click();
    await stabiliser();

    fixture.componentRef.setInput('photos', [
      { id: 91, url: '/fichiers/rex-1.jpg', ordre: 1 },
      { id: 92, url: '/fichiers/rex-2.jpg', ordre: 2 },
    ]);
    await stabiliser();

    expect(photo(galerie)?.getAttribute('src')).toBe('/fichiers/rex-1.jpg');
    expect(compteur(galerie)).toBe('1 / 2');
  });
});
