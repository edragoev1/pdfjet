#!/bin/bash
# Makes the NuGet package PDFjet of a release, from the PDFjet.dll files of
# its PDFjet-For.NET-Core zip, as released and strong-name signed, not built
# again: the .NET 8 one, and the .NET Framework 4.8 one, whose System.Memory
# is a dependency of the package rather than a file in it.
#
#   ./package-nuget.sh 9.0.9 ~/Projects/PDFjet-For.NET-Core-v9.0.9.zip
#
# It writes out/nuget/PDFjet.9.0.9.nupkg. Pushing it is the owner's, with
# their API key of nuget.org, which never goes in a file of the repository:
#
#   dotnet nuget push out/nuget/PDFjet.9.0.9.nupkg --api-key KEY --source https://api.nuget.org/v3/index.json

set -euo pipefail
cd "$(dirname "$0")"
version=${1:?the version, such as 9.0.9}
zip=${2:?the PDFjet-For.NET-Core zip of the release}
commit=$(git rev-list -n1 "v$version")
stage=out/nuget/stage
rm -rf "$stage" && mkdir -p "$stage/lib/net8.0" "$stage/lib/net48"
top="PDFjet-For.NET-Core-v$version"
unzip -q -j -o "$zip" "$top/PDFjet.dll" -d "$stage/lib/net8.0"
unzip -q -j -o "$zip" "$top/net48/PDFjet.dll" -d "$stage/lib/net48"
cp nuget/README.md nuget/icon.png "$stage/"
sed -e "s/\$version\\$/$version/" -e "s/\$commit\\$/$commit/" nuget/PDFjet.nuspec > "$stage/PDFjet.nuspec"
# dotnet pack needs a project, even one it does not build: a stub, with the
# .nuspec doing the rest
cat > "$stage/pack.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <NoWarn>NU5128</NoWarn>
  </PropertyGroup>
</Project>
PROJECT
dotnet pack "$stage/pack.csproj" -p:NuspecFile=PDFjet.nuspec -p:NuspecBasePath=. \
    -o out/nuget --nologo -v quiet
echo "out/nuget/PDFjet.$version.nupkg: $(du -h "out/nuget/PDFjet.$version.nupkg" | cut -f1), from $(basename "$zip"), commit $commit"
