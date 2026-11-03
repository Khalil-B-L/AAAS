import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  ElementRef,
  OnDestroy,
  afterNextRender,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

let prochainId = 0;

/**
 * Dialogue de confirmation d'une action admin destructive (DESIGN.md § Dialogue de confirmation, EXPERIENCE.md
 * § Accessibility Floor) : « Êtes-vous sûr ? », la phrase et le libellé d'action fournis par l'appelant, bouton
 * d'action puis « Retour ». À afficher sous `@if` : le dialogue s'ouvre à sa création et se ferme à sa destruction.
 *
 * Focus sur « Retour » à l'ouverture, Tab / Maj+Tab piégés, Échap et « Retour » émettent `retour` sans rien faire
 * d'autre, focus rendu à `origine` (par défaut l'élément actif à l'ouverture) à la fermeture. Pendant l'action
 * (`occupe`), les deux boutons sont inactifs et Échap est ignorée ; le focus reste dans le dialogue.
 */
@Component({
  selector: 'app-dialogue-confirmation',
  templateUrl: './dialogue-confirmation.html',
  styleUrl: './dialogue-confirmation.css',
  // Clavier écouté sur le document : Échap et Tab agissent où que soit le focus tant que le dialogue est ouvert.
  host: {
    '(document:focusin)': 'retenirFocus($event)',
    '(document:keydown)': 'clavier($event)',
  },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DialogueConfirmation implements OnDestroy {
  private readonly document = inject(DOCUMENT);

  /** Phrase nommant l'objet (« Supprimer Rex ? Sa fiche et ses photos seront effacées. »). */
  readonly phrase = input.required<string>();
  /** Libellé du bouton d'action destructive (« Supprimer », « Annuler la réservation »). */
  readonly action = input.required<string>();
  /** Action en cours : boutons inactifs, Échap ignorée. */
  readonly occupe = input(false);
  /** Élément à qui rendre le focus à la fermeture ; par défaut, l'élément actif à l'ouverture. */
  readonly origine = input<HTMLElement | null>(null);

  /** L'utilisateur confirme l'action. */
  readonly confirme = output<void>();
  /** « Retour » ou Échap : l'appelant ferme le dialogue sans rien changer. */
  readonly retour = output<void>();

  protected readonly idTitre = `dialogue-confirmation-titre-${prochainId}`;
  protected readonly idPhrase = `dialogue-confirmation-phrase-${prochainId++}`;

  private readonly boiteDialogue = viewChild.required<ElementRef<HTMLElement>>('boiteDialogue');
  private readonly boutonAction = viewChild.required<ElementRef<HTMLButtonElement>>('boutonAction');
  private readonly boutonRetour = viewChild.required<ElementRef<HTMLButtonElement>>('boutonRetour');

  private readonly actifALOuverture = this.document.activeElement;

  constructor() {
    afterNextRender(() => this.boutonRetour().nativeElement.focus());
  }

  ngOnDestroy(): void {
    const cible = this.origine() ?? this.actifALOuverture;
    if (cible instanceof HTMLElement && cible.isConnected) {
      cible.focus();
    }
  }

  protected confirmer(): void {
    if (!this.occupe()) {
      this.confirme.emit();
    }
  }

  protected revenir(): void {
    if (!this.occupe()) {
      this.retour.emit();
    }
  }

  protected clavier(evenement: KeyboardEvent): void {
    if (evenement.key === 'Escape') {
      evenement.preventDefault();
      this.revenir();
      return;
    }
    if (evenement.key !== 'Tab') {
      return;
    }
    const premier = this.boutonAction().nativeElement;
    const dernier = this.boutonRetour().nativeElement;
    const actif = this.document.activeElement;
    if (actif !== premier && actif !== dernier) {
      // Focus sur la boîte ou sur `<body>` (clic hors d'un bouton) : on entre par un bout du dialogue.
      evenement.preventDefault();
      (evenement.shiftKey ? dernier : premier).focus();
    } else if (evenement.shiftKey && actif === premier) {
      evenement.preventDefault();
      dernier.focus();
    } else if (!evenement.shiftKey && actif === dernier) {
      evenement.preventDefault();
      premier.focus();
    }
  }

  /** Clic sur le voile : le focus ne quitte pas le dialogue (il irait sur `<body>` sans `focusin`). */
  protected retenirClicVoile(evenement: MouseEvent): void {
    if (evenement.target === evenement.currentTarget) {
      evenement.preventDefault();
      this.boutonRetour().nativeElement.focus();
    }
  }

  /** Focus parti hors du dialogue (clic sur le voile, lecteur d'écran…) : ramené sur « Retour ». */
  protected retenirFocus(evenement: FocusEvent): void {
    const cible = evenement.target;
    if (cible instanceof Node && !this.boiteDialogue().nativeElement.contains(cible)) {
      this.boutonRetour().nativeElement.focus();
    }
  }
}
