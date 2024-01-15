using Azure.Core;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure;
using Azure.Identity;
using System.Diagnostics;
using System.Linq.Expressions;

public static class Program
{
    private const int N = 10;
    private const int MaxTimesToSleep = 10;

    private static async Task Main()
    {
        var targetSubscriptionId = GetEnvVar("AZURE_SUBSCRIPTION_ID");
        var targetResourceGroupName = GetEnvVar("TARGET_RESOURCE_GROUP_NAME");
        var containerGroupName = GetEnvVar("CONTAINER_GROUP_NAME");

        var credentials = new ManagedIdentityCredential();
        var armClient = new ArmClient(credentials);

        Console.WriteLine($"\nEnsuring that there are no existing container groups in the resource group...");
        await DeleteAllContainerGroups(armClient, targetSubscriptionId, targetResourceGroupName);

        // Wait for the deletion of the resources to propagate through all ARM regions.
        SleepUntil(() => GetContainerGroups(armClient, targetSubscriptionId, targetResourceGroupName).Count() == 0);

        Console.WriteLine($"\nParallel creation of {N} container groups starting...");
        var stopWatch = Stopwatch.StartNew();
        List<Task> creationTasks = new List<Task>();
        for (var i = 0; i < N; i++)
        {
            var creationTask = CreateContainerGroup(armClient, targetSubscriptionId, targetResourceGroupName, $"{containerGroupName}-{i}");
            creationTasks.Add(creationTask);
        }
        await Task.WhenAll(creationTasks);
        stopWatch.Stop();

        Console.WriteLine($"\nParallel creation of {N} container groups succeeded! [{stopWatch.Elapsed.TotalMilliseconds}ms]");

        // Wait for the registration of the newly-created resources to propagate through all ARM regions.
        SleepUntil(() => GetContainerGroups(armClient, targetSubscriptionId, targetResourceGroupName).Count() == N);

        await DeleteAllContainerGroups(armClient, targetSubscriptionId, targetResourceGroupName);
        Console.WriteLine("\nDone!");
    }

    private static async Task<string> CreateContainerGroup(ArmClient armClient, string targetSubscriptionId, string targetResourceGroupName, string containerGroupName)
    {
        Console.WriteLine($"\nCreating container group {containerGroupName}...");
        var stopWatch = Stopwatch.StartNew();
        var containerGroups = GetContainerGroups(armClient, targetSubscriptionId, targetResourceGroupName);
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
        ArmOperation<ContainerGroupResource> lro = await containerGroups.CreateOrUpdateAsync(WaitUntil.Completed, containerGroupName, data);
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

    private static async Task DeleteContainerGroup(ContainerGroupResource containerGroup)
    {
        Console.WriteLine($"\nDeleting container group {containerGroup.Data.Name}");
        var stopWatch = Stopwatch.StartNew();
        ArmOperation<ContainerGroupResource> operation = await containerGroup.DeleteAsync(WaitUntil.Completed);
        ContainerGroupResource result = operation.Value;
        ContainerGroupData resourceData = result.Data;
        stopWatch.Stop();
        Console.WriteLine($"\nSuccessfully deleted container group {containerGroup.Data.Name} (ID {resourceData.Id}) [{stopWatch.Elapsed.TotalMilliseconds}ms]");
    }

    private static async Task DeleteAllContainerGroups(ArmClient armClient, string targetSubscriptionId, string targetResourceGroupName)
    {
        Console.WriteLine($"\nDeleting all container groups in resource group {targetResourceGroupName} for subscription {targetSubscriptionId}...\n");
        var stopWatch = Stopwatch.StartNew();
        var containerGroups = GetContainerGroups(armClient, targetSubscriptionId, targetResourceGroupName);
        List<Task> deletionTasks = new List<Task>();
        for (var i = 0; i < containerGroups.Count(); i++)
        {
            try
            {
                ContainerGroupResource containerGroup = containerGroups.ElementAt(i);
                var deletionTask = DeleteContainerGroup(containerGroup);
                deletionTasks.Add(deletionTask);
            }
            catch (Exception ex)
            {
                throw new Exception($"Index: {i}", ex);
            }
        }
        await Task.WhenAll(deletionTasks);

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

    private static ContainerGroupCollection GetContainerGroups(ArmClient armClient, string targetSubscriptionId, string targetResourceGroupName)
    {
        Console.WriteLine($"\nRetrieving all container groups in resource group {targetResourceGroupName} for subscription {targetSubscriptionId}...\n");
        var stopWatch = Stopwatch.StartNew();
        SubscriptionCollection subscriptions = armClient.GetSubscriptions();
        SubscriptionResource subscription = subscriptions.Get(targetSubscriptionId);
        ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();
        ResourceGroupResource resourceGroup = resourceGroups.Get(targetResourceGroupName);
        ContainerGroupCollection collection = resourceGroup.GetContainerGroups();
        stopWatch.Stop();
        Console.WriteLine($"\nSuccessfully retrieved {collection.Count()} container groups [{stopWatch.Elapsed.TotalMilliseconds}ms]");
        return collection;
    }

    private static void Sleep(int seconds)
    {
        var milliseconds = seconds * 1000;
        Console.WriteLine($"\nSleeping for {seconds} seconds");
        Thread.Sleep(milliseconds);
    }

    private static void SleepUntil(Expression<Func<bool>> expression)
    {
        var timesSlept = 0;
        var predicate = expression.Compile();
        var isPredicateTrue = predicate();
        while (!isPredicateTrue && timesSlept < MaxTimesToSleep)
        {
            Sleep(5);
            isPredicateTrue = predicate();
            timesSlept++;
        }

        if (!isPredicateTrue && timesSlept >= MaxTimesToSleep)
        {
            throw new Exception($"Expected predicate {(LambdaExpression) expression.Body} to be true after {MaxTimesToSleep} evaluations but was still false.");
        }
    }
}