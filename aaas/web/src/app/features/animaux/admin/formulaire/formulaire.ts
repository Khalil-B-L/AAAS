import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { distinctUntilChanged, map } from 'rxjs';
import { AdminAnimauxService } from '../../../../core/admin-animaux.service';
import {
  AnimalDetailDto,
  AnimalEcritureDto,
  ESPECES,
  Entente,
  Espece,
  PhotoDto,
  SEXES,
  Sexe,
  StatutAnimal,
  TAILLES,
  TRANCHES_AGE,
  Taille,
  TrancheAge,
} from '../../../../core/api/animal.dto';
import { lireErreurApi } from '../../../../core/api/erreur-api';
import { DialogueConfirmation } from '../../../../shared/dialogue-confirmation/dialogue-confirmation';
import {
  CRITERES_SANTE,
  CritereSante,
  LIBELLES_ENTENTE,
  LIBELLES_ESPECE,
  LIBELLES_SANTE,
  LIBELLES_SEXE,
  LIBELLES_STATUT_ANIMAL,
  LIBELLES_TAILLE,
  LIBELLES_TRANCHE_AGE,
} from '../../../../shared/libelles';
import { PastilleStatut } from '../../../../shared/pastille-statut/pastille-statut';
import { interpreterErreur } from '../../../auth/erreurs-formulaire';

/** Les 12 champs d'`AnimalEcritureDto`, clés des erreurs `errors.<champ>` de l'API. */
const CHAMPS = [
  'nom',
  'espece',
  'sexe',
  'age',
  'taille',
  'description',
  'vaccine',
  'sterilise',
  'puce',
  'ententeEnfants',
  'ententeChiens',
  'ententeChats',
] as const;
type Champ = (typeof CHAMPS)[number];

/** Choix obligatoires (aucune valeur par défaut). */
const CHOIX_REQUIS = [
  'espece',
  'sexe',
  'age',
  'taille',
  'ententeEnfants',
  'ententeChiens',
  'ententeChats',
] as const satisfies readonly Champ[];

/** Longueurs maximales, alignées sur l'API (`Animal.NomMax`, `Animal.DescriptionMax`). */
export const NOM_MAX = 50;
export const DESCRIPTION_MAX = 2000;

/** Messages de vérification, repris de l'API (`AnimalEcritureDto.Valider`). */
const MESSAGES = {
  nomVide: "Indiquez le nom de l'animal.",
  nomLong: `Nom trop long (${NOM_MAX} caractères maximum).`,
  descriptionVide: "Indiquez la description de l'animal.",
  descriptionLongue: `Description trop longue (${DESCRIPTION_MAX} caractères maximum).`,
  choix: 'Choisissez une valeur.',
} as const;

const CODE_MODIFIE_AILLEURS = 'modifie-ailleurs';
const CODE_A_DES_RESERVATIONS = 'animal-a-des-reservations';
const CODE_RESERVATION_ACTIVE = 'reservation-active';

const MESSAGE_CREE = 'Animal enregistré.';
const MESSAGE_MODIFIE = 'Modifications enregistrées.';
const MESSAGE_CONFLIT =
  'Cet animal a été modifié entre-temps : le formulaire affiche maintenant la version enregistrée. Refaites vos modifications puis enregistrez.';
const MESSAGE_CHARGEMENT =
  "La fiche de l'animal n'a pas pu être chargée. Vérifiez votre connexion et rechargez la page.";
const MESSAGE_ENVOI_PHOTO =
  "L'envoi de la photo n'a pas abouti. Vérifiez votre connexion et réessayez.";
const MESSAGE_RETRAIT_PHOTO =
  "Le retrait de la photo n'a pas abouti. Vérifiez votre connexion et réessayez.";
const MESSAGE_SUPPRESSION = "La suppression n'a pas abouti. Vérifiez votre connexion et réessayez.";
const MESSAGE_STATUT =
  "Le changement de statut n'a pas abouti. Vérifiez votre connexion et réessayez.";

const TITRE_AJOUT = 'Admin · Ajouter un animal · AAAS';
const TITRE_MODIFICATION = 'Admin · Modifier un animal · AAAS';
const TITRE_INTROUVABLE = 'Admin · Animal introuvable · AAAS';

