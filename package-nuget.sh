#!/bin/bash
# Makes the NuGet packages of a release: PDFjet, its PDFjet.dll for .NET 8 and
# for .NET Framework 4.8, built from the tag of the release with the fonts
# PDFjet carries for the European languages inside them, IBM Plex Sans and
# Serif and Cascadia Mono (-p:PDFjetFonts=true), so that the package works
# out of the box (the owner, 29 October 2026), with the licenses of the
# fonts beside them; and PDFjet.Fonts.World, the fonts of the other scripts,
# IBM Plex Sans JP, KR, SC and TC and IBM Plex Sans Arabic, Hebrew and Thai,
# in one assembly for .NET Standard 2.0, PDFjet.Fonts.World.dll, which serves
# both, so that they are stored once (the owner, 30 October 2026); PDFjet
# loads it by its name when one of its fonts is asked for. The System.Memory
# of the .NET Framework 4.8 PDFjet.dll is a dependency of the package rather
# than a file in it. The PDFjet.dll of the release zips on GitHub is the same
# library without the fonts, small, which reads them from the folder
# resources/fonts of its package, and the others from resources/fonts/world.
#
# The three DLLs are strong-named with the key of package-dotnet.sh, which is
# not in the repository but in the owner's private files, given by PDFJET_SNK,
# so that they have the identity of the DLLs of the zips, PublicKeyToken
# e66c1909913f295d; without it the script stops:
#
#   PDFJET_SNK=~/Projects/private/pdfjet-pro-private/signing/PDFjet.snk ./package-nuget.sh 9.1.7
#
# It writes out/nuget/PDFjet.9.1.7.nupkg and out/nuget/PDFjet.Fonts.World.9.1.7.nupkg
# from the tag v9.1.7, with HEAD at the tag, code, nuspec and README alike, once
# util/check-version.sh has passed the tag. PDFJET_REF, such as HEAD, takes that
# commit instead, for packages to look at before the tag, and PDFJET_UNSIGNED=1,
# without PDFJET_SNK, builds DLLs that are not strong-named, to look at and to
# measure. Neither is to upload, and their packages say so: the version in them
# and in their names is 9.1.7-draft, 9.1.7-unsigned or 9.1.7-draft-unsigned, as
# a version once on nuget.org cannot be used again. Pushing both is the
# owner's, who uploads them on nuget.org.
#
# It needs the .NET 8 SDK and, for the .NET Framework 4.8 DLL, the .NET 10 SDK,
# which net48/build.sh finds.

set -euo pipefail
cd "$(dirname "$0")"
version=${1:?the version, such as 9.1.7}
sign=()
if [ -n "${PDFJET_UNSIGNED:-}" ]; then
    echo "PDFJET_UNSIGNED: the DLLs are not strong-named; these packages are not for nuget.org." >&2
elif [ -z "${PDFJET_SNK:-}" ] || [ ! -f "$PDFJET_SNK" ]; then
    echo "PDFJET_SNK must name the strong-name key, PDFjet.snk, to sign PDFjet.dll." >&2
    exit 1
else
    PDFJET_SNK=$(cd "$(dirname "$PDFJET_SNK")" && pwd)/$(basename "$PDFJET_SNK")
    sign=(-p:SignAssembly=true -p:AssemblyOriginatorKeyFile="$PDFJET_SNK")
fi
ref=${PDFJET_REF:-v$version}
if ! commit=$(git rev-list -n1 "$ref" 2> /dev/null); then
    echo "There is no $ref: tag the release, or set PDFJET_REF for a draft." >&2
    exit 1
fi
draft=
if [ -n "${PDFJET_REF:-}" ]; then
    draft=-draft
elif [ "$(git rev-parse HEAD)" != "$commit" ]; then
    echo "HEAD is $(git describe --tags --always HEAD), not the tag $ref: check out the tag, or set PDFJET_REF for a draft." >&2
    exit 1
