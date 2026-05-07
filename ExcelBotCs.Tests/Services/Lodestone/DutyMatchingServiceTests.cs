using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.Lodestone;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExcelBotCs.Tests.Services.Lodestone;

public class DutyMatchingServiceTests
{
    private readonly DutyMatchingService _service;
    private readonly Mock<ILogger<DutyMatchingService>> _loggerMock;

    public DutyMatchingServiceTests()
    {
        _loggerMock = new Mock<ILogger<DutyMatchingService>>();
        _service = new DutyMatchingService(_loggerMock.Object);
    }

    #region NormalizeName Tests

    [Fact]
    public void NormalizeName_RemovesExtremeSuffix()
    {
        // Arrange
        var name = "Titan (Extreme)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        result.ShouldBe("titan");
    }

    [Fact]
    public void NormalizeName_RemovesSavageSuffix()
    {
        // Arrange
        var name = "M1S (Savage)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        result.ShouldBe("m1s");
    }

    [Fact]
    public void NormalizeName_RemovesUltimateSuffix()
    {
        // Arrange
        var name = "The Weapon's Refrain (Ultimate)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        result.ShouldBe("the weapon's refrain");
    }

    [Fact]
    public void NormalizeName_RemovesChaoticSuffix()
    {
        // Arrange
        var name = "Cloud of Darkness (Chaotic)";

        // Act
        var result = _service.NormalizeName(name);

        // Assert
        result.ShouldBe("cloud of darkness");
    }

    [Fact]
    public void NormalizeName_HandlesNullInput()
    {
        // Act
        var result = _service.NormalizeName(null);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void NormalizeName_HandlesEmptyString()
    {
        // Act
        var result = _service.NormalizeName("");

        // Assert
        result.ShouldBe(string.Empty);
    }

    #endregion

    #region ExtractSignificantWords Tests

    [Fact]
    public void ExtractSignificantWords_FiltersStopWords()
    {
        // Arrange
        var text = "The Binding Coil of Bahamut - Turn 5 (Savage)";

        // Act
        var result = _service.ExtractSignificantWords(text);

        // Assert (case-insensitive comparison since the method preserves original case)
        result.ShouldContain(s => s.Equals("Binding", StringComparison.OrdinalIgnoreCase));
        result.ShouldContain(s => s.Equals("Coil", StringComparison.OrdinalIgnoreCase));
        result.ShouldContain(s => s.Equals("Bahamut", StringComparison.OrdinalIgnoreCase));
        result.ShouldNotContain(s => s.Equals("The", StringComparison.OrdinalIgnoreCase));
        result.ShouldNotContain(s => s.Equals("of", StringComparison.OrdinalIgnoreCase));
        result.ShouldNotContain(s => s.Equals("Savage", StringComparison.OrdinalIgnoreCase));
        result.ShouldNotContain(s => s.Equals("Turn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExtractSignificantWords_FiltersShortWords()
    {
        // Arrange
        var text = "M1S - Turn of the Heavens";

        // Act
        var result = _service.ExtractSignificantWords(text);

        // Assert
        result.ShouldNotContain(s => s.Equals("of", StringComparison.OrdinalIgnoreCase));
        result.ShouldContain(s => s.Equals("M1S", StringComparison.OrdinalIgnoreCase));
        result.ShouldContain(s => s.Equals("Heavens", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExtractSignificantWords_HandlesNullInput()
    {
        // Act
        var result = _service.ExtractSignificantWords(null);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractSignificantWords_HandlesEmptyString()
    {
        // Act
        var result = _service.ExtractSignificantWords("");

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractSignificantWords_SplitsOnMultipleDelimiters()
    {
        // Arrange
        var text = "Eden's Gate: Resurrection (Savage)";

        // Act
        var result = _service.ExtractSignificantWords(text);

        // Assert
        result.ShouldContain(s => s.Equals("Eden's", StringComparison.OrdinalIgnoreCase));
        result.ShouldNotContain(s => s.Equals("Gate", StringComparison.OrdinalIgnoreCase));
        result.ShouldContain(s => s.Equals("Resurrection", StringComparison.OrdinalIgnoreCase));
        result.ShouldNotContain(s => s.Equals("Savage", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region FindBestMatch Tests

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Abyssos: The Eighth Circle (Savage)");
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Titan (Extreme)");
        result.ExpansionId.ShouldBe(0);
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Alphascape V4.0 (Savage)");
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldContain("Abyssos");
    }

    [Fact]
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
        result.ShouldBeNull(); // Should not match because it's below 4 char minimum
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("The Binding Coil of Bahamut - Turn 5");
        result.BossNames.ShouldContain("Twintania");
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldContain("Resurrection");
    }

    [Fact]
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
        result.ShouldBeNull();
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.LodestoneId.ShouldBe("correct");
        result.ExpansionId.ShouldBe(0);
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Titan (Extreme)");
    }

    [Fact]
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
        result.ShouldNotBeNull();
        result!.Name.ShouldContain("Titan");
        result.Name.ShouldNotContain("Titania");
    }

    #endregion
}