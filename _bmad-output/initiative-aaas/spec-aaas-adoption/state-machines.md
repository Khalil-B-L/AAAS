# Machines à états

## Statut d'un animal

```mermaid
stateDiagram-v2
    [*] --> Disponible : admin crée l'animal
    Disponible --> Réservé : membre réserve (CAP-4)
    Réservé --> Adopté : admin confirme (CAP-6)
    Réservé --> Disponible : admin annule (CAP-6)
    Réservé --> Disponible : 15 min sans confirmation (CAP-4)
```

- Une seule réservation active par animal : réserver un animal non Disponible est refusé.
- Au plus 2 réservations En attente par membre : une troisième est refusée.
- L'admin peut aussi forcer le statut depuis le CRUD animaux (CAP-7).
- Adopté : retiré du catalogue (CAP-2), affiché dans « Ils ont trouvé une famille » (CAP-11).

## Statut d'une réservation

```mermaid
stateDiagram-v2
    [*] --> EnAttente : membre réserve
    EnAttente --> Confirmée : admin confirme
    EnAttente --> Annulée : admin annule
    EnAttente --> Expirée : 15 min sans confirmation de l'admin
```

- Le délai court à partir de la création de la réservation ; le membre doit appeler l'admin, qui confirme pendant l'appel.
