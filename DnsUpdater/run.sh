#!/bin/sh
az login --identity --username "4ea00b67-f860-4b9b-829a-d7519ca5f350"
az network private-dns record-set a add-record -g jericos-stuff-uaen -z jericos.stuff -n some-record-set -a "1.2.3.4"
dotnet DnsUpdater.dll