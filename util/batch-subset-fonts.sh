#!/bin/bash

FONT_DIR="../fonts/NotoSansSC/"
UNICODES_FILE="../data/languages/SC_3500.txt"

# Explicitly force the commas and periods that were disappearing
# We combine the range with specific codepoints to ensure they are hit
# U+3001 (Ideographic Comma), U+3002 (Ideographic Full Stop)
# U+FF0C (Fullwidth Comma), U+FF0E (Fullwidth Full Stop)
EXPLICIT_PUNCTUATION="U+3001,U+3002,U+FF0C,U+FF0E,U+3000-303F,U+FF00-FFEF,U+2000-206F"

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
        --unicodes-file="$UNICODES_FILE" \
	--unicodes="$CJK_PUNCTUATION" \
        --output-file="$FONT_DIR/$output"
done
