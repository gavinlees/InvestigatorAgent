using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace InvestigatorAgent.Plugins;

/// <summary>
/// A Semantic Kernel plugin that interfaces with the Graphiti MCP Server
/// to provide knowledge graph capabilities to the agent.
/// </summary>
public sealed class GraphitiPlugin : IAsyncDisposable
{
    private readonly McpClient _mcpClient;
    private readonly ILogger _logger;
    private bool _isDisposed;

    private GraphitiPlugin(McpClient mcpClient, ILogger logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    /// <summary>
    /// Factory method to create and initialize the plugin with an active MCP connection.
    /// </summary>
    public static async Task<GraphitiPlugin> CreateAsync(string mcpUrl, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<GraphitiPlugin>();
        
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(mcpUrl),
            TransportMode = HttpTransportMode.Sse,
            MaxReconnectionAttempts = 5,
            DefaultReconnectionInterval = TimeSpan.FromSeconds(1)
        });

        var client = await McpClient.CreateAsync(transport);
        logger.LogInformation("Successfully connected to Graphiti MCP Server at {Url}", mcpUrl);
        
        return new GraphitiPlugin(client, logger);
    }

    [KernelFunction("search_knowledge_graph")]
    [Description("Search the knowledge graph for relevant entities (nodes) based on a query. Use this to find what entities exist in memory.")]
    public async Task<string> SearchNodesAsync(
        [Description("The search query to find relevant entities")] string query,
        [Description("Maximum number of nodes to return (default: 5)")] int maxNodes = 5)
    {
        _logger.LogInformation("Executing search_memory_nodes via Graphiti MCP. Query: '{Query}'", query);

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["query"] = query,
                ["max_nodes"] = maxNodes
            };

            var result = await _mcpClient.CallToolAsync("search_memory_nodes", parameters, cancellationToken: CancellationToken.None);
            
            if (result.IsError == true)
            {
                var errorMsg = ExtractTextContent(result.Content);
                _logger.LogError("Graphiti MCP Error (search_nodes): {Error}", errorMsg);
                return $"Error searching knowledge graph: {errorMsg}";
            }

            return ExtractTextContent(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call search_memory_nodes tool");
            return $"Error connecting to knowledge graph: {ex.Message}";
        }
    }

    [KernelFunction("search_facts")]
    [Description("Search the knowledge graph for relevant facts (relationships between entities). Use this to understand how entities connect.")]
    public async Task<string> SearchFactsAsync(
        [Description("The search query to find relevant facts/relationships")] string query,
        [Description("Maximum number of facts to return (default: 10)")] int maxFacts = 10)
    {
        _logger.LogInformation("Executing search_memory_facts via Graphiti MCP. Query: '{Query}'", query);

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["query"] = query,
                ["max_facts"] = maxFacts
            };

            var result = await _mcpClient.CallToolAsync("search_memory_facts", parameters, cancellationToken: CancellationToken.None);
            
            if (result.IsError == true)
            {
                var errorMsg = ExtractTextContent(result.Content);
                _logger.LogError("Graphiti MCP Error (search_facts): {Error}", errorMsg);
                return $"Error searching facts: {errorMsg}";
            }

            return ExtractTextContent(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call search_memory_facts tool");
            return $"Error connecting to knowledge graph: {ex.Message}";
        }
    }

    [KernelFunction("add_knowledge")]
    [Description("Add new information (an 'episode') to the knowledge graph. The system will automatically extract entities and facts from this information.")]
    public async Task<string> AddKnowledgeAsync(
        [Description("A short, descriptive name for this piece of information")] string name,
        [Description("The detailed text content of the information block to memorize")] string content)
    {
        _logger.LogInformation("Executing add_memory via Graphiti MCP. Name: '{Name}'", name);

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["name"] = name,
                ["episode_body"] = content,
                ["source"] = "text",
                ["source_description"] = "Added by Investigator Agent"
            };

            var result = await _mcpClient.CallToolAsync("add_memory", parameters, cancellationToken: CancellationToken.None);
            
            if (result.IsError == true)
            {
                var errorMsg = ExtractTextContent(result.Content);
                _logger.LogError("Graphiti MCP Error (add_memory): {Error}", errorMsg);
                return $"Error adding memory to knowledge graph: {errorMsg}";
            }

            return ExtractTextContent(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call add_memory tool");
            return $"Error connecting to knowledge graph: {ex.Message}";
        }
    }

    [KernelFunction("get_recent_episodes")]
    [Description("Retrieve the most recently added episodes (raw information blocks) from the knowledge graph.")]
    public async Task<string> GetRecentEpisodesAsync(
        [Description("The number of recent episodes to retrieve (default: 5)")] int limit = 5)
    {
        _logger.LogInformation("Executing get_episodes via Graphiti MCP. Limit: {Limit}", limit);

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["last_n"] = limit
            };

            var result = await _mcpClient.CallToolAsync("get_episodes", parameters, cancellationToken: CancellationToken.None);
            
            if (result.IsError == true)
            {
                var errorMsg = ExtractTextContent(result.Content);
                _logger.LogError("Graphiti MCP Error (get_episodes): {Error}", errorMsg);
                return $"Error retrieving recent episodes: {errorMsg}";
            }

            return ExtractTextContent(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call get_episodes tool");
            return $"Error connecting to knowledge graph: {ex.Message}";
        }
    }

    private static string ExtractTextContent(object content)
    {
        try
        {
            if (content is System.Collections.IEnumerable list)
            {
                var sb = new System.Text.StringBuilder();
                foreach (dynamic item in list)
                {
                    try { sb.AppendLine(item.Text?.ToString()); } catch { }
                }
                var text = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(text)) return text;
            }
        }
        catch { }
        return content?.ToString() ?? string.Empty;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        if (_mcpClient is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        
        _isDisposed = true;
    }
}
