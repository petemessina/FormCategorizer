using Azure.AI.DocumentIntelligence;
using Azure;
using Moq;
using FormCategorizer.Convertors;

namespace FormCategorizer.Tests.Converters
{
    public class FiletoMarkdownScenarios
    {
        [Test]
        public async Task Should_Return_Markdown()
        {
            // Arrange  
            string filePath = "testfile.pdf";
            string expectedMarkdownContent = "# Markdown Content";
            var uriSource = new Uri("https://fakeuri.com/");
            var expectedResult = new Mock<Operation<AnalyzeResult>>();
            var mockDocumentIntelligenceClient = new Mock<DocumentIntelligenceClient>();
            var mockAnalyzeDocumentResult = new Mock<Response<Operation<AnalyzeResult>>>();
            var analyzeResult = CreateMockAnalyzeResult(expectedMarkdownContent);

            expectedResult.SetupGet(op => op.Value).Returns(analyzeResult);
            mockDocumentIntelligenceClient
                .Setup(client => client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    It.IsAny<AnalyzeDocumentOptions>(),
                    default))
                .ReturnsAsync(expectedResult.Object);

            var fileToMarkdown = new FiletoMarkdown(mockDocumentIntelligenceClient.Object);

            File.WriteAllText(filePath, "Dummy PDF Content");

            // Act  
            var result = await fileToMarkdown.Convert(filePath);

            // Assert  
            await Assert.That(expectedMarkdownContent).IsEqualTo(result);

            // Cleanup  
            File.Delete(filePath);
        }

        [Test]
        public async Task Should_ThrowsException_WhenFileDoesNotExist()
        {
            // Arrange  
            string filePath = "nonexistentfile.pdf";

            var mockDocumentIntelligenceClient = new Mock<DocumentIntelligenceClient>();
            var fileToMarkdown = new FiletoMarkdown(mockDocumentIntelligenceClient.Object);

            // Act & Assert  
            await Assert.ThrowsAsync<FileNotFoundException>(async () => await fileToMarkdown.Convert(filePath));
        }

        [Test]
        public async Task Should_ThrowsException_WhenDocumentAnalysisFails()
        {
            // Arrange  
            string filePath = "document_failure_file.pdf";
            var mockDocumentIntelligenceClient = new Mock<DocumentIntelligenceClient>();

            mockDocumentIntelligenceClient
                .Setup(client => client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    It.IsAny<AnalyzeDocumentOptions>(),
                    default))
                .ThrowsAsync(new RequestFailedException("Document analysis failed"));

            var fileToMarkdown = new FiletoMarkdown(mockDocumentIntelligenceClient.Object);

            File.WriteAllText(filePath, "Dummy PDF Content");

            // Act & Assert  
            await Assert.ThrowsAsync<RequestFailedException>(async () => await fileToMarkdown.Convert(filePath));

            // Cleanup  
            File.Delete(filePath);
        }

        private AnalyzeResult CreateMockAnalyzeResult(string content)
        {   
            var document = DocumentIntelligenceModelFactory.AnalyzedDocument("groceries:groceries");
            var documents = new List<AnalyzedDocument>() { document };

            return DocumentIntelligenceModelFactory.AnalyzeResult("groceries", content: content);
        }
    }
}