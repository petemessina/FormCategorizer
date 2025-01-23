using Microsoft.SemanticKernel.ChatCompletion;

namespace FormCategorizer.Services
{
    public interface IChatHistoryService
    {
        Task<ChatHistory> GetHistoryAsync();
    }
}
