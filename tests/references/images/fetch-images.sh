#!/bin/sh
# Fetches the images that tests/references/images/check-images.py reads: the
# test images of Pillow, at the commit of the release the check is run with,
# and the BMP Suite of Jason Summers, at the release below, whose checksum is
# checked, so that a run reads the same files as the last.
#
#     tests/references/images/fetch-images.sh [DIR]
#
# DIR is .images by default. The files are fetched, not committed: their
# copyright belongs to many people. Pillow's images are real files from many
# writers -- Photoshop, GIMP, Paint, the cameras of phones with their Exif,
# CMYK and progressive JPEGs, PNGs of 16 bits, with palettes, tRNS chunks or
# interlaced, BMPs of every header -- and a good part of them are broken on
# purpose. The BMP Suite has the good, the questionable and the bad BMPs of
# every kind the format allows. The PngSuite and the images of the repository
# are read where they are, and are not fetched.
set -e

# The commit of the tag 12.3.0, the Pillow the check decodes with.
PILLOW_COMMIT=bb1d8e8ab8d29048624d96e3ee53cecf7c13d13d
BMPSUITE_URL=https://entropymine.com/jason/bmpsuite/releases/bmpsuite-2.8.zip
BMPSUITE_SHA256=f1b1f18c4f310b76bf317378e64f25b84f875a3d0976fab33ff10befdb35d13f
DIR=${1:-.images}

# fetch URL DIR COMMIT [SPARSE PATHS...]
fetch() {
    url=$1 dir=$2 commit=$3
    shift 3
    if [ "$(git -C "$dir" rev-parse HEAD 2> /dev/null)" = "$commit" ]; then
        return
    fi
    rm -rf "$dir"
    git init -q "$dir"
    git -C "$dir" remote add origin "$url"
    if [ $# -gt 0 ]; then
        git -C "$dir" sparse-checkout set "$@"
    fi
    git -C "$dir" fetch -q --depth 1 --filter=blob:none origin "$commit"
    git -C "$dir" checkout -q FETCH_HEAD
}

# download URL SHA256 FILE
download() {
    if [ -f "$3" ] && echo "$2  $3" | sha256sum -c --status; then
        return
    fi
    curl -sSfL -o "$3.part" "$1"
    if ! echo "$2  $3.part" | sha256sum -c --status; then
        echo "$1 does not have the checksum $2." >&2
        rm -f "$3.part"
        exit 1
    fi
    mv "$3.part" "$3"
}

mkdir -p "$DIR"
fetch https://github.com/python-pillow/Pillow.git "$DIR/pillow" $PILLOW_COMMIT Tests/images
download $BMPSUITE_URL $BMPSUITE_SHA256 "$DIR/bmpsuite.zip"
if [ ! -d "$DIR/bmpsuite" ] || [ "$DIR/bmpsuite.zip" -nt "$DIR/bmpsuite" ]; then
    rm -rf "$DIR/bmpsuite" "$DIR/bmpsuite-2.8"
    (cd "$DIR" && unzip -q bmpsuite.zip && mv bmpsuite-2.8 bmpsuite)
fi

# The count of the files the check reads, by kind, of what was fetched.
for kind in png jpg jpeg bmp; do
    printf '%6d .%s\n' "$(find "$DIR/pillow/Tests/images" "$DIR/bmpsuite" -type f -iname "*.$kind" | wc -l)" $kind
done
