using InvestigatorAgent.Agent;
using InvestigatorAgent.Configuration;
using InvestigatorAgent.Persistence;
using InvestigatorAgent.Plugins;
using InvestigatorAgent.Utils;
using InvestigatorAgent.Observability;
using InvestigatorAgent.Resilience;
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

    // Configure open telemetry
    using var tracerProvider = TelemetrySetup.ConfigureTracing(settings);

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

    // 3. Initialise Agent Orchestrator & Register Plugins
    var mapper = new FeatureFolderMapper(settings.DataDirectory ?? "incoming_data/");
    var toolRetryPolicy = RetryPolicies.CreateToolRetryPolicy(settings.Retry ?? new RetryConfiguration());

    var jiraPlugin = new JiraPlugin(mapper, toolRetryPolicy);
    kernel.Plugins.AddFromObject(jiraPlugin, "JiraPlugin");

    var analysisPlugin = new AnalysisPlugin(settings.DataDirectory ?? "incoming_data/", mapper, toolRetryPolicy);
    kernel.Plugins.AddFromObject(analysisPlugin, "AnalysisPlugin");

    IConversationStore conversationStore = new FileConversationStore(settings.ConversationOutputDir ?? "conversations/");
    var agent = new AgentOrchestrator(kernel, conversationStore, settings);

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

            string response = await agent.SendMessageAsync(input, settings.Temperature);
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
