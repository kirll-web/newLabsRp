using Moq;
using StackExchange.Redis;
using Xunit;


namespace RankCalculator.tests;

public class RankCalculatorTests
{
    private readonly Mock<IDatabase> _redisMock;
    private readonly RankCalculator _rankCalculator;

    public RankCalculatorTests()
    {
        _redisMock = new Mock<IDatabase>();
        var db = _redisMock.Object;
        _rankCalculator = new RankCalculator();
        _rankCalculator.Start(db);
    }

    [Fact]
    public void CalculateRank_EmptyText_ReturnsZero()
    {
        // Arrange
        const string id = "test1";
        const double expectResult = 0;
        _redisMock.Setup(x => x.StringGet($"TEXT-{id}", It.IsAny<CommandFlags>())).Returns(RedisValue.Null);

        // Act
        var result = _rankCalculator.CalculateRank(id);

        // Assert
        Assert.Equal(expectResult, result);
    }

    [Fact]
    public void CalculateRank_AllAlphabetic_ReturnsZero()
    {
        const string id = "test2";
        const string text = "абвabc";
        const double expectResult = 0;
        _redisMock.Setup(x => x.StringGet($"TEXT-{id}", It.IsAny<CommandFlags>())).Returns(text);

        var result = _rankCalculator.CalculateRank(id);
        
        Assert.Equal(expectResult, result);
    }

    [Fact]
    public void CalculateRank_AllNonAlphabetic_ReturnsOne()
    {
        const string id = "test3";
        const string text = "123!@#";
        const double expectResult = 1;
        _redisMock.Setup(m => m.StringGet($"TEXT-{id}", It.IsAny<CommandFlags>())).Returns(text);
        
        var result = _rankCalculator.CalculateRank(id);
        
        Assert.Equal(expectResult, result);
    }
    
    [Fact]
    public void CalculateRank_MixedText_ReturnsCorrectRatio()
    {
        string id = "test4";
        const string text = "a1b2"; //  2 - not letters, all - 4, 2/4 = 0.5
        const double expectResult = 0.5;
        _redisMock.Setup(x => x.StringGet($"TEXT-{id}", It.IsAny<CommandFlags>())).Returns(text);

        var result = _rankCalculator.CalculateRank(id);
        
        Assert.Equal(expectResult, result);
    }
    
    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('ё')]
    [InlineData('Я')]
    public void IsAlphabetic_ValidChars_ReturnsTrue(char c)
    {
        Assert.True(RankCalculator.IsAlphabetic(c));
    }
    
    [Theory]
    [InlineData('1')]
    [InlineData('@')]
    [InlineData(' ')]
    [InlineData('€')]
    public void IsAlphabetic_InvalidChars_ReturnsFalse(char c)
    {
        Assert.False(RankCalculator.IsAlphabetic(c));
    }
}
