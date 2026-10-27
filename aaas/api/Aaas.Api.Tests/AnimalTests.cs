using Aaas.Api.Features.Animaux;
using Aaas.Api.Tests.Outils;
using Microsoft.Extensions.Time.Testing;

namespace Aaas.Api.Tests;

/// <summary>Transitions de statut de l'entité Animal (AD-4), sans base.</summary>
public class AnimalTests
{
    private static readonly DateTimeOffset Maintenant = new(2026, 3, 1, 9, 30, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _temps = new(Maintenant);

    [Fact]
    public void Nouvel_animal_est_Disponible_et_cree_a_l_heure_du_TimeProvider()
    {
        var animal = new Animal(_temps);

        Assert.Equal(StatutAnimal.Disponible, animal.Statut);
        Assert.Equal(Maintenant, animal.CreeLe);
        Assert.Null(animal.AdopteLe);
    }

    [Fact]
    public void Reserver_passe_de_Disponible_a_Reserve()
    {
        var animal = Animaux.Nouveau();

        animal.Reserver();

        Assert.Equal(StatutAnimal.Reserve, animal.Statut);
    }

    [Fact]
    public void Liberer_passe_de_Reserve_a_Disponible()
    {
        var animal = Animaux.Nouveau().Reserve();

        animal.Liberer();

        Assert.Equal(StatutAnimal.Disponible, animal.Statut);
    }

    [Fact]
    public void MarquerAdopte_passe_de_Reserve_a_Adopte_et_pose_AdopteLe()
    {
        var animal = Animaux.Nouveau().Reserve();

        animal.MarquerAdopte(_temps);

        Assert.Equal(StatutAnimal.Adopte, animal.Statut);
        Assert.Equal(Maintenant, animal.AdopteLe);
    }

    [Fact]
    public void ForcerStatut_Adopte_pose_AdopteLe_et_Disponible_l_efface()
    {
        var animal = Animaux.Nouveau();

        animal.ForcerStatut(StatutAnimal.Adopte, _temps);
        Assert.Equal(StatutAnimal.Adopte, animal.Statut);
        Assert.Equal(Maintenant, animal.AdopteLe);

        animal.ForcerStatut(StatutAnimal.Disponible, _temps);
        Assert.Equal(StatutAnimal.Disponible, animal.Statut);
        Assert.Null(animal.AdopteLe);
    }

    [Fact]
    public void ForcerStatut_Adopte_sur_un_Adopte_garde_AdopteLe()
    {
        var animal = Animaux.Nouveau();
        animal.ForcerStatut(StatutAnimal.Adopte, _temps);

        _temps.Advance(TimeSpan.FromDays(30));
        animal.ForcerStatut(StatutAnimal.Adopte, _temps);

        Assert.Equal(StatutAnimal.Adopte, animal.Statut);
        Assert.Equal(Maintenant, animal.AdopteLe);
    }

    [Fact]
    public void Reserver_hors_de_Disponible_leve_TransitionInterditeException()
    {
        Assert.Throws<TransitionInterditeException>(() => Animaux.Nouveau().Reserve().Reserver());
        Assert.Throws<TransitionInterditeException>(() => Animaux.Nouveau().Adopte().Reserver());
    }

    [Fact]
    public void Liberer_sur_Disponible_ou_Adopte_leve_TransitionInterditeException()
    {
        var disponible = Animaux.Nouveau();
        var exception = Assert.Throws<TransitionInterditeException>(disponible.Liberer);
        Assert.Equal(StatutAnimal.Disponible, exception.Depuis);
        Assert.Equal(StatutAnimal.Disponible, disponible.Statut);

        Assert.Throws<TransitionInterditeException>(Animaux.Nouveau().Adopte().Liberer);
    }

    [Fact]
    public void MarquerAdopte_hors_de_Reserve_leve_TransitionInterditeException()
    {
        var disponible = Animaux.Nouveau();

        Assert.Throws<TransitionInterditeException>(() => disponible.MarquerAdopte(_temps));
        Assert.Equal(StatutAnimal.Disponible, disponible.Statut);
        Assert.Null(disponible.AdopteLe);

        Assert.Throws<TransitionInterditeException>(() => Animaux.Nouveau().Adopte().MarquerAdopte(_temps));
    }

    [Fact]
    public void ForcerStatut_Reserve_leve_TransitionInterditeException()
    {
        var animal = Animaux.Nouveau();

        Assert.Throws<TransitionInterditeException>(() => animal.ForcerStatut(StatutAnimal.Reserve, _temps));
        Assert.Equal(StatutAnimal.Disponible, animal.Statut);
    }
}