/** Plus grand identifiant transmis à l'API (`int` côté serveur). */
const ID_MAX = 2_147_483_647;

/** Identifiant de route valide (entier positif représentable côté API), sinon `null`. */
function lireId(texte: string): number | null {
  if (!/^\d{1,10}$/.test(texte)) {
    return null;
  }
  const id = Number(texte);
  return id >= 1 && id <= ID_MAX ? id : null;
}

type Etat = 'chargement' | 'formulaire' | 'introuvable' | 'erreur';

/** Photo choisie sur l'appareil, pas encore enregistrée : en attente, en cours d'envoi ou en échec. */
interface PhotoLocale {
  readonly cle: string;
  readonly fichier: File;
  /** URL `blob:` de l'aperçu, libérée au retrait, à l'enregistrement et à la destruction. */
  readonly apercu: string;
  readonly etat: 'attente' | 'envoi' | 'echec';
  readonly erreur: string | null;
}

/** Vignette affichée : photo enregistrée (par `ordre`) puis photos locales, dans l'ordre du choix. */
interface Vignette {
  readonly cle: string;
  readonly url: string;
  readonly etat: 'enregistree' | PhotoLocale['etat'];
  readonly erreur: string | null;
  readonly photo: PhotoDto | null;
}

/** Retrait d'une photo enregistrée en attente de confirmation. */
interface Retrait {
  readonly photo: PhotoDto;
  readonly numero: number;
  readonly origine: HTMLElement | null;
}

let prochaineCle = 0;

/**
 * Admin · Ajouter / modifier un animal (`/admin/animaux/nouveau`, `/admin/animaux/:id`, CAP-7). Un seul formulaire
 * pour les deux : 12 champs vérifiés avant envoi, erreurs de l'API sous les champs, photos envoyées une par une dans
 * l'ordre (`POST …/photos`), chaque échec renvoyable seul. Création : `POST` puis envoi des photos en attente,
 * l'adresse devient `/admin/animaux/:id` sans quitter la page. Modification : `GET`, photos envoyées dès leur choix,
 * `PUT` ; 409 `modifie-ailleurs` → formulaire rechargé avec une alerte. Retrait d'une photo enregistrée et
 * suppression de l'animal après le dialogue de confirmation. Le statut ne passe jamais par le `PUT` : en modification,
 * section dédiée (`POST …/statut`, voir « Statut forcé »).
 */
