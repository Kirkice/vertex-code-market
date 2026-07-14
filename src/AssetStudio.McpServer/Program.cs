using AssetStudio.McpServer.Services;
using AssetStudio.McpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AssetStudio.McpServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging to stderr (stdout is used for MCP stdio protocol)
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new StdErrLoggerProvider());

        // Register services
        builder.Services.AddSingleton<AssetManagerService>();
        builder.Services.AddSingleton<ExportService>();

        // Register MCP Server
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            // Basic tools
            .WithTools<LoadAssetsTool>()
            .WithTools<LoadApkTool>()
            .WithTools<ListAssetsTool>()
            .WithTools<GetAssetInfoTool>()
            .WithTools<SearchAssetsTool>()
            .WithTools<ExportAssetTool>()
            .WithTools<ExtractBundleTool>()
            .WithTools<GetTypeTreeTool>()
            // P0: Core analysis
            .WithTools<AnalyzeShaderTool>()
            .WithTools<AnalyzeTextureTool>()
            .WithTools<AnalyzeMeshTool>()
            .WithTools<GetHierarchyTool>()
            .WithTools<GetAssetStatsTool>()
            .WithTools<ClearAssetsTool>()
            // P1: Deep analysis
            .WithTools<GetMaterialInfoTool>()
            .WithTools<GetRendererInfoTool>()
            .WithTools<AnalyzeAnimationTool>()
            .WithTools<GetMemoryProfileTool>()
            // P2: Advanced capabilities
            .WithTools<DecompileShaderTool>()
            .WithTools<BatchTextureReportTool>()
            .WithTools<GetBundleDependenciesTool>()
            .WithTools<DumpMonoBehaviourTool>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
