#!/bin/bash
# Writes the metrics files of the fonts of a folder.
#
#   util/generate-font-metrics-files.sh fonts/IBMPlexSans

cd "$(dirname "$0")/.." || exit 1
mkdir -p util/out
javac -d util/out util/GenerateFontMetricsFiles.java || exit 1
java -cp util/out GenerateFontMetricsFiles "$1"
