#!/bin/sh
# Fetches the fonts that tests/references/fonts/check-fonts.py reads besides
# the ones PDFjet ships: six popular fonts that are built in six different
# ways, and Adobe's AFM files of the 14 core fonts, each at the commit or
# with the checksum below, so that a run reads the same files as the last.
#
#     tests/references/fonts/fetch-fonts.sh [DIR]
#
# DIR is .reference-fonts by default. The files are fetched, not committed:
# their licenses are their own. It writes:
#
#   afm/*.afm, afm/MustRead.html  Adobe's Core14 AFM files, as Apache PDFBox
#       ships them, with the license note Adobe gives them in MustRead.html,
#       and afm/WinAnsiEncoding.java, PDFBox's table of the glyph names of
#       WinAnsiEncoding from Annex D of ISO 32000 (Apache License 2.0).
#   roboto/Roboto[wdth,wght].ttf  Google Fonts' build: a variable TrueType
#       font, which PDFjet reads at its default instance, with the metrics of
#       its hmtx table and no static glyphs to fall back on.
#   inter/Inter-Regular.ttf  A static instance cut from a variable design by
#       fontmake, with a cmap that starts at U+0000, format 12 beside format
#       4, and USE_TYPO_METRICS set.
#   dejavu/DejaVuSans.ttf  FontForge's build of a large font: 6,253 glyphs,
#       fewer advance widths than glyphs, characters past the BMP, a kern
#       table beside GPOS and a version 1 OS/2 table, which has no cap height.
#   liberation/LiberationSerif-Regular.ttf  The metric-compatible Times New
#       Roman: hinted TrueType with a line gap in hhea.
#   sourcehan/SourceHanSansJP-Regular.otf  Adobe's CID-keyed CFF font, the
#       build of Noto Sans CJK: 17,944 glyphs, 17,617 advance widths, and a
#       cmap with 653 characters past the BMP.
#   vera/Vera.ttf  Bitstream Vera Sans of 2003, the old style of TrueType:
#       a kern table and no GPOS, a Macintosh format 0 cmap and a Windows
#       format 4 one, and a version 1 OS/2 table.
set -e

PDFBOX_COMMIT=c11132642d6a5456694cec48b7c0e9c3815c9517
GOOGLE_FONTS_COMMIT=e44c4b011a820c2cbe2fd2cfa8052037d7edb571
SOURCE_HAN_SANS_COMMIT=a4f7cf94edfb9d7ffbdfc4841de276358bd7e0f2
DIR=${1:-.reference-fonts}

# fetch URL DIR COMMIT PATHS...: the paths of the repository at the commit.
fetch() {
    url=$1 dir=$2 commit=$3
    shift 3
    if [ "$(git -C "$dir" rev-parse HEAD 2> /dev/null)" = "$commit" ]; then
        git -C "$dir" sparse-checkout set --no-cone "$@"
        return
    fi
    rm -rf "$dir"
    git init -q "$dir"
    git -C "$dir" remote add origin "$url"
    git -C "$dir" sparse-checkout set --no-cone "$@"
    git -C "$dir" fetch -q --depth 1 --filter=blob:none origin "$commit"
    git -C "$dir" checkout -q FETCH_HEAD
}

sha256() {
    if command -v sha256sum > /dev/null; then
        sha256sum "$1" | cut -d ' ' -f 1
    else
        shasum -a 256 "$1" | cut -d ' ' -f 1
    fi
}

# download URL SHA256 FILE: the file at the URL, which must have the checksum.
download() {
    url=$1 sum=$2 file=$3
    if [ -f "$file" ] && [ "$(sha256 "$file")" = "$sum" ]; then
        return
    fi
    curl -sSLf -o "$file.part" "$url"
    if [ "$(sha256 "$file.part")" != "$sum" ]; then
        echo "$url does not have the SHA-256 $sum" >&2
        exit 1
    fi
    mv "$file.part" "$file"
}

