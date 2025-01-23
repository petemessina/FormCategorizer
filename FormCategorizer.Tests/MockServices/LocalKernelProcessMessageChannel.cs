using Microsoft.SemanticKernel;

namespace FormCategorizer.Tests.MockServices
{
    public class LocalKernelProcessMessageChannel : IKernelProcessMessageChannel
    {
        public KernelProcessEvent EmmittedEvent { get; internal set; }
        public ValueTask EmitEventAsync(KernelProcessEvent processEvent)
        {
            EmmittedEvent = processEvent;
            return ValueTask.CompletedTask;
        }
    }
}
