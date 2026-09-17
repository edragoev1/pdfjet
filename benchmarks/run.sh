#!/bin/bash
# Benchmarks PDFjet for Java, and writes the results to
# benchmarks/build/results-<date>.log. See benchmarks/README.md.
#
#   benchmarks/run.sh              both benchmarks, a few minutes
#   benchmarks/run.sh text         the multilingual text document only
#   benchmarks/run.sh table        Example_43's big table only
#   benchmarks/run.sh all --quick  a short run that checks that everything works
#
# Needs a JDK (21 was used) and GNU time; mutool for the checks
# of the sample files, if it is installed. Nothing else should run meanwhile.

set -euo pipefail
cd "$(dirname "$0")/.."
ROOT=$(pwd)
B=benchmarks/build
WHAT=${1:-all}
QUICK=${2:-}
mkdir -p "$B/pdf"
LOG="$B/results-$(date +%Y-%m-%d-%H%M).log"

# The PDFjet library from this checkout, without the examples.
rm -rf "$B/jet-classes" "$B/classes"
javac -O -encoding utf-8 --release 8 -nowarn -d "$B/jet-classes" \
    com/pdfjet/*.java com/pdfjet/barcodes/*.java com/pdfjet/pdf417/*.java com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java com/pdfjet/fonts/*.java com/pdfjet/encryption/*.java 2>&1 | grep -v '^Note:' || true
jar cf "$B/PDFjet.jar" -C "$B/jet-classes" .

CP="$B/PDFjet.jar"
javac -nowarn -encoding utf-8 -d "$B/classes" -cp "$CP" benchmarks/TextBench.java benchmarks/BigTableBench.java
CP="$B/classes:$CP"
PROPS="-Dpdfjet.root=$ROOT"

# The median peak resident size, in KB, of a new JVM at its defaults.
peak() {
    local runs=$1; shift
    for ((i = 0; i < runs; i++)); do
        /usr/bin/time -o "$B/peak.txt" -f "%M" "$@" > /dev/null 2>&1 || echo "failed"
        tail -1 "$B/peak.txt"
    done | sort -n | awk '{v[NR] = $1} END {print v[int((NR + 1) / 2)]}'
}

{
    echo "== environment"
    java -version 2>&1 | head -1
    echo "cpus $(nproc), $(grep -m1 'model name' /proc/cpuinfo | sed 's/.*: //')"
    date -Iseconds
    git log --oneline -1 | cat

    if [ "$WHAT" = all ] || [ "$WHAT" = text ]; then
        CONFIGS="jet-plex jet-noto"
        SIZES="50 100 200 500"; COLD=20; PEAK_PAGES=500; RUNS=3
        if [ -n "$QUICK" ]; then SIZES="50"; COLD=5; PEAK_PAGES=50; RUNS=1; fi
        J="java -Xmx4g $PROPS -cp $CP TextBench"
        echo "== text: Latin, Greek and Cyrillic, 60 lines per page"
        for pages in $SIZES; do
            for c in $CONFIGS; do $J bench $c $pages 2>&1 | grep 'pages:'; done
        done
        echo "== text: first document in a new JVM, $COLD pages"
        for c in $CONFIGS; do $J cold $c $COLD 2>&1 | grep cold; done
        echo "== text: peak memory, one $PEAK_PAGES-page document, JVM defaults, median of $RUNS"
        for c in $CONFIGS; do
            echo "$c peak $(peak $RUNS java $PROPS -cp "$CP" TextBench cold $c $PEAK_PAGES) KB"
        done
        echo "== text: 3-page samples"
        for c in $CONFIGS; do
            $J sample $c 3 "$B/pdf/text-$c.pdf"
            if command -v mutool > /dev/null; then
                echo "$c: $(stat -c %s "$B/pdf/text-$c.pdf") bytes, page 1: $(mutool draw -q -F txt "$B/pdf/text-$c.pdf" 1 2>/dev/null | head -3 | tr '\n' '|')"
            fi
        done
    fi

    if [ "$WHAT" = all ] || [ "$WHAT" = table ]; then
        CONFIGS=${TABLE_CONFIGS:-"jet jet-table jet-page jet-page-stream"}
        CSV="$ROOT/data/Electric_Vehicle_Population_Data.csv"; RUNS=3; HEAPS="32m 64m 128m 256m 512m 1g 2g 4g 8g"
        if [ -n "$QUICK" ]; then CSV="$ROOT/data/Electric_Vehicle_Population_10_Pages.csv"; RUNS=1; HEAPS="32m 64m"; fi
        J="java -Xmx8g $PROPS -cp $CP BigTableBench"
        echo "== table: Example_43, $(basename "$CSV")"
        for c in $CONFIGS; do $J bench $c "$CSV" 2>&1 | grep -E 'median|Exception'; done
        echo "== table: first run in a new JVM"
        for c in $CONFIGS; do $J cold $c "$CSV" 2>&1 | grep -E 'cold|Exception'; done
        echo "== table: peak memory, JVM defaults, median of $RUNS"
        for c in $CONFIGS; do
            echo "$c peak $(peak $RUNS java $PROPS -cp "$CP" BigTableBench cold $c "$CSV") KB"
        done
        echo "== table: smallest heap that finishes, of $HEAPS"
        for c in $CONFIGS; do
            found=""
            for h in $HEAPS; do
                if java -Xmx$h $PROPS -cp "$CP" BigTableBench cold $c "$CSV" > "$B/heap.txt" 2>&1 && grep -q cold "$B/heap.txt"; then
                    found=$h
                    break
                fi
            done
            echo "$c: ${found:-more than the largest tried}"
        done
        echo "== table: samples"
        for c in $CONFIGS; do
            $J sample $c "$CSV" "$B/pdf/table-$c.pdf"
            if command -v mutool > /dev/null; then
                n=$(mutool info "$B/pdf/table-$c.pdf" 2>/dev/null | awk '/^Pages/ {print $2}')
                echo "$c: $(stat -c %s "$B/pdf/table-$c.pdf") bytes, $n pages, last footer: $(mutool draw -q -F txt "$B/pdf/table-$c.pdf" "$n" 2>/dev/null | grep 'Page ' | tail -1)"
            fi
        done
    fi
    date -Iseconds
} 2>&1 | tee "$LOG"