fi
packageVersion=$version$draft${PDFJET_UNSIGNED:+-unsigned}
# The version is the same in all that holds it, at this commit; a draft asks
# no date of CHANGELOG.md and may be of a commit before the version was set.
found=$(util/check-version.sh --at "$commit" ${draft:+--draft})
if [ -z "$draft" ] && [ "$found" != "v$version" ]; then
    echo "$ref holds the version $found, not v$version." >&2
    exit 1
fi
stage=$PWD/out/nuget/stage
build=$PWD/out/nuget/build
worldStage=$PWD/out/nuget/stage-world
rm -rf "$stage" "$worldStage" "$build" && mkdir -p "$stage/lib/net8.0" "$stage/lib/net48" "$stage/font-licenses" \
    "$worldStage/lib/netstandard2.0" "$worldStage/font-licenses" "$build"

# The sources of the library and the fonts of the tag, built apart from this
# working tree; through a file, not a pipe, so that a failing archive stops.
git archive -o "$build.tar" "$commit" net net48 PDFjet.csproj resources/fonts nuget
tar -x -C "$build" -f "$build.tar"
rm "$build.tar"

# The DLLs, with the European fonts, the csproj files leaving out those of
# resources/fonts/world, and the release as their file version
props=(-p:PDFjetFonts=true "${sign[@]}" -p:FileVersion="$version" -p:InformationalVersion="$packageVersion")
dotnet build "$build/PDFjet.csproj" -c release -p:TreatWarningsAsErrors=true "${props[@]}"
"$build/net48/build.sh" "${props[@]}"
cp "$build/bin/release/net8.0/PDFjet.dll" "$stage/lib/net8.0/"
cp "$build/net48/bin/release/net48/PDFjet.dll" "$stage/lib/net48/"
for dll in "$stage"/lib/*/PDFjet.dll; do
    if ! grep -q -a "PDFjet.Fonts/IBMPlexSans/IBMPlexSans-Regular.ttf" "$dll"; then
        echo "$dll does not carry the fonts" >&2
        exit 1
    fi
    if grep -q -a "PDFjet.Fonts/world/" "$dll"; then
        echo "$dll carries the fonts of PDFjet.Fonts.World" >&2
        exit 1
    fi
done

# The assembly of the fonts of the other scripts, for .NET Standard 2.0
dotnet build "$build/nuget/world/PDFjet.Fonts.World.csproj" -c release -p:TreatWarningsAsErrors=true \
    "${sign[@]}" -p:FileVersion="$version" -p:InformationalVersion="$packageVersion"
cp "$build/nuget/world/bin/release/netstandard2.0/PDFjet.Fonts.World.dll" "$worldStage/lib/netstandard2.0/"
for face in JP-Regular.otf KR-Regular.otf SC-Regular.otf TC-Regular.otf \
        Arabic-Regular.ttf Hebrew-Regular.ttf Thai-Regular.ttf; do
    family=IBMPlexSans${face%%-*}
    if ! grep -q -a "PDFjet.Fonts/world/$family/IBMPlexSans$face" \
            "$worldStage/lib/netstandard2.0/PDFjet.Fonts.World.dll"; then
        echo "PDFjet.Fonts.World.dll does not carry $family" >&2
        exit 1
    fi
done

# The licenses of the fonts the DLLs carry, which go with every copy, as the
# SIL Open Font License asks: one a family, as <Family>-<its file>, such as
# IBMPlexSans-license.txt, in the package that carries the family
copyLicenses() {   # The folder of the families, the folder of the licenses
    local families=0 family license
    for family in "$1"/*/; do
        family=${family%/}
        if [ "$(basename "$family")" = world ] || [ "$(basename "$family")" = cjk ]; then
            continue
        fi
        families=$((families + 1))
        for license in "$family"/license.txt "$family"/OFL.txt "$family"/LICENSE; do
            if [ -f "$license" ]; then
                cp "$license" "$2/$(basename "$family")-$(basename "$license")"
            fi
        done
    done
    if [ "$(find "$2" -type f | wc -l)" -ne "$families" ]; then
        echo "Every family of $1 needs its license" >&2
        exit 1
    fi
}
copyLicenses "$build/resources/fonts" "$stage/font-licenses"
copyLicenses "$build/resources/fonts/world" "$worldStage/font-licenses"

