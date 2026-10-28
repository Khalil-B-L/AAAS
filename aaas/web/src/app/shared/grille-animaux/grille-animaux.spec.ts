import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AnimalCarteDto } from '../../core/api/animal.dto';
import { derniersPensionnaires } from '../../testing/animaux';
import { CatalogueVide } from './catalogue-vide';
import { GrilleAnimaux } from './grille-animaux';

describe('GrilleAnimaux', () => {
  async function rendre(animaux: readonly AnimalCarteDto[]): Promise<HTMLElement> {
    TestBed.configureTestingModule({ imports: [GrilleAnimaux], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(GrilleAnimaux);
    fixture.componentRef.setInput('animaux', animaux);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('affiche une carte par animal, dans l’ordre, en liste', async () => {
    const grille = await rendre(derniersPensionnaires);

    const elements = Array.from(
      grille.querySelectorAll('ul.grille-animaux > li > app-carte-animal'),
    );
    expect(elements.map((carte) => carte.querySelector('h3')?.textContent?.trim())).toEqual([
      'Caramel',
      'Rex',
      'Mimi',
      'Filou',
    ]);
    expect(elements.map((carte) => carte.querySelector('a')?.getAttribute('href'))).toEqual([
      '/animaux/7',
      '/animaux/12',
      '/animaux/3',
      '/animaux/4',
    ]);
  });

  it('liste vide : grille sans carte', async () => {
    const grille = await rendre([]);

    expect(grille.querySelectorAll('app-carte-animal')).toHaveLength(0);
  });
});

describe('CatalogueVide', () => {
  it('affiche « Tous nos protégés… » et le lien vers Ils ont trouvé une famille', async () => {
    TestBed.configureTestingModule({ imports: [CatalogueVide], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(CatalogueVide);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.etat-vide')?.textContent).toContain(
      "Tous nos protégés ont trouvé une famille pour l'instant !",
    );
    const lien = element.querySelector('.etat-vide a');
    expect(lien?.textContent?.trim()).toBe('Ils ont trouvé une famille');
    expect(lien?.getAttribute('href')).toBe('/ils-ont-trouve-une-famille');
  });
});
