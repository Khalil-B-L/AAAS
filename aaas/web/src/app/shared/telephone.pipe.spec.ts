import { TelephonePipe } from './telephone.pipe';

describe('TelephonePipe', () => {
  const pipe = new TelephonePipe();

  it('groupe un numéro tunisien en 2-3-3', () => {
    expect(pipe.transform('+21672000000')).toBe('+216 72 000 000');
  });

  it('affiche tel quel tout autre E.164', () => {
    expect(pipe.transform('+33612345678')).toBe('+33612345678');
    expect(pipe.transform('+2167200000')).toBe('+2167200000');
  });

  it.each([null, undefined, ''])('rend une chaîne vide pour %s', (valeur) => {
    expect(pipe.transform(valeur)).toBe('');
  });
});
