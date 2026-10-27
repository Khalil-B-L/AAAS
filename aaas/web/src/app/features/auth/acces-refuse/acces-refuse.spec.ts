import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AccesRefuse } from './acces-refuse';

describe('AccesRefuse', () => {
  it("affiche le message et le retour à l'accueil", async () => {
    TestBed.configureTestingModule({ imports: [AccesRefuse], providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(AccesRefuse);
    await fixture.whenStable();
    const racine = fixture.nativeElement as HTMLElement;

    expect(racine.querySelector('h1')?.textContent?.trim()).toBe(
      "Cette page est réservée à l'association.",
    );
    const retour = racine.querySelector('a');
    expect(retour?.textContent?.trim()).toBe("Retour à l'accueil");
    expect(retour?.getAttribute('href')).toBe('/');
  });
});
