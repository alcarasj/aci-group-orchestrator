# aci-group-orchestrator
Some sample code to create and delete Azure Container Instances container groups.
## Setup
Requires the following:
- A subscription and resource group in Azure.
- A VNET with at least two subnets.
- A VM instance within the resource group that is deployed to one subnet.
- The VM instance must have the system-assigned managed identity enabled with permissions to create, read and delete container groups, and to read resource groups within the scope of the resource group.
## Instructions
1. Clone the repo on the VM.
2. Open a terminal.
3. Create a script called `init.ps1` or `init.sh` that sets the environment variables `AZURE_SUBSCRIPTION_ID`, `TARGET_RESOURCE_GROUP_NAME`, `CONTAINER_GROUP_NAME` and `TARGET_SUBNET_RESOURCE_ID`
4. `dotnet run AciGroupOrchestrator.csproj`