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

    const appel = service.lister(4);
    const requete = http.expectOne('/api/animaux?limite=4');
    expect(requete.request.method).toBe('GET');
    requete.flush(reponse);

    await expect(appel).resolves.toEqual(reponse);
  });

  it('sans limite, appelle GET /api/animaux sans paramètre', async () => {
    const appel = service.lister();
    http.expectOne('/api/animaux').flush(collection([]));

    await expect(appel).resolves.toEqual(collection([]));
  });

  it("propage un échec HTTP à l'appelant", async () => {
    const appel = service.lister(4);
    http.expectOne('/api/animaux?limite=4').flush(null, { status: 500, statusText: 'Erreur' });

    await expect(appel).rejects.toBeTruthy();
  });
});
