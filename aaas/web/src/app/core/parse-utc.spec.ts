import { parseUtc } from './parse-utc';

describe('parseUtc', () => {
  it('lit une date UTC suffixée Z', () => {
    expect(parseUtc('2026-10-12T08:00:00Z').toISOString()).toBe('2026-10-12T08:00:00.000Z');
  });

  it('lit les fractions de seconde et un décalage explicite', () => {
    expect(parseUtc('2026-10-12T08:00:00.1234567Z').toISOString()).toBe('2026-10-12T08:00:00.123Z');
    expect(parseUtc('2026-10-12T09:00:00+01:00').toISOString()).toBe('2026-10-12T08:00:00.000Z');
  });

  it.each(['2026-10-12T08:00:00', '2026-10-12', '', 'pas une date', '2026-13-45T99:00:00Z'])(
    'rejette « %s »',
    (valeur) => {
      expect(() => parseUtc(valeur)).toThrow();
    },
  );
});
