using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.PrivateDns;
using Azure.ResourceManager.PrivateDns.Models;
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

        private static readonly ArmClient armClient;

        static DnsUpdater()
        {
            var domainNameValue = Environment.GetEnvironmentVariable("DOMAIN_NAME");
            if (string.IsNullOrEmpty(domainNameValue))
            {
                throw new ArgumentNullException("DOMAIN_NAME cannot be null or empty.");
            }
            domainName = domainNameValue;

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
                "jericos.stuff",
                domainName
            );
            try
            {
                var dnsRecord = armClient.GetPrivateDnsARecordResource(dnsRecordResourceId).Get().Value;
                var dnsRecordData = dnsRecord.Data;
                var newIp = new PrivateDnsARecordInfo();
                newIp.IPv4Address = GetLocalIPAddress();
                dnsRecordData.PrivateDnsARecords.Clear();
                dnsRecordData.PrivateDnsARecords.Add(newIp);
                dnsRecord.Update(dnsRecordData);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Record {domainName} not found.");
                throw;
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
    }
}
