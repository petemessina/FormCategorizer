using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using FormCategorizer.Convertors;

namespace FormCategorizer.Tests.Converters
{
    public class PDFtoImageScenarios
    {
        [Test]
        public async Task Should_RerurnNonEmpty_ReadOnlyMemory()
        {
            // Arrange 
            string tempFilePath = CreateTemporaryPdfFile(); // Helper method to create a test PDF  
            var pdfToImage = new PDFtoImage();

            // Act  
            ReadOnlyMemory<byte> result = pdfToImage.Convert(tempFilePath);

            // Assert  
            await Assert.That(result).IsNotNull();
            await Assert.That(result.IsEmpty).IsFalse();

            // Cleanup  
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        [Test]
        public void Should_Throw_FileNotFoundException()
        {
            // Arrange  
            string invalidPath = "nonexistent.pdf";
            var pdfToImage = new PDFtoImage();

            // Act & Assert  
            Assert.Throws<FileNotFoundException>(() => pdfToImage.Convert(invalidPath));
        }

        private string CreateTemporaryPdfFile(bool empty = false)
        {
            // Helper method to create a temporary PDF file
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
            PdfDocumentBuilder builder = new();
            PdfPageBuilder page = builder.AddPage(PageSize.A4);
            PdfPoint pageTop = new(0, page.PageSize.Top);
            PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.TimesRoman);
            IReadOnlyList<Letter> letters = page.AddText("This is some text added to the output file near the top of the page.",
                12,
                pageTop.Translate(20, -25),
                font);

            byte[] fileBytes = builder.Build();
            File.WriteAllBytes(tempFilePath, fileBytes);

            return tempFilePath;
        }
    }
}
