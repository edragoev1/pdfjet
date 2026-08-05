#!/bin/bash

FONT_DIR="../fonts/NotoSansSC/"
# Combined ranges for fully functional Chinese typography:
# 1. CJK Symbols and Punctuation (Ideographic comma, stop, brackets)
# 2. General Punctuation (Dashes, quotes, ellipsis)
# 3. Halfwidth and Fullwidth Forms (Full-width comma, period, Latin/Cyrillic)
CJK_PUNCTUATION="U+3000-303F,U+2000-206F,U+FF00-FFEF"
UNICODES_FILE="../data/languages/SC_3500.txt"

# Process each font file
for font in "$FONT_DIR"/*.ttf; do
    # Skip if no matches (glob pattern not expanded)
    [[ ! -f "$font" ]] && continue
    
    # Extract filename without path
    filename=$(basename "$font")
    
    # Insert "-3500" before .ttf extension
    basename_noext="${filename%.ttf}"
    output="${basename_noext}-SC3500.ttf"
    
    echo "Processing: $filename → $output"
    
    pyftsubset "$font" \
	--unicodes="$CJK_PUNCTUATION" \
        --unicodes-file="$UNICODES_FILE" \
        --output-file="$FONT_DIR/$output"
done
