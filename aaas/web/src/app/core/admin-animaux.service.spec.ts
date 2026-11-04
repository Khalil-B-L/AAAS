import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { carteAnimal, caramel, collection, ficheCaramel, rex } from '../testing/animaux';
import { AnimalEcritureDto } from './api/animal.dto';
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

  const ecriture: AnimalEcritureDto = {
    nom: 'Luna',
    espece: 'Chien',
    sexe: 'Femelle',
    age: 'Jeune',
    taille: 'Moyenne',
    description: 'Trouvée à Kélibia.',
    vaccine: true,
    sterilise: false,
    puce: true,
    ententeEnfants: 'Oui',
    ententeChiens: 'Oui',
    ententeChats: 'AVoir',
  };

  it('obtenir(id) appelle GET /api/admin/animaux/:id', async () => {
    const appel = service.obtenir(7);
    const requete = http.expectOne('/api/admin/animaux/7');
    expect(requete.request.method).toBe('GET');
    requete.flush(ficheCaramel);

    await expect(appel).resolves.toEqual(ficheCaramel);
  });

  it('obtenir(id) rejette sur 404', async () => {
    const appel = service.obtenir(999);
    http
      .expectOne('/api/admin/animaux/999')
      .flush({ error: 'Introuvable.' }, { status: 404, statusText: 'Not Found' });

    await expect(appel).rejects.toMatchObject({ status: 404 });
  });

  it('creer(dto) appelle POST /api/admin/animaux avec les 12 champs', async () => {
    const appel = service.creer(ecriture);
    const requete = http.expectOne('/api/admin/animaux');
    expect(requete.request.method).toBe('POST');
    expect(requete.request.body).toEqual(ecriture);
    requete.flush(ficheCaramel, { status: 201, statusText: 'Created' });

    await expect(appel).resolves.toEqual(ficheCaramel);
  });

  it('modifier(id, dto) appelle PUT /api/admin/animaux/:id ; 409 propagé', async () => {
    const appel = service.modifier(7, ecriture);
    const requete = http.expectOne('/api/admin/animaux/7');
    expect(requete.request.method).toBe('PUT');
    expect(requete.request.body).toEqual(ecriture);
    requete.flush(ficheCaramel);
    await expect(appel).resolves.toEqual(ficheCaramel);

    const conflit = service.modifier(7, ecriture);
    http
      .expectOne('/api/admin/animaux/7')
      .flush(
        { error: 'Modifié entre-temps.', code: 'modifie-ailleurs' },
        { status: 409, statusText: 'Conflict' },
      );
    const erreur: unknown = await conflit.catch((e: unknown) => e);
    expect(erreur).toMatchObject({ status: 409 });
    expect(lireErreurApi(erreur)?.code).toBe('modifie-ailleurs');
  });

  it('ajouterPhoto(id, fichier) envoie un FormData avec le champ « fichier »', async () => {
    const fichier = new File(['x'], 'luna.jpg', { type: 'image/jpeg' });
    const appel = service.ajouterPhoto(7, fichier);
    const requete = http.expectOne('/api/admin/animaux/7/photos');
    expect(requete.request.method).toBe('POST');
    const corps = requete.request.body as FormData;
    expect(corps).toBeInstanceOf(FormData);
    expect((corps.get('fichier') as File).name).toBe('luna.jpg');
    const photo = { id: 74, url: '/fichiers/luna.jpg', ordre: 4 };
    requete.flush(photo, { status: 201, statusText: 'Created' });

    await expect(appel).resolves.toEqual(photo);
  });

  it('ajouterPhoto rejette sur 400 avec errors.fichier', async () => {
    const appel = service.ajouterPhoto(7, new File(['x'], 'a.gif'));
    const corps = { error: 'Données invalides.', errors: { fichier: 'Format non accepté.' } };
    http
      .expectOne('/api/admin/animaux/7/photos')
      .flush(corps, { status: 400, statusText: 'Bad Request' });

    const erreur: unknown = await appel.catch((e: unknown) => e);
    expect(lireErreurApi(erreur)).toEqual(corps);
  });

  it('supprimerPhoto(id, photoId) appelle DELETE /api/admin/animaux/:id/photos/:photoId', async () => {
    const appel = service.supprimerPhoto(7, 72);
    const requete = http.expectOne('/api/admin/animaux/7/photos/72');
    expect(requete.request.method).toBe('DELETE');
    requete.flush(null, { status: 204, statusText: 'No Content' });

    await expect(appel).resolves.toBeUndefined();
  });
});
