#!/bin/bash
# Measures Table in the four ports of PDFjet: the 9 columns of the Electric
# Vehicle Population CSV built as Cell objects and drawn on as many Letter
# pages as they need. The results go to
# benchmarks/table/build/results-<date>.log. See benchmarks/table/README.md.
#
#   benchmarks/table/run.sh            all four ports, about 15 minutes
#   benchmarks/table/run.sh java go    only the ports named
#
# ROWS overrides the table sizes, for example ROWS="2000 10000".
#
# Needs a JDK, the .NET SDK, Go and Swift, and GNU time; mutool for the checks
# of the sample files. Nothing else should run meanwhile.

set -euo pipefail
cd "$(dirname "$0")/../.."
B=benchmarks/table/build
PORTS=${*:-"java dotnet go swift"}
ROWS=${ROWS:-"2000 10000 50000"}
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
    javac -nowarn -encoding utf-8 -d "$B/classes" -cp "$B/jet" benchmarks/table/TableBench.java
fi
if has dotnet; then
    dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true --nologo -v quiet
    dotnet build benchmarks/table/TableBench.csproj -c release --nologo -v quiet -o "$B/dotnet"
fi
if has go; then
    go build -o "$B/tablebench" benchmarks/table/tablebench/main.go
fi
if has swift; then
    swift build --package-path benchmarks/table/swift -c release -Xswiftc -warnings-as-errors
    SWIFT_BIN=$(swift build --package-path benchmarks/table/swift -c release --show-bin-path)
fi

# How each port is run, with its runtime at its defaults.
run() {
    case $1 in
    java)   shift; java -cp "$B/classes:$B/jet" TableBench "$@" ;;
    dotnet) shift; dotnet "$B/dotnet/TableBench.dll" "$@" ;;
    go)     shift; "$B/tablebench" "$@" ;;
    swift)  shift; "$SWIFT_BIN/TableBench" "$@" ;;
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

    echo "== table: 9 columns of the EV CSV as cells, IBM Plex Sans 8 pt, Letter"
    for rows in $ROWS; do
        for p in $PORTS; do run "$p" bench "$rows"; done
    done
    echo "== table: first document in a new process, 50000 rows"
    for p in $PORTS; do run "$p" cold 50000; done
    echo "== table: peak memory, one 50000-row table, runtime defaults, median of 3"
    for p in $PORTS; do
        case $p in
        java)   echo "java peak $(peak java -cp "$B/classes:$B/jet" TableBench cold 50000) KB" ;;
        dotnet) echo "dotnet peak $(peak dotnet "$B/dotnet/TableBench.dll" cold 50000) KB" ;;
        go)     echo "go peak $(peak "$B/tablebench" cold 50000) KB" ;;
        swift)  echo "swift peak $(peak "$SWIFT_BIN/TableBench" cold 50000) KB" ;;
        esac
    done
    echo "== table: 40-row samples"
    for p in $PORTS; do
        run "$p" sample 40 "$B/pdf/table-$p.pdf"
        if command -v mutool > /dev/null; then
            echo "$p: $(stat -c %s "$B/pdf/table-$p.pdf") bytes, page 1: $(mutool draw -q -F txt "$B/pdf/table-$p.pdf" 1 2>/dev/null | tr '\n' '|' | cut -c1-120)"
        fi
    done
    date -Iseconds
} 2>&1 | tee "$LOG"
