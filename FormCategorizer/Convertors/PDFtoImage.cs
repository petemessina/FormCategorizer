using FormCategorizer.Models;
using Spire.Pdf.Graphics;
using Spire.Pdf;
using System.Drawing.Imaging;
using System.Drawing;

namespace FormCategorizer.Convertors
{
    public interface IPDFtoImage
    {
        ReadOnlyMemory<byte> Convert(string path);
    }

    public class PDFtoImage : IPDFtoImage
    {
        public ReadOnlyMemory<byte> Convert(string path)
        {
            PdfDocument pdf = new PdfDocument();
            FormContent formContent = new() { FullFilePath = path };
            ReadOnlyMemory<byte> readonlyImageBytes = null;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File was not found.", path);
            }

            pdf.LoadFromFile(path);

            for (int i = 0; i < pdf.Pages.Count; i++)
            {
                Image image = pdf.SaveAsImage(i, PdfImageType.Bitmap, 500, 500);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, ImageFormat.Png);

                    byte[] imageBytes = memoryStream.ToArray();
                    readonlyImageBytes = new(imageBytes);
                }

            }

            return readonlyImageBytes;
        }
    }
}
