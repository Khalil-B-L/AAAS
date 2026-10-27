using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aaas.Api.Data;

/// <summary>
/// Contexte EF unique (AD-7). Chaque slice déclare ses entités dans un
/// <see cref="IEntityTypeConfiguration{TEntity}"/> de son dossier, chargé ici.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Conventions imposées à toutes les slices : <see cref="DateTimeOffset"/> en <c>datetimeoffset</c> écrit en UTC (AD-11),
    /// énumérations stockées en chaîne (AD-7), aucune propriété <see cref="DateTime"/> (refusée à la construction du modèle).
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveColumnType("datetimeoffset")
            .HaveConversion<UtcDateTimeOffsetValueConverter>();

        configurationBuilder.Properties<Enum>().HaveConversion<string>();

        configurationBuilder.Conventions.Add(_ => new InterdireDateTimeConvention());
    }
}

/// <summary>Ramène tout <see cref="DateTimeOffset"/> à l'UTC avant écriture en base.</summary>
public sealed class UtcDateTimeOffsetValueConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(
    valeur => valeur.ToUniversalTime(),
    valeur => valeur.ToUniversalTime());

/// <summary>Une propriété <see cref="DateTime"/> fait échouer la construction du modèle (AD-11).</summary>
internal sealed class InterdireDateTimeConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entite in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var propriete in entite.GetDeclaredProperties())
            {
                if (propriete.ClrType == typeof(DateTime) || propriete.ClrType == typeof(DateTime?))
                {
                    throw new InvalidOperationException(
                        $"La propriété {entite.DisplayName()}.{propriete.Name} est un DateTime : utilisez DateTimeOffset UTC (AD-11).");
                }
            }
        }
    }
}
