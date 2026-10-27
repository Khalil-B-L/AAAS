import { TestBed } from '@angular/core/testing';
import { StatutAnimal } from '../../core/api/animal.dto';
import { LIBELLES_STATUT_ANIMAL } from '../libelles';
import { PastilleStatut } from './pastille-statut';

describe('PastilleStatut', () => {
  async function rendre(statut: StatutAnimal): Promise<HTMLElement> {
    TestBed.configureTestingModule({ imports: [PastilleStatut] });
    const fixture = TestBed.createComponent(PastilleStatut);
    fixture.componentRef.setInput('statut', statut);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it.each<[StatutAnimal, string]>([
    ['Disponible', 'Disponible'],
    ['Reserve', 'Réservé'],
    ['Adopte', 'Adopté'],
  ])('%s : libellé « %s » et classe de couleur du statut', async (statut, libelle) => {
    const pastille = await rendre(statut);

    expect(pastille.textContent?.trim()).toBe(libelle);
    expect(pastille.textContent?.trim()).toBe(LIBELLES_STATUT_ANIMAL[statut]);
    expect(pastille.classList).toContain('pastille');
    expect(pastille.classList).toContain(`pastille-${statut}`);
  });
});
