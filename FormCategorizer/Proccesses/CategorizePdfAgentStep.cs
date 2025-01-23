using FormCategorizer.Convertors;
using FormCategorizer.Events;
using FormCategorizer.Extensions;
using FormCategorizer.Models;
using FormCategorizer.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace FormCategorizer.Proccesses
{
    public class CategorizePdfAgentStep : KernelProcessStep
    {
        private readonly IPDFtoImage _pdfToImageConverter;

        public const string AgentServiceKey = $"{nameof(CategorizePdfAgentStep)}:{nameof(AgentServiceKey)}";
        public static readonly OpenAIPromptExecutionSettings ExecutionSettings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), ResponseFormat = typeof(FormMetaData), Temperature = 0, TopP = 0 };
        public const string Name = "CPA";
        public const string Instructions = """
            You are a Principal CPA responsible for classifying tax form types.
            You are well versed in the different forms such as W2, 1098, 1099, and 1040. In order for the team to work on these documents you must classify them correctly every time.
            If there are any pages that are blank or are stated as blank do no categorize them and just respond with the word NONE.
            You only provide the form name nothing else. Always leave the ImageBytes field null.
        """;

        public CategorizePdfAgentStep(IPDFtoImage pdfToImageConverter) 
        {
            _pdfToImageConverter = pdfToImageConverter;
        }

        [KernelFunction]
        public async Task CategorizeDocument(KernelProcessStepContext context, Kernel kernel, string pdfFilePath)
        {
            ReadOnlyMemory<byte> imageBytes = _pdfToImageConverter.Convert(pdfFilePath);
            ChatCompletionAgent agent = kernel.GetAgent<ChatCompletionAgent>(AgentServiceKey);
            ImageContent imageContent = new(new BinaryData(imageBytes), "image/png");
            ChatMessageContent message = new(AuthorRole.User, items: [imageContent]);
            IChatHistoryService historyProvider = kernel.GetHistory();
            ChatHistory history = await historyProvider.GetHistoryAsync();

            history.Add(message);

            List<ChatMessageContent> response = await agent.InvokeAsync(history).ToListAsync();
            ChatMessageContent lastMessage = response.Last();

            if (lastMessage.Role != AuthorRole.Assistant)
            {
                throw new Exception($"{AuthorRole.Assistant} message was not returned: {lastMessage.Content}");
            }
            
            FormMetaData formMetaData = JsonSerializer.Deserialize<FormMetaData>(lastMessage.Content);

            if (formMetaData is null)
            {
                throw new Exception($"formMetaData was null");
            }

            formMetaData.FilePath = pdfFilePath;
            formMetaData.ImageBytes = imageBytes;

            await context.EmitEventAsync(new() 
            {
                Id = Categorization.GenerateStructure,
                Data = formMetaData
            });
        }
    }
}
