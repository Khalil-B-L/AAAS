import { formatDate } from '@angular/common';
import { LOCALE_ID, Pipe, PipeTransform, inject } from '@angular/core';
import { parseUtc } from '../core/parse-utc';

/** Date d'un DTO (UTC avec `Z`) affichée « 12 octobre 2026 » ; vide → chaîne vide. */
@Pipe({ name: 'dateFr' })
export class DateFrPipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);

  transform(valeur: string | null | undefined): string {
    return valeur ? formatDate(parseUtc(valeur), 'd MMMM y', this.locale) : '';
  }
}
