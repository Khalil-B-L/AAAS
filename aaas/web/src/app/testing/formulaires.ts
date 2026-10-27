/** Helpers communs aux specs de formulaire (connexion, inscription, …). */
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, Type } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

// Sélecteur explicite : un autre composant de page vide (template '') ailleurs dans les specs aurait le même ID (NG0912).
@Component({ selector: 'app-page-vide', template: '' })
class PageVide {}

export interface FormulaireRendu<T> {
  fixture: ComponentFixture<T>;
  racine: HTMLElement;
  http: HttpTestingController;
  router: Router;
}

/** Rend le composant de formulaire seul (toute route → page vide), avec `returnUrl` en entrée si fourni. */
export async function rendreFormulaire<T>(
  composant: Type<T>,
  returnUrl?: string,
): Promise<FormulaireRendu<T>> {
  TestBed.configureTestingModule({
    imports: [composant],
    providers: [
      provideRouter([{ path: '**', component: PageVide }]),
      provideHttpClient(),
      provideHttpClientTesting(),
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  const router = TestBed.inject(Router);
  const fixture = TestBed.createComponent(composant);
  if (returnUrl !== undefined) {
    fixture.componentRef.setInput('returnUrl', returnUrl);
  }
  const racine = fixture.nativeElement as HTMLElement;
  await stable(fixture);
  return { fixture, racine, http, router };
}

/** Laisse passer les tâches en attente (navigation, promesses) puis attend la stabilité du composant. */
export async function stable(fixture: ComponentFixture<unknown>): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  await fixture.whenStable();
}

export function champ(racine: HTMLElement, id: string): HTMLInputElement {
  return racine.querySelector<HTMLInputElement>(`#${id}`)!;
}

/** Saisit chaque valeur dans le champ d'id donné (dans l'ordre), soumet le formulaire puis attend la stabilité. */
export async function saisirEtEnvoyer(
  fixture: ComponentFixture<unknown>,
  valeursParId: Readonly<Record<string, string>>,
): Promise<void> {
  const racine = fixture.nativeElement as HTMLElement;
  for (const [id, valeur] of Object.entries(valeursParId)) {
    const element = champ(racine, id);
    element.value = valeur;
    element.dispatchEvent(new Event('input'));
  }
  racine.querySelector('form')!.dispatchEvent(new Event('submit'));
  await stable(fixture);
}
