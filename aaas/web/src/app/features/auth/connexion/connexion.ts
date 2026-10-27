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

const CHAMPS = ['email', 'motDePasse'] as const;
type Champ = (typeof CHAMPS)[number];

/** Connexion (CAP-1) : email, mot de passe, puis retour à `returnUrl`. */
@Component({
  selector: 'app-connexion',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './connexion.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Connexion {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly hote = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  /** Lié depuis le query param `returnUrl` (`withComponentInputBinding`). */
  readonly returnUrl = input<string>();

  private readonly retour = computed(() => returnUrlSur(this.returnUrl()));
  /** « Créer mon compte » garde la page demandée. */
  protected readonly parametresInscription = computed(() =>
    this.retour() === '/' ? {} : { returnUrl: this.retour() },
  );

  protected readonly formulaire = inject(NonNullableFormBuilder).group({
    email: '',
    motDePasse: '',
  });
  protected readonly envoi = signal(false);
  protected readonly erreurs = signal<Partial<Record<Champ, string>>>({});
  protected readonly alerte = signal<string | null>(null);

  protected async envoyer(): Promise<void> {
    if (this.envoi()) {
      return;
    }
    this.envoi.set(true);
    this.erreurs.set({});
    this.alerte.set(null);
    try {
      await this.auth.connecter(this.formulaire.getRawValue());
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