# extract ARCHIVE OUTDIR MEMBERS...: the members, without their folders.
extract() {
    archive=$1 out=$2
    shift 2
    rm -rf "$out"
    mkdir -p "$out"
    case $archive in
    *.zip) unzip -q -j -o "$archive" "$@" -d "$out" ;;
    *.tar.bz2) tar -xjf "$archive" -C "$out" "$@" ;;
    *.tar.gz) tar -xzf "$archive" -C "$out" "$@" ;;
    esac
    for member in "$@"; do
        case $member in
        */*) if [ -f "$out/$member" ]; then mv "$out/$member" "$out/"; fi ;;
        esac
    done
    find "$out" -mindepth 1 -type d -exec rm -rf {} +
}

mkdir -p "$DIR/downloads"

fetch https://github.com/apache/pdfbox.git "$DIR/git/pdfbox" $PDFBOX_COMMIT \
    /pdfbox/src/main/resources/org/apache/pdfbox/resources/afm/ \
    /pdfbox/src/main/java/org/apache/pdfbox/pdmodel/font/encoding/WinAnsiEncoding.java
rm -rf "$DIR/afm"
cp -R "$DIR/git/pdfbox/pdfbox/src/main/resources/org/apache/pdfbox/resources/afm" "$DIR/afm"
cp "$DIR/git/pdfbox/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/font/encoding/WinAnsiEncoding.java" "$DIR/afm/"

fetch https://github.com/google/fonts.git "$DIR/git/google-fonts" $GOOGLE_FONTS_COMMIT \
    /ofl/roboto/
rm -rf "$DIR/roboto"
mkdir -p "$DIR/roboto"
cp "$DIR/git/google-fonts/ofl/roboto/Roboto[wdth,wght].ttf" "$DIR/git/google-fonts/ofl/roboto/OFL.txt" "$DIR/roboto/"

fetch https://github.com/adobe-fonts/source-han-sans.git "$DIR/git/source-han-sans" $SOURCE_HAN_SANS_COMMIT \
    /SubsetOTF/JP/SourceHanSansJP-Regular.otf /LICENSE.txt
rm -rf "$DIR/sourcehan"
mkdir -p "$DIR/sourcehan"
cp "$DIR/git/source-han-sans/SubsetOTF/JP/SourceHanSansJP-Regular.otf" "$DIR/git/source-han-sans/LICENSE.txt" "$DIR/sourcehan/"

download https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip \
    9883fdd4a49d4fb66bd8177ba6625ef9a64aa45899767dde3d36aa425756b11e "$DIR/downloads/Inter-4.1.zip"
extract "$DIR/downloads/Inter-4.1.zip" "$DIR/inter" extras/ttf/Inter-Regular.ttf LICENSE.txt

download https://github.com/dejavu-fonts/dejavu-fonts/releases/download/version_2_37/dejavu-fonts-ttf-2.37.tar.bz2 \
    fa9ca4d13871dd122f61258a80d01751d603b4d3ee14095d65453b4e846e17d7 "$DIR/downloads/dejavu-fonts-ttf-2.37.tar.bz2"
extract "$DIR/downloads/dejavu-fonts-ttf-2.37.tar.bz2" "$DIR/dejavu" \
    dejavu-fonts-ttf-2.37/ttf/DejaVuSans.ttf dejavu-fonts-ttf-2.37/LICENSE

download https://github.com/liberationfonts/liberation-fonts/files/7261482/liberation-fonts-ttf-2.1.5.tar.gz \
    7191c669bf38899f73a2094ed00f7b800553364f90e2637010a69c0e268f25d0 "$DIR/downloads/liberation-fonts-ttf-2.1.5.tar.gz"
extract "$DIR/downloads/liberation-fonts-ttf-2.1.5.tar.gz" "$DIR/liberation" \
    liberation-fonts-ttf-2.1.5/LiberationSerif-Regular.ttf liberation-fonts-ttf-2.1.5/LICENSE

download https://download.gnome.org/sources/ttf-bitstream-vera/1.10/ttf-bitstream-vera-1.10.tar.bz2 \
    db5b27df7bbb318036ebdb75acd3e98f1bd6eb6608fb70a67d478cd243d178dc "$DIR/downloads/ttf-bitstream-vera-1.10.tar.bz2"
extract "$DIR/downloads/ttf-bitstream-vera-1.10.tar.bz2" "$DIR/vera" \
    ttf-bitstream-vera-1.10/Vera.ttf ttf-bitstream-vera-1.10/COPYRIGHT.TXT
