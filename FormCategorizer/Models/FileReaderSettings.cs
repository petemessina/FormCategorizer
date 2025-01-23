namespace FormCategorizer.Records
{
    public class FileReaderSettings
    {
        public string FolderPath { get; set; }

        public List<string> AllowedImageExtensions { get; set; } = new List<string>();
    }
}
