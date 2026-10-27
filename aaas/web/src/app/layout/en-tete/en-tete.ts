import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';

interface LienNavigation {
  readonly libelle: string;
  readonly lien: string;
  /** Préfixe d'URL qui rend le lien actif (le lien lui-même par défaut). */
  readonly prefixe?: string;
}

const LIENS: readonly LienNavigation[] = [
  { libelle: 'Accueil', lien: '/' },
  { libelle: 'Adopter', lien: '/adopter' },
  { libelle: 'Actualités', lien: '/actualites' },
  { libelle: 'Nous aider', lien: '/nous-aider/dons', prefixe: '/nous-aider' },
  { libelle: 'Contact', lien: '/contact' },
];

const LIENS_MEMBRE: readonly LienNavigation[] = [
  { libelle: 'Mes réservations', lien: '/mes-reservations' },
  { libelle: 'Mes favoris', lien: '/mes-favoris' },
];

const LIENS_ADMIN: readonly LienNavigation[] = [
  { libelle: 'Réservations', lien: '/admin/reservations' },
  { libelle: 'Animaux', lien: '/admin/animaux' },
  { libelle: 'Actualités', lien: '/admin/actualites' },
];

/**
 * En-tête : logo et sigle (+ pastille « Admin » dans l'espace admin), liens (ordinateur)
 * ou menu burger (téléphone, tablette), puis Connexion ou le menu membre.
 */
@Component({
  selector: 'app-en-tete',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './en-tete.html',
  styleUrl: './en-tete.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(keydown.escape)': 'fermerAuClavier()',
    '(document:click)': 'fermerCompteAilleurs($event)',
  },
})
export class EnTete {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly burger = viewChild.required<ElementRef<HTMLButtonElement>>('boutonMenu');
  private readonly boutonCompte = viewChild<ElementRef<HTMLButtonElement>>('boutonCompte');
  private readonly zoneCompte = viewChild<ElementRef<HTMLElement>>('zoneCompte');

  protected readonly liens = LIENS;
  protected readonly liensMembre = LIENS_MEMBRE;
  protected readonly liensAdmin = LIENS_ADMIN;
  protected readonly ouvert = signal(false);
  protected readonly compteOuvert = signal(false);
  protected readonly utilisateur = this.auth.utilisateur;
  protected readonly estAdmin = this.auth.estAdmin;

  private readonly navigations = this.router.events.pipe(
    filter((evenement) => evenement instanceof NavigationEnd),
  );

  private readonly chemin = toSignal(
    this.navigations.pipe(map((fin) => cheminSeul(fin.urlAfterRedirects))),
    { initialValue: cheminSeul(this.router.url) },
  );

  /** Lien de la page courante, marqué `aria-current="page"` et souligné. */
  protected readonly lienActif = computed(() => {
    const chemin = this.chemin();
    return LIENS.find(({ lien, prefixe }) =>
      prefixe ? chemin === prefixe || chemin.startsWith(`${prefixe}/`) : chemin === lien,
    );
  });

  /** Routes `/admin/**` : pastille « Admin » après le sigle. */
  protected readonly espaceAdmin = computed(() => {
    const chemin = this.chemin();
    return chemin === '/admin' || chemin.startsWith('/admin/');
  });

  constructor() {
    this.navigations.pipe(takeUntilDestroyed()).subscribe(() => this.fermerTout());
  }

  protected basculer(): void {
    this.ouvert.update((ouvert) => !ouvert);
  }

  protected basculerCompte(): void {
    this.compteOuvert.update((ouvert) => !ouvert);
  }

  /** Toucher un lien ferme les menus, même vers la page courante (aucun NavigationEnd). */
  protected fermerSurLien(evenement: Event): void {
    if ((evenement.target as Element | null)?.closest('a')) {
      this.fermerTout();
    }
  }

  protected fermerAuClavier(): void {
    if (this.compteOuvert()) {
      this.compteOuvert.set(false);
      this.boutonCompte()?.nativeElement.focus();
    } else if (this.ouvert()) {
      this.ouvert.set(false);
      this.burger().nativeElement.focus();
    }
  }

  /** Un toucher hors du menu membre le referme. */
  protected fermerCompteAilleurs(evenement: Event): void {
    const cible = evenement.target as Node | null;
    if (this.compteOuvert() && !this.zoneCompte()?.nativeElement.contains(cible)) {
      this.compteOuvert.set(false);
    }
  }

  protected deconnecter(): void {
    this.auth.deconnecter();
    this.fermerTout();
    void this.router.navigateByUrl('/');
  }

  private fermerTout(): void {
    this.ouvert.set(false);
    this.compteOuvert.set(false);
  }
}

function cheminSeul(url: string): string {
  return url.split(/[?#]/)[0] || '/';
}
