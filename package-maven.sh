#!/bin/bash
# Makes the Maven Central bundle of a release: com.pdfjet:pdfjet, from the
# PDFjet.jar and the API reference of its PDFjet-ForJava-Core zip, as
# released, not built again, the fonts PDFjet carries for the European
# languages, IBM Plex Sans and Serif and Cascadia Mono, and the Java sources
# of its tag; and com.pdfjet:pdfjet-fonts-world, the fonts of the other
# scripts alone, IBM Plex Sans JP, KR, SC and TC and IBM Plex Sans Arabic,
# Hebrew and Thai, with their licenses (the owner, 30 October 2026). Each artifact has its jar, sources jar, javadoc jar and POM,
# each signed with the owner's GPG key and with its MD5, SHA-1, SHA-256 and
# SHA-512 checksums, in the layout of the repository, both in one zip.
#
#   ./package-maven.sh 9.1.1 ~/Projects/PDFjet-ForJava-Core-v9.1.1.zip
#
# Run it with HEAD at the tag v9.1.1: the jars, the sources and the POMs are
# all of the tag, once util/check-version.sh has passed it. It writes
# out/maven/pdfjet-9.1.1-bundle.zip, which has both artifacts. gpg
# asks the owner for the key's passphrase, which never goes in a file or a
# command; GPG_KEY chooses a key other than the default one. Uploading the
# bundle is the owner's, on https://central.sonatype.com/publishing, Publish
# Component: the one zip publishes both artifacts.
#
# PDFJET_REF=HEAD takes the fonts and the sources of that commit instead of
# the tag, with a PDFjet-ForJava-Core zip of the release or a draft one of
# package-java.sh, and PDFJET_UNSIGNED=1 leaves out the signatures, for a
# bundle to look at and to measure before the release. Neither is to upload,
# and their bundles say so: the version of their files, and in their POMs, is
# 9.1.1-draft, 9.1.1-unsigned or 9.1.1-draft-unsigned.

set -euo pipefail
cd "$(dirname "$0")"
version=${1:?the version, such as 9.1.1}
zip=${2:?the PDFjet-ForJava-Core zip of the release}
ref=${PDFJET_REF:-v$version}
if ! commit=$(git rev-list -n1 "$ref" 2> /dev/null); then
    echo "There is no $ref: tag the release, or set PDFJET_REF for a draft." >&2
    exit 1
fi
draft=
if [ -n "${PDFJET_REF:-}" ]; then
    draft=-draft
elif [ "$(git rev-parse HEAD)" != "$commit" ]; then
    echo "HEAD is $(git describe --tags --always HEAD), not the tag $ref: check out the tag, or set PDFJET_REF for a draft." >&2
    exit 1
fi
packageVersion=$version$draft${PDFJET_UNSIGNED:+-unsigned}
# The version is the same in all that holds it, at this commit; a draft asks
# no date of CHANGELOG.md and may be of a commit before the version was set.
found=$(util/check-version.sh --at "$commit" ${draft:+--draft})
if [ -z "$draft" ] && [ "$found" != "v$version" ]; then
    echo "$ref holds the version $found, not v$version." >&2
    exit 1
fi
# The entries of the jars made here have the time of the tag's commit, so that
# two bundles of a release are the same (review of 17 October 2026)
date=$(TZ=UTC git log -1 --format=%cd --date=format-local:%Y-%m-%dT%H:%M:%SZ "$ref")
top="PDFjet-ForJava-Core-v$version"
if [ -n "$draft" ] && [[ $(unzip -Z1 "$zip") == *"$top-draft/PDFjet.jar"* ]]; then
    top=$top-draft
fi
work=out/maven/work
dir=out/maven/stage/com/pdfjet/pdfjet/$packageVersion
base=$dir/pdfjet-$packageVersion
worldDir=out/maven/stage/com/pdfjet/pdfjet-fonts-world/$packageVersion
worldBase=$worldDir/pdfjet-fonts-world-$packageVersion
rm -rf out/maven/stage "$work" && mkdir -p "$dir" "$worldDir" "$work/docs" "$work/sources"

