#!/bin/bash

FONT_DIR="fonts/NotoSansTC/"
UNICODES_FILE="data/languages/TC_4808.txt"
CJK_PUNCTUATION="U+0000-007F,U+2000-206F,U+3000-303F,U+FF00-FFEF"

# Process each font file
for font in "$FONT_DIR"/*.ttf; do
    # Skip if no matches (glob pattern not expanded)
    [[ ! -f "$font" ]] && continue
    
    # Extract filename without path
    filename=$(basename "$font")
    
    # Insert "-TC4808" before .ttf extension
    basename_noext="${filename%.ttf}"
    output="${basename_noext}-TC4808.ttf"
    
    echo "Processing: $filename → $output"
    
    pyftsubset "$font" \
	--unicodes="$CJK_PUNCTUATION" \
        --unicodes-file="$UNICODES_FILE" \
        --output-file="$FONT_DIR/$output"
done
