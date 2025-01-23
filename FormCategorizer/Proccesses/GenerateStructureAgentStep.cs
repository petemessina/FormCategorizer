using FormCategorizer.Models;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using FormCategorizer.Convertors;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System;
using FormCategorizer.Extensions;
using FormCategorizer.Services;
using FormCategorizer.Events;

namespace FormCategorizer.Proccesses
{
    public class GenerateStructureAgentStep : KernelProcessStep
    {
        private readonly IFiletoMarkdown _fileToMarkdownConverter;

        public const string AgentServiceKey = $"{nameof(GenerateStructureAgentStep)}:{nameof(AgentServiceKey)}";
        public static readonly OpenAIPromptExecutionSettings ExecutionSettings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(), ResponseFormat = typeof(FormData), Temperature = 0, TopP = 0 };
        public const string Name = "ContentExtractor";
        public const string Instructions = $"""
                Extract the data from the supplied image
                - If a value is not present, provide null.
                - Dates should be in the format YYYY-MM-DD.
                - Only respond in the Response JSON.
                - Use the image as the primary sorce of informaiton.
                - You can use the markdown to enhance your responses.
            """;

        public GenerateStructureAgentStep(
            IFiletoMarkdown fileToMarkdownConverter
        ) {
            _fileToMarkdownConverter = fileToMarkdownConverter;
        }

        [KernelFunction]
        public async Task CategorizeDocument(
            KernelProcessStepContext context,
            Kernel kernel,
            FormMetaData formMetaData
        ) {
            string markdown = await _fileToMarkdownConverter.Convert(formMetaData.FilePath);
            ChatCompletionAgent agent = kernel.GetAgent<ChatCompletionAgent>(AgentServiceKey);
            ImageContent imageContent = new(new BinaryData(formMetaData.ImageBytes), "image/png");
            ChatMessageContent message = new(AuthorRole.User, items: [imageContent]);
            IChatHistoryService historyProvider = kernel.GetHistory();
            ChatHistory history = await historyProvider.GetHistoryAsync();

            history.Add(message);
            history.AddUserMessage($"# Markdown{Environment.NewLine}{markdown}");
            List<ChatMessageContent> response = await agent.InvokeAsync(history).ToListAsync();
            ChatMessageContent lastMessage = response.Last();

            if (lastMessage.Role != AuthorRole.Assistant)
            {
                throw new Exception($"{AuthorRole.Assistant} message was not returned: {lastMessage.Content}");
            }

            FormData formData = JsonSerializer.Deserialize<FormData>(lastMessage.Content);
            
            await context.EmitEventAsync(new()
            {
                Id = Categorization.StopProcess,
                Data = formData
            });
        }
    }
}
