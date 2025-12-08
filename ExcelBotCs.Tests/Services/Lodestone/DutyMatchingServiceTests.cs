using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.Lodestone;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExcelBotCs.Tests.Services.Lodestone;

[TestFixture]
public class DutyMatchingServiceTests
{
    private DutyMatchingService _service;
    private Mock<ILogger<DutyMatchingService>> _loggerMock;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<DutyMatchingService>>();
        _service = new DutyMatchingService(_loggerMock.Object);
    }

    #region NormalizeName Tests

    [Test]
    public void NormalizeName_RemovesExtremeSuffix()
    {
        // Arrange
        var name = "Titan (Extreme)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        Assert.That(result, Is.EqualTo("titan"));
    }

    [Test]
    public void NormalizeName_RemovesSavageSuffix()
    {
        // Arrange
        var name = "M1S (Savage)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        Assert.That(result, Is.EqualTo("m1s"));
    }

    [Test]
    public void NormalizeName_RemovesUltimateSuffix()
    {
        // Arrange
        var name = "The Weapon's Refrain (Ultimate)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        Assert.That(result, Is.EqualTo("the weapon's refrain"));
    }

    [Test]
    public void NormalizeName_RemovesChaoticSuffix()
    {
        // Arrange
        var name = "Cloud of Darkness (Chaotic)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        Assert.That(result, Is.EqualTo("cloud of darkness"));
    }

    [Test]
    public void NormalizeName_HandlesNullInput()
    {
        // Act
        var result = _service.NormalizeName(null);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeName_HandlesEmptyString()
    {
        // Act
        var result = _service.NormalizeName("");

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion

    #region ExtractSignificantWords Tests

    [Test]
    public void ExtractSignificantWords_FiltersStopWords()
    {
        // Arrange
        var text = "The Binding Coil of Bahamut - Turn 5 (Savage)";

        // Act
        var result = _service.ExtractSignificantWords(text);

        // Assert (case-insensitive comparison since the method preserves original case)
        Assert.That(result, Does.Contain("Binding").IgnoreCase);
        Assert.That(result, Does.Contain("Coil").IgnoreCase);
        Assert.That(result, Does.Contain("Bahamut").IgnoreCase);
        Assert.That(result, Does.Not.Contain("The").IgnoreCase);
        Assert.That(result, Does.Not.Contain("of").IgnoreCase);
        Assert.That(result, Does.Not.Contain("Savage").IgnoreCase);
        Assert.That(result, Does.Not.Contain("Turn").IgnoreCase);
    }

    [Test]
    public void ExtractSignificantWords_FiltersShortWords()
    {
        // Arrange
        var text = "M1S - Turn of the Heavens";

        // Act
        var result = _service.ExtractSignificantWords(text);

        // Assert
        Assert.That(result, Does.Not.Contain("of")); // Too short
        Assert.That(result, Does.Contain("M1S").IgnoreCase);
        Assert.That(result, Does.Contain("Heavens").IgnoreCase);
    }

    [Test]
    public void ExtractSignificantWords_HandlesNullInput()
    {
        // Act
        var result = _service.ExtractSignificantWords(null);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExtractSignificantWords_HandlesEmptyString()
    {
        // Act
        var result = _service.ExtractSignificantWords("");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExtractSignificantWords_SplitsOnMultipleDelimiters()
    {
        // Arrange
        var text = "Eden's Gate: Resurrection (Savage)";

        // Act
        var result = _service.ExtractSignificantWords(text);

        // Assert
        Assert.That(result, Does.Contain("Eden's").IgnoreCase);
        Assert.That(result, Does.Not.Contain("Gate").IgnoreCase); // "Gate" is a stopword
        Assert.That(result, Does.Contain("Resurrection").IgnoreCase);
        Assert.That(result, Does.Not.Contain("Savage").IgnoreCase); // "Savage" is also a stopword
    }

    #endregion

    #region FindBestMatch Tests

    [Test]
    public void FindBestMatch_ExactMatch_WithinExpansion()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "Abyssos: The Eighth Circle (Savage)",
            FFLogsExpansionId = 4
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Abyssos: The Eighth Circle (Savage)", ExpansionId = 4 },
            new() { Name = "Some Other Duty", ExpansionId = 4 }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Abyssos: The Eighth Circle (Savage)"));
    }

    [Test]
    public void FindBestMatch_ExactMatch_OutsideExpansionWarning()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "Titan (Extreme)",
            FFLogsExpansionId = 5 // Wrong expansion
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Titan (Extreme)", ExpansionId = 0 }, // Correct expansion is 0
            new() { Name = "Titania (Extreme)", ExpansionId = 3 }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Titan (Extreme)"));
        Assert.That(result.ExpansionId, Is.EqualTo(0));
    }

    [Test]
    public void FindBestMatch_BossNameExactMatch()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "Omega",
            FFLogsExpansionId = 2
        };

        var duties = new List<LodestoneDuty>
        {
            new()
            {
                Name = "Alphascape V4.0 (Savage)",
                ExpansionId = 2,
                BossNames = new List<string> { "Omega", "Omega-M", "Omega-F" }
            }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Alphascape V4.0 (Savage)"));
    }

    [Test]
    public void FindBestMatch_ContainsMatch_WithMinLength()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "Abyssos",
            FFLogsExpansionId = 4
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Abyssos: The Fifth Circle (Savage)", ExpansionId = 4 },
            new() { Name = "Anabaseios: The Ninth Circle (Savage)", ExpansionId = 4 }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Does.Contain("Abyssos"));
    }

    [Test]
    public void FindBestMatch_PreventsShortSubstringMatch()
    {
        // Arrange - This should NOT match because "Titan" is too short and could match "Titania"
        var fight = new Fight
        {
            Name = "Tit", // Only 3 characters
            FFLogsExpansionId = 3
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Titania (Extreme)", ExpansionId = 3 }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Null); // Should not match because it's below 4 char minimum
    }

    [Test]
    public void FindBestMatch_BossNamePartialMatch()
    {
        // Arrange - Use a boss name that doesn't appear in the duty name
        var fight = new Fight
        {
            Name = "Twintania",
            FFLogsExpansionId = 0
        };

        var duties = new List<LodestoneDuty>
        {
            new()
            {
                Name = "The Binding Coil of Bahamut - Turn 5",
                ExpansionId = 0,
                BossNames = new List<string> { "Twintania" }
            },
            new()
            {
                Name = "The Final Coil of Bahamut - Turn 4",
                ExpansionId = 0,
                BossNames = new List<string> { "Bahamut Prime" }
            }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("The Binding Coil of Bahamut - Turn 5"));
        Assert.That(result.BossNames, Does.Contain("Twintania"));
    }

    [Test]
    public void FindBestMatch_WordBasedMatching()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "Eden Gate Resurrection",
            FFLogsExpansionId = 3
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Eden's Gate: Resurrection (Savage)", ExpansionId = 3 },
            new() { Name = "Eden's Gate: Descent (Savage)", ExpansionId = 3 }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Does.Contain("Resurrection"));
    }

    [Test]
    public void FindBestMatch_NoMatch_ReturnsNull()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "NonExistent Fight",
            FFLogsExpansionId = 5
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Some Other Duty", ExpansionId = 5 }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindBestMatch_PrioritizesExpansionFilter()
    {
        // Arrange - Two duties with same name in different expansions
        var fight = new Fight
        {
            Name = "Titan (Extreme)",
            FFLogsExpansionId = 0
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Titan (Extreme)", ExpansionId = 0, LodestoneId = "correct" },
            new() { Name = "Titan (Extreme)", ExpansionId = 3, LodestoneId = "wrong" }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.LodestoneId, Is.EqualTo("correct"));
        Assert.That(result.ExpansionId, Is.EqualTo(0));
    }

    [Test]
    public void FindBestMatch_WithoutExpansionId_SearchesAllExpansions()
    {
        // Arrange
        var fight = new Fight
        {
            Name = "Titan (Extreme)",
            FFLogsExpansionId = null
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Titan (Extreme)", ExpansionId = 0 },
            new() { Name = "Titania (Extreme)", ExpansionId = 3 }
        };

        // Act
        var result = _service.FindBestMatch(fight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Titan (Extreme)"));
    }

    [Test]
    public void FindBestMatch_TitanVsTitania_DoesNotCrossMatch()
    {
        // Arrange - This is the key edge case from the original issue
        var titanFight = new Fight
        {
            Name = "Titan",
            FFLogsExpansionId = 0
        };

        var duties = new List<LodestoneDuty>
        {
            new() { Name = "Titan (Extreme)", ExpansionId = 0 },
            new() { Name = "Titania (Extreme)", ExpansionId = 3 }
        };

        // Act
        var result = _service.FindBestMatch(titanFight, duties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Does.Contain("Titan"));
        Assert.That(result.Name, Does.Not.Contain("Titania"));
    }

    #endregion
}