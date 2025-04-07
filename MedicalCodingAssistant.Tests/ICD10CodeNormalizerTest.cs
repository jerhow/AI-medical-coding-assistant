using MedicalCodingAssistant.Utils;

namespace MedicalCodingAssistant.Tests
{
    [TestClass]
    public class ICD10CodeNormalizerTest
    {
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
}
