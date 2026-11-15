#!/bin/bash
# Keeps the guides for AI coding assistants true: llms-java.txt,
# llms-dotnet.txt, llms-go.txt and llms-swift.txt. It takes the code samples
# out of each guide, builds them against the library of this checkout, runs
# them, and checks the PDFs they write: the number of pages, the same in the
# four ports, and, when veraPDF is there, run as "verapdf" or as the command in
# the VERAPDF environment variable, PDF/UA-1.
#
#   ./check-llms.sh                 # the four guides
#   ./check-llms.sh go swift        # some of them: java, dotnet, go, swift
#
# A guide's code blocks are found by their language, java, csharp, go or
# swift. A block with a main, or, in Swift, a call of complete(), is a whole
# program; in Java and C#, a block of import or using lines alone goes before
# it. The block of the Markdown sample, which has no main, is put in a harness that
# gives it the PDF, the five fonts and the text, markdownText, it uses, and
# completes the PDF after it. It works in build/check-llms, and the samples
# run there with a link to resources, as the examples run in the repository.
cd "$(dirname "$0")" || exit 1
ROOT=$(pwd)
WORK=$ROOT/build/check-llms

# The pages each sample writes, in every port: the page of text, the table of
# 200 rows with its footers, and the Markdown of the harness.
EXPECTED="Hello=1 Report=6 Markdown=2"

ports=("$@")
if [ ${#ports[@]} -eq 0 ]; then
    ports=(java dotnet go swift)
fi
for port in "${ports[@]}"; do
    case $port in
        java|dotnet|go|swift) ;;
        *) echo "Not a port: $port. The ports are java, dotnet, go and swift."; exit 1 ;;
    esac
done
rm -rf "$WORK"
mkdir -p "$WORK"

# The Markdown that the harness of each port draws: two pages of it.
{
    echo "# Markdown"
    echo
    echo "A paragraph with **bold**, *italic* and \`code\` text, and a [link](https://pdfjet.com)."
    echo
    for i in $(seq 1 40); do
        echo "- Item $i of a list that is long enough to go on to a second page"
    done
    echo
    echo "| Name | Value |"
    echo "|---|---|"
    echo "| One | 1 |"
    echo "| Two | 2 |"
    echo
    echo '```'
    echo 'pdf.complete();'
    echo '```'
} > "$WORK/sample.md"

