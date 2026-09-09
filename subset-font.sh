#!/bin/bash

# subset_font.sh - A script to create a subsetted TTF font while preserving Latin, Greek, Cyrillic, and punctuation.
# Usage: ./subset_font.sh <input_font> <common_char_file> <output_font_ttf>

set -e # Exit immediately if any command fails

# Check for required arguments
if [ $# -lt 3 ]; then
        echo "Error: Missing required arguments."
        echo "Usage: $0 <input_font> <common_char_file> <output_font_ttf>"
        echo "  <input_font>        Path to the source font file (e.g., NotoSansJP-Regular.ttf)"
        echo "  <common_char_file>  Path to the file with common characters (one per line)"
        echo "  <output_font_ttf>   Path for the generated subset TTF font (will be compressed later)"
        exit 1
fi

# Assign input parameters
INPUT_FONT="$1"
COMMON_CHAR_FILE="$2"
OUTPUT_FONT="$3"

# Validate input file exists
if [ ! -f "$INPUT_FONT" ]; then
        echo "Error: Input font file not found: $INPUT_FONT"
        exit 1
fi

if [ ! -f "$COMMON_CHAR_FILE" ]; then
        echo "Error: Common character file not found: $COMMON_CHAR_FILE"
        exit 1
fi

# Define Unicode ranges to PRESERVE (Latin, Greek, Cyrillic, punctuation, symbols)
UNICODE_RANGES="U+0000-007F"   # Basic Latin
UNICODE_RANGES+=",U+0080-00FF" # Latin-1 Supplement
UNICODE_RANGES+=",U+0100-017F" # Latin Extended-A
UNICODE_RANGES+=",U+0180-024F" # Latin Extended-B
UNICODE_RANGES+=",U+0370-03FF" # Greek and Coptic
UNICODE_RANGES+=",U+0400-04FF" # Cyrillic
UNICODE_RANGES+=",U+1F00-1FFF" # Greek Extended
UNICODE_RANGES+=",U+2000-206F" # General Punctuation
UNICODE_RANGES+=",U+20A0-20CF" # Currency Symbols
UNICODE_RANGES+=",U+2100-214F" # Letterlike Symbols
UNICODE_RANGES+=",U+2200-22FF" # Mathematical Operators

echo "Starting TTF font subsetting..."
echo "  Input Font: $INPUT_FONT"
echo "  Common Chars: $COMMON_CHAR_FILE"
echo "  Output Font: $OUTPUT_FONT"
echo "  Preserving: Latin, Greek, Cyrillic, Punctuation, Symbols"

# Execute the pyftsubset command - outputting to TTF
pyftsubset "$INPUT_FONT" \
    --text-file="$COMMON_CHAR_FILE" \
    --unicodes="$UNICODE_RANGES" \
    --output-file="$OUTPUT_FONT" \
    --flavor="ttf" \  # Explicitly output TTF format
    --verbose

# Check if the command succeeded
if [ $? -eq 0 ]; then
        echo "Successfully created subset TTF font: $OUTPUT_FONT"
        # Display file size information
        INPUT_SIZE=$(du -h "$INPUT_FONT" | cut -f1)
        OUTPUT_SIZE=$(du -h "$OUTPUT_FONT" | cut -f1)
        echo "File size reduced: $INPUT_SIZE --> $OUTPUT_SIZE"
        echo "Next step: Compress this TTF file to .ttf.stream format using PDFjet's tool"
else
        echo "Error: Font subsetting failed. Please check the output above."
        exit 1
fi
