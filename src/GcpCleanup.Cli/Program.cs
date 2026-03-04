using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using GcpCleanup.Services;
using GcpCleanup.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GcpCleanup.Cli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                ConfigureAuthentication();

                return await Parser.Default.ParseArguments<CommandLineOptions>(args)
                    .MapResult(
                        (CommandLineOptions opts) => RunAsync(opts),
                        errs => Task.FromResult(1));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL ERROR] {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static void ConfigureAuthentication()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("K_SERVICE")))
            {
                var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

                string keyPath = isDocker
                    ? "/app/config/key.json"
                    : @"C:\Users\user_2\Downloads\graphic-pathway-348514-ee7c984bbc85.json";

                if (System.IO.File.Exists(keyPath))
                {
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyPath);
                }
            }
        }

        static async Task<int> RunAsync(CommandLineOptions options)
        {
            var serviceProvider = ConfigureServices().BuildServiceProvider();

            string projectId = options.ProjectId ?? GetProjectId();
            if (string.IsNullOrEmpty(projectId))
            {
                Console.WriteLine("Error: Could not resolve Project ID. Use --project or set GOOGLE_CLOUD_PROJECT.");
                return 1;
            }

            var type = MapResourceType(options.ResourceType);

            if (options.Mode.ToLower() == "list")
            {
                return await HandleListCommand(serviceProvider, projectId, type, options);
            }
            else
            {
                return await HandleCleanupCommand(serviceProvider, projectId, type, options);
            }
        }

        private static async Task<int> HandleListCommand(IServiceProvider sp, string projectId, ResourceType type, CommandLineOptions opts)
        {
            var services = sp.GetServices<IResourceService>();
            var validator = sp.GetRequiredService<CleanupEligibilityValidator>();
            var criteria = new CleanupCriteria { MinAgeDays = opts.MinAge };

            var allResources = new List<CleanupResource>();
            foreach (var service in services)
            {
                allResources.AddRange(await service.GetResourcesAsync(projectId));
            }

            ConsoleHelper.DisplayResourceTable(allResources, validator, criteria);
            return 0;
        }

        private static async Task<int> HandleCleanupCommand(IServiceProvider sp, string projectId, ResourceType type, CommandLineOptions opts)
        {
            var cleanupService = sp.GetRequiredService<CleanupService>();
            var criteria = new CleanupCriteria { MinAgeDays = opts.MinAge };

            if (opts.Mode.ToLower() == "dry-run")
                Console.WriteLine("NOTICE: Running in DRY-RUN mode. No deletions will occur.");

            var summary = await cleanupService.RunCleanupAsync(projectId, criteria, opts.Mode, type);
            ConsoleHelper.DisplaySummary(summary);
            return 0;
        }

        private static IServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => {
                builder.AddFilter("GcpCleanup", LogLevel.Warning);
                builder.AddConsole();
            });

            services.AddSingleton<CleanupEligibilityValidator>();
            services.AddTransient<IResourceService, GcsResourceService>();
            services.AddTransient<IResourceService, ComputeResourceService>();

            services.AddTransient(sp => new CleanupService(
                sp.GetServices<IResourceService>(),
                sp.GetRequiredService<ILogger<CleanupService>>()
            ));

            return services;
        }

        private static string GetProjectId()
        {
            var envId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
            if (!string.IsNullOrEmpty(envId)) return envId;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "gcloud",
                    Arguments = "config get-value project",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                return process?.StandardOutput.ReadToEnd().Trim();
            }
            catch { return null; }
        }

        private static ResourceType MapResourceType(string type) => type.ToLower() switch
        {
            "gcs" => ResourceType.GcsBucket,
            "compute" => ResourceType.ComputeInstance,
            _ => ResourceType.All
        };
    }
}