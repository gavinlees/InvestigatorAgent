using InvestigatorAgent.Agent;
using InvestigatorAgent.Configuration;
using Microsoft.SemanticKernel;

/// <summary>
/// The entry point for the Investigator Agent CLI application.
/// Initialises configuration, builds the Semantic Kernel, and starts the REPL loop.
/// </summary>
try
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("========================================");
    Console.WriteLine("     Investigator Agent (CLI)           ");
    Console.WriteLine("========================================");
    Console.ResetColor();

    // 1. Load configuration
    AgentSettings settings = ConfigurationLoader.Load();

    // 2. Build Semantic Kernel with OpenRouter (OpenAI connector)
    var builder = Kernel.CreateBuilder();

    #pragma warning disable SKEXP0010 // OpenAI connector is currently experimental
    builder.AddOpenAIChatCompletion(
        modelId: settings.ModelName,
        apiKey: settings.OpenRouterApiKey,
        endpoint: new Uri("https://openrouter.ai/api/v1")
    );
    #pragma warning restore SKEXP0010

    Kernel kernel = builder.Build();

    // 3. Initialise Agent Orchestrator
    var agent = new AgentOrchestrator(kernel);

    Console.WriteLine("\nAgent initialised. Type 'exit' or 'quit' to end the session.\n");

    // 4. REPL Loop
    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("You: ");
        Console.ResetColor();

        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            continue;
        }

        if (input.Trim().ToLowerInvariant() is "exit" or "quit")
        {
            Console.WriteLine("\nGoodbye!");
            break;
        }

        try
        {
            string response = await agent.SendMessageAsync(input);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Agent: ");
            Console.ResetColor();
            Console.WriteLine(response);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Agent Error]: {ex.Message}");
            Console.ResetColor();
        }
    }
}
catch (InvalidOperationException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[Configuration Error]: {ex.Message}");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[Fatal Error]: {ex.Message}");
    Console.ResetColor();
}
