using Aaas.Api.Common;

namespace Aaas.Api.Tests;

/// <summary>Normalisation des numéros de téléphone (unitaire).</summary>
public class TelephoneTests
{
    [Theory]
    [InlineData("72 000 000", "+21672000000")]
    [InlineData("0612345678", null)]
    [InlineData("+44 20 7946 0958", "+442079460958")]
    [InlineData("+12345678", "+12345678")]
    [InlineData("+123456789012345", "+123456789012345")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("72 000 000 9", null)]
    public void Telephone_Normaliser(string? saisie, string? attendu) =>
        Assert.Equal(attendu, Telephone.Normaliser(saisie));
}