@Component({
  selector: 'app-admin-animal-formulaire',
  imports: [ReactiveFormsModule, RouterLink, DialogueConfirmation, PastilleStatut],
  templateUrl: './formulaire.html',
  styleUrl: './formulaire.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAnimalFormulaire {
  private readonly service = inject(AdminAnimauxService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly titre = inject(Title);
  private readonly hote = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly especes = ESPECES;
  protected readonly sexes = SEXES;
  protected readonly tranchesAge = TRANCHES_AGE;
  protected readonly tailles = TAILLES;
  protected readonly ententesPossibles: readonly Entente[] = ['Oui', 'AVoir', 'Non'];
  protected readonly criteresSante = CRITERES_SANTE;
  protected readonly libelles = {
    espece: LIBELLES_ESPECE,
    sexe: LIBELLES_SEXE,
    age: LIBELLES_TRANCHE_AGE,
    taille: LIBELLES_TAILLE,
    entente: LIBELLES_ENTENTE,
    statut: LIBELLES_STATUT_ANIMAL,
  };
  protected readonly lignesEntente = [
    { champ: 'ententeEnfants', icone: '👧', libelle: 'Enfants' },
    { champ: 'ententeChiens', icone: '🐶', libelle: 'Chiens' },
    { champ: 'ententeChats', icone: '🐱', libelle: 'Chats' },
  ] as const;

  protected readonly formulaire = this.fb.group({
    nom: '',
    espece: this.fb.control<Espece | null>(null),
    sexe: this.fb.control<Sexe | null>(null),
    age: this.fb.control<TrancheAge | null>(null),
    taille: this.fb.control<Taille | null>(null),
    description: '',
    vaccine: false,
    sterilise: false,
    puce: false,
    ententeEnfants: this.fb.control<Entente | null>(null),
    ententeChiens: this.fb.control<Entente | null>(null),
    ententeChats: this.fb.control<Entente | null>(null),
  });

  protected readonly etat = signal<Etat>('chargement');
  /** Animal enregistré (modification, ou création réussie) ; `null` tant qu'il n'existe pas côté API. */
  protected readonly animal = signal<AnimalDetailDto | null>(null);
  private readonly enregistrees = signal<readonly PhotoDto[]>([]);
  private readonly locales = signal<readonly PhotoLocale[]>([]);

  protected readonly erreurs = signal<Partial<Record<Champ, string>>>({});
  /** Alerte d'erreur au-dessus du bouton d'enregistrement. */
  protected readonly alerte = signal<string | null>(null);
  /** Confirmation d'enregistrement (« Modifications enregistrées. »). */
  protected readonly message = signal<string | null>(null);
  /** Alerte de la zone photo (retrait en échec). */
  protected readonly alertePhotos = signal<string | null>(null);
  protected readonly envoi = signal(false);

  protected readonly retrait = signal<Retrait | null>(null);
  protected readonly retraitEnCours = signal(false);
  /** Suppression de l'animal en attente de confirmation : bouton d'origine du dialogue. */
  protected readonly suppression = signal<{ readonly origine: HTMLElement | null } | null>(null);
  protected readonly suppressionEnCours = signal(false);

  // --- Statut forcé (modification seulement, `POST …/statut`) ---
  protected readonly statutsAnimal: readonly StatutAnimal[] = ['Disponible', 'Reserve', 'Adopte'];
  /** Option choisie ; revient au statut effectif chargé à chaque animal affiché (chargement, PUT, statut). */
  protected readonly statutChoisi = linkedSignal<StatutAnimal>(
    () => this.animal()?.statut ?? 'Disponible',
  );
  protected readonly statutEnCours = signal(false);
  protected readonly alerteStatut = signal<string | null>(null);
  protected readonly messageStatut = signal<string | null>(null);
  /** « Changer le statut » actif : choix forçable, différent du statut chargé, animal non Réservé. */
  protected readonly statutChangeable = computed(() => {
    const statut = this.animal()?.statut;
    const choisi = this.statutChoisi();
    return (
      statut !== undefined && statut !== 'Reserve' && choisi !== 'Reserve' && choisi !== statut
    );
  });

  /** Enregistrées par `ordre` puis locales : la première est la photo principale. */
  protected readonly vignettes = computed((): readonly Vignette[] => [
    ...[...this.enregistrees()]
      .sort((a, b) => a.ordre - b.ordre)
      .map((photo): Vignette => ({
        cle: `photo-${photo.id}`,
        url: photo.url,
        etat: 'enregistree',
        erreur: null,
        photo,
      })),
    ...this.locales().map((locale): Vignette => ({
      cle: locale.cle,
      url: locale.apercu,
      etat: locale.etat,
      erreur: locale.erreur,
      photo: null,
    })),
  ]);

  /** Photos en échec, avec leur numéro d'affichage. */
  protected readonly echecs = computed(() =>
    this.vignettes()
      .map((vignette, index) => ({ vignette, numero: index + 1 }))
      .filter(({ vignette }) => vignette.etat === 'echec'),
  );

  protected readonly photosEnAttente = computed(() =>
    this.locales().some((locale) => locale.etat === 'attente'),
  );

  private readonly envoisEnCours = signal(false);
  /** Photos traitées (envoyées ou en échec) depuis le début de la file d'envoi en cours. */
  private readonly traiteesDansLot = signal(0);

  /** « Envoi des photos : n sur total… » tant que la file d'envoi tourne ; `null` sinon. */
  protected readonly progression = computed(() => {
    if (!this.envoisEnCours()) {
      return null;
    }
    const restantes = this.locales().filter(
      (locale) => locale.etat === 'attente' || locale.etat === 'envoi',
    ).length;
    const traitees = this.traiteesDansLot();
    return restantes === 0 ? null : { numero: traitees + 1, total: traitees + restantes };
  });

  private chargementCourant = 0;

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => params.get('id')),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((texte) => this.ouvrir(texte));
    inject(DestroyRef).onDestroy(() => this.viderLocales());
  }

  /** Prénom saisi ou enregistré, pour les textes alternatifs et les phrases de confirmation. */
  protected prenom(): string {
    return this.animal()?.nom ?? (this.formulaire.controls.nom.value.trim() || "l'animal");
  }

  protected numeroPhoto(numero: number): string {
    return `photo ${numero} de ${this.prenom()}`;
  }

  /** Libellé de la case santé, accordé au sexe choisi (« Vaccinée »), « Vacciné(e) » sans sexe. */
  protected libelleSante(critere: CritereSante): string {
    const sexe = this.formulaire.controls.sexe.value;
    return sexe ? LIBELLES_SANTE[critere][sexe] : `${LIBELLES_SANTE[critere].Male}(e)`;
  }

  /** `aria-describedby` d'un champ : son message d'erreur s'il en a un. */
  protected decritPar(champ: Champ): string | null {
    return this.erreurs()[champ] ? `formulaire-animal-${champ}-erreur` : null;
  }

  protected async envoyer(): Promise<void> {
    if (this.envoi() || this.statutEnCours()) {
      return;
    }
    this.message.set(null);
    this.alerte.set(null);
    const erreurs = this.verifier();
    this.erreurs.set(erreurs);
    if (Object.keys(erreurs).length > 0) {
      this.focaliserPremiereErreur();
      return;
    }
    const corps = this.lireCorps();
    const animal = this.animal();
    this.envoi.set(true);
    try {
      if (animal === null) {
        const cree = await this.service.creer(corps);
        this.afficher(cree, true);
        this.message.set(MESSAGE_CREE);
        // Même route (matcher `nouveauOuId`) : le composant est réutilisé, les envois de photos continuent.
        void this.router.navigate(['/admin/animaux', cree.id], { replaceUrl: true });
        void this.lancerEnvois();
      } else {
        // Les photos ne passent pas par le PUT : la liste locale fait foi (envois en cours compris).
        this.afficher(await this.service.modifier(animal.id, corps), false);
        this.message.set(MESSAGE_MODIFIE);
      }
    } catch (erreur) {
      await this.traiterEchecEnvoi(erreur, animal);
    } finally {
      this.envoi.set(false);
    }
  }

  protected choisirPhotos(evenement: Event): void {
    const champ = evenement.target as HTMLInputElement;
    const fichiers = Array.from(champ.files ?? []);
    // Vidé pour pouvoir choisir à nouveau le même fichier.
    champ.value = '';
    if (fichiers.length === 0) {
      return;
    }
    this.alertePhotos.set(null);
    this.locales.update((locales) => [
      ...locales,
      ...fichiers.map((fichier): PhotoLocale => ({
        cle: `locale-${prochaineCle++}`,
        fichier,
        apercu: URL.createObjectURL(fichier),
        etat: 'attente',
        erreur: null,
      })),
    ]);
    if (this.animal() !== null) {
      void this.lancerEnvois();
    }
  }

  protected renvoyer(cle: string): void {
    this.majLocale(cle, { etat: 'attente', erreur: null });
    void this.lancerEnvois();
  }

  /** × : photo locale retirée tout de suite ; photo enregistrée retirée après confirmation. */
  protected retirer(vignette: Vignette, numero: number, evenement: Event): void {
    this.alertePhotos.set(null);
    if (vignette.photo === null) {
      if (vignette.etat !== 'envoi') {
        this.retirerLocale(vignette.cle);
        this.focaliserAjoutPhotos();
      }
      return;
    }
    const origine = evenement.currentTarget instanceof HTMLElement ? evenement.currentTarget : null;
    this.retrait.set({ photo: vignette.photo, numero, origine });
  }

  protected phraseRetrait(retrait: Retrait): string {
    return `Retirer la ${this.numeroPhoto(retrait.numero)} ?`;
  }

  protected annulerRetrait(): void {
    this.retrait.set(null);
  }

  protected async confirmerRetrait(): Promise<void> {
    const retrait = this.retrait();
    const animal = this.animal();
    if (retrait === null || animal === null || this.retraitEnCours()) {
      return;
    }
    this.retraitEnCours.set(true);
    try {
      await this.service.supprimerPhoto(animal.id, retrait.photo.id);
      this.oublierPhoto(retrait.photo.id);
      this.focaliserAjoutPhotos();
    } catch (erreur) {
      if (erreur instanceof HttpErrorResponse && erreur.status === 404) {
        // Déjà retirée ailleurs.
        this.oublierPhoto(retrait.photo.id);
        this.focaliserAjoutPhotos();
      } else {
        console.error('Impossible de retirer la photo (DELETE …/photos/:photoId).', erreur);
        this.alertePhotos.set(MESSAGE_RETRAIT_PHOTO);
      }
    } finally {
      this.retraitEnCours.set(false);
      this.retrait.set(null);
    }
  }

  protected phraseSuppression(): string {
    return `Supprimer ${this.prenom()} ? Sa fiche et ses photos seront effacées.`;
  }

  protected demanderSuppression(evenement: Event): void {
    const origine = evenement.currentTarget instanceof HTMLElement ? evenement.currentTarget : null;
    this.suppression.set({ origine });
  }

  protected annulerSuppression(): void {
    this.suppression.set(null);
  }

  protected async confirmerSuppression(): Promise<void> {
    const animal = this.animal();
    if (this.suppression() === null || animal === null || this.suppressionEnCours()) {
      return;
    }
    this.suppressionEnCours.set(true);
    this.alerte.set(null);
    this.message.set(null);
    try {
      await this.service.supprimer(animal.id);
    } catch (erreur) {
      const statut = erreur instanceof HttpErrorResponse ? erreur.status : null;
      const corps = lireErreurApi(erreur);
      if (statut !== 404) {
        if (statut === 409 && corps?.code === CODE_A_DES_RESERVATIONS) {
          this.alerte.set(corps.error);
        } else {
          console.error(
            'Impossible de supprimer l’animal (DELETE /api/admin/animaux/:id).',
            erreur,
          );
          this.alerte.set(MESSAGE_SUPPRESSION);
        }
        this.suppressionEnCours.set(false);
        this.suppression.set(null);
        return;
      }
      // 404 : déjà supprimé ailleurs, la liste fait foi.
    }
    this.suppressionEnCours.set(false);
    this.suppression.set(null);
    await this.router.navigateByUrl('/admin/animaux');
  }

  /**
   * « Changer le statut » : `statut` = option choisie, `statutAttendu` = statut chargé. Succès → animal à jour (statut,
   * pastille, titre) sans toucher aux champs saisis ; 409 `reservation-active` / `modifie-ailleurs` → formulaire
   * rechargé puis message de l'API ; 404 → introuvable ; autre échec → choix remis sur le statut chargé.
   */
  protected async changerStatut(): Promise<void> {
    const animal = this.animal();
    const statut = this.statutChoisi();
    if (
      animal === null ||
      this.statutEnCours() ||
      this.envoi() ||
      !this.statutChangeable() ||
      statut === 'Reserve'
    ) {
      return;
    }
    this.statutEnCours.set(true);
    this.alerteStatut.set(null);
    this.messageStatut.set(null);
    try {
      const maj = await this.service.forcerStatut(animal.id, statut, animal.statut);
      if (this.animal()?.id === maj.id) {
        this.animal.set(maj);
        this.titre.setTitle(`Admin · Modifier ${maj.nom} · AAAS`);
        this.messageStatut.set(`Statut enregistré : ${LIBELLES_STATUT_ANIMAL[maj.statut]}.`);
      }
    } catch (erreur) {
      const code = erreur instanceof HttpErrorResponse ? erreur.status : null;
      const api = lireErreurApi(erreur);
      if (this.animal()?.id !== animal.id) {
        // Autre animal ouvert entre-temps : la réponse ne le concerne pas.
        console.error(
          'Changement de statut abandonné (POST /api/admin/animaux/:id/statut).',
          erreur,
        );
      } else if (
        code === 409 &&
        api !== null &&
        (api.code === CODE_RESERVATION_ACTIVE || api.code === CODE_MODIFIE_AILLEURS)
      ) {
        await this.charger(animal.id);
        this.alerteStatut.set(api.error);
      } else if (code === 404) {
        this.introuvable();
      } else {
        console.error(
          'Impossible de changer le statut (POST /api/admin/animaux/:id/statut).',
          erreur,
        );
        this.statutChoisi.set(this.animal()?.statut ?? animal.statut);
        this.alerteStatut.set(MESSAGE_STATUT);
      }
    } finally {
      this.statutEnCours.set(false);
    }
  }

  private ouvrir(texte: string | null): void {
    if (texte === null) {
      this.reinitialiser();
      this.titre.setTitle(TITRE_AJOUT);
      this.etat.set('formulaire');
      return;
    }
    const id = lireId(texte);
    if (id !== null && id === this.animal()?.id) {
      // Adresse mise à jour après la création : l'animal est déjà affiché.
      return;
    }
    this.reinitialiser();
    if (id === null) {
      this.introuvable();
      return;
    }
    this.etat.set('chargement');
    this.titre.setTitle(TITRE_MODIFICATION);
    void this.charger(id);
  }

  private async charger(id: number): Promise<void> {
    const jeton = ++this.chargementCourant;
    try {
      const animal = await this.service.obtenir(id);
      if (jeton === this.chargementCourant) {
        this.afficher(animal, true);
        this.etat.set('formulaire');
      }
    } catch (erreur) {
      if (jeton !== this.chargementCourant) {
        return;
      }
      if (erreur instanceof HttpErrorResponse && erreur.status === 404) {
        this.introuvable();
        return;
      }
      console.error(`Impossible de charger l’animal (/api/admin/animaux/${id}).`, erreur);
      if (this.etat() === 'chargement') {
        this.etat.set('erreur');
      } else {
        // Rechargement après un conflit : le formulaire reste affiché.
        this.alerte.set(MESSAGE_CHARGEMENT);
      }
    }
  }

  /** Affiche l'animal enregistré : champs, titre d'onglet et, si demandé, ses photos. */
  private afficher(animal: AnimalDetailDto, avecPhotos: boolean): void {
    this.alerteStatut.set(null);
    this.messageStatut.set(null);
    this.animal.set(animal);
    this.formulaire.setValue({
      nom: animal.nom,
      espece: animal.espece,
      sexe: animal.sexe,
      age: animal.age,
      taille: animal.taille,
      description: animal.description,
      vaccine: animal.vaccine,
      sterilise: animal.sterilise,
      puce: animal.puce,
      ententeEnfants: animal.ententeEnfants,
      ententeChiens: animal.ententeChiens,
      ententeChats: animal.ententeChats,
    });
    if (avecPhotos) {
      this.enregistrees.set(animal.photos);
    }
    this.titre.setTitle(`Admin · Modifier ${animal.nom} · AAAS`);
  }

  private async traiterEchecEnvoi(erreur: unknown, animal: AnimalDetailDto | null): Promise<void> {
    const statut = erreur instanceof HttpErrorResponse ? erreur.status : null;
    if (
      animal !== null &&
      statut === 409 &&
      lireErreurApi(erreur)?.code === CODE_MODIFIE_AILLEURS
    ) {
      this.alerte.set(MESSAGE_CONFLIT);
      await this.charger(animal.id);
      return;
    }
    if (animal !== null && statut === 404) {
      this.introuvable();
      return;
    }
    const { champs, alerte } = interpreterErreur(erreur, CHAMPS);
    this.erreurs.set(champs);
    this.alerte.set(alerte);
    this.focaliserPremiereErreur();
  }

  private verifier(): Partial<Record<Champ, string>> {
    const valeurs = this.formulaire.getRawValue();
    const erreurs: Partial<Record<Champ, string>> = {};
    const nom = valeurs.nom.trim();
    if (nom.length === 0) {
      erreurs.nom = MESSAGES.nomVide;
    } else if (nom.length > NOM_MAX) {
      erreurs.nom = MESSAGES.nomLong;
    }
    const description = valeurs.description.trim();
    if (description.length === 0) {
      erreurs.description = MESSAGES.descriptionVide;
    } else if (description.length > DESCRIPTION_MAX) {
      erreurs.description = MESSAGES.descriptionLongue;
    }
    for (const champ of CHOIX_REQUIS) {
      if (valeurs[champ] === null) {
        erreurs[champ] = MESSAGES.choix;
      }
    }
    return erreurs;
  }

  /** Corps de l'appel ; à n'appeler qu'après une vérification sans erreur (tous les choix faits). */
  private lireCorps(): AnimalEcritureDto {
    const valeurs = this.formulaire.getRawValue();
    return {
      nom: valeurs.nom,
      espece: valeurs.espece!,
      sexe: valeurs.sexe!,
      age: valeurs.age!,
      taille: valeurs.taille!,
      description: valeurs.description,
      vaccine: valeurs.vaccine,
      sterilise: valeurs.sterilise,
      puce: valeurs.puce,
      ententeEnfants: valeurs.ententeEnfants!,
      ententeChiens: valeurs.ententeChiens!,
      ententeChats: valeurs.ententeChats!,
    };
  }

  /**
   * File d'envoi : une photo à la fois, la première en attente dans l'ordre d'affichage, jusqu'à ce qu'il n'y en ait
   * plus. Un échec garde la photo (message de l'API, « Renvoyer ») et passe à la suivante.
   */
  private async lancerEnvois(): Promise<void> {
    if (this.envoisEnCours()) {
      return;
    }
    this.traiteesDansLot.set(0);
    this.envoisEnCours.set(true);
    try {
      for (;;) {
        const animal = this.animal();
        const suivante = this.locales().find((locale) => locale.etat === 'attente');
        if (animal === null || suivante === undefined) {
          return;
        }
        this.majLocale(suivante.cle, { etat: 'envoi', erreur: null });
        try {
          const photo = await this.service.ajouterPhoto(animal.id, suivante.fichier);
          const toujoursLa = this.locales().some((locale) => locale.cle === suivante.cle);
          this.retirerLocale(suivante.cle);
          if (toujoursLa && this.animal()?.id === animal.id) {
            this.enregistrees.update((photos) =>
              photos.some((autre) => autre.id === photo.id) ? photos : [...photos, photo],
            );
          }
        } catch (erreur) {
          const api = lireErreurApi(erreur);
          if (api === null) {
            console.error('Impossible d’envoyer la photo (POST …/photos).', erreur);
          }
          this.majLocale(suivante.cle, {
            etat: 'echec',
            erreur: api?.errors?.['fichier'] ?? api?.error ?? MESSAGE_ENVOI_PHOTO,
          });
        }
        this.traiteesDansLot.update((traitees) => traitees + 1);
      }
    } finally {
      this.envoisEnCours.set(false);
    }
  }

  private majLocale(cle: string, modifs: Pick<PhotoLocale, 'etat' | 'erreur'>): void {
    this.locales.update((locales) =>
      locales.map((locale) => (locale.cle === cle ? { ...locale, ...modifs } : locale)),
    );
  }

  private retirerLocale(cle: string): void {
    const locale = this.locales().find((autre) => autre.cle === cle);
    if (locale !== undefined) {
      URL.revokeObjectURL(locale.apercu);
      this.locales.update((locales) => locales.filter((autre) => autre.cle !== cle));
    }
  }

  private oublierPhoto(photoId: number): void {
    this.enregistrees.update((photos) => photos.filter((photo) => photo.id !== photoId));
  }

  private viderLocales(): void {
    for (const locale of this.locales()) {
      URL.revokeObjectURL(locale.apercu);
    }
    this.locales.set([]);
  }

  private reinitialiser(): void {
    this.chargementCourant++;
    this.animal.set(null);
    this.formulaire.reset();
    this.enregistrees.set([]);
    this.viderLocales();
    this.erreurs.set({});
    this.alerte.set(null);
    this.message.set(null);
    this.alertePhotos.set(null);
    this.alerteStatut.set(null);
    this.messageStatut.set(null);
    this.retrait.set(null);
    this.suppression.set(null);
  }

  private introuvable(): void {
    this.etat.set('introuvable');
    this.titre.setTitle(TITRE_INTROUVABLE);
  }

  /** Après un retrait, la vignette (et son ×) disparaît : focus sur l'ajout de photos, cible stable. */
  private focaliserAjoutPhotos(): void {
    afterNextRender(
      () => this.hote.nativeElement.querySelector<HTMLElement>('input[type="file"]')?.focus(),
      { injector: this.injector },
    );
  }

  private focaliserPremiereErreur(): void {
    afterNextRender(
      () => this.hote.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
