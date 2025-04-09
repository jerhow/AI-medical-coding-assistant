using MedicalCodingAssistant.Utils;

namespace MedicalCodingAssistant.Tests;

/// <summary>
/// Unit tests for the ICD10CodeNormalizer class.
/// </summary>
[TestClass]
public class ICD10CodeNormalizerTest
{
    /// <summary>
    /// Tests the ToHumanReadableFormat method with null input.
    /// </summary>
    [TestMethod]
    public void ToHumanReadableFormat_NullInput_ReturnsEmptyString()
    {
        // Arrange
        string? input = null;

        // Act
        string result = ICD10CodeNormalizer.ToHumanReadableFormat(input ?? "");

        // Assert
        Assert.AreEqual("", result);
    }

    /// <summary>
    /// Tests the ToHumanReadableFormat method with an empty string.
    /// </summary>
    [TestMethod]
    public void ToHumanReadableFormat_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        string input = "";

        // Act
        string result = ICD10CodeNormalizer.ToHumanReadableFormat(input);

        // Assert
        Assert.AreEqual("", result);
    }

    /// <summary>
    /// Tests the ToHumanReadableFormat method with a string less than three characters.
    /// </summary>
    [TestMethod]
    public void ToHumanReadableFormat_LessThanThreeCharacters_ReturnsSameString()
    {
        // Arrange
        string input = "J4";

        // Act
        string result = ICD10CodeNormalizer.ToHumanReadableFormat(input);

        // Assert
        Assert.AreEqual("J4", result);
    }

    /// <summary>
    /// Tests the ToHumanReadableFormat method with a string exactly three characters long.
    /// </summary>
    [TestMethod]
    public void ToHumanReadableFormat_ExactlyThreeCharacters_ReturnsSameString()
    {
        // Arrange
        string input = "J44";

        // Act
        string result = ICD10CodeNormalizer.ToHumanReadableFormat(input);

        // Assert
        Assert.AreEqual("J44", result);
    }

    /// <summary>
    /// Tests the ToHumanReadableFormat method with a string longer than three characters.
    /// Verifies that a dot is inserted after the third character.
    /// </summary>
    [TestMethod]
    public void ToHumanReadableFormat_MoreThanThreeCharacters_InsertsDotAfterThirdCharacter()
    {
        // Arrange
        string input = "J449";

        // Act
        string result = ICD10CodeNormalizer.ToHumanReadableFormat(input);

        // Assert
        Assert.AreEqual("J44.9", result);
    }

    /// <summary>
    /// Tests the ToHumanReadableFormat method with a string containing whitespace.
    /// </summary>
    [TestMethod]
    public void ToHumanReadableFormat_InputWithWhitespace_TrimsAndFormatsCorrectly()
    {
        // Arrange
        string input = "  j449  ";

        // Act
        string result = ICD10CodeNormalizer.ToHumanReadableFormat(input);

        // Assert
        Assert.AreEqual("J44.9", result);
    }

    /// <summary>
    /// Tests the ToHumanReadableFormat method with an already formatted input.
    /// Verifies that the method returns the same string without modification.
    /// </summary>
    [TestMethod]
    public void ToHumanReadableFormat_AlreadyFormattedInput_ReturnsSameString()
    {
        // Arrange
        string input = "J44.9";

        // Act
        string result = ICD10CodeNormalizer.ToHumanReadableFormat(input);

        // Assert
        Assert.AreEqual("J44.9", result);
    }

    /// <summary>
    /// Tests the ToCMSFormat method with null input.
    /// </summary>
    [TestMethod]
    public void ToCMSFormat_NullInput_ReturnsEmptyString()
    {
        // Arrange
        string? input = null;

        // Act
        string result = ICD10CodeNormalizer.ToCMSFormat(input ?? "");

        // Assert
        Assert.AreEqual("", result);
    }

    /// <summary>
    /// Tests the ToCMSFormat method with an empty string.
    /// </summary>
    [TestMethod]
    public void ToCMSFormat_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        string input = "";

        // Act
        string result = ICD10CodeNormalizer.ToCMSFormat(input);

        // Assert
        Assert.AreEqual("", result);
    }

    /// <summary>
    /// Tests the ToCMSFormat method with a string less than three characters.
    /// </summary>
    [TestMethod]
    public void ToCMSFormat_LessThanThreeCharacters_ReturnsSameString()
    {
        // Arrange
        string input = "J4";

        // Act
        string result = ICD10CodeNormalizer.ToCMSFormat(input);

        // Assert
        Assert.AreEqual("J4", result);
    }

    /// <summary>
    /// Tests the ToCMSFormat method with a string exactly three characters long.
    /// </summary>
    [TestMethod]
    public void ToCMSFormat_ExactlyThreeCharacters_ReturnsSameString()
    {
        // Arrange
        string input = "J44";

        // Act
        string result = ICD10CodeNormalizer.ToCMSFormat(input);

        // Assert
        Assert.AreEqual("J44", result);
    }

    /// <summary>
    /// Tests the ToCMSFormat method with a string longer than three characters.
    /// Verifies that the dot is removed from the string.
    /// </summary>
    [TestMethod]
    public void ToCMSFormat_MoreThanThreeCharacters_RemovesDot()
    {
        // Arrange
        string input = "J44.9";

        // Act
        string result = ICD10CodeNormalizer.ToCMSFormat(input);

        // Assert
        Assert.AreEqual("J449", result);
    }

    /// <summary>
    /// Tests the ToCMSFormat method with a string containing whitespace.
    /// </summary>
    [TestMethod]
    public void ToCMSFormat_InputWithWhitespace_TrimsAndFormatsCorrectly()
    {
        // Arrange
        string input = "  j44.9  ";

        // Act
        string result = ICD10CodeNormalizer.ToCMSFormat(input);

        // Assert
        Assert.AreEqual("J449", result);
    }

    /// <summary>
    /// Tests the ToCMSFormat method with an already formatted input.
    /// Verifies that the method returns the same string without modification.
    /// </summary>
    [TestMethod]
    public void ToCMSFormat_AlreadyFormattedInput_ReturnsSameString()
    {
        // Arrange
        string input = "J449";

        // Act
        string result = ICD10CodeNormalizer.ToCMSFormat(input);

        // Assert
        Assert.AreEqual("J449", result);
    }
}
