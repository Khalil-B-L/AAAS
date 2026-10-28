import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { collection, derniersPensionnaires } from '../testing/animaux';
import { AnimauxService } from './animaux.service';

describe('AnimauxService', () => {
  let service: AnimauxService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AnimauxService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('appelle GET /api/animaux?limite=4 et renvoie { items, serverNow }', async () => {
    const reponse = collection(derniersPensionnaires);

    const appel = service.lister({ limite: 4 });
    const requete = http.expectOne('/api/animaux?limite=4');
    expect(requete.request.method).toBe('GET');
    requete.flush(reponse);

    await expect(appel).resolves.toEqual(reponse);
  });

  it('sans filtre, appelle GET /api/animaux sans paramètre', async () => {
    const appel = service.lister();
    http.expectOne('/api/animaux').flush(collection([]));

    await expect(appel).resolves.toEqual(collection([]));
  });

  it('transmet chaque filtre de contrats-api.md §2 sous son nom, dans l’ordre du contrat', async () => {
    const appel = service.lister({
      limite: 10,
      chats: 'Oui',
      chiens: 'Oui',
      enfants: 'Oui',
      taille: 'Grande',
      age: 'Bebe',
      sexe: 'Femelle',
      espece: 'Chien',
    });
    http
      .expectOne(
        '/api/animaux?espece=Chien&sexe=Femelle&age=Bebe&taille=Grande&enfants=Oui&chiens=Oui&chats=Oui&limite=10',
      )
      .flush(collection([]));

    await expect(appel).resolves.toEqual(collection([]));
  });

  it('ne transmet pas un filtre absent', async () => {
    const appel = service.lister({ espece: 'Chat', taille: undefined });
    http.expectOne('/api/animaux?espece=Chat').flush(collection([]));

    await expect(appel).resolves.toEqual(collection([]));
  });

  it("propage un échec HTTP à l'appelant", async () => {
    const appel = service.lister({ limite: 4 });
    http.expectOne('/api/animaux?limite=4').flush(null, { status: 500, statusText: 'Erreur' });

    await expect(appel).rejects.toBeTruthy();
  });
});
