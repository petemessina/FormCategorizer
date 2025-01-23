using Azure.AI.DocumentIntelligence;
using Azure;
using FormCategorizer.Convertors;
using FormCategorizer.Proccesses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FormCategorizer.Events;
using FormCategorizer.Models;
using FormCategorizer.Records;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FormCategorizer.Services
{
    internal class CategorizationHostService : IHostedService
    {
        private readonly FileReaderSettings _fileReaderSettings;
        private readonly AzureOpenAISettings _azureOpenAISettings;
        private readonly DocumentIntelligenceSettings _documentIntelligenceSettings;

        public CategorizationHostService(
            IOptions<FileReaderSettings> fileReaderSettings,
            IOptions<AzureOpenAISettings> azureOpenAISettings,
            IOptions<DocumentIntelligenceSettings> documentIntelligenceSettings
        ) {
            _fileReaderSettings = fileReaderSettings.Value;
            _azureOpenAISettings = azureOpenAISettings.Value;
            _documentIntelligenceSettings = documentIntelligenceSettings.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ProcessBuilder process = new("TaxFormStructure");
            ChatHistory history = [];
            Kernel kernel = SetupKernel(history);
            ProcessStepBuilder readDirectoryStep = process.AddStepFromType<ReadDirectoryStep>();
            ProcessStepBuilder categorizeImageAgentStep = process.AddStepFromType<CategorizeImageAgentStep>();
            ProcessStepBuilder categorizePdfAgentStep = process.AddStepFromType<CategorizePdfAgentStep>();
            ProcessStepBuilder generateStructureAgentStep = process.AddStepFromType<GenerateStructureAgentStep>();

            process.OnInputEvent(Categorization.ReadDirectory)
                .SendEventTo(new ProcessFunctionTargetBuilder(readDirectoryStep, parameterName: "directoryPath"));

            readDirectoryStep.OnEvent(Categorization.CategorizePDFFile)
                .SendEventTo(new ProcessFunctionTargetBuilder(categorizePdfAgentStep));

            readDirectoryStep.OnEvent(Categorization.CategorizImageFile)
                .SendEventTo(new ProcessFunctionTargetBuilder(categorizeImageAgentStep));

            categorizePdfAgentStep.OnEvent(Categorization.GenerateStructure)
                .SendEventTo(new ProcessFunctionTargetBuilder(generateStructureAgentStep));

            categorizeImageAgentStep.OnEvent(Categorization.GenerateStructure)
                .SendEventTo(new ProcessFunctionTargetBuilder(generateStructureAgentStep));

            KernelProcess kernelProcess = process.Build();

            using LocalKernelProcessContext localProcess = await kernelProcess.StartAsync(
                kernel,
                new KernelProcessEvent()
                    {
                        Id = Categorization.ReadDirectory,
                        Data = _fileReaderSettings.FolderPath
                });
        }

        private Kernel SetupKernel(ChatHistory history)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                _azureOpenAISettings.ChatDeploymentName,
                _azureOpenAISettings.Endpoint,
                _azureOpenAISettings.Key);


            var documentIntelligenceClient = new DocumentIntelligenceClient(
                new Uri(_documentIntelligenceSettings.Endpoint),
                new AzureKeyCredential(_documentIntelligenceSettings.Key)
            );

            builder.Services.AddSingleton(_fileReaderSettings);
            builder.Services.AddSingleton<IImagetoBytes, ImagetoBytes>();
            builder.Services.AddSingleton<IPDFtoImage, PDFtoImage>();
            builder.Services.AddSingleton<IFiletoMarkdown, FiletoMarkdown>();
            builder.Services.AddSingleton<IChatHistoryService>(new ChatHistoryService(history));
            builder.Services.AddSingleton(documentIntelligenceClient);

            SetupAgents(builder, builder.Build());

            return builder.Build();
        }

        private static void SetupAgents(IKernelBuilder builder, Kernel kernel)
        {
            ChatCompletionAgent cpaAgent = CreateAgent(
                CategorizePdfAgentStep.Name,
                CategorizePdfAgentStep.Instructions,
                kernel.Clone(), 
                CategorizePdfAgentStep.ExecutionSettings
            );

            ChatCompletionAgent generateStructureAgent = CreateAgent(
                GenerateStructureAgentStep.Name,
                GenerateStructureAgentStep.Instructions,
                kernel.Clone(),
                GenerateStructureAgentStep.ExecutionSettings
            );

            builder.Services.AddKeyedSingleton(CategorizePdfAgentStep.AgentServiceKey, cpaAgent);
            builder.Services.AddKeyedSingleton(GenerateStructureAgentStep.AgentServiceKey, generateStructureAgent);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static ChatCompletionAgent CreateAgent(string name, string instructions, Kernel kernel, OpenAIPromptExecutionSettings openAIPromptExecutionSettings = null)
        {
            var promptExecutionSettings = openAIPromptExecutionSettings ?? new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = 0,
            };

            return new()
            {
                Name = name,
                Instructions = instructions,
                Kernel = kernel.Clone(),
                Arguments = new KernelArguments(promptExecutionSettings)
            };
        }
    }
}
