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
}
