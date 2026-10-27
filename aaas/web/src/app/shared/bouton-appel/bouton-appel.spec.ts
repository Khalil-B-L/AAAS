import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SiteDto } from '../../core/api/site.dto';
import { SiteService } from '../../core/site.service';
import { BoutonAppel } from './bouton-appel';

describe('BoutonAppel', () => {
  const site = signal<SiteDto | null>(null);

  beforeEach(() => {
    site.set(null);
    TestBed.configureTestingModule({
      imports: [BoutonAppel],
      providers: [{ provide: SiteService, useValue: { site } }],
    });
  });

  async function rendre(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(BoutonAppel);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('appelle le numéro E.164 et affiche le numéro formaté', async () => {
    site.set({
      telephone: '+21672000000',
      facebook: 'https://fb.test',
      instagram: 'https://ig.test',
    });

    const lien = (await rendre()).querySelector('a');

    expect(lien?.getAttribute('href')).toBe('tel:+21672000000');
    expect(lien?.getAttribute('aria-label')).toBe("Appeler l'association");
    expect(lien?.querySelector('small')?.textContent?.trim()).toBe('+216 72 000 000');
    expect(lien?.querySelector('[aria-hidden="true"]')?.textContent).toContain('📞');
  });

  it("n'affiche rien si le site n'est pas chargé", async () => {
    expect((await rendre()).querySelector('a')).toBeNull();
  });
});
