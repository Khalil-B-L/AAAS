import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SiteDto } from './api/site.dto';
import { SiteService } from './site.service';

describe('SiteService', () => {
  const site: SiteDto = {
    telephone: '+33123456789',
    facebook: 'https://www.facebook.com/aaas-test',
    instagram: 'https://www.instagram.com/aaas-test',
  };

  let service: SiteService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SiteService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });

  it('charge /api/site dans le signal site()', async () => {
    expect(service.site()).toBeNull();

    const chargement = service.charger();
    const requete = http.expectOne('/api/site');
    expect(requete.request.method).toBe('GET');
    requete.flush(site);
    await chargement;

    expect(service.site()).toEqual(site);
  });

  it('avale et journalise un échec HTTP', async () => {
    const erreur = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    const chargement = service.charger();
    http.expectOne('/api/site').flush(null, { status: 502, statusText: 'Bad Gateway' });

    await expect(chargement).resolves.toBeUndefined();
    expect(service.site()).toBeNull();
    expect(erreur).toHaveBeenCalledOnce();
  });
});
