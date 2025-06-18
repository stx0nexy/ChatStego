using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Options;
using PixChat.Application.Config;
using PixChat.Application.Services;

namespace PixChat.Tests;

public class SteganographyServiceTests : IDisposable
{
    private readonly Mock<ILogger<SteganographyService>> _loggerMock;
    private readonly Mock<IOptions<ImageConfig>> _imageConfigMock;
    private readonly SteganographyService _steganographyService;
    private readonly string _tempImageFolderPath;

    private const string ServiceEndMarker = "|X7K9P2M|";

    public SteganographyServiceTests()
    {
        _loggerMock = new Mock<ILogger<SteganographyService>>();
        _imageConfigMock = new Mock<IOptions<ImageConfig>>();

        _tempImageFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempImageFolderPath);

        _imageConfigMock.Setup(o => o.Value).Returns(new ImageConfig { ImageFolderPath = _tempImageFolderPath });

        _steganographyService = new SteganographyService(
            _loggerMock.Object,
            _imageConfigMock.Object
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempImageFolderPath))
        {
            Directory.Delete(_tempImageFolderPath, true);
        }
    }

    private byte[] CreateDummyPngImage(int width = 20, int height = 20)
    {
        using (var bitmap = new Bitmap(width, height))
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.FillRectangle(Brushes.White, 0, 0, width, height);
            }
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }

    private string CreateFullMessage(byte[] message, DateTime timestamp, string encryptedAESKey, byte[] aesIV)
    {
        return $"{Convert.ToBase64String(message)}|{timestamp.ToString("o")}|{encryptedAESKey}|{Convert.ToBase64String(aesIV)}{ServiceEndMarker}";
    }

    [Fact]
    public void EmbedMessage_ValidMessageAndImage_ReturnsEmbeddedImage()
    {
        // Arrange
        byte[] originalImage = CreateDummyPngImage();
        string secretKey = "testKey123";
        byte[] originalMessage = Encoding.UTF8.GetBytes("Hello, Steganography!");
        DateTime timestamp = DateTime.UtcNow;
        string encryptedAESKey = "EncryptedAESKeyBase64";
        byte[] aesIV = Encoding.UTF8.GetBytes("aesIVBase64");

        string fullMessage = CreateFullMessage(originalMessage, timestamp, encryptedAESKey, aesIV);

        // Act
        byte[] embeddedImage = _steganographyService.EmbedMessage(originalImage, fullMessage, secretKey);

        // Assert
        Assert.NotNull(embeddedImage);
        Assert.True(embeddedImage.Length > 0);
        Assert.NotEqual(originalImage, embeddedImage);
    }

    [Fact]
    public void EmbedMessage_MessageTooLarge_ThrowsArgumentException()
    {
        // Arrange
        byte[] originalImage = CreateDummyPngImage(5, 5);
        string secretKey = "smallImageKey";
        string veryLargeMessage = new string('A', 1000);
        string fullMessage = CreateFullMessage(Encoding.UTF8.GetBytes(veryLargeMessage), DateTime.UtcNow, "key", new byte[16]);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _steganographyService.EmbedMessage(originalImage, fullMessage, secretKey)
        );

        Assert.Contains("Message is too large to embed.", ex.Message);
    }

    [Fact]
    public void ExtractFullMessage_ValidEmbeddedImage_ReturnsCorrectData()
    {
        // Arrange
        byte[] originalImage = CreateDummyPngImage();
        string secretKey = "testKey123";
        byte[] expectedMessage = Encoding.UTF8.GetBytes("Secret Data Here!");
        DateTime expectedTimestamp = DateTime.UtcNow.AddMinutes(-5);
        string expectedEncryptedAESKey = "AnotherEncryptedKey";
        byte[] expectedAesIV = new byte[16];
        new Random().NextBytes(expectedAesIV);

        string fullMessageToEmbed = CreateFullMessage(expectedMessage, expectedTimestamp, expectedEncryptedAESKey, expectedAesIV);

        byte[] embeddedImage = _steganographyService.EmbedMessage(originalImage, fullMessageToEmbed, secretKey);

        // Act
        var (extractedMessage, extractedTimestamp, extractedEncryptedAESKey, extractedAesIV) =
            _steganographyService.ExtractFullMessage(embeddedImage, secretKey);

        // Assert
        Assert.NotNull(extractedMessage);
        Assert.Equal(expectedMessage, extractedMessage);
        Assert.Equal(expectedTimestamp.ToString("o"), extractedTimestamp.ToString("o"));
        Assert.Equal(expectedEncryptedAESKey, extractedEncryptedAESKey);
        Assert.Equal(expectedAesIV, extractedAesIV);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Extracted full message:")),
                null,
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ), Times.Once
        );
    }

    [Fact]
    public void ExtractFullMessage_EndMarkerNotFound_ThrowsFormatException()
    {
        // Arrange
        byte[] originalImage = CreateDummyPngImage();
        string secretKey = "testKey123";
        string shortMessageWithoutMarker = "no-marker-test";
        byte[] embeddedImage = _steganographyService.EmbedMessage(originalImage, shortMessageWithoutMarker, secretKey);

        // Act & Assert
        var ex = Assert.Throws<FormatException>(() =>
            _steganographyService.ExtractFullMessage(embeddedImage, secretKey)
        );

        Assert.Contains("End marker not found in image data.", ex.Message);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("End marker not found in image data.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ), Times.Once
        );
    }

    [Fact]
    public void ExtractFullMessage_InvalidDataFormat_ThrowsFormatException()
    {
        // Arrange
        byte[] originalImage = CreateDummyPngImage();
        string secretKey = "badFormatKey";
        string malformedMessageContent = "part1|part2";
        string fullMessageToEmbed = malformedMessageContent + ServiceEndMarker;

        byte[] embeddedImage = _steganographyService.EmbedMessage(originalImage, fullMessageToEmbed, secretKey);

        // Act & Assert
        var ex = Assert.Throws<FormatException>(() =>
            _steganographyService.ExtractFullMessage(embeddedImage, secretKey)
        );

        Assert.Contains("Invalid steganography data format. Expected 4 parts", ex.Message);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalid steganography data format.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ), Times.Once
        );
    }

    [Fact]
    public void ExtractFullMessage_InvalidTimestampFormat_ThrowsFormatException()
    {
        // Arrange
        byte[] originalImage = CreateDummyPngImage();
        string secretKey = "badTimestampKey";
        byte[] expectedMessage = Encoding.UTF8.GetBytes("Some data");
        string badTimestamp = "NOT-A-DATETIME";
        string expectedEncryptedAESKey = "Key";
        byte[] expectedAesIV = new byte[16];
        new Random().NextBytes(expectedAesIV);

        string fullMessageToEmbed = $"{Convert.ToBase64String(expectedMessage)}|{badTimestamp}|{expectedEncryptedAESKey}|{Convert.ToBase64String(expectedAesIV)}{ServiceEndMarker}";

        byte[] embeddedImage = _steganographyService.EmbedMessage(originalImage, fullMessageToEmbed, secretKey);

        // Act & Assert
        var ex = Assert.Throws<FormatException>(() =>
            _steganographyService.ExtractFullMessage(embeddedImage, secretKey)
        );

        Assert.Contains("not recognized", ex.Message, StringComparison.OrdinalIgnoreCase);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to parse timestamp:")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ), Times.Once
        );
    }


    [Fact]
    public void GetRandomImage_ImagesExist_ReturnsByteArray()
    {
        // Arrange
        string dummyImagePath = Path.Combine(_tempImageFolderPath, "dummy.png");
        File.WriteAllBytes(dummyImagePath, CreateDummyPngImage());

        // Act
        byte[] imageBytes = _steganographyService.GetRandomImage();

        // Assert
        Assert.NotNull(imageBytes);
        Assert.True(imageBytes.Length > 0);
    }

    [Fact]
    public void GetRandomImage_NoImagesExist_ThrowsFileNotFoundException()
    {
        // Arrange

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() =>
            _steganographyService.GetRandomImage()
        );

        Assert.Contains("No images found in the folder.", ex.Message);
    }
}
