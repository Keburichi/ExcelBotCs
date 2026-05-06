using NUnit.Framework.Legacy;

namespace ExcelBotCs.Tests;

[TestFixture]
public class PrngTests
{
    private readonly Prng _prng = new();

    [Test]
    public void NextByte_ReturnsAByte()
    {
        // Act
        var result = _prng.NextByte();

        // Assert
        Assert.That(result, Is.TypeOf<byte>());
    }

    [Test]
    public void NextInt_ReturnsAnInt()
    {
        // Act
        var result = _prng.NextInt();

        // Assert
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void NextInt_CalledMultipleTimes_ReturnsDifferentValues()
    {
        // Arrange
        const int sampleSize = 10;
        var results = new HashSet<int>();

        // Act
        for (var i = 0; i < sampleSize; i++) results.Add(_prng.NextInt());

        // Assert
        Assert.That(results.Count, Is.GreaterThan(1),
            "because over a sample of 10, we expect more than one unique value");
    }

    [Test]
    [Repeat(1000)]
    public void NextInt_WithMinAndMax_ReturnsValueWithinInclusiveRange()
    {
        // Arrange
        const int min = 10;
        const int max = 20;

        // Act
        var result = _prng.NextInt(min, max);

        // Assert
        Assert.That(result, Is.GreaterThanOrEqualTo(min).And.LessThanOrEqualTo(max));
    }

    [Test]
    public void NextFloat_ReturnsValueBetweenZeroAndOne()
    {
        // Act
        for (var i = 0; i < 1000; i++)
        {
            var result = _prng.NextFloat();

            // Assert
            Assert.That(result, Is.GreaterThanOrEqualTo(0.0f).And.LessThanOrEqualTo(1.0f));
        }
    }

    [Test]
    public void NextBool_ReturnsABool()
    {
        // Act
        var result = _prng.NextBool();

        // Assert
        Assert.That(result, Is.TypeOf<bool>());
    }

    [Test]
    public void NextBool_ReturnsBothTrueAndFalse()
    {
        // Arrange
        const int sampleSize = 100;
        var results = new HashSet<bool>();

        // Act
        for (var i = 0; i < sampleSize; i++) results.Add(_prng.NextBool());

        // Assert
        Assert.That(results, Contains.Item(true), "because a random boolean generator should produce true");
        Assert.That(results, Contains.Item(false), "because a random boolean generator should produce false");
        Assert.That(results.Count, Is.EqualTo(2),
            "because both true and false should be generated over a large sample");
    }

    [Test]
    public void Pick_ReturnsCorrectNumberOfItems()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };
        const int count = 3;

        // Act
        var result = _prng.Pick(source, count).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(count));
    }

    [Test]
    public void Pick_ReturnsItemsFromSource()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };
        const int count = 3;

        // Act
        var result = _prng.Pick(source, count).ToList();

        // Assert
        Assert.That(source, Is.SupersetOf(result));
    }

    [Test]
    public void Pick_ReturnsEmptyWhenSourceIsEmpty()
    {
        // Arrange
        var source = Array.Empty<int>();

        // Act
        var result = _prng.Pick(source).ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Pick_MoreItemsThanInSource_ReturnsAllItemsShuffled()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };
        var count = source.Length + 1;

        // Act
        var result = _prng.Pick(source, count).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(source.Length));
        CollectionAssert.AreEquivalent(source, result);
    }

    [Test]
    public void Pick_SingleItem_ReturnsOneItemFromSource()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _prng.Pick(source).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(source, Contains.Item(result.Single()));
    }

    [Test]
    public void Shuffle_ReturnsSameItems()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _prng.Shuffle(source).ToList();

        // Assert
        CollectionAssert.AreEquivalent(source, result);
    }

    [Test]
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
        CollectionAssert.AreNotEqual(source, result, "because shuffling should change the order");
    }

    [Test]
    public void Shuffle_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var source = Array.Empty<int>();

        // Act
        var result = _prng.Shuffle(source).ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }
}