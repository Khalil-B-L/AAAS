using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aaas.Api.Features.Animaux;

public sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    /// <summary>Longueur des colonnes d'énumération (stockées en chaîne, AD-7).</summary>
    private const int EnumMax = 16;

    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("Animaux");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nom).HasMaxLength(Animal.NomMax).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(Animal.DescriptionMax).IsRequired();

        builder.Property(a => a.Espece).HasMaxLength(EnumMax).IsRequired();
        builder.Property(a => a.Sexe).HasMaxLength(EnumMax).IsRequired();
        builder.Property(a => a.Age).HasMaxLength(EnumMax).IsRequired();
        builder.Property(a => a.Taille).HasMaxLength(EnumMax).IsRequired();
        builder.Property(a => a.EntenteEnfants).HasMaxLength(EnumMax).IsRequired();
        builder.Property(a => a.EntenteChiens).HasMaxLength(EnumMax).IsRequired();
        builder.Property(a => a.EntenteChats).HasMaxLength(EnumMax).IsRequired();
        builder.Property(a => a.Statut).HasMaxLength(EnumMax).IsRequired();

        builder.Property(a => a.RowVersion).IsRowVersion();

        // Catalogue et accueil : non adoptés, creeLe décroissant (contrats-api.md §3).
        builder.HasIndex(a => new { a.Statut, a.CreeLe });
    }
}
