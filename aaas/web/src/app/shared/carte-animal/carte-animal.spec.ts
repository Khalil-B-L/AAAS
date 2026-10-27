import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AnimalCarteDto } from '../../core/api/animal.dto';
import { caramel, rex } from '../../testing/animaux';
import { CarteAnimal } from './carte-animal';

describe('CarteAnimal', () => {
  async function rendre(animal: AnimalCarteDto): Promise<HTMLElement> {
    TestBed.configureTestingModule({ imports: [CarteAnimal], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(CarteAnimal);
    fixture.componentRef.setInput('animal', animal);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('affiche le nom, la méta « Espèce · âge · sexe » et la pastille, et mène à la fiche', async () => {
    const carte = await rendre(caramel);

    expect(carte.querySelector('a')?.getAttribute('href')).toBe('/animaux/7');
    expect(carte.querySelector('h3')?.textContent?.trim()).toBe('Caramel');
    expect(carte.querySelector('.meta')?.textContent?.trim()).toBe('Chat · Jeune · Femelle');
    const pastille = carte.querySelector('app-pastille-statut');
    expect(pastille?.textContent?.trim()).toBe('Disponible');
    expect(pastille?.classList).toContain('carte-pastille');
    expect(pastille?.classList).toContain('pastille-Disponible');
  });

  it('sans photo : dégradé de remplacement, aucune image', async () => {
    const carte = await rendre(caramel);

    expect(carte.querySelector('.photo')).not.toBeNull();
    expect(carte.querySelector('img')).toBeNull();
    expect(carte.querySelector('.photo-reserve')).toBeNull();
  });

  it('Réservé : photo désaturée et pastille Réservé', async () => {
    const carte = await rendre(rex);

    expect(carte.querySelector('.photo')?.classList).toContain('photo-reserve');
    expect(carte.querySelector('img')?.getAttribute('src')).toBe('/fichiers/rex.jpg');
    expect(carte.querySelector('app-pastille-statut')?.textContent?.trim()).toBe('Réservé');
    expect(carte.querySelector('.meta')?.textContent?.trim()).toBe('Chien · Adulte · Mâle');
  });
});