# PDFjet finds the fonts of the other scripts in PDFjet.Fonts.World.dll
# beside it, and without
# it, says what to add, in a folder with no resources/fonts, as a program
# has: checked with the .NET 8 DLLs made here
check=$build/check
mkdir -p "$check"
cat > "$check/Check.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="PDFjet"><HintPath>$stage/lib/net8.0/PDFjet.dll</HintPath></Reference>
  </ItemGroup>
  <!-- As the package PDFjet.Fonts.World adds it: in the deps.json of the
       program, where .NET finds what Assembly.Load asks for -->
  <ItemGroup Condition="'\$(World)' == 'true'">
    <Reference Include="PDFjet.Fonts.World"><HintPath>$worldStage/lib/netstandard2.0/PDFjet.Fonts.World.dll</HintPath></Reference>
  </ItemGroup>
</Project>
PROJECT
cat > "$check/Check.cs" <<'CODE'
using System;
using System.IO;
using PDFjet.NET;

PDF pdf = new PDF(new MemoryStream());
new Font(pdf, IBMPlexSans.Regular);
try {
    new Font(pdf, IBMPlexSansArabic.Regular);
    new Font(pdf, IBMPlexSansJP.Regular);
    Console.WriteLine("found");
} catch (FileNotFoundException e) {
    Console.WriteLine(e.Message);
}
CODE
dotnet build "$check/Check.csproj" -c release -o "$check/without" --nologo -v quiet
rm -rf "$check/obj"
dotnet build "$check/Check.csproj" -c release -p:World=true -o "$check/with" --nologo -v quiet
without=$(cd "$check/without" && dotnet Check.dll)
with=$(cd "$check/with" && dotnet Check.dll)
case $without in
    *"dotnet add package PDFjet.Fonts.World"*) ;;
    *) echo "Without PDFjet.Fonts.World.dll: $without" >&2; exit 1 ;;
esac
if [ "$with" != found ]; then
    echo "With PDFjet.Fonts.World.dll: $with" >&2; exit 1
fi
echo "Without PDFjet.Fonts.World: $without"

# The nuspecs and the READMEs of the commit the DLLs are of
cp "$build/nuget/README.md" "$build/nuget/icon.png" "$stage/"
sed -e "s/\$version\\$/$packageVersion/" -e "s/\$commit\\$/$commit/" "$build/nuget/PDFjet.nuspec" > "$stage/PDFjet.nuspec"
cp "$build/nuget/world/README.md" "$build/nuget/icon.png" "$worldStage/"
sed -e "s/\$version\\$/$packageVersion/" -e "s/\$commit\\$/$commit/" "$build/nuget/world/PDFjet.Fonts.World.nuspec" \
    > "$worldStage/PDFjet.Fonts.World.nuspec"
rm -rf "$build"
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
cp "$stage/pack.csproj" "$worldStage/pack.csproj"
dotnet pack "$stage/pack.csproj" -p:NuspecFile=PDFjet.nuspec -p:NuspecBasePath=. \
    -o out/nuget --nologo -v quiet
dotnet pack "$worldStage/pack.csproj" -p:NuspecFile=PDFjet.Fonts.World.nuspec -p:NuspecBasePath=. \
    -o out/nuget --nologo -v quiet
echo "out/nuget/PDFjet.$packageVersion.nupkg: $(du -h "out/nuget/PDFjet.$packageVersion.nupkg" | cut -f1), from $ref, commit $commit"
echo "out/nuget/PDFjet.Fonts.World.$packageVersion.nupkg: $(du -h "out/nuget/PDFjet.Fonts.World.$packageVersion.nupkg" | cut -f1)"
