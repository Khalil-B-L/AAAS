import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Accueil } from './accueil';

describe('Accueil', () => {
  async function rendre(): Promise<HTMLElement> {
    TestBed.configureTestingModule({ imports: [Accueil], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(Accueil);
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

  it("ne rend pas la section Actualités tant qu'il n'y a pas de données", async () => {
    const accueil = await rendre();

    expect(accueil.querySelector('#titre-actualites')).toBeNull();
    expect(accueil.textContent).not.toContain('Actualités');
  });
});