# The jar, as released, and the European fonts PDFjet carries in it, so that
# the jar of Maven Central works out of the box (the owner, 29 October 2026);
# the PDFjet.jar of the release on GitHub is small, its package having the
# fonts in resources/fonts. The fonts, with their licenses, are those of the
# tag, without the Go and Swift files beside them, at
# com/pdfjet/fonts/Family/File. The fonts of the other scripts, of
# resources/fonts/world, are the jar com.pdfjet:pdfjet-fonts-world, at
# com/pdfjet/fonts/world/Family/File, the path PDFjet looks for them at on the
# classpath (the owner, 30 October 2026).
unzip -q -j -o "$zip" "$top/PDFjet.jar" -d "$work"
cp "$work/PDFjet.jar" "$base.jar"
mkdir -p "$work/fonts/com/pdfjet/fonts" "$work/world/com/pdfjet/fonts"
git archive "$ref" resources/fonts | tar -x -C "$work/fonts"
find "$work/fonts/resources/fonts" \( -name '*.go' -o -name '*.swift' \) -delete
find "$work/fonts/resources/fonts" -type d -empty -delete
mv "$work/fonts/resources/fonts/world" "$work/world/com/pdfjet/fonts/world"
for family in "$work"/fonts/resources/fonts/*/; do
    mv "${family%/}" "$work/fonts/com/pdfjet/fonts/"
done
jar --update --date="$date" --file "$base.jar" -C "$work/fonts" com/pdfjet/fonts
# Through a variable, not a pipe: grep -q stops at the first match, unzip then
# dies of SIGPIPE, and with pipefail the pipe fails just when the fonts are there.
jarFiles=$(unzip -Z1 "$base.jar")
if grep -q '^com/pdfjet/fonts/world/' <<< "$jarFiles"; then
    echo "$base.jar has the fonts of pdfjet-fonts-world" >&2; exit 1
fi

# The jar of the fonts of the other scripts: the fonts and their licenses,
# each family with its own, and a manifest that names its module for the
# module path, where PDFjet reads them through the class loader, the package
# being open
for family in "$work"/world/com/pdfjet/fonts/world/*/; do
    [ -f "$family/license.txt" ] || { echo "$family has no license" >&2; exit 1; }
done
if [ "$(find "$work/world/com/pdfjet/fonts/world" -name '*.otf' | wc -l)" -ne 12 ] ||
        [ "$(find "$work/world/com/pdfjet/fonts/world" -name '*.ttf' | wc -l)" -ne 21 ]; then
    echo "The fonts of pdfjet-fonts-world are not the 12 of IBM Plex Sans JP, KR, SC and TC" \
        "and the 21 of IBM Plex Sans Arabic, Hebrew and Thai" >&2; exit 1
fi
printf 'Automatic-Module-Name: com.pdfjet.fonts.world\nImplementation-Title: PDFjet world fonts\nImplementation-Version: %s\n' \
    "$packageVersion" > "$work/world-manifest.txt"
jar --create --date="$date" --file "$worldBase.jar" --manifest "$work/world-manifest.txt" -C "$work/world" com
# Maven Central asks every jar for a sources jar and a javadoc jar; this one
# has no code, and they have a README that says so.
mkdir -p "$work/world-readme"
cat > "$work/world-readme/README.txt" <<README
com.pdfjet:pdfjet-fonts-world $packageVersion has no code: it is the fonts PDFjet
carries for Chinese, Japanese, Korean, Arabic, Hebrew and Thai, IBM Plex Sans
JP, KR, SC and TC, Regular, SemiBold and Bold, and IBM Plex Sans Arabic,
Hebrew and Thai, Thin to Bold, with their licenses (SIL Open Font License
1.1), as resources at com/pdfjet/fonts/world. PDFjet, com.pdfjet:pdfjet,
finds them on the classpath: a path such as IBMPlexSansJP.Regular or
IBMPlexSansArabic.Regular works when this jar is next to it.
https://github.com/edragoev1/pdfjet
README
jar --create --date="$date" --file "$worldBase-sources.jar" -C "$work/world-readme" README.txt
jar --create --date="$date" --file "$worldBase-javadoc.jar" -C "$work/world-readme" README.txt

# The API reference of the release, as the javadoc jar
unzip -q -o "$zip" "$top/docs/java/*" -d "$work/docs"
jar --create --date="$date" --file "$base-javadoc.jar" -C "$work/docs/$top/docs/java" .

# The sources of the tag: the packages build-java.sh builds the jar of
git archive "$ref" com/pdfjet | tar -x -C "$work/sources"
jar --create --date="$date" --file "$base-sources.jar" -C "$work/sources" com

