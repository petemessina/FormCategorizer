using Microsoft.SemanticKernel.ChatCompletion;

namespace FormCategorizer.Services
{
    internal class ChatHistoryService : IChatHistoryService
    {
        private readonly ChatHistory _history;

        public ChatHistoryService(ChatHistory history) 
        {
            _history = history;
        }

        public Task<ChatHistory> GetHistoryAsync() => Task.FromResult(_history);
    }
}
