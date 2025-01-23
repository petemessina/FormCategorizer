using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using FormCategorizer.Services;

namespace FormCategorizer.Extensions
{
    public static class KernelExtensions
    {
        public static TAgent GetAgent<TAgent>(this Kernel kernel, string key) where TAgent : KernelAgent =>
            kernel.Services.GetRequiredKeyedService<TAgent>(key);

        public static IChatHistoryService GetHistory(this Kernel kernel) =>
            kernel.Services.GetRequiredService<IChatHistoryService>();
    }
}
