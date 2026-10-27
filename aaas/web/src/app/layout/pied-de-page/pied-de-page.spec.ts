import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SiteDto } from '../../core/api/site.dto';
import { SiteService } from '../../core/site.service';
import { PiedDePage } from './pied-de-page';

describe('PiedDePage', () => {
  const site = signal<SiteDto | null>(null);

  beforeEach(() => {
    site.set(null);
    TestBed.configureTestingModule({
      imports: [PiedDePage],
      providers: [provideRouter([]), { provide: SiteService, useValue: { site } }],
    });
  });

  async function rendre(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(PiedDePage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function liensUtiles(pied: HTMLElement): [string | undefined, string | null][] {
    return Array.from(pied.querySelectorAll<HTMLAnchorElement>('nav a')).map((lien) => [
      lien.textContent?.trim(),
      lien.getAttribute('href'),
    ]);
  }

  const liensAttendus = [
    ['Dons', '/nous-aider/dons'],
    ['Bénévolat', '/nous-aider/benevolat'],
    ["Famille d'accueil", '/nous-aider/famille-accueil'],
    ['Contact', '/contact'],
    ['Ils ont trouvé une famille', '/ils-ont-trouve-une-famille'],
  ];

  it('affiche le numéro cliquable et les réseaux en nouvel onglet', async () => {
    site.set({
      telephone: '+21672000000',
      facebook: 'https://www.facebook.com/aaas-test',
      instagram: 'https://www.instagram.com/aaas-test',
    });

    const pied = await rendre();

    const telephone = pied.querySelector<HTMLAnchorElement>('a[href^="tel:"]');
    expect(telephone?.getAttribute('href')).toBe('tel:+21672000000');
    expect(telephone?.querySelector('.telephone')?.textContent?.trim()).toBe('+216 72 000 000');

    const facebook = pied.querySelector('a[aria-label="Facebook"]');
    const instagram = pied.querySelector('a[aria-label="Instagram"]');
    expect(facebook?.getAttribute('href')).toBe('https://www.facebook.com/aaas-test');
    expect(instagram?.getAttribute('href')).toBe('https://www.instagram.com/aaas-test');
    for (const reseau of [facebook, instagram]) {
      expect(reseau?.getAttribute('target')).toBe('_blank');
      expect(reseau?.getAttribute('rel')).toContain('noopener');
    }

    expect(liensUtiles(pied)).toEqual(liensAttendus);
  });

  it('site chargé sans réseaux : pas de liste de réseaux vide', async () => {
    site.set({ telephone: '+21672000000', facebook: '', instagram: '' });

    const pied = await rendre();

    expect(pied.querySelector('.reseaux')).toBeNull();
    expect(pied.querySelector('a[href^="tel:"]')).not.toBeNull();
  });

  it('sans site chargé : ni numéro ni réseaux, liens présents', async () => {
    const pied = await rendre();

    expect(pied.querySelector('a[href^="tel:"]')).toBeNull();
    expect(pied.querySelector('a[aria-label="Facebook"]')).toBeNull();
    expect(pied.querySelector('a[aria-label="Instagram"]')).toBeNull();
    expect(liensUtiles(pied)).toEqual(liensAttendus);
  });
});
