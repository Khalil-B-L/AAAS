import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { connecterEnTantQue, stockerJeton } from '../../testing/session';
import { utilisateurEnTantQue } from '../../testing/utilisateurs';
import { CLE_JETON } from './auth.service';
import { adminGuard, membreGuard } from './gardes';

@Component({ template: '<h1>page</h1>' })
class PageTest {}

describe('membreGuard et adminGuard', () => {
  let http: HttpTestingController;
  let harnais: RouterTestingHarness;
  let router: Router;

  async function preparer(jetonStocke?: string): Promise<void> {
    stockerJeton(jetonStocke);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'membre', canActivate: [membreGuard], component: PageTest },
          { path: 'admin/zone', canActivate: [adminGuard], component: PageTest },
          { path: '**', component: PageTest },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harnais = await RouterTestingHarness.create();
  }

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('page membre sans session → Connexion avec returnUrl', async () => {
    await preparer();

    await harnais.navigateByUrl('/membre?x=1');

    expect(router.url).toBe('/connexion?returnUrl=%2Fmembre%3Fx%3D1');
  });

  it('page membre avec session → page affichée', async () => {
    await preparer();
    await connecterEnTantQue('Membre');

    await harnais.navigateByUrl('/membre');

    expect(router.url).toBe('/membre');
  });

  it('page admin sans session → Connexion avec returnUrl', async () => {
    await preparer();

    await harnais.navigateByUrl('/admin/zone');

    expect(router.url).toBe('/connexion?returnUrl=%2Fadmin%2Fzone');
  });

  it('page admin par un Membre → /acces-refuse', async () => {
    await preparer();
    await connecterEnTantQue('Membre');

    await harnais.navigateByUrl('/admin/zone');

    expect(router.url).toBe('/acces-refuse');
  });

  it('page admin par un Admin → page affichée', async () => {
    await preparer();
    await connecterEnTantQue('Admin');

    await harnais.navigateByUrl('/admin/zone');

    expect(router.url).toBe('/admin/zone');
  });

  it('attend la restauration de la session avant de décider', async () => {
    await preparer('jeton-stocke');

    const navigation = harnais.navigateByUrl('/admin/zone');
    const moi = await vi.waitFor(() => http.expectOne('/api/auth/moi'));
    expect(moi.request.method).toBe('GET');
    moi.flush(utilisateurEnTantQue('Admin'));
    await navigation;

    expect(router.url).toBe('/admin/zone');
  });

  it('restauration refusée (401) → visiteur, Connexion', async () => {
    await preparer('jeton-perime');

    const navigation = harnais.navigateByUrl('/membre');
    const moi = await vi.waitFor(() => http.expectOne('/api/auth/moi'));
    moi.flush(
      { error: 'Connectez-vous pour continuer.' },
      { status: 401, statusText: 'Unauthorized' },
    );
    await navigation;

    expect(router.url).toBe('/connexion?returnUrl=%2Fmembre');
    expect(localStorage.getItem(CLE_JETON)).toBeNull();
  });
});
