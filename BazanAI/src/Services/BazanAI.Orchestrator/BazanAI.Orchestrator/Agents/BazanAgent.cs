
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace BazanAI.Orchestrator.Agents;

public class BazanAgent
{
    private readonly Kernel _kernel;

    public BazanAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ProcessAsync(string input)
    {
        var result = await _kernel.InvokePromptAsync(input);
        return result.GetValue<string>() ?? string.Empty;
    }
}
