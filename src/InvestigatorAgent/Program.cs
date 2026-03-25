using InvestigatorAgent.Agent;
using InvestigatorAgent.Configuration;
using InvestigatorAgent.Persistence;
using InvestigatorAgent.Plugins;
using InvestigatorAgent.Utils;
using InvestigatorAgent.Observability;
using InvestigatorAgent.Resilience;
using InvestigatorAgent.Evaluation;
using Microsoft.Extensions.Logging;
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

    var planningPlugin = new PlanningPlugin(settings.DataDirectory ?? "incoming_data/", mapper, toolRetryPolicy);
    kernel.Plugins.AddFromObject(planningPlugin, "PlanningPlugin");

    // Conditionally load Graphiti plugin if MCP URL is configured
    GraphitiPlugin? graphitiPlugin = null;
    if (!string.IsNullOrWhiteSpace(settings.GraphitiMcpUrl))
    {
        Console.WriteLine("Initializing Graphiti MCP knowledge graph connection...");
        try
        {
            graphitiPlugin = await GraphitiPlugin.CreateAsync(settings.GraphitiMcpUrl, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            kernel.Plugins.AddFromObject(graphitiPlugin, "GraphitiPlugin");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Warning] Failed to initialize Graphiti plugin: {ex.Message}");
            Console.ResetColor();
        }
    }

    IConversationStore conversationStore = new FileConversationStore(settings.ConversationOutputDir ?? "conversations/");
    var agent = new AgentOrchestrator(kernel, conversationStore, settings);

    Console.WriteLine($"\nAgent initialised with model: {settings.ModelName}");
    
    // Evaluation Mode Check
    if (args.Contains("--eval"))
    {
        bool createBaseline = args.Contains("--create-baseline");
        var evalRunner = new EvaluationRunner(agent, settings);
        await evalRunner.RunEvaluationAsync(createBaseline: createBaseline);
        return;
    }

    // Ingestion Mode Check
    if (args.Contains("--ingest"))
    {
        if (graphitiPlugin == null)
        {
            Console.WriteLine("Cannot ingest data: Graphiti plugin failed to initialize or GraphitiMcpUrl is missing.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Starting data ingestion into knowledge graph...");
        Console.ResetColor();

        string dataDir = settings.DataDirectory ?? "incoming_data/";
        string[] textFiles = Directory.GetFiles(dataDir, "*.md", SearchOption.AllDirectories);
        
        foreach (var file in textFiles)
        {
            string fileName = Path.GetFileName(file);
            Console.WriteLine($"Ingesting {fileName}...");
            string content = await File.ReadAllTextAsync(file);
            var result = await graphitiPlugin.AddKnowledgeAsync(fileName, content);
            Console.WriteLine($"  -> Done.");
        }
        Console.WriteLine("\nIngestion complete!");
        return;
    }

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
