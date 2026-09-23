#!/bin/bash
# Writes the TypeScript metrics files of the fonts of a folder, for
# pdfjet-client, to the output folder, or to the current one without it. The
# output folder is taken from where the script is run.
#
#   util/generate-font-metrics-files.sh fonts/IBMPlexSans ../pdfjet-client/src/fonts/IBMPlexSans
#
# The generator reads the fonts with OTF, which is not public, so it is built
# with the library, in its package.

FONTS=$(cd "$1" && pwd) || exit 1
OUT=$(cd "${2:-.}" && pwd) || exit 1
cd "$(dirname "$0")/.." || exit 1
mkdir -p util/out
javac -encoding utf-8 -nowarn -d util/out com/pdfjet/*.java com/pdfjet/*/*.java util/GenerateFontMetricsFiles.java || exit 1
java -cp util/out com.pdfjet.GenerateFontMetricsFiles "$FONTS" "$OUT"
