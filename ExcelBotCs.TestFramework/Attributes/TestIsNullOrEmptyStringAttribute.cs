using System.Collections;
using NUnit.Framework;

namespace ExcelBotCs.TestFramework.Attributes;

/// <summary>
///     Provides test cases for null, empty, and whitespace-only strings.
///     Usage: [Test, IsNullOrEmptyString] public void TestMethod(string input) { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestIsNullOrEmptyStringAttribute : TestCaseSourceAttribute
{
    public TestIsNullOrEmptyStringAttribute() : base(typeof(TestIsNullOrEmptyStringAttribute), nameof(TestCases))
    {
    }

    public static IEnumerable TestCases
    {
        get
        {
            // Null
            yield return new TestCaseData(null).SetName("Null");

            // Empty string
            yield return new TestCaseData(string.Empty).SetName("Empty");

            // Whitespace variations
            yield return new TestCaseData(" ").SetName("SingleSpace");
            yield return new TestCaseData("  ").SetName("DoubleSpace");
            yield return new TestCaseData("   ").SetName("TripleSpace");

            // Tab variations
            yield return new TestCaseData("\t").SetName("SingleTab");
            yield return new TestCaseData("\t\t").SetName("DoubleTab");

            // Line break variations
            yield return new TestCaseData("\n").SetName("LineFeed");
            yield return new TestCaseData("\r").SetName("CarriageReturn");
            yield return new TestCaseData("\r\n").SetName("CRLF");

            // Combinations of spaces and tabs
            yield return new TestCaseData(" \t").SetName("SpaceTab");
            yield return new TestCaseData("\t ").SetName("TabSpace");
            yield return new TestCaseData(" \t ").SetName("SpaceTabSpace");

            // Combinations with line breaks
            yield return new TestCaseData(" \n").SetName("SpaceLineFeed");
            yield return new TestCaseData("\n ").SetName("LineFeedSpace");
            yield return new TestCaseData("\t\n").SetName("TabLineFeed");
            yield return new TestCaseData("\n\t").SetName("LineFeedTab");

            // Complex combinations
            yield return new TestCaseData(" \t\n").SetName("SpaceTabLineFeed");
            yield return new TestCaseData("\t \n").SetName("TabSpaceLineFeed");
            yield return new TestCaseData(" \n\t").SetName("SpaceLineFeedTab");
            yield return new TestCaseData(" \t\n\r").SetName("SpaceTabLineFeedCarriageReturn");
            yield return new TestCaseData(" \r\n\t ").SetName("SpaceCRLFTabSpace");
        }
    }
}