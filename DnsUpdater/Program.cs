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
        private const string ContainerGroupSubnetIpPrefix = "10.0.1.";

        private static readonly string domainName;

        private static readonly string dnsZoneName;

        private static readonly ArmClient armClient;

        static DnsUpdater()
        {
            var domainNameValue = Environment.GetEnvironmentVariable("DOMAIN_NAME");
            if (string.IsNullOrEmpty(domainNameValue))
            {
                throw new ArgumentNullException("DOMAIN_NAME cannot be null or empty.");
            }
            var dnsZoneNameValue = Environment.GetEnvironmentVariable("DNS_ZONE_NAME");
            if (string.IsNullOrEmpty(dnsZoneNameValue))
            {
                throw new ArgumentNullException("DNS_ZONE_NAME cannot be null or empty.");
            }

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
            var dnsZoneResourceId = PrivateDnsZoneResource.CreateResourceIdentifier(
                "e0f91dc0-102c-41ae-a3b3-d256a2ee118d",
                "jericos-stuff-uaen",
                dnsZoneName
            );
            try
            {
                var containerGroupIp = GetContainerGroupIpAddress();
                var dnsZone = armClient.GetPrivateDnsZoneResource(dnsZoneResourceId);
                var dnsRecords = dnsZone.GetPrivateDnsARecords();
                var newIp = new PrivateDnsARecordInfo();
                newIp.IPv4Address = containerGroupIp;
                var newRecord = new PrivateDnsARecordData();
                newRecord.TtlInSeconds = 3600;
                newRecord.PrivateDnsARecords.Add(newIp);
                dnsRecords.CreateOrUpdate(WaitUntil.Completed, domainName, newRecord);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static IPAddress GetContainerGroupIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                
                if (ip.ToString().StartsWith(ContainerGroupSubnetIpPrefix))
                {
                    Console.WriteLine($"IP found: {ip}");
                    return ip;
                }
            }
            throw new Exception($"No network adapters with prefix {ContainerGroupSubnetIpPrefix} in the system!");
        }
    }
}
