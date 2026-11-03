import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { carteAnimal, caramel, collection, rex } from '../testing/animaux';
import { AdminAnimauxService } from './admin-animaux.service';
import { lireErreurApi } from './api/erreur-api';

describe('AdminAnimauxService', () => {
  let service: AdminAnimauxService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminAnimauxService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lister() appelle GET /api/admin/animaux sans paramètre et renvoie { items, serverNow }', async () => {
    const reponse = collection([
      caramel,
      rex,
      carteAnimal({ id: 21, nom: 'Simba', statut: 'Adopte' }),
    ]);

    const appel = service.lister();
    const requete = http.expectOne('/api/admin/animaux');
    expect(requete.request.method).toBe('GET');
    expect(requete.request.params.keys()).toEqual([]);
    requete.flush(reponse);

    await expect(appel).resolves.toEqual(reponse);
  });

  it("lister() propage un échec HTTP à l'appelant", async () => {
    const appel = service.lister();
    http.expectOne('/api/admin/animaux').flush(null, { status: 500, statusText: 'Erreur' });

    await expect(appel).rejects.toMatchObject({ status: 500 });
  });

  it('supprimer(id) appelle DELETE /api/admin/animaux/:id et se résout sur 204', async () => {
    const appel = service.supprimer(12);
    const requete = http.expectOne('/api/admin/animaux/12');
    expect(requete.request.method).toBe('DELETE');
    requete.flush(null, { status: 204, statusText: 'No Content' });

    await expect(appel).resolves.toBeUndefined();
  });

  it('supprimer(id) rejette sur 409 avec le corps { error, code } de l’API', async () => {
    const corps = {
      error: 'Rex a des réservations : il ne peut pas être supprimé.',
      code: 'animal-a-des-reservations',
    };
    const appel = service.supprimer(12);
    http.expectOne('/api/admin/animaux/12').flush(corps, { status: 409, statusText: 'Conflict' });

    const erreur: unknown = await appel.catch((e: unknown) => e);
    expect(erreur).toMatchObject({ status: 409 });
    expect(lireErreurApi(erreur)).toEqual(corps);
  });

  it('supprimer(id) rejette sur 404', async () => {
    const appel = service.supprimer(999999);
    http
      .expectOne('/api/admin/animaux/999999')
      .flush(
        { error: 'La ressource demandée est introuvable.' },
        { status: 404, statusText: 'Not Found' },
      );

    await expect(appel).rejects.toMatchObject({ status: 404 });
  });
});
