namespace FormCategorizer.Events
{
    public class Categorization
    {
        public static readonly string ReadDirectory = nameof(ReadDirectory);
        public static readonly string CategorizePDFFile = nameof(CategorizePDFFile);
        public static readonly string CategorizImageFile = nameof(CategorizImageFile);
        public static readonly string GenerateStructure = nameof(GenerateStructure);
        public static readonly string StopProcess = nameof(StopProcess);
    }
}
