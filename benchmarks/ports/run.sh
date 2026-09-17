#!/bin/bash
# Compares the four ports of PDFjet with each other on the text document of
# section 3 of pdfjet-benchmarks.html, and writes the results to
# benchmarks/ports/build/results-<date>.log. See benchmarks/ports/README.md.
#
#   benchmarks/ports/run.sh            all four ports, about 10 minutes
#   benchmarks/ports/run.sh java go    only the ports named
#
# Needs a JDK, the .NET SDK, Go and Swift, and GNU time; mutool for the checks
# of the sample files. Nothing else should run meanwhile.

set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT=$(pwd)
B=benchmarks/ports/build
PORTS=${*:-"java dotnet go swift"}
mkdir -p "$B/pdf"
SWIFT_BIN=""
LOG="$B/results-$(date +%Y-%m-%d-%H%M).log"

has() { [[ " $PORTS " == *" $1 "* ]]; }

# Everything is built first, so that no build runs while a port is measured.
if has java; then
    rm -rf "$B/jet" "$B/classes"
    javac -O -encoding utf-8 --release 8 -nowarn -d "$B/jet" \
        com/pdfjet/*.java com/pdfjet/barcodes/*.java com/pdfjet/pdf417/*.java com/pdfjet/qrcode/*.java \
        com/pdfjet/datamatrix/*.java com/pdfjet/fonts/*.java com/pdfjet/encryption/*.java 2>&1 | grep -v '^Note:' || true
    javac -nowarn -encoding utf-8 -d "$B/classes" -cp "$B/jet" benchmarks/ports/PortBench.java
fi
if has dotnet; then
    dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true --nologo -v quiet
    dotnet build benchmarks/ports/PortBench.csproj -c release --nologo -v quiet -o "$B/dotnet"
fi
if has go; then
    go build -o "$B/portbench" benchmarks/ports/portbench/main.go
fi
if has swift; then
    swift build --package-path benchmarks/ports/swift -c release -Xswiftc -warnings-as-errors
    SWIFT_BIN=$(swift build --package-path benchmarks/ports/swift -c release --show-bin-path)
fi

# How each port is run, with its runtime at its defaults.
run() {
    case $1 in
    java)   shift; java -cp "$B/classes:$B/jet" PortBench "$@" ;;
    dotnet) shift; dotnet "$B/dotnet/PortBench.dll" "$@" ;;
    go)     shift; "$B/portbench" "$@" ;;
    swift)  shift; "$SWIFT_BIN/PortBench" "$@" ;;
    esac
}

# The median peak resident size, in KB, of a new process at its defaults.
peak() {
    for i in 1 2 3; do
        /usr/bin/time -o "$B/peak.txt" -f "%M" "$@" > /dev/null 2>&1 || echo "failed"
        tail -1 "$B/peak.txt"
    done | sort -n | awk '{v[NR] = $1} END {print v[int((NR + 1) / 2)]}'
}

{
    echo "== environment"
    if has java; then java -version 2>&1 | head -1; fi
    if has dotnet; then echo ".NET SDK $(dotnet --version)"; fi
    if has go; then go version; fi
    if has swift; then swift --version 2>&1 | head -1; fi
    echo "cpus $(nproc), $(grep -m1 'model name' /proc/cpuinfo | sed 's/.*: //')"
    date -Iseconds
    git log --oneline -1 | cat

    echo "== ports: Latin, Greek and Cyrillic, 60 lines per page, IBM Plex Sans"
    for pages in 100 500; do
        for p in $PORTS; do run "$p" bench "$pages"; done
    done
    echo "== ports: first document in a new process, 500 pages"
    for p in $PORTS; do run "$p" cold 500; done
    echo "== ports: peak memory, one 500-page document, runtime defaults, median of 3"
    for p in $PORTS; do
        case $p in
        java)   echo "java peak $(peak java -cp "$B/classes:$B/jet" PortBench cold 500) KB" ;;
        dotnet) echo "dotnet peak $(peak dotnet "$B/dotnet/PortBench.dll" cold 500) KB" ;;
        go)     echo "go peak $(peak "$B/portbench" cold 500) KB" ;;
        swift)  echo "swift peak $(peak "$SWIFT_BIN/PortBench" cold 500) KB" ;;
        esac
    done
    echo "== ports: 3-page samples"
    for p in $PORTS; do
        run "$p" sample 3 "$B/pdf/text-$p.pdf"
        if command -v mutool > /dev/null; then
            echo "$p: $(stat -c %s "$B/pdf/text-$p.pdf") bytes, page 1: $(mutool draw -q -F txt "$B/pdf/text-$p.pdf" 1 2>/dev/null | head -3 | tr '\n' '|')"
        fi
    done
    date -Iseconds
} 2>&1 | tee "$LOG"
