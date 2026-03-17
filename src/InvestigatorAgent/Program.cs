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

    // 1. Load environment and configuration
    ConfigurationLoader.LoadEnv();
    AgentSettings settings = ConfigurationLoader.Load();

    // 2. Build Semantic Kernel (encapsulated helper)
    Kernel kernel;
    if (!string.IsNullOrWhiteSpace(settings.GoogleApiKey))
    {
        kernel = AgentOrchestrator.CreateGoogleKernel(settings.ModelName, settings.GoogleApiKey);
    }
    else
    {
        kernel = AgentOrchestrator.CreateOpenRouterKernel(settings.ModelName, settings.OpenRouterApiKey!);
    }

    // 3. Initialise Agent Orchestrator
    var agent = new AgentOrchestrator(kernel);

    Console.WriteLine($"\nAgent initialised with model: {settings.ModelName}");
    Console.WriteLine("Type 'exit' or 'quit' to end the session.\n");

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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Agent: ");
            Console.ResetColor();

            // Use the streaming method to write chunks as they arrive
            await foreach (var chunk in agent.SendMessageStreamAsync(input, settings.Temperature))
            {
                Console.Write(chunk);
            }
            
            Console.WriteLine();
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
