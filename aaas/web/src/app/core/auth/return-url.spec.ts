import { returnUrlSur } from './return-url';

describe('returnUrlSur', () => {
  it.each([
    '/',
    '/mes-reservations',
    '/animaux/7/reserver?x=1',
    '/admin/reservations?statut=EnAttente',
  ])('garde le chemin interne %s', (url) => {
    expect(returnUrlSur(url)).toBe(url);
  });

  it.each([
    'https://evil.fr',
    '//evil.fr',
    '/\\evil.fr',
    'javascript:alert(1)',
    'mes-reservations',
    '',
    null,
    undefined,
  ])('remplace %s par /', (url) => {
    expect(returnUrlSur(url)).toBe('/');
  });
});
