#!/bin/bash
# Makes the NuGet packages of a release: PDFjet, its PDFjet.dll for .NET 8 and
# for .NET Framework 4.8, built from the tag of the release with the fonts
# PDFjet carries inside them but the CJK ones (-p:PDFjetFonts=true), so that
# the package works out of the box (the owner, 29 October 2026), with the
# licenses of the fonts beside them; and PDFjet.Fonts.CJK, the CJK fonts,
# IBM Plex Sans JP, KR, SC and TC, about 40 MB, in one assembly for .NET
# Standard 2.0, PDFjet.Fonts.CJK.dll, which serves both, so that they are
# stored once (the owner, 30 October 2026); PDFjet loads it by its name when
# a CJK font is asked for. The System.Memory of the .NET Framework 4.8
# PDFjet.dll is a dependency of the package rather than a file in it. The
# PDFjet.dll of the release zips on GitHub is the same library without the
# fonts, small, which reads them from the folder resources/fonts of its
# package, and the CJK ones from resources/fonts/cjk.
#
# The three DLLs are strong-named with the key of package-dotnet.sh, which is
# not in the repository but in the owner's private files, given by PDFJET_SNK,
# so that they have the identity of the DLLs of the zips, PublicKeyToken
# e66c1909913f295d; without it the script stops:
#
#   PDFJET_SNK=~/Projects/private/pdfjet-pro-private/signing/PDFjet.snk ./package-nuget.sh 9.1.7
#
# It writes out/nuget/PDFjet.9.1.7.nupkg and out/nuget/PDFjet.Fonts.CJK.9.1.7.nupkg,
# from the tag v9.1.7, or from the commit of PDFJET_REF, such as HEAD, for
# packages to look at before the tag. PDFJET_UNSIGNED=1, without PDFJET_SNK,
# builds DLLs that are not strong-named, to look at and to measure, never to
# upload. Pushing both is the owner's, who uploads them on nuget.org.
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
commit=$(git rev-list -n1 "$ref")
stage=$PWD/out/nuget/stage
build=$PWD/out/nuget/build
cjkStage=$PWD/out/nuget/stage-cjk
rm -rf "$stage" "$cjkStage" "$build" && mkdir -p "$stage/lib/net8.0" "$stage/lib/net48" "$stage/font-licenses" \
    "$cjkStage/lib/netstandard2.0" "$cjkStage/font-licenses" "$build"

# The sources of the library and the fonts of the tag, built apart from this
# working tree; through a file, not a pipe, so that a failing archive stops.
git archive -o "$build.tar" "$commit" net net48 PDFjet.csproj resources/fonts nuget/cjk
tar -x -C "$build" -f "$build.tar"
rm "$build.tar"

# The DLLs, with the fonts but the CJK ones, which the csproj files leave
# out, and the release as their file version
props=(-p:PDFjetFonts=true "${sign[@]}" -p:FileVersion="$version" -p:InformationalVersion="$version")
dotnet build "$build/PDFjet.csproj" -c release -p:TreatWarningsAsErrors=true "${props[@]}"
"$build/net48/build.sh" "${props[@]}"
cp "$build/bin/release/net8.0/PDFjet.dll" "$stage/lib/net8.0/"
cp "$build/net48/bin/release/net48/PDFjet.dll" "$stage/lib/net48/"
for dll in "$stage"/lib/*/PDFjet.dll; do
    if ! grep -q -a "PDFjet.Fonts/IBMPlexSans/IBMPlexSans-Regular.ttf" "$dll"; then
        echo "$dll does not carry the fonts" >&2
        exit 1
    fi
    if grep -q -a "PDFjet.Fonts/cjk/" "$dll"; then
        echo "$dll carries the CJK fonts" >&2
        exit 1
    fi
done

# The assembly of the CJK fonts, for .NET Standard 2.0
dotnet build "$build/nuget/cjk/PDFjet.Fonts.CJK.csproj" -c release -p:TreatWarningsAsErrors=true \
    "${sign[@]}" -p:FileVersion="$version" -p:InformationalVersion="$version"
cp "$build/nuget/cjk/bin/release/netstandard2.0/PDFjet.Fonts.CJK.dll" "$cjkStage/lib/netstandard2.0/"
for family in JP KR SC TC; do
    if ! grep -q -a "PDFjet.Fonts/cjk/IBMPlexSans$family/IBMPlexSans$family-Regular.otf" \
            "$cjkStage/lib/netstandard2.0/PDFjet.Fonts.CJK.dll"; then
        echo "PDFjet.Fonts.CJK.dll does not carry IBM Plex Sans $family" >&2
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
        if [ "$(basename "$family")" = cjk ]; then
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
copyLicenses "$build/resources/fonts/cjk" "$cjkStage/font-licenses"

# PDFjet finds the CJK fonts in PDFjet.Fonts.CJK.dll beside it, and without
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
  <!-- As the package PDFjet.Fonts.CJK adds it: in the deps.json of the
       program, where .NET finds what Assembly.Load asks for -->
  <ItemGroup Condition="'\$(Cjk)' == 'true'">
    <Reference Include="PDFjet.Fonts.CJK"><HintPath>$cjkStage/lib/netstandard2.0/PDFjet.Fonts.CJK.dll</HintPath></Reference>
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
    new Font(pdf, IBMPlexSansJP.Regular);
    Console.WriteLine("found");
} catch (FileNotFoundException e) {
    Console.WriteLine(e.Message);
}
CODE
dotnet build "$check/Check.csproj" -c release -o "$check/without" --nologo -v quiet > /dev/null
rm -rf "$check/obj"
dotnet build "$check/Check.csproj" -c release -p:Cjk=true -o "$check/with" --nologo -v quiet > /dev/null
without=$(cd "$check/without" && dotnet Check.dll)
with=$(cd "$check/with" && dotnet Check.dll)
case $without in
    *"dotnet add package PDFjet.Fonts.CJK"*) ;;
    *) echo "Without PDFjet.Fonts.CJK.dll: $without" >&2; exit 1 ;;
esac
if [ "$with" != found ]; then
    echo "With PDFjet.Fonts.CJK.dll: $with" >&2; exit 1
fi
echo "Without PDFjet.Fonts.CJK: $without"
rm -rf "$build"

cp nuget/README.md nuget/icon.png "$stage/"
sed -e "s/\$version\\$/$version/" -e "s/\$commit\\$/$commit/" nuget/PDFjet.nuspec > "$stage/PDFjet.nuspec"
cp nuget/cjk/README.md nuget/icon.png "$cjkStage/"
sed -e "s/\$version\\$/$version/" -e "s/\$commit\\$/$commit/" nuget/cjk/PDFjet.Fonts.CJK.nuspec > "$cjkStage/PDFjet.Fonts.CJK.nuspec"
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
cp "$stage/pack.csproj" "$cjkStage/pack.csproj"
dotnet pack "$stage/pack.csproj" -p:NuspecFile=PDFjet.nuspec -p:NuspecBasePath=. \
    -o out/nuget --nologo -v quiet
dotnet pack "$cjkStage/pack.csproj" -p:NuspecFile=PDFjet.Fonts.CJK.nuspec -p:NuspecBasePath=. \
    -o out/nuget --nologo -v quiet
echo "out/nuget/PDFjet.$version.nupkg: $(du -h "out/nuget/PDFjet.$version.nupkg" | cut -f1), from $ref, commit $commit"
echo "out/nuget/PDFjet.Fonts.CJK.$version.nupkg: $(du -h "out/nuget/PDFjet.Fonts.CJK.$version.nupkg" | cut -f1)"
