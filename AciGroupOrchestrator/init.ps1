$env:AZURE_SUBSCRIPTION_ID = "e0f91dc0-102c-41ae-a3b3-d256a2ee118d"
$env:TARGET_RESOURCE_GROUP_NAME = "jericos-stuff-uaen"
$env:CONTAINER_GROUP_NAME = "hello-world-cg"
$env:TARGET_SUBNET_RESOURCE_ID = "/subscriptions/e0f91dc0-102c-41ae-a3b3-d256a2ee118d/resourceGroups/jericos-stuff-uaen/providers/Microsoft.Network/virtualNetworks/jericos-vnet/subnets/aci-cg-subnet"
$env:TARGET_SUBNET_NAME ="aci-cg-subnet"
$env:TEMPLATE_FILE_NAME = "container-group-with-az-dns-vnet.json"
$env:AMOUNT_TO_CREATE = 3