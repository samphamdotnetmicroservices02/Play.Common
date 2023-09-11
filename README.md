# Play.Common
Common library used by Play Economy services.

## Create and publish package
```powershell
$version="1.0.6"
$owner="samphamdotnetmicroservices02"
dotnet pack src\Play.Common\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Play.Common -o ../packages
```

```mac
version="1.0.6"
owner="samphamdotnetmicroservices02"
dotnet pack src/Play.Common/ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Play.Common -o ../packages
```