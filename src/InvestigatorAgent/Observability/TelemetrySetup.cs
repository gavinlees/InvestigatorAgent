using System.Text;
using InvestigatorAgent.Configuration;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace InvestigatorAgent.Observability;

/// <summary>
/// Handles the setup and configuration of OpenTelemetry for the agent.
/// </summary>
public static class TelemetrySetup
{
    /// <summary>
    /// Configures OpenTelemetry tracing to export Semantic Kernel spans to Langfuse.
    /// </summary>
    /// <param name="settings">The agent settings containing Langfuse credentials.</param>
    /// <returns>The configured TracerProvider, or null if setup fails or is disabled.</returns>
    public static TracerProvider? ConfigureTracing(AgentSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.LangfuseBaseUrl) || 
            string.IsNullOrWhiteSpace(settings.LangfusePublicKey) || 
            string.IsNullOrWhiteSpace(settings.LangfuseSecretKey))
        {
            Console.WriteLine("Warning: Langfuse observability configuration is incomplete. Tracing is disabled.");
            return null;
        }

        // Enable Semantic Kernel diagnostic output for LLM spans
        AppContext.SetSwitch(
            "Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive",
            true);

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService("InvestigatorAgent");

        return Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource("Microsoft.SemanticKernel*")
            .AddSource("InvestigatorAgent.*")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri($"{settings.LangfuseBaseUrl.TrimEnd('/')}/api/public/otel/v1/traces");
                options.Headers = $"Authorization=Basic {Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{settings.LangfusePublicKey}:{settings.LangfuseSecretKey}"))}";
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            })
            .Build();
    }
}
