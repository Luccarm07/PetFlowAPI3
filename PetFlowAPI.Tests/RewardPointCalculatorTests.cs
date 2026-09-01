using PetFlowAPI.Domain;

namespace PetFlowAPI.Tests;

public class RewardPointCalculatorTests
{
    private readonly IRewardPointCalculator _sut = new RewardPointCalculator();

    [Theory]
    [InlineData(10, 1, 10)]
    [InlineData(10, 2, 20)]
    [InlineData(25, 3, 75)]
    [InlineData(0, 5, 0)]
    public void Calculate_ComPontosBaseEMultiplicadorValidos_DeveRetornarPontosMultiplicados(
        int eventTypeBasePoints, int planPointsMultiplier, int expected)
    {
        // Arrange feito via [Theory]/[InlineData] (Arrange implícito nos parâmetros)

        // Act
        var result = _sut.Calculate(eventTypeBasePoints, planPointsMultiplier);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_ComPontosBaseNegativos_DeveLancarArgumentOutOfRangeException()
    {
        // Arrange
        const int eventTypeBasePoints = -5;
        const int planPointsMultiplier = 1;

        // Act
        var exception = Record.Exception(() => _sut.Calculate(eventTypeBasePoints, planPointsMultiplier));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_ComMultiplicadorZeroOuNegativo_DeveLancarArgumentOutOfRangeException(int planPointsMultiplier)
    {
        // Arrange
        const int eventTypeBasePoints = 10;

        // Act
        var exception = Record.Exception(() => _sut.Calculate(eventTypeBasePoints, planPointsMultiplier));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }
}
