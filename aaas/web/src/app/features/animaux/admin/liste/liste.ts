import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminAnimauxService } from '../../../../core/admin-animaux.service';
import { AnimalCarteDto } from '../../../../core/api/animal.dto';
import { lireErreurApi } from '../../../../core/api/erreur-api';
import { DialogueConfirmation } from '../../../../shared/dialogue-confirmation/dialogue-confirmation';
import { LIBELLES_ESPECE, LIBELLES_TRANCHE_AGE } from '../../../../shared/libelles';
import { PastilleStatut } from '../../../../shared/pastille-statut/pastille-statut';

/** Refus de suppression d'un animal qui a des réservations (contrats-api.md §5). */
const CODE_A_DES_RESERVATIONS = 'animal-a-des-reservations';

const MESSAGE_CHARGEMENT =
  "La liste des animaux n'a pas pu être chargée. Vérifiez votre connexion et rechargez la page.";
const MESSAGE_SUPPRESSION = "La suppression n'a pas abouti. Vérifiez votre connexion et réessayez.";

/** Animal dont la suppression attend confirmation, et le bouton « Supprimer » qui a ouvert le dialogue. */
interface Suppression {
  readonly animal: AnimalCarteDto;
  readonly origine: HTMLElement | null;
}

/**
 * Admin · Animaux (`/admin/animaux`, CAP-7) : tous les animaux, tous statuts, dans l'ordre de l'API ; chaque ligne
 * ouvre le formulaire `/admin/animaux/:id` et propose « Supprimer » après confirmation. 409
 * `animal-a-des-reservations` : alerte avec le message de l'API, ligne conservée ; 404 : liste rechargée ; autre
 * échec : alerte générique, erreur journalisée.
 */
@Component({
  selector: 'app-admin-animaux-liste',
  imports: [RouterLink, PastilleStatut, DialogueConfirmation],
  templateUrl: './liste.html',
  styleUrl: './liste.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAnimauxListe {
  private readonly service = inject(AdminAnimauxService);

  /** `null` jusqu'à la première réponse de l'API (et après un échec du premier chargement). */
  protected readonly animaux = signal<readonly AnimalCarteDto[] | null>(null);
  /** Message de l'alerte d'erreur au-dessus de la liste. */
  protected readonly alerte = signal<string | null>(null);
  /** Suppression en attente de confirmation : dialogue ouvert tant qu'elle n'est pas `null`. */
  protected readonly suppression = signal<Suppression | null>(null);
  protected readonly suppressionEnCours = signal(false);

  constructor() {
    this.charger();
  }

  protected meta(animal: AnimalCarteDto): string {
    return `${LIBELLES_ESPECE[animal.espece]} · ${LIBELLES_TRANCHE_AGE[animal.age]}`;
  }

  protected phrase(animal: AnimalCarteDto): string {
    return `Supprimer ${animal.nom} ? Sa fiche et ses photos seront effacées.`;
  }

  protected demanderSuppression(animal: AnimalCarteDto, evenement: Event): void {
    const origine = evenement.currentTarget instanceof HTMLElement ? evenement.currentTarget : null;
    this.suppression.set({ animal, origine });
  }

  protected annulerSuppression(): void {
    this.suppression.set(null);
  }

  protected async confirmerSuppression(): Promise<void> {
    const suppression = this.suppression();
    if (suppression === null || this.suppressionEnCours()) {
      return;
    }
    const { animal } = suppression;
    this.suppressionEnCours.set(true);
    this.alerte.set(null);
    try {
      await this.service.supprimer(animal.id);
      this.animaux.update((animaux) => animaux?.filter((autre) => autre.id !== animal.id) ?? null);
    } catch (erreur) {
      this.traiterEchecSuppression(erreur);
    } finally {
      this.suppressionEnCours.set(false);
      this.suppression.set(null);
    }
  }

  private traiterEchecSuppression(erreur: unknown): void {
    const statut = erreur instanceof HttpErrorResponse ? erreur.status : null;
    if (statut === 404) {
      // Déjà supprimé ailleurs : la liste du serveur fait foi.
      this.charger();
      return;
    }
    const corps = lireErreurApi(erreur);
    if (statut === 409 && corps?.code === CODE_A_DES_RESERVATIONS) {
      this.alerte.set(corps.error);
      return;
    }
    console.error('Impossible de supprimer l’animal (DELETE /api/admin/animaux/:id).', erreur);
    this.alerte.set(MESSAGE_SUPPRESSION);
  }

  private charger(): void {
    this.service.lister().then(
      (reponse) => {
        this.animaux.set(reponse.items);
        // Seule l'alerte d'un chargement précédent en échec est périmée ; un refus 409 reste affiché.
        this.alerte.update((message) => (message === MESSAGE_CHARGEMENT ? null : message));
      },
      (erreur: unknown) => {
        console.error('Impossible de charger les animaux (/api/admin/animaux).', erreur);
        // Pas de liste périmée (un animal peut-être supprimé) sous l'alerte.
        this.animaux.set(null);
        this.alerte.set(MESSAGE_CHARGEMENT);
      },
    );
  }
}
