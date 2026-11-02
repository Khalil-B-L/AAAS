using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aaas.Api.Features.Animaux;

public sealed class PhotoAnimalConfiguration : IEntityTypeConfiguration<PhotoAnimal>
{
    public void Configure(EntityTypeBuilder<PhotoAnimal> builder)
    {
        builder.ToTable("PhotosAnimaux");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Cle).HasMaxLength(PhotoAnimal.CleMax).IsRequired();

        // Suppression fixée par AD-7 : les photos suivent leur animal (fichiers supprimés par la commande après le commit).
        builder.HasOne<Animal>().WithMany(a => a.Photos).HasForeignKey(p => p.AnimalId).OnDelete(DeleteBehavior.Cascade);

        // Photos d'un animal par ordre (photo principale = ordre minimal) ; un ordre n'est attribué qu'une fois par animal.
        builder.HasIndex(p => new { p.AnimalId, p.Ordre }).IsUnique();
    }
}
