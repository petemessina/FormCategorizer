namespace FormCategorizer.Models
{
    public class FormMetaData
    {
        public string FilePath { get; set; }

        public string FileName
        {
            get
            {
                return Path.GetFileName(FilePath);
            }
        }

        public string FormType { get; set; }

        public int PageNumber { get; set; }

        public ReadOnlyMemory<byte> ImageBytes { get; set; }
    }

    public class FormContent
    {
        public string FullFilePath { get; set; }

        public List<FormMetaData> Forms { get; } = new List<FormMetaData>();
    }
}
