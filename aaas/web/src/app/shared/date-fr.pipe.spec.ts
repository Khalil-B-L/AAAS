import { registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DateFrPipe } from './date-fr.pipe';

describe('DateFrPipe', () => {
  let pipe: DateFrPipe;

  beforeEach(() => {
    registerLocaleData(localeFr);
    TestBed.configureTestingModule({ providers: [{ provide: LOCALE_ID, useValue: 'fr-FR' }] });
    pipe = TestBed.runInInjectionContext(() => new DateFrPipe());
  });

  it('affiche une date UTC en toutes lettres', () => {
    expect(pipe.transform('2026-10-12T08:00:00Z')).toBe('12 octobre 2026');
  });

  it('rejette une date sans fuseau', () => {
    expect(() => pipe.transform('2026-10-12T08:00:00')).toThrow();
  });

  it.each([null, undefined, ''])('rend une chaîne vide pour %s', (valeur) => {
    expect(pipe.transform(valeur)).toBe('');
  });
});
