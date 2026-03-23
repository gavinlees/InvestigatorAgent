namespace InvestigatorAgent.Agent;

/// <summary>
/// Contains system prompt definitions for the Investigator Agent.
/// </summary>
public static class SystemPrompts
{
    /// <summary>
    /// The primary system prompt that defines the Investigator Agent's role,
    /// purpose, and behaviour guidelines.
    /// Loaded from an external SYSTEM.md file.
    /// </summary>
    public static string InvestigatorAgent
    {
        get
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Agent", "SYSTEM.md");
            if (!File.Exists(filePath))
            {
                // Fallback or error
                return "Error: System prompt file (SYSTEM.md) not found.";
            }
            return File.ReadAllText(filePath);
        }
    }

    /// <summary>
    /// System prompt used to instruct the LLM on how to summarise conversation history.
    /// </summary>
    public static string ConversationSummarizer => 
        "Summarise the following conversation history between a User and an AI Assistant. " +
        "Ensure the summary is concise but retains key facts, decisions, and any Feature IDs or Jira Keys mentioned. " +
        "Maintain the current state of the investigation and any pending actions.";
}
