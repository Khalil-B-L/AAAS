import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DialogueConfirmation } from './dialogue-confirmation';

/** Hôte de test : un bouton d'origine, un bouton hors dialogue, le dialogue sous `@if`. */
@Component({
  imports: [DialogueConfirmation],
  template: `
    <button type="button" class="origine" (click)="ouvert.set(true)">Supprimer</button>
    <button type="button" class="dehors">Ailleurs</button>
    @if (ouvert()) {
      <app-dialogue-confirmation
        phrase="Supprimer Rex ? Sa fiche et ses photos seront effacées."
        action="Supprimer"
        [occupe]="occupe()"
        (confirme)="confirmations = confirmations + 1"
        (retour)="retours = retours + 1; ouvert.set(false)"
      />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
class Hote {
  readonly ouvert = signal(false);
  readonly occupe = signal(false);
  confirmations = 0;
  retours = 0;
}

describe('DialogueConfirmation', () => {
  let fixture: ComponentFixture<Hote>;
  let racine: HTMLElement;

  async function ouvrir(): Promise<void> {
    TestBed.configureTestingModule({ imports: [Hote] });
    fixture = TestBed.createComponent(Hote);
    racine = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    const origine = bouton('.origine');
    origine.focus();
    origine.click();
    await fixture.whenStable();
  }

  const bouton = (selecteur: string) => racine.querySelector<HTMLButtonElement>(selecteur)!;
  const dialogue = () => racine.querySelector<HTMLElement>('[role="dialog"]');
  const boutonsDialogue = () => Array.from(dialogue()!.querySelectorAll('button'));
  const boutonAction = () => boutonsDialogue()[0];
  const boutonRetour = () => boutonsDialogue()[1];

  function touche(cible: HTMLElement, key: string, shiftKey = false): KeyboardEvent {
    const evenement = new KeyboardEvent('keydown', {
      key,
      shiftKey,
      bubbles: true,
      cancelable: true,
    });
    cible.dispatchEvent(evenement);
    return evenement;
  }

  afterEach(() => fixture.destroy());

  it('modal nommé par « Êtes-vous sûr ? », phrase et action de l’appelant, puis « Retour »', async () => {
    await ouvrir();

    const boite = dialogue()!;
    expect(boite.getAttribute('aria-modal')).toBe('true');
    const titre = document.getElementById(boite.getAttribute('aria-labelledby')!);
    expect(titre?.textContent?.trim()).toBe('Êtes-vous sûr ?');
    expect(
      document.getElementById(boite.getAttribute('aria-describedby')!)?.textContent?.trim(),
    ).toBe('Supprimer Rex ? Sa fiche et ses photos seront effacées.');
    expect(boutonsDialogue().map((b) => b.textContent?.trim())).toEqual(['Supprimer', 'Retour']);
    expect(boutonAction().classList).toContain('bouton-danger');
    expect(boutonRetour().classList).toContain('bouton-contour');
  });

  it('focus sur « Retour » à l’ouverture', async () => {
    await ouvrir();

    expect(document.activeElement).toBe(boutonRetour());
  });

  it('« Retour » : retour émis, aucune confirmation, focus rendu au bouton d’origine', async () => {
    await ouvrir();

    boutonRetour().click();
    await fixture.whenStable();

    expect(fixture.componentInstance.retours).toBe(1);
    expect(fixture.componentInstance.confirmations).toBe(0);
    expect(dialogue()).toBeNull();
    expect(document.activeElement).toBe(bouton('.origine'));
  });

  it('Échap : retour émis, aucune confirmation, focus rendu au bouton d’origine', async () => {
    await ouvrir();

    const evenement = touche(boutonRetour(), 'Escape');
    await fixture.whenStable();

    expect(evenement.defaultPrevented).toBe(true);
    expect(fixture.componentInstance.retours).toBe(1);
    expect(fixture.componentInstance.confirmations).toBe(0);
    expect(dialogue()).toBeNull();
    expect(document.activeElement).toBe(bouton('.origine'));
  });

  it('Tab depuis le dernier bouton revient au premier, Maj+Tab depuis le premier va au dernier', async () => {
    await ouvrir();

    expect(touche(boutonRetour(), 'Tab').defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(boutonAction());

    expect(touche(boutonAction(), 'Tab', true).defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(boutonRetour());
  });

  it('Tab entre les deux boutons suit l’ordre naturel', async () => {
    await ouvrir();

    expect(touche(boutonRetour(), 'Tab', true).defaultPrevented).toBe(false);
    boutonAction().focus();
    expect(touche(boutonAction(), 'Tab').defaultPrevented).toBe(false);
  });

  it('focus parti hors du dialogue : ramené sur « Retour »', async () => {
    await ouvrir();

    bouton('.dehors').focus();

    expect(document.activeElement).toBe(boutonRetour());
  });

  it('Échap depuis <body> (focus perdu sans focusin) : retour émis, dialogue fermé', async () => {
    await ouvrir();
    boutonRetour().blur();
    expect(document.activeElement).toBe(document.body);

    const evenement = touche(document.body, 'Escape');
    await fixture.whenStable();

    expect(evenement.defaultPrevented).toBe(true);
    expect(fixture.componentInstance.retours).toBe(1);
    expect(dialogue()).toBeNull();
  });

  it('Tab / Maj+Tab depuis <body> : le focus entre par un bout du dialogue', async () => {
    await ouvrir();

    boutonRetour().blur();
    expect(touche(document.body, 'Tab').defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(boutonAction());

    boutonAction().blur();
    expect(touche(document.body, 'Tab', true).defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(boutonRetour());
  });

  it('clic sur le voile : focus gardé sur « Retour », dialogue ouvert', async () => {
    await ouvrir();

    const voile = racine.querySelector<HTMLElement>('.voile')!;
    const evenement = new MouseEvent('mousedown', { bubbles: true, cancelable: true });
    voile.dispatchEvent(evenement);

    expect(evenement.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(boutonRetour());
    expect(dialogue()).not.toBeNull();
  });

  it('boîte du dialogue focalisable par clic (tabindex="-1"), hors de l’ordre de tabulation', async () => {
    await ouvrir();

    expect(dialogue()!.getAttribute('tabindex')).toBe('-1');
  });

  it('action : confirmation émise, le dialogue reste à la main de l’appelant', async () => {
    await ouvrir();

    boutonAction().click();
    await fixture.whenStable();

    expect(fixture.componentInstance.confirmations).toBe(1);
    expect(fixture.componentInstance.retours).toBe(0);
    expect(dialogue()).not.toBeNull();
  });

  it('occupé : boutons inactifs, clics et Échap ignorés', async () => {
    await ouvrir();
    fixture.componentInstance.occupe.set(true);
    await fixture.whenStable();

    expect(boutonAction().getAttribute('aria-disabled')).toBe('true');
    expect(boutonRetour().getAttribute('aria-disabled')).toBe('true');
    boutonAction().click();
    boutonRetour().click();
    touche(boutonRetour(), 'Escape');
    await fixture.whenStable();

    expect(fixture.componentInstance.confirmations).toBe(0);
    expect(fixture.componentInstance.retours).toBe(0);
    expect(dialogue()).not.toBeNull();
  });
});
