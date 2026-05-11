param(
  [string]$Version
)

Write-Host "Packing local artifacts..."

# Build first
dotnet build ElBruno.NetAgent.sln -c Release

if ($Version) {
  dotnet pack src/ElBruno.NetAgent/ElBruno.NetAgent.csproj -c Release -o artifacts\packages --no-build /p:PackageVersion=$Version
} else {
  dotnet pack src/ElBruno.NetAgent/ElBruno.NetAgent.csproj -c Release -o artifacts\packages --no-build
}

Write-Host "Pack complete. Packages in artifacts\packages"