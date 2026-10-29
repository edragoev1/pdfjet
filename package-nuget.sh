#!/bin/bash
# Makes the NuGet package PDFjet of a release: its PDFjet.dll for .NET 8 and
# for .NET Framework 4.8, built from the tag of the release with the fonts
# PDFjet carries inside them (-p:PDFjetFonts=true), so that the package works
# out of the box (the owner, 29 October 2026), with the licenses of the fonts
# beside them; the System.Memory of the .NET Framework 4.8 one is a dependency
# of the package rather than a file in it. The PDFjet.dll of the release zips
# on GitHub is the same library without the fonts, small, which reads them
# from the folder resources/fonts of its package.
#
# Both DLLs are strong-named with the key of package-dotnet.sh, which is not in
# the repository but in the owner's private files, given by PDFJET_SNK, so that
# they have the identity of the DLLs of the zips, PublicKeyToken
# e66c1909913f295d; without it the script stops:
#
#   PDFJET_SNK=~/Projects/private/pdfjet-pro-private/signing/PDFjet.snk ./package-nuget.sh 9.1.7
#
# It writes out/nuget/PDFjet.9.1.7.nupkg, from the tag v9.1.7, or from the
# commit of PDFJET_REF, such as HEAD, for a package to look at before the tag.
# Pushing it is the owner's, who uploads it on nuget.org.
#
# It needs the .NET 8 SDK and, for the .NET Framework 4.8 DLL, the .NET 10 SDK,
# which net48/build.sh finds.

set -euo pipefail
cd "$(dirname "$0")"
version=${1:?the version, such as 9.1.7}
if [ -z "${PDFJET_SNK:-}" ] || [ ! -f "$PDFJET_SNK" ]; then
    echo "PDFJET_SNK must name the strong-name key, PDFjet.snk, to sign PDFjet.dll." >&2
    exit 1
fi
PDFJET_SNK=$(cd "$(dirname "$PDFJET_SNK")" && pwd)/$(basename "$PDFJET_SNK")
ref=${PDFJET_REF:-v$version}
commit=$(git rev-list -n1 "$ref")
stage=$PWD/out/nuget/stage
build=$PWD/out/nuget/build
rm -rf "$stage" "$build" && mkdir -p "$stage/lib/net8.0" "$stage/lib/net48" "$stage/font-licenses" "$build"

# The sources of the library and the fonts of the tag, built apart from this
# working tree; through a file, not a pipe, so that a failing archive stops.
git archive -o "$build.tar" "$commit" net net48 PDFjet.csproj resources/fonts
tar -x -C "$build" -f "$build.tar"
rm "$build.tar"

# The DLLs, with the fonts and the release as their file version
props=(-p:PDFjetFonts=true -p:SignAssembly=true -p:AssemblyOriginatorKeyFile="$PDFJET_SNK"
    -p:FileVersion="$version" -p:InformationalVersion="$version")
dotnet build "$build/PDFjet.csproj" -c release -p:TreatWarningsAsErrors=true "${props[@]}"
"$build/net48/build.sh" "${props[@]}"
cp "$build/bin/release/net8.0/PDFjet.dll" "$stage/lib/net8.0/"
cp "$build/net48/bin/release/net48/PDFjet.dll" "$stage/lib/net48/"
for dll in "$stage"/lib/*/PDFjet.dll; do
    if ! grep -q -a "PDFjet.Fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.otf" "$dll"; then
        echo "$dll does not carry the fonts" >&2
        exit 1
    fi
done

# The licenses of the fonts the DLLs carry, which go with every copy, as the
# SIL Open Font License asks: one a family, as <Family>-<its file>, such as
# IBMPlexSans-license.txt
families=0
for family in "$build"/resources/fonts/*/; do
    family=${family%/}
    families=$((families + 1))
    for license in "$family"/license.txt "$family"/OFL.txt "$family"/LICENSE; do
        if [ -f "$license" ]; then
            cp "$license" "$stage/font-licenses/$(basename "$family")-$(basename "$license")"
        fi
    done
done
if [ "$(find "$stage/font-licenses" -type f | wc -l)" -ne "$families" ]; then
    echo "Every family of resources/fonts needs its license" >&2
    exit 1
fi
rm -rf "$build"

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
echo "out/nuget/PDFjet.$version.nupkg: $(du -h "out/nuget/PDFjet.$version.nupkg" | cut -f1), from $ref, commit $commit"
