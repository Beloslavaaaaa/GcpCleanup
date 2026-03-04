using CommandLine;

namespace GcpCleanup.Cli
{
    public class CommandLineOptions
    {
        [Option('p', "project", Required = true, HelpText = "The Google Cloud Project ID.")]
        public string ProjectId { get; set; }

        [Option('m', "mode", Default = "interactive", HelpText = "Mode: list, interactive, dry-run, force.")]
        public string Mode { get; set; }

        [Option('t', "type", Default = "all", HelpText = "Type: gcs, compute, all.")]
        public string ResourceType { get; set; }

        [Option('a', "age", Default = 2, HelpText = "Minimum age in days.")]
        public int MinAge { get; set; }
    }
}