using Azure.AI.DocumentIntelligence;
using Azure;

namespace FormCategorizer.Convertors
{
    public interface IFiletoMarkdown
    {
        Task<string> Convert(string path);
    }

    public class FiletoMarkdown : IFiletoMarkdown
    {
        private readonly DocumentIntelligenceClient _documentIntelligenceClient;

        public FiletoMarkdown(DocumentIntelligenceClient documentIntelligenceClient) 
        {
            _documentIntelligenceClient = documentIntelligenceClient;
        }

        public async Task<string> Convert(string path)
        {
            var docFileBytes = File.ReadAllBytes(path);
            AnalyzeDocumentOptions content = new("prebuilt-layout", BinaryData.FromBytes(docFileBytes))
            {
                OutputContentFormat = DocumentContentFormat.Markdown
            };

            using var stream = new FileStream(path, FileMode.Open);
            var response = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, content);

            return response.Value.Content;
        }
    }
}
