#!/bin/bash
# Makes a folder to run the 50 examples on .NET Framework 4.8 on Windows, with
# nothing to install: RunExamples.exe and the net48 PDFjet.dll with its DLLs,
# and what the examples read, resources/data, resources/images,
# tests/data/PngSuite, and the sources of examples/ some embed. The fonts the
# examples draw in are in PDFjet.dll.
#
#   net48/make-examples-folder.sh <folder>
#
# On Windows: copy the folder, open a Command Prompt in it, run RunExamples.exe.
# The PDFs are made there, and the results in net48-results.txt.
set -e
if [ -z "$1" ]; then
    echo "Usage: net48/make-examples-folder.sh <folder>" >&2
    exit 1
fi
mkdir -p "$1"
OUT=$(cd "$1" && pwd)
cd "$(dirname "$0")"
./build.sh > /dev/null
DOTNET=${DOTNET:-$HOME/.dotnet10/dotnet}
"$DOTNET" build run-examples/RunExamples.csproj -c release -p:TreatWarningsAsErrors=true > /dev/null
cd ..
cp net48/run-examples/bin/release/net48/*.exe net48/run-examples/bin/release/net48/*.exe.config \
    net48/run-examples/bin/release/net48/*.dll "$OUT/"
mkdir -p "$OUT/resources"
rsync -a --delete --exclude=go.mod resources/data resources/images "$OUT/resources/"
mkdir -p "$OUT/tests/data"
rsync -a --delete tests/data/PngSuite "$OUT/tests/data/"
# The sources some examples embed or draw
for file in $(grep -rhoE '"examples/[^"]+"' examples/*/Example_*.cs | tr -d '"' | sort -u); do
    mkdir -p "$OUT/$(dirname "$file")"
    cp "$file" "$OUT/$file"
done
cat > "$OUT/README.txt" <<'TXT'
The 50 examples of PDFjet, run on .NET Framework 4.8 against its PDFjet.dll.

Open a Command Prompt in this folder, and run:

    RunExamples.exe

It makes Example_01.pdf to Example_06.pdf here, and writes what each did to
net48-results.txt: its time, "ok" when its PDF was made whole, else what went
wrong. RunExamples.exe 7 29 runs those alone.
TXT
echo "$OUT: $(du -sh "$OUT" | cut -f1)"
