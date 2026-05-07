namespace ExcelBotCs.Tests;

public class PrngTests
{
    private readonly Prng _prng = new();

    [Fact]
    public void NextByte_ReturnsAByte()
    {
        // Act
        var result = _prng.NextByte();

        // Assert
        result.ShouldBeOfType<byte>();
    }

    [Fact]
    public void NextInt_ReturnsAnInt()
    {
        // Act
        var result = _prng.NextInt();

        // Assert
        result.ShouldBeOfType<int>();
    }

    [Fact]
    public void NextInt_CalledMultipleTimes_ReturnsDifferentValues()
    {
        // Arrange
        const int sampleSize = 10;
        var results = new HashSet<int>();

        // Act
        for (var i = 0; i < sampleSize; i++) results.Add(_prng.NextInt());

        // Assert
        results.Count.ShouldBeGreaterThan(1,
            "because over a sample of 10, we expect more than one unique value");
    }

    [Fact]
    public void NextInt_WithMinAndMax_ReturnsValueWithinInclusiveRange()
    {
        // Arrange
        const int min = 10;
        const int max = 20;

        // Act & Assert
        for (var i = 0; i < 1000; i++)
        {
            var result = _prng.NextInt(min, max);
            result.ShouldBeGreaterThanOrEqualTo(min);
            result.ShouldBeLessThanOrEqualTo(max);
        }
    }

    [Fact]
    public void NextFloat_ReturnsValueBetweenZeroAndOne()
    {
        // Act & Assert
        for (var i = 0; i < 1000; i++)
        {
            var result = _prng.NextFloat();
            result.ShouldBeGreaterThanOrEqualTo(0.0f);
            result.ShouldBeLessThanOrEqualTo(1.0f);
        }
    }

    [Fact]
    public void NextBool_ReturnsABool()
    {
        // Act
        var result = _prng.NextBool();

        // Assert
        result.ShouldBeOfType<bool>();
    }

    [Fact]
    public void NextBool_ReturnsBothTrueAndFalse()
    {
        // Arrange
        const int sampleSize = 100;
        var results = new HashSet<bool>();

        // Act
        for (var i = 0; i < sampleSize; i++) results.Add(_prng.NextBool());

        // Assert
        results.ShouldContain(true, "because a random boolean generator should produce true");
        results.ShouldContain(false, "because a random boolean generator should produce false");
        results.Count.ShouldBe(2,
            "because both true and false should be generated over a large sample");
    }

    [Fact]
    public void Pick_ReturnsCorrectNumberOfItems()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };
        const int count = 3;

        // Act
        var result = _prng.Pick(source, count).ToList();

        // Assert
        result.Count.ShouldBe(count);
    }

    [Fact]
    public void Pick_ReturnsItemsFromSource()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };
        const int count = 3;

        // Act
        var result = _prng.Pick(source, count).ToList();

        // Assert
        result.All(item => source.Contains(item)).ShouldBeTrue();
    }

    [Fact]
    public void Pick_ReturnsEmptyWhenSourceIsEmpty()
    {
        // Arrange
        var source = Array.Empty<int>();

        // Act
        var result = _prng.Pick(source).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Pick_MoreItemsThanInSource_ReturnsAllItemsShuffled()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };
        var count = source.Length + 1;

        // Act
        var result = _prng.Pick(source, count).ToList();

        // Assert
        result.Count.ShouldBe(source.Length);
        result.ShouldBe(source, ignoreOrder: true);
    }

    [Fact]
    public void Pick_SingleItem_ReturnsOneItemFromSource()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _prng.Pick(source).ToList();

        // Assert
        result.Count.ShouldBe(1);
        source.ShouldContain(result.Single());
    }

    [Fact]
    public void Shuffle_ReturnsSameItems()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _prng.Shuffle(source).ToList();

        // Assert
        result.ShouldBe(source, ignoreOrder: true);
    }

    [Fact]
    public void Shuffle_DoesNotReturnSameOrder_Usually()
    {
        // Arrange
        var source = new List<int>();
        for (var i = 0; i < 50; i++) source.Add(i);

        // Act
        var result = _prng.Shuffle(source).ToList();

        // Assert
        // This test is probabilistic. There's a tiny chance it could fail
        // if the shuffled list happens to be the same as the original.
        // For a 50-element list, this is astronomically unlikely.
        source.SequenceEqual(result).ShouldBeFalse("because shuffling should change the order");
    }

    [Fact]
    public void Shuffle_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var source = Array.Empty<int>();

        // Act
        var result = _prng.Shuffle(source).ToList();

        // Assert
        result.ShouldBeEmpty();
    }
}