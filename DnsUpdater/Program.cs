using Azure.ResourceManager.PrivateDns;
using System.Diagnostics;

public static class Program
{
    private static void Main()
    {
        Console.WriteLine("Start!");
        DnsUpdater.RenewDnsRecord();

        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }

    private static class DnsUpdater
    {
        private static readonly string dnsName;

        private static readonly string uamiResourceId;

        static DnsUpdater()
        {
            var dnsNameValue = Environment.GetEnvironmentVariable("DNS_NAME");
            if (string.IsNullOrEmpty(dnsNameValue))
            {
                throw new ArgumentNullException("DNS_NAME cannot be null or empty.");
            }
            var uamiResourceIdValue = Environment.GetEnvironmentVariable("UAMI_RESOURCE_ID");
            if (string.IsNullOrEmpty(uamiResourceIdValue))
            {
                throw new ArgumentNullException("UAMI_RESOURCE_ID cannot be null or empty.");
            }

            dnsName = dnsNameValue;
            uamiResourceId = uamiResourceIdValue;
        }

        internal static void RenewDnsRecord()
        {
            CreateDnsRecord();
        }

        private static void CreateDnsRecord()
        {
            ExecuteAzureCliCommand("az network private-dns record-set a add-record -g jericos-stuff-uaen -z jericos.stuff -n some-record-set -a \"1.2.3.4\"");
        }

        private static void ExecuteAzureCliCommand(string armCommand)
        {
            // Command to run
            string command = $"az login --identity --username \"{uamiResourceId}\" && {armCommand}";       // Create a new process
            Process process = new Process();
            process.StartInfo.FileName = "/bin/sh"; // Use bash to run the command
            process.StartInfo.Arguments = $"-c \"{command}\""; // Pass the command as an argument
            process.StartInfo.RedirectStandardOutput = true; // Redirect output
            process.StartInfo.UseShellExecute = false; // Don't use shell execute
            process.StartInfo.CreateNoWindow = true; // Don't create a window

            try
            {
                // Start the process
                process.Start();

                // Read the output
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(); // Wait for the process to exit

                // Print the output
                Console.WriteLine("Command Output:");
                Console.WriteLine(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
