using Azure.Core;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure;
using Azure.Identity;

public static class Program
{
    private static void Main()
    {
        // See https://aka.ms/new-console-template for more information
        Console.WriteLine("Hello, World!");
        StartContainerGroup();
    }

    private static void StartContainerGroup()
    {
        var targetSubscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        var targetResourceGroupName = Environment.GetEnvironmentVariable("TARGET_RESOURCE_GROUP_NAME");
        var containerGroupName = "hello-world-container-group"; // Environment.GetEnvironmentVariable("CONTAINER_GROUP_NAME");
        var shouldOrchestrateOnTheCloud = !string.IsNullOrEmpty(targetSubscriptionId) && !string.IsNullOrEmpty(targetResourceGroupName);

        if (shouldOrchestrateOnTheCloud)
        {
            var credentials = new ManagedIdentityCredential();
            var armClient = new ArmClient(credentials);
            SubscriptionCollection subscriptions = armClient.GetSubscriptions();
            SubscriptionResource subscription = subscriptions.Get(targetSubscriptionId);
            ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
            ResourceGroupResource resourceGroup = resourceGroups.Get(targetResourceGroupName);
            ContainerGroupCollection collection = resourceGroup.GetContainerGroups();
            ContainerGroupData data = new ContainerGroupData(new AzureLocation("westeurope"), new ContainerInstanceContainer[]
                {
                    new ContainerInstanceContainer("accdemo", "mcr.microsoft.com/azuredocs/aci-helloworld", new ContainerResourceRequirements(new ContainerResourceRequestsContent(1.5, 1)))
                    {
                        Command={},
                        Ports ={new ContainerPort(8000)},
                        EnvironmentVariables = {},
                        SecurityContext = new ContainerSecurityContextDefinition(){IsPrivileged = false}}
                    }, 
                    ContainerInstanceOperatingSystemType.Linux)
            {
                ImageRegistryCredentials ={},
                IPAddress = new ContainerGroupIPAddress(new ContainerGroupPort[]{new ContainerGroupPort(8000){Protocol = ContainerGroupNetworkProtocol.Tcp}}, ContainerGroupIPAddressType.Public),
                Sku = ContainerGroupSku.Confidential,
                ConfidentialComputeCcePolicy = "eyJhbGxvd19hbGwiOiB0cnVlLCAiY29udGFpbmVycyI6IHsibGVuZ3RoIjogMCwgImVsZW1lbnRzIjogbnVsbH19",
            };
            ArmOperation<ContainerGroupResource> lro = collection.CreateOrUpdate(WaitUntil.Completed, containerGroupName, data);
            ContainerGroupResource result = lro.Value;
            ContainerGroupData resourceData = result.Data;
            Console.WriteLine($"Successfully created container group with ID {resourceData.Id}");
        }

        // TO-DO Establish and verify connections to the required containers.
    }
}