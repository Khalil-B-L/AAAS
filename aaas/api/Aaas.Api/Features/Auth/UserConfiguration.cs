using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aaas.Api.Features.Auth;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Prenom).HasMaxLength(User.PrenomMax).IsRequired();
        builder.Property(u => u.Nom).HasMaxLength(User.NomMax).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(User.EmailMax).IsRequired();
        builder.Property(u => u.Telephone).HasMaxLength(User.TelephoneMax).IsRequired();
        builder.Property(u => u.MotDePasseHash).HasMaxLength(User.MotDePasseHashMax).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(16).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
