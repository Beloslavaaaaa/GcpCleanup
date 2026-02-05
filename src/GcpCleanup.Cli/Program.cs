using System;

namespace GcpCleanup.Cli
{
    public class Program
    {
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                @"C:\Users\user_2\Downloads\graphic-pathway-348514-ee7c984bbc85.json");

            Console.WriteLine("GCP Cleanup tool started.");
            Console.ReadLine();
        }
    }
}
