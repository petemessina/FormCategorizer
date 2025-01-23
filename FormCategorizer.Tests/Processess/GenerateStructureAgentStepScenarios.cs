using FormCategorizer.Convertors;
using FormCategorizer.Events;
using FormCategorizer.Models;
using FormCategorizer.Proccesses;
using FormCategorizer.Services;
using FormCategorizer.Tests.MockServices;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Moq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace FormCategorizer.Tests.Processess
{
    public class GenerateStructureAgentStepScenarios
    {
        private Mock<IFiletoMarkdown> _mockFiletoMarkdown;
        private Mock<IChatHistoryService> _mockHistoryService;
        private GenerateStructureAgentStep _generateStructureAgentStep;
        private FormMetaData _formMetaData;
        private FormData _formData;

        public GenerateStructureAgentStepScenarios()
        {
            _mockFiletoMarkdown = new Mock<IFiletoMarkdown>();
            _mockHistoryService = new Mock<IChatHistoryService>();
            _formMetaData = new() { FormType = "W2", FilePath = "E:\\TestPath\\dummy.pdf", ImageBytes = new(new byte[] { 1, 2, 3 }) };
            _formData = new() { EmployeeName = "Chimera Kronos", City = "Olympus" };

            _mockFiletoMarkdown
                .Setup(m => m.Convert(_formMetaData.FilePath)).ReturnsAsync("# Sample Markdown");

            _generateStructureAgentStep = new(_mockFiletoMarkdown.Object);
        }

        [Test]
        public async Task Should_Return_ValidResponse_EmitsEvent()
        {
            // Arrange  
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            LocalKernelProcessMessageChannel channel = new();
            KernelProcessStepContext context = new(channel);
            ChatHistory history = new();
            Mock<IChatCompletionService> mockChatCompletion = new();

            _mockHistoryService.Setup(m => m.GetHistoryAsync()).ReturnsAsync(history);
            mockChatCompletion
                .Setup(x => x.GetChatMessageContentsAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<PromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, JsonSerializer.Serialize(_formData))]);

            kernelBuilder.Services.AddSingleton(_mockHistoryService.Object);
            kernelBuilder.Services.AddSingleton(mockChatCompletion.Object);

            ChatCompletionAgent agent = new() { Kernel = kernelBuilder.Build() };
            kernelBuilder.Services.AddKeyedSingleton(GenerateStructureAgentStep.AgentServiceKey, agent);

            // Act  
            await _generateStructureAgentStep.CategorizeDocument(context, kernelBuilder.Build(), _formMetaData);

            // Assert
            FormMetaData channelFormMetaData = (channel.EmmittedEvent.Data as FormMetaData) ?? new FormMetaData();

            Assert.Equals(channel.EmmittedEvent.Id, Categorization.GenerateStructure);
            Assert.Equals(channelFormMetaData.FormType, _formData.EmployeeName);
            Assert.Equals(channelFormMetaData.FilePath, _formData.City);
        }

        [Test]
        public async Task Should_Trow_Exception_For_Last_User_Message()
        {
            // Arrange 
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            LocalKernelProcessMessageChannel channel = new();
            KernelProcessStepContext context = new(channel);
            ChatHistory history = new();
            Mock<IChatCompletionService> mockChatCompletion = new();

            _mockHistoryService.Setup(m => m.GetHistoryAsync()).ReturnsAsync(history);
            mockChatCompletion
                .Setup(x => x.GetChatMessageContentsAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<PromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new ChatMessageContent(AuthorRole.User, "Test Message")]);

            kernelBuilder.Services.AddSingleton(_mockHistoryService.Object);
            kernelBuilder.Services.AddSingleton(mockChatCompletion.Object);

            ChatCompletionAgent agent = new() { Kernel = kernelBuilder.Build() };
            kernelBuilder.Services.AddKeyedSingleton(GenerateStructureAgentStep.AgentServiceKey, agent);

            // Assert  
            Assert.ThrowsAsync<Exception>(_generateStructureAgentStep.CategorizeDocument(context, kernelBuilder.Build(), _formMetaData));
        }
    }
}