# The POMs of the same commit as the sources
git archive "$commit" maven | tar -x -C "$work"
sed -e "s/\$version\\$/$packageVersion/g" -e "s/\$commit\\$/$commit/g" "$work/maven/pdfjet.pom" > "$base.pom"
sed -e "s/\$version\\$/$packageVersion/g" -e "s/\$commit\\$/$commit/g" "$work/maven/pdfjet-fonts-world.pom" > "$worldBase.pom"

# The jar and the sources jar are of the same code: a class for each source
# file, and a source for each class, declared in a file of its own name or
# beside another, as FontTable is
classes=$(unzip -Z1 "$base.jar" | grep '\.class$' | sed 's/\$.*//; s/\.class$//' | sort -u)
sources=$(unzip -Z1 "$base-sources.jar" | grep '\.java$' | sed 's/\.java$//' | sort -u)
for s in $(comm -13 <(echo "$classes") <(echo "$sources")); do
    echo "$s.java has no class in the jar." >&2; exit 1
done
for c in $(comm -23 <(echo "$classes") <(echo "$sources")); do
    grep -rqE "^(final |abstract )*(class|interface|enum) $(basename "$c")\b" "$work/sources/$(dirname "$c")" ||
        { echo "$c is in the jar and not in the sources." >&2; exit 1; }
done

# PDFjet finds the fonts of the other scripts in the jar
# com.pdfjet:pdfjet-fonts-world on the classpath, and without it, says what
# to add, in a folder with no resources/fonts, as
# a program has: checked with the jars made here, as a program has them
check=$work/check
mkdir -p "$check"
cat > "$check/Check.java" <<'JAVA'
import com.pdfjet.*;
import com.pdfjet.fonts.*;

public class Check {
    public static void main(String[] args) throws Exception {
        PDF pdf = new PDF(new java.io.ByteArrayOutputStream());
        new Font(pdf, IBMPlexSans.Regular);
        try {
            new Font(pdf, IBMPlexSansArabic.Regular);
            new Font(pdf, IBMPlexSansJP.Regular);
            System.out.println("found");
        } catch (java.io.FileNotFoundException e) {
            System.out.println(e.getMessage());
        }
    }
}
JAVA
javac -cp "$base.jar" -d "$check" "$check/Check.java"
mainJar=$PWD/$base.jar
worldJar=$PWD/$worldBase.jar
without=$(cd "$check" && java -cp "$mainJar:." Check)
with=$(cd "$check" && java -cp "$mainJar:$worldJar:." Check)
case $without in
    *"<artifactId>pdfjet-fonts-world</artifactId>"*) ;;
    *) echo "Without pdfjet-fonts-world: $without" >&2; exit 1 ;;
esac
if [ "$with" != found ]; then
    echo "With pdfjet-fonts-world: $with" >&2; exit 1
fi
echo "Without com.pdfjet:pdfjet-fonts-world: $without"

files=("$base.jar" "$base-sources.jar" "$base-javadoc.jar" "$base.pom"
    "$worldBase.jar" "$worldBase-sources.jar" "$worldBase-javadoc.jar" "$worldBase.pom")
if [ -z "${PDFJET_UNSIGNED:-}" ]; then
    for f in "${files[@]}"; do
        gpg --yes --armor --detach-sign ${GPG_KEY:+--local-user "$GPG_KEY"} "$f"
    done
else
    echo "PDFJET_UNSIGNED: no signatures; this bundle is not for Maven Central." >&2
fi
for f in "$dir"/* "$worldDir"/*; do
    case $f in *.asc|*.md5|*.sha1|*.sha256|*.sha512) continue ;; esac
    md5sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.md5"
    sha1sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.sha1"
    sha256sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.sha256"
    sha512sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.sha512"
done
if [ -z "${PDFJET_UNSIGNED:-}" ]; then
    for f in "${files[@]}"; do
        gpg --verify "$f.asc" "$f" 2> /dev/null || { echo "The signature of $f does not verify." >&2; exit 1; }
    done
fi

bundle=out/maven/pdfjet-$packageVersion-bundle.zip
rm -f "$bundle"
(cd out/maven/stage && zip -q -r "../$(basename "$bundle")" com)
echo "$bundle: $(du -h "$bundle" | cut -f1), $(unzip -Z1 "$bundle" | grep -vc '/$') files, from $(basename "$zip"), commit $commit"
echo "  $(basename "$base.jar"): $(du -h "$base.jar" | cut -f1)"
echo "  $(basename "$worldBase.jar"): $(du -h "$worldBase.jar" | cut -f1)"
