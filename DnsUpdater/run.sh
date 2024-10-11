az login --identity --username "/subscriptions/e0f91dc0-102c-41ae-a3b3-d256a2ee118d/resourceGroups/jericos-stuff-uaen/providers/Microsoft.ManagedIdentity/userAssignedIdentities/jericos-uami"
az network private-dns record-set a add-record -g jericos-stuff-uaen -z jericos.stuff -n some-record-set -a "1.2.3.4"
dotnet DnsUpdater.dll