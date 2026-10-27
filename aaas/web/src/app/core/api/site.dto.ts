/** Réponse de `GET /api/site` (contrats-api.md §6). */
export interface SiteDto {
  /** Numéro de l'association au format E.164. */
  telephone: string;
  facebook: string;
  instagram: string;
}
