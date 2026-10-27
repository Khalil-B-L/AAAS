import { Pipe, PipeTransform } from '@angular/core';

const TUNISIE = /^\+216(\d{2})(\d{3})(\d{3})$/;

/**
 * Affichage d'un numéro E.164 (AD-14). Tunisie : `+216 72 000 000` (groupes 2-3-3) ;
 * tout autre numéro est affiché tel quel ; vide → chaîne vide.
 * L'appel se fait toujours par `tel:` + la valeur E.164 brute, jamais par ce texte.
 */
@Pipe({ name: 'telephone' })
export class TelephonePipe implements PipeTransform {
  transform(valeur: string | null | undefined): string {
    if (!valeur) {
      return '';
    }
    const tunisie = TUNISIE.exec(valeur);
    return tunisie ? `+216 ${tunisie[1]} ${tunisie[2]} ${tunisie[3]}` : valeur;
  }
}
