using FormCategorizer.Events;
using FormCategorizer.Records;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace FormCategorizer.Proccesses
{
    public class ReadDirectoryStep : KernelProcessStep
    {
        private readonly FileReaderSettings _fileReaderSettings;

        public ReadDirectoryStep(FileReaderSettings fileReaderSettings) 
        {
            _fileReaderSettings = fileReaderSettings;
        }

        [KernelFunction]
        public async Task ReadDirectory(KernelProcessStepContext context, string directoryPath)
        {
            var filePaths = Directory.EnumerateFiles(directoryPath);

            
            foreach (var filePath in filePaths)
            {
                string fileExtension = Path.GetExtension(filePath);
                string eventId = _fileReaderSettings.AllowedImageExtensions.Contains(fileExtension) ? Categorization.CategorizImageFile : Categorization.CategorizePDFFile;

                await context.EmitEventAsync(new()
                {
                    Id = eventId,
                    Data = filePath
                });
            }
            
        }
    }
}