# extract GUIDE LANG DIR: writes the code blocks of LANG in GUIDE to DIR, as
# 1.txt, 2.txt and so on.
extract() {
    mkdir -p "$3"
    awk -v lang="$2" -v dir="$3" '
        /^```/ && !inside { if ($0 == "```" lang) { inside = 1; n++; file = dir "/" n ".txt"; printf "" > file }; next }
        /^```/ && inside { inside = 0; close(file); next }
        inside { print >> file }
    ' "$1"
}

# The kind of a block: a program, the Markdown fragment, the preamble of
# Java and C# (import or using lines alone), or another, such as a Go import
# block or a Package.swift, which is shown and not run.
kind() {   # FILE PORT
    if case $2 in
            java) grep -q 'static void main' "$1" ;;
            dotnet) grep -q 'static void Main' "$1" ;;
            go) grep -q '^func main()' "$1" ;;
            swift) grep -q 'complete()' "$1" ;;
        esac; then
        echo program
    elif grep -q 'Markdown(' "$1"; then
        echo fragment
    elif [ $2 = java ] || [ $2 = dotnet ] && ! grep -qvE '^(import|using) |^$' "$1"; then
        echo preamble
    else
        echo other
    fi
}

# The name of the PDF a block writes, Hello for Hello.pdf, which names the
# sample; the Markdown fragment writes Markdown.pdf in its harness.
pdfname() {
    grep -oE '"[A-Za-z]+\.pdf"' "$1" | head -n 1 | tr -d '"' | sed 's/\.pdf$//'
}

# The pages of a PDF, from the /Count of its page tree, which PDFjet writes
# in the clear; pdfinfo when it is there.
pagecount() {
    if command -v pdfinfo > /dev/null; then
        pdfinfo "$1" 2> /dev/null | awk '/^Pages:/ { print $2 }'
    else
        LC_ALL=C awk '/\/Type \/Pages/ { tree = 1 } tree && match($0, /\/Count [0-9]+/) {
            print substr($0, RSTART + 7, RLENGTH - 7); exit }' "$1"
    fi
}

failed=0
fail() {
    echo "::error::$*"
    failed=1
}

# The harness of the Markdown fragment, in each language: FILE FRAGMENT PREAMBLE
harness() {
    case $port in
    java)
        cat "$3"
        cat <<'JAVA'
public class Markdown_ {
    public static void main(String[] args) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Markdown.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Markdown");
        Font regular = new Font(pdf, IBMPlexSans.Regular);
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        Font boldItalic = new Font(pdf, IBMPlexSans.SemiBoldItalic);
        Font code = new Font(pdf, CascadiaMono.Regular);
        String markdownText = Content.ofTextFile("sample.md");
JAVA
        sed 's/^/        /' "$2"
        printf '        pdf.complete();\n    }\n}\n'
        ;;
    dotnet)
        cat "$3"
        cat <<'CS'
public class Markdown_ {
    public static void Main() {
        PDF pdf = new PDF(new BufferedStream(new FileStream("Markdown.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Markdown");
        Font regular = new Font(pdf, IBMPlexSans.Regular);
        Font bold = new Font(pdf, IBMPlexSans.SemiBold);
        Font italic = new Font(pdf, IBMPlexSans.Italic);
        Font boldItalic = new Font(pdf, IBMPlexSans.SemiBoldItalic);
        Font code = new Font(pdf, CascadiaMono.Regular);
        string markdownText = Content.OfTextFile("sample.md");
CS
        sed 's/^/        /' "$2"
        printf '        pdf.Complete();\n    }\n}\n'
        ;;
    go)
        cat <<'GO'
package main

import (
	"log"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/CascadiaMono"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

var _ = letter.Portrait

func main() {
	pdf, err := pdfjet.NewPDFFile("Markdown.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Markdown")
	regular := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	bold := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	italic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Italic)
	boldItalic := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBoldItalic)
	code := pdfjet.NewFontFromFile(pdf, CascadiaMono.Regular)
	markdownText := content.OfTextFile("sample.md")
GO
        sed 's/^/\t/' "$2"
        printf '\tif err := pdf.Complete(); err != nil {\n\t\tlog.Fatal(err)\n\t}\n}\n'
        ;;
    swift)
        cat <<'SWIFT'
import Foundation
import PDFjet

let pdf = PDF(OutputStream(toFileAtPath: "Markdown.pdf", append: false)!)
pdf.setCompliance(Compliance.PDF_UA_1)
pdf.setTitle("Markdown")
let regular = try Font(pdf, IBMPlexSans.Regular)
let bold = try Font(pdf, IBMPlexSans.SemiBold)
let italic = try Font(pdf, IBMPlexSans.Italic)
let boldItalic = try Font(pdf, IBMPlexSans.SemiBoldItalic)
let code = try Font(pdf, CascadiaMono.Regular)
let markdownText = try Content.ofTextFile("sample.md")
SWIFT
        cat "$2"
        printf 'try pdf.complete()\n'
        ;;
    esac > "$1"
}

# Makes the sources of a port's samples, SRC/NAME.EXT, from the guide's
# blocks, and prints the names, as written by the samples: Hello, Report,
# Markdown.
sources() {   # GUIDE LANG EXT SRC
    local blocks=$WORK/$port/blocks preamble=$WORK/$port/preamble.txt
    extract "$1" "$2" "$blocks"
    : > "$preamble"
    for block in $(ls "$blocks" | sort -n); do
        if [ "$(kind "$blocks/$block" $port)" = preamble ]; then
            cat "$blocks/$block" >> "$preamble"
            echo >> "$preamble"
        fi
    done
    for block in $(ls "$blocks" | sort -n); do
        case $(kind "$blocks/$block" $port) in
        program)
            name=$(pdfname "$blocks/$block")
            cat "$preamble" "$blocks/$block" > "$4/$name.$3"
            echo "$name"
            ;;
        fragment)
            harness "$4/Markdown.$3" "$blocks/$block" "$preamble"
            echo Markdown
            ;;
        esac
    done
}

for port in "${ports[@]}"; do
    out=$WORK/$port/out
    src=$WORK/$port/src
    mkdir -p "$out" "$src"
    ln -s "$ROOT/resources" "$out/resources"
    cp "$WORK/sample.md" "$out/"
    case $port in
        java) lang=java ext=java ;;
        dotnet) lang=csharp ext=cs ;;
        go) lang=go ext=go ;;
        swift) lang=swift ext=swift ;;
    esac
    names=$(sources "llms-$port.txt" $lang $ext "$src")
    if [ "$(echo $names)" != "Hello Report Markdown" ]; then
        fail "llms-$port.txt: the samples are \"$(echo $names)\", not Hello, Report and Markdown"
        continue
    fi
    echo "Building and running the samples of llms-$port.txt"
    log=$WORK/$port/build.log
    case $port in
    java)
        RELEASE="--release 8"
        if ! javac --release 8 -version > /dev/null 2>&1; then
            RELEASE=""
        fi
        # The class of the harness is Markdown_, as com.pdfjet has a Markdown
        mv "$src/Markdown.java" "$src/Markdown_.java"
        if ! javac -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror -d "$WORK/java/classes" \
                com/pdfjet/*.java com/pdfjet/*/*.java "$src"/*.java > "$log" 2>&1; then
            cat "$log"; fail "llms-java.txt: a sample does not compile"; continue
        fi
        # The fonts PDFjet carries, as the jar of Maven Central has them
        mkdir -p "$WORK/java/classes/com/pdfjet/fonts"
        cp -R resources/fonts/* "$WORK/java/classes/com/pdfjet/fonts/"
        for class in Hello Report Markdown_; do
            (cd "$out" && java -cp "$WORK/java/classes" $class) || fail "llms-java.txt: $class failed"
        done
        ;;
    dotnet)
        if ! dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true > "$log" 2>&1; then
            tail -n 20 "$log"; fail "PDFjet.csproj does not build"; continue
        fi
        for name in Hello Report Markdown; do
            mkdir -p "$src/$name"
            mv "$src/$name.cs" "$src/$name/"
            cat > "$src/$name/$name.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="PDFjet"><HintPath>$ROOT/bin/release/net8.0/PDFjet.dll</HintPath></Reference>
  </ItemGroup>
</Project>
PROJECT
            if ! dotnet build "$src/$name/$name.csproj" -c release -p:TreatWarningsAsErrors=true \
                    -o "$WORK/dotnet/bin/$name" >> "$log" 2>&1; then
                tail -n 20 "$log"; fail "llms-dotnet.txt: $name does not compile"; continue
            fi
            (cd "$out" && dotnet "$WORK/dotnet/bin/$name/$name.dll") || fail "llms-dotnet.txt: $name failed"
        done
        ;;
    go)
        # In the module of the repository, so that they build against it
        for name in Hello Report Markdown; do
            mkdir -p "$src/$name"
            mv "$src/$name.go" "$src/$name/main.go"
            pkg=./${src#"$ROOT"/}/$name
            # gofmt prints the files it would change
            if [ -n "$(gofmt -l "$src/$name/main.go")" ]; then
                fail "llms-go.txt: $name is not formatted as gofmt formats it"
            fi
            if ! { go vet "$pkg" && go build -o "$WORK/go/bin/$name" "$pkg"; } >> "$log" 2>&1; then
                cat "$log"; fail "llms-go.txt: $name does not build"; continue
            fi
            (cd "$out" && "$WORK/go/bin/$name") || fail "llms-go.txt: $name failed"
        done
        ;;
    swift)
        # A package of their own that depends on this checkout, as an app does
        identity=$(basename "$ROOT" | tr '[:upper:]' '[:lower:]')
        pkg=$WORK/swift/package
        for name in Hello Report Markdown; do
            mkdir -p "$pkg/Sources/$name"
            mv "$src/$name.swift" "$pkg/Sources/$name/main.swift"
        done
        cat > "$pkg/Package.swift" <<PACKAGE
// swift-tools-version: 6.1
import PackageDescription

let package = Package(
    name: "LLMsSamples",
    dependencies: [.package(path: "$ROOT")],
    targets: ["Hello", "Report", "Markdown"].map {
        .executableTarget(name: \$0, dependencies: [.product(name: "PDFjet", package: "$identity")])
    }
)
PACKAGE
        if ! swift build --package-path "$pkg" -Xswiftc -warnings-as-errors > "$log" 2>&1; then
            tail -n 30 "$log"; fail "llms-swift.txt: a sample does not compile"; continue
        fi
        bin=$(swift build --package-path "$pkg" --show-bin-path)
        for name in Hello Report Markdown; do
            (cd "$out" && "$bin/$name") || fail "llms-swift.txt: $name failed"
        done
        ;;
    esac

    for pair in $EXPECTED; do
        name=${pair%=*}
        want=${pair#*=}
        if [ ! -s "$out/$name.pdf" ] || ! tail -c 32 "$out/$name.pdf" | grep -q '%%EOF'; then
            fail "llms-$port.txt: $name.pdf was not written"
            continue
        fi
        got=$(pagecount "$out/$name.pdf")
        if [ "$got" != "$want" ]; then
            fail "llms-$port.txt: $name.pdf has ${got:-no} pages, not $want"
        fi
    done
done

VERAPDF=${VERAPDF:-verapdf}
pdfs=$(ls "$WORK"/*/out/*.pdf 2> /dev/null)
if [ -n "$pdfs" ] && command -v "$VERAPDF" > /dev/null; then
    # One line a PDF, PASS or FAIL, and the rules it breaks
    result=$("$VERAPDF" --flavour ua1 --format text $pdfs)
    while IFS= read -r line; do
        case $line in
            PASS*) ;;
            *) fail "veraPDF ua1: $line" ;;
        esac
    done <<< "$result"
    if [ "$(grep -c '^PASS' <<< "$result")" -ne "$(echo "$pdfs" | wc -l)" ]; then
        fail "veraPDF ua1 did not pass every PDF"
    fi
    echo "Checked $(echo "$pdfs" | wc -l) PDFs with veraPDF ua1."
else
    echo "veraPDF was not found, so the PDFs were not checked for PDF/UA-1."
fi

if [ $failed = 1 ]; then
    echo "The guides for AI coding assistants do not hold."
    exit 1
fi
echo "The samples of the guides for ${ports[*]} build, run and write the pages they should."
