using System.Drawing;
using System.Drawing.Imaging;

namespace FormCategorizer.Convertors
{
    public interface IImagetoBytes
    {
        ReadOnlyMemory<byte> Convert(string imageFilePath);
    }

    public class ImagetoBytes : IImagetoBytes
    {
        public ReadOnlyMemory<byte> Convert(string imageFilePath)
        {
            using (Image image = Image.FromFile(imageFilePath))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, ImageFormat.Png);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}