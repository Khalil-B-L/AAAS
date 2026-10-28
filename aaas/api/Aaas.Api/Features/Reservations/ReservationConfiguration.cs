using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Features.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aaas.Api.Features.Reservations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    /// <summary>Longueur des colonnes d'énumération (stockées en chaîne, AD-7).</summary>
    private const int EnumMax = 16;

    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Statut).HasMaxLength(EnumMax).IsRequired();
        builder.Property(r => r.Logement).HasMaxLength(EnumMax).IsRequired();
        builder.Property(r => r.Experience).HasMaxLength(Reservation.ExperienceMax);
        builder.Property(r => r.RowVersion).IsRowVersion();

        // Suppressions fixées par AD-7 : une réservation bloque la suppression de son animal et de son membre.
        builder.HasOne<Animal>().WithMany().HasForeignKey(r => r.AnimalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        // Une seule réservation EnAttente par animal, garantie par la base (AD-6).
        builder.HasIndex(r => r.AnimalId, "IX_Reservations_AnimalId_EnAttente")
            .IsUnique()
            .HasFilter($"[{nameof(Reservation.Statut)}] = '{nameof(StatutReservation.EnAttente)}'");

        // Toutes les réservations d'un animal (clé étrangère, suppression d'un animal, historique).
        builder.HasIndex(r => r.AnimalId, "IX_Reservations_AnimalId");
    }
}
