#!/bin/bash
# Builds PDFjet for .NET Framework 4.8, net48/bin/release/net48/PDFjet.dll and
# the System.Memory DLLs beside it, from the sources of net/pdfjet as they are.
# It needs the .NET 10 SDK or later, for C# 14: the dotnet of DOTNET, else
# ~/.dotnet10/dotnet, else the dotnet of the path when it has one.
#
#   net48/build.sh [-p:SignAssembly=true -p:AssemblyOriginatorKeyFile=PDFjet.snk]
cd "$(dirname "$0")" || exit 1
sdk10() {
    "$1" --list-sdks 2>/dev/null | awk -F. '$1 >= 10 {found = 1} END {exit !found}'
}
for candidate in "$DOTNET" "$HOME/.dotnet10/dotnet" "$(command -v dotnet)"; do
    if [ -n "$candidate" ] && [ -x "$candidate" ] && sdk10 "$candidate"; then
        DOTNET=$candidate
        break
    fi
    DOTNET=
done
if [ -z "$DOTNET" ]; then
    echo "The .NET 10 SDK or later is needed, for C# 14: see net48/README.md" >&2
    exit 1
fi
rm -rf bin obj
"$DOTNET" build PDFjet.Net48.csproj -c release -p:TreatWarningsAsErrors=true "$@"
