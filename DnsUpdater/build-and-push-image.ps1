docker build -t dnsupdater:latest .
docker tag dnsupdater:latest jericosacr.azurecr.io/dnsupdater:latest 
docker push jericosacr.azurecr.io/dnsupdater:latest