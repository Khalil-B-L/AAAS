import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { returnUrlSur } from '../../../core/auth/return-url';
import { interpreterErreur } from '../erreurs-formulaire';

const CHAMPS = ['prenom', 'nom', 'email', 'telephone', 'motDePasse'] as const;
type Champ = (typeof CHAMPS)[number];

/** Créer mon compte (CAP-1) : téléphone obligatoire ; l'API valide tout et crée un Membre. */
@Component({
  selector: 'app-inscription',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './inscription.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inscription {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly hote = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  /** Lié depuis le query param `returnUrl` (`withComponentInputBinding`). */
  readonly returnUrl = input<string>();

  private readonly retour = computed(() => returnUrlSur(this.returnUrl()));
  protected readonly parametresConnexion = computed(() =>
    this.retour() === '/' ? {} : { returnUrl: this.retour() },
  );

  protected readonly formulaire = inject(NonNullableFormBuilder).group({
    prenom: '',
    nom: '',
    email: '',
    telephone: '',
    motDePasse: '',
  });
  protected readonly envoi = signal(false);
  protected readonly erreurs = signal<Partial<Record<Champ, string>>>({});
  protected readonly alerte = signal<string | null>(null);

  /** `aria-describedby` d'un champ : son aide éventuelle puis son message d'erreur. */
  protected decritPar(champ: Champ, aide?: string): string | null {
    const ids = [aide, this.erreurs()[champ] ? `inscription-${champ}-erreur` : undefined].filter(
      Boolean,
    );
    return ids.length > 0 ? ids.join(' ') : null;
  }

  protected async envoyer(): Promise<void> {
    if (this.envoi()) {
      return;
    }
    this.envoi.set(true);
    this.erreurs.set({});
    this.alerte.set(null);
    try {
      await this.auth.inscrire(this.formulaire.getRawValue());
    } catch (erreur) {
      const { champs, alerte } = interpreterErreur(erreur, CHAMPS);
      this.erreurs.set(champs);
      this.alerte.set(alerte);
      this.envoi.set(false);
      this.focaliserPremiereErreur();
      return;
    }
    this.envoi.set(false);
    await this.router.navigateByUrl(this.retour());
  }

  private focaliserPremiereErreur(): void {
    afterNextRender(
      () => this.hote.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
