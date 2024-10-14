using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.PrivateDns;
using Azure.ResourceManager.PrivateDns.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public static class Program
{
    private static void Main()
    {
        Console.WriteLine("Start!");
        DnsUpdater.UpdateDnsRecord();

        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }

    private static class DnsUpdater
    {
        private static readonly string domainName;

        private static readonly string dnsZoneName;

        private static readonly string uamiResourceId;

        private static readonly ArmClient armClient;

        static DnsUpdater()
        {
            var domainNameValue = Environment.GetEnvironmentVariable("DOMAIN_NAME");
            if (string.IsNullOrEmpty(domainNameValue))
            {
                throw new ArgumentNullException("DOMAIN_NAME cannot be null or empty.");
            }
            var uamiResourceIdValue = Environment.GetEnvironmentVariable("UAMI_RESOURCE_ID");
            if (string.IsNullOrEmpty(uamiResourceIdValue))
            {
                throw new ArgumentNullException("UAMI_RESOURCE_ID cannot be null or empty.");
            }
            var dnsZoneNameValue = Environment.GetEnvironmentVariable("DNS_ZONE_NAME");
            if (string.IsNullOrEmpty(dnsZoneNameValue))
            {
                throw new ArgumentNullException("DNS_ZONE_NAME cannot be null or empty.");
            }

            uamiResourceId = uamiResourceIdValue;
            domainName = domainNameValue;
            dnsZoneName = dnsZoneNameValue;

            TokenCredential credentials;
            var isVsDebugMode = Environment.GetEnvironmentVariable("VS_DEBUG_MODE");
            if (isVsDebugMode == "true")
            {
                var options = new InteractiveBrowserCredentialOptions() { TenantId = "e272c4af-9772-49f8-b060-a283d2a31cdf" };
                credentials = new InteractiveBrowserCredential(options);
            }
            else
            {
                credentials = new ManagedIdentityCredential();
            }
            armClient = new ArmClient(credentials);
        }

        internal static void UpdateDnsRecord()
        {
            var dnsRecordResourceId = PrivateDnsARecordResource.CreateResourceIdentifier(
                "e0f91dc0-102c-41ae-a3b3-d256a2ee118d",
                "jericos-stuff-uaen",
                dnsZoneName,
                domainName
            );
            var containerGroupIp = GetLocalIPAddress();

            try
            {

                var dnsRecord = armClient.GetPrivateDnsARecordResource(dnsRecordResourceId).Get().Value;
                var dnsRecordData = dnsRecord.Data;
                var newIp = new PrivateDnsARecordInfo();
                newIp.IPv4Address = containerGroupIp;
                dnsRecordData.PrivateDnsARecords.Clear();
                dnsRecordData.PrivateDnsARecords.Add(newIp);
                dnsRecord.Update(dnsRecordData);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Record {domainName} not found, creating...");
                CreateDnsRecord(containerGroupIp);
            }
        }

        private static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void CreateDnsRecord(IPAddress ip) => ExecuteAzureCliCommand($"az network private-dns record-set a add-record -g jericos-stuff-uaen -z {dnsZoneName} -n {domainName} -a {ip}");

        private static void ExecuteAzureCliCommand(string armCommand)
        {
            string command = $"az login --identity --username \"{uamiResourceId}\" && {armCommand}";
            Process process = new Process();
            process.StartInfo.FileName = "/bin/sh";
            process.StartInfo.Arguments = $"-c \"{command}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
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
