#!/bin/bash
# Copies the Swift package of a release into the repository pdfjet-swift,
# beside this one or in PDFJET_SWIFT, and commits and tags it there with the
# release's number:
#
#   .packaging/package-swift.sh 9.1.9
#
# SwiftPM clones the whole history of the repository a package names, and
# this one's is about 1 GB, of fonts, data and example PDFs of every release.
# pdfjet-swift has the Swift library alone: Package.swift with the products
# PDFjet, PDFjetFontsWorld and PDFjetFonts, Sources/PDFjet, the fonts PDFjet
# carries (resources/fonts), LICENSE, its README and llms-swift.txt, about
# 60 MB, and each release adds only what changed. The examples, the tests and
# the other ports stay here.
#
# The files are those of the tag vX.Y.Z of this repository, not of the
# working tree. Pushing pdfjet-swift and its tag is the owner's word.
set -e
cd "$(dirname "$0")/.."

VERSION=${1#v}
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Give the version of the release, such as 9.1.9." >&2
    exit 1
fi
TAG=v$VERSION
git rev-parse -q --verify "refs/tags/$TAG" > /dev/null || { echo "No tag $TAG here." >&2; exit 1; }
util/check-version.sh --at "$TAG" > /dev/null

DEST=${PDFJET_SWIFT:-../pdfjet-swift}
if [ ! -d "$DEST/.git" ]; then
    echo "$DEST is not a git repository: clone or make pdfjet-swift there first." >&2
    exit 1
fi
if [ -n "$(git -C "$DEST" status --porcelain)" ]; then
    echo "$DEST has changes that are not committed." >&2
    exit 1
fi
if git -C "$DEST" rev-parse -q --verify "refs/tags/$TAG" > /dev/null; then
    echo "$DEST already has the tag $TAG." >&2
    exit 1
fi

# The files of the tag, into a fresh folder, then over the old ones, so that
# a file the release no longer has goes too.
STAGE=$(mktemp -d)
trap 'rm -rf "$STAGE"' EXIT
git archive "$TAG" Sources/PDFjet resources/fonts LICENSE llms-swift.txt | tar -x -C "$STAGE"
# The Go files of the fonts are the Go port's
find "$STAGE/resources/fonts" -name '*.go' -delete

find "$DEST" -mindepth 1 -maxdepth 1 ! -name .git ! -name README.md ! -name .gitignore -exec rm -rf {} +
cp -R "$STAGE"/. "$DEST"/

# Package.swift: the library's products and their targets, as this
# repository's has them, without the examples and the tests
cat > "$DEST/Package.swift" <<'EOF'
// swift-tools-version: 6.1
// PDFjet for Swift, copied from the release of the same tag of
// https://github.com/edragoev1/pdfjet by its .packaging/package-swift.sh.
// Its examples, its tests and the issues are there.
import PackageDescription

let package = Package(
    name: "PDFjet",
    products: [
        // PDFjet, with the fonts PDFjet carries for every European language,
        // in Latin, Greek and Cyrillic, about 7 MB, in the bundle of the
        // target PDFjetFonts.
        .library(name: "PDFjet", targets: ["PDFjet"]),
        // The fonts of every other script, IBM Plex Sans JP, KR, SC and TC,
        // and IBM Plex Sans Arabic, Hebrew and Thai, about 52 MB, for an app
        // that draws in them:
        // .product(name: "PDFjetFontsWorld", package: "pdfjet-swift").
        .library(name: "PDFjetFontsWorld", targets: ["PDFjetFontsWorld"]),
        // The European fonts, which the product PDFjet has: kept so that an
        // app that listed it still builds; it adds nothing to PDFjet.
        .library(name: "PDFjetFonts", targets: ["PDFjetFonts"]),
    ],
    targets: [
        .target(
            name: "PDFjetFonts",
            path: "resources/fonts",
            exclude: [
                "world",
                "IBMPlexSerif/license.txt",
            ],
            sources: ["PDFjetFonts.swift"],
            resources: [
                .process("CascadiaMono"),
                .process("IBMPlexSans"),
                .process("IBMPlexSerif"),
            ]
        ),
        .target(
            name: "PDFjetFontsWorld",
            path: "resources/fonts/world",
            exclude: [
                "IBMPlexSansKR/license.txt",
                "IBMPlexSansSC/license.txt",
                "IBMPlexSansTC/license.txt",
                "IBMPlexSansArabic/license.txt",
                "IBMPlexSansHebrew/license.txt",
                "IBMPlexSansThai/license.txt",
            ],
            sources: ["PDFjetFontsWorld.swift"],
            resources: [
                .process("IBMPlexSansJP"),
                .process("IBMPlexSansKR"),
                .process("IBMPlexSansSC"),
                .process("IBMPlexSansTC"),
                .process("IBMPlexSansArabic"),
                .process("IBMPlexSansHebrew"),
                .process("IBMPlexSansThai"),
            ]
        ),
        .target(name: "PDFjet", dependencies: ["PDFjetFonts"]),
    ]
)
EOF

(cd "$DEST" && swift build -c release --product PDFjet > /dev/null && swift build -c release --product PDFjetFontsWorld > /dev/null)
rm -rf "$DEST/.build"
git -C "$DEST" add -A
git -C "$DEST" commit -q -m "PDFjet $VERSION, from the tag $TAG of https://github.com/edragoev1/pdfjet"
git -C "$DEST" tag -a "$TAG" -m "$TAG RELEASE"
echo "$DEST: PDFjet $VERSION committed and tagged $TAG, $(du -sh --exclude=.git "$DEST" | cut -f1); push it with: git -C $DEST push origin main $TAG"
