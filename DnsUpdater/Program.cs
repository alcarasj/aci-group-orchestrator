using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.PrivateDns;
using Azure.ResourceManager.PrivateDns.Models;
using System.Diagnostics;

public static class Program
{
    private static void Main()
    {
        Console.WriteLine("Start!");

        // Command to run
        string command = "az login --identity --username \"4ea00b67-f860-4b9b-829a-d7519ca5f350\" && az network private-dns record-set a add-record -g jericos-stuff-uaen -z jericos.stuff -n some-record-set -a \"1.2.3.4\""; // You can replace this with any command you want

        // Create a new process
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
        
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}
