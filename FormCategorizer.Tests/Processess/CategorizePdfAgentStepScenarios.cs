using FormCategorizer.Convertors;
using FormCategorizer.Events;
using FormCategorizer.Models;
using FormCategorizer.Proccesses;
using FormCategorizer.Services;
using FormCategorizer.Tests.MockServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using System.Text.Json;

namespace FormCategorizer.Tests.Processess
{
    public class CategorizePdfAgentStepScenarios
    {
        private Mock<IPDFtoImage> _mockPDFToImage;
        private Mock<IChatHistoryService> _mockHistoryService;
        private CategorizePdfAgentStep _categorizePdfAgentStep;
        private FormMetaData _formMetaData;

        public CategorizePdfAgentStepScenarios()
        {
            _mockPDFToImage = new Mock<IPDFtoImage>();
            _mockHistoryService = new Mock<IChatHistoryService>();
            _formMetaData = new() { FormType = "W2", FilePath = "E:\\TestPath\\dummy.pdf", ImageBytes = new(new byte[] { 1, 2, 3 }) };

            _mockPDFToImage
                .Setup(m => m.Convert(_formMetaData.FilePath)).Returns(_formMetaData.ImageBytes);

            _categorizePdfAgentStep = new(_mockPDFToImage.Object);
        }

        [Test]
        public async Task Should_Return_ValidResponse_EmitsEvent()
        {
            // Arrange  
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            LocalKernelProcessMessageChannel channel = new();
            KernelProcessStepContext context = new(channel);
            ChatHistory history = new ();
            Mock<IChatCompletionService> mockChatCompletion = new();
                        
            _mockHistoryService.Setup(m => m.GetHistoryAsync()).ReturnsAsync(history);
            mockChatCompletion
                .Setup(x => x.GetChatMessageContentsAsync(
                    It.IsAny<ChatHistory>(),
                    It.IsAny<PromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, JsonSerializer.Serialize(_formMetaData))]);

            kernelBuilder.Services.AddSingleton(_mockHistoryService.Object);
            kernelBuilder.Services.AddSingleton(mockChatCompletion.Object);

            ChatCompletionAgent agent = new() { Kernel = kernelBuilder.Build() };
            kernelBuilder.Services.AddKeyedSingleton(CategorizePdfAgentStep.AgentServiceKey, agent);

            // Act  
            await _categorizePdfAgentStep.CategorizeDocument(context, kernelBuilder.Build(), _formMetaData.FilePath);

            // Assert
            FormMetaData channelFormMetaData = (channel.EmmittedEvent.Data as FormMetaData) ?? new FormMetaData();

            Assert.Equals(channel.EmmittedEvent.Id, Categorization.GenerateStructure);
            Assert.Equals(channelFormMetaData.FormType, _formMetaData.FormType);
            Assert.Equals(channelFormMetaData.FilePath, _formMetaData.FilePath);
            Assert.Equals(channelFormMetaData.FileName, _formMetaData.FileName);
            Assert.Equals(channelFormMetaData.ImageBytes, _formMetaData.ImageBytes);
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
            kernelBuilder.Services.AddKeyedSingleton(CategorizePdfAgentStep.AgentServiceKey, agent);

            // Assert  
            Assert.ThrowsAsync<Exception>(_categorizePdfAgentStep.CategorizeDocument(context, kernelBuilder.Build(), _formMetaData.FilePath));
        }

        [Test]
        public async Task Should_Trow_Exception_If_Content_Is_Null()
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
                .ReturnsAsync([new ChatMessageContent(AuthorRole.User, "{}")]);

            kernelBuilder.Services.AddSingleton(_mockHistoryService.Object);
            kernelBuilder.Services.AddSingleton(mockChatCompletion.Object);

            ChatCompletionAgent agent = new() { Kernel = kernelBuilder.Build() };
            kernelBuilder.Services.AddKeyedSingleton(CategorizePdfAgentStep.AgentServiceKey, agent);

            // Assert  
            Assert.ThrowsAsync<Exception>(_categorizePdfAgentStep.CategorizeDocument(context, kernelBuilder.Build(), _formMetaData.FilePath));
        }
    }
}