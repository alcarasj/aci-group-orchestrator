using Azure.Core;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure;
using Azure.Identity;
using System.Diagnostics;
using Azure.ResourceManager.Models;

public static class Program
{
    private static void Main()
    {
        var targetSubscriptionId = GetEnvVar("AZURE_SUBSCRIPTION_ID");
        var targetResourceGroupName = GetEnvVar("TARGET_RESOURCE_GROUP_NAME");
        var containerGroupName = GetEnvVar("CONTAINER_GROUP_NAME");

        var credentials = new ManagedIdentityCredential();
        var armClient = new ArmClient(credentials);

        StartContainerGroup(armClient, targetSubscriptionId, targetResourceGroupName, containerGroupName);
        Sleep(3);
        DeleteContainerGroup(armClient, targetSubscriptionId, targetResourceGroupName, containerGroupName);

        for (var i = 0; i < 10; i++)
        {
            StartContainerGroup(armClient, targetSubscriptionId, targetResourceGroupName, $"{containerGroupName}-{i}");
        }
        Sleep(3);
        DeleteAllContainerGroups(armClient, targetSubscriptionId, targetResourceGroupName);
    }

    private static string StartContainerGroup(ArmClient armClient, string targetSubscriptionId, string targetResourceGroupName, string containerGroupName)
    {
        Console.WriteLine($"\nCreating container group {containerGroupName}...");
        var stopWatch = Stopwatch.StartNew();
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
                        SecurityContext = new ContainerSecurityContextDefinition(){IsPrivileged = false}
                    }
                },
                ContainerInstanceOperatingSystemType.Linux)
        {
            ImageRegistryCredentials = { },
            IPAddress = new ContainerGroupIPAddress(new ContainerGroupPort[] { new ContainerGroupPort(8000) { Protocol = ContainerGroupNetworkProtocol.Tcp } }, ContainerGroupIPAddressType.Public),
            Sku = ContainerGroupSku.Confidential,
            ConfidentialComputeCcePolicy = "eyJhbGxvd19hbGwiOiB0cnVlLCAiY29udGFpbmVycyI6IHsibGVuZ3RoIjogMCwgImVsZW1lbnRzIjogbnVsbH19",
        };
        ArmOperation<ContainerGroupResource> lro = collection.CreateOrUpdate(WaitUntil.Completed, containerGroupName, data);
        ContainerGroupResource result = lro.Value;
        ContainerGroupData resourceData = result.Data;
        stopWatch.Stop();
        Console.WriteLine($"\nSuccessfully created container group {containerGroupName} (ID {resourceData.Id}) [{stopWatch.Elapsed.TotalMilliseconds}ms]");
        return resourceData.Id;
    }

    private static void DeleteContainerGroup(ArmClient armClient, string targetSubscriptionId, string targetResourceGroupName, string containerGroupName)
    {
        Console.WriteLine($"\nDeleting container group {containerGroupName}");
        var stopWatch = Stopwatch.StartNew();
        ResourceIdentifier containerGroupResourceId = ContainerGroupResource.CreateResourceIdentifier(targetSubscriptionId, targetResourceGroupName, containerGroupName);
        ContainerGroupResource containerGroup = armClient.GetContainerGroupResource(containerGroupResourceId);
        ArmOperation<ContainerGroupResource> operation = containerGroup.Delete(WaitUntil.Completed);
        ContainerGroupResource result = operation.Value;
        ContainerGroupData resourceData = result.Data;
        stopWatch.Stop();
        Console.WriteLine($"\nSuccessfully deleted container group {containerGroupName} (ID {resourceData.Id}) [{stopWatch.Elapsed.TotalMilliseconds}ms]");
    }

    private static void DeleteAllContainerGroups(ArmClient armClient, string targetSubscriptionId, string targetResourceGroupName)
    {
        Console.WriteLine($"\nDeleting all container groups in resource group {targetResourceGroupName} for subscription {targetSubscriptionId}...\n");
        var stopWatch = Stopwatch.StartNew();
        SubscriptionCollection subscriptions = armClient.GetSubscriptions();
        SubscriptionResource subscription = subscriptions.Get(targetSubscriptionId);
        ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
        ResourceGroupResource resourceGroup = resourceGroups.Get(targetResourceGroupName);
        ContainerGroupCollection collection = resourceGroup.GetContainerGroups();

        for (var i = 0; i < collection.Count(); i++)
        {
            ContainerGroupResource containerGroup = collection.ElementAt(i);
            ArmOperation<ContainerGroupResource> operation = containerGroup.Delete(WaitUntil.Completed);
            ContainerGroupResource result = operation.Value;
            ContainerGroupData resourceData = result.Data;
            Console.WriteLine($"Successfully deleted container group {containerGroup.Data.Name}");
        }

        stopWatch.Stop();
        Console.WriteLine($"\nSuccessfully deleted all container groups [{stopWatch.Elapsed.TotalMilliseconds}ms]");
    }

    private static string GetEnvVar(string envVarName)
    {
        var value = Environment.GetEnvironmentVariable(envVarName);
        if (string.IsNullOrEmpty(value))
        {
            throw new Exception($"{envVarName} is not set!");
        }
        Console.WriteLine($"{envVarName} is \"{value}\"");
        return value;
    }

    private static void Sleep(int seconds)
    {
        var milliseconds = seconds * 1000;
        Console.WriteLine($"\nSleeping for {milliseconds}ms");
        Thread.Sleep(milliseconds);
    }
}