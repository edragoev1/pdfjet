#!/bin/bash
# Makes the Maven Central bundle of a release: com.pdfjet:pdfjet, from the
# PDFjet.jar and the API reference of its PDFjet-ForJava-Core zip, as
# released, not built again, the fonts PDFjet carries but the CJK ones and
# the Java sources of its tag; and com.pdfjet:pdfjet-fonts-cjk, the CJK fonts
# alone, IBM Plex Sans JP, KR, SC and TC, with their licenses (the owner, 30
# October 2026). Each artifact has its jar, sources jar, javadoc jar and POM,
# each signed with the owner's GPG key and with its MD5, SHA-1, SHA-256 and
# SHA-512 checksums, in the layout of the repository, both in one zip.
#
#   ./package-maven.sh 9.1.1 ~/Projects/PDFjet-ForJava-Core-v9.1.1.zip
#
# It writes out/maven/pdfjet-9.1.1-bundle.zip, which has both artifacts. gpg
# asks the owner for the key's passphrase, which never goes in a file or a
# command; GPG_KEY chooses a key other than the default one. Uploading the
# bundle is the owner's, on https://central.sonatype.com/publishing, Publish
# Component: the one zip publishes both artifacts.
#
# PDFJET_REF=HEAD takes the fonts and the sources of that commit instead of
# the tag, and PDFJET_UNSIGNED=1 leaves out the signatures, for a bundle to
# look at and to measure before the release, never to upload.

set -euo pipefail
cd "$(dirname "$0")"
version=${1:?the version, such as 9.1.1}
zip=${2:?the PDFjet-ForJava-Core zip of the release}
ref=${PDFJET_REF:-v$version}
commit=$(git rev-list -n1 "$ref")
# The entries of the jars made here have the time of the tag's commit, so that
# two bundles of a release are the same (review of 17 October 2026)
date=$(TZ=UTC git log -1 --format=%cd --date=format-local:%Y-%m-%dT%H:%M:%SZ "$ref")
top="PDFjet-ForJava-Core-v$version"
work=out/maven/work
dir=out/maven/stage/com/pdfjet/pdfjet/$version
base=$dir/pdfjet-$version
cjkDir=out/maven/stage/com/pdfjet/pdfjet-fonts-cjk/$version
cjkBase=$cjkDir/pdfjet-fonts-cjk-$version
rm -rf out/maven/stage "$work" && mkdir -p "$dir" "$cjkDir" "$work/docs" "$work/sources"

# The jar, as released, and the fonts PDFjet carries in it but the CJK ones,
# so that the jar of Maven Central works out of the box (the owner, 29
# October 2026); the PDFjet.jar of the release on GitHub is small, its
# package having the fonts in resources/fonts. The fonts, with their
# licenses, are those of the tag, without the Go and Swift files beside them,
# at com/pdfjet/fonts/Family/File. The CJK fonts, about 40 MB compressed, of
# resources/fonts/cjk, are the jar com.pdfjet:pdfjet-fonts-cjk, at
# com/pdfjet/fonts/cjk/Family/File, the path PDFjet looks for them at on the
# classpath (the owner, 30 October 2026).
unzip -q -j -o "$zip" "$top/PDFjet.jar" -d "$work"
cp "$work/PDFjet.jar" "$base.jar"
mkdir -p "$work/fonts/com/pdfjet/fonts" "$work/cjk/com/pdfjet/fonts" "$work/cjk/META-INF"
git archive "$ref" resources/fonts | tar -x -C "$work/fonts"
find "$work/fonts/resources/fonts" \( -name '*.go' -o -name '*.swift' \) -delete
find "$work/fonts/resources/fonts" -type d -empty -delete
mv "$work/fonts/resources/fonts/cjk" "$work/cjk/com/pdfjet/fonts/cjk"
for family in "$work"/fonts/resources/fonts/*/; do
    mv "${family%/}" "$work/fonts/com/pdfjet/fonts/"
done
jar --update --date="$date" --file "$base.jar" -C "$work/fonts" com/pdfjet/fonts
if unzip -Z1 "$base.jar" | grep -q '^com/pdfjet/fonts/cjk/'; then
    echo "$base.jar has the CJK fonts" >&2; exit 1
fi

# The jar of the CJK fonts: the fonts and their licenses, each family with
# its own, and a manifest that names its module for the module path, where
# PDFjet reads them through the class loader, the package being open
for family in "$work"/cjk/com/pdfjet/fonts/cjk/*/; do
    [ -f "$family/license.txt" ] || { echo "$family has no license" >&2; exit 1; }
done
if [ "$(find "$work/cjk/com/pdfjet/fonts/cjk" -name '*.otf' | wc -l)" -ne 12 ]; then
    echo "The CJK fonts are not the 12 of IBM Plex Sans JP, KR, SC and TC" >&2; exit 1
fi
printf 'Automatic-Module-Name: com.pdfjet.fonts.cjk\nImplementation-Title: PDFjet CJK fonts\nImplementation-Version: %s\n' \
    "$version" > "$work/cjk-manifest.txt"
jar --create --date="$date" --file "$cjkBase.jar" --manifest "$work/cjk-manifest.txt" -C "$work/cjk" com
# Maven Central asks every jar for a sources jar and a javadoc jar; this one
# has no code, and they have a README that says so.
mkdir -p "$work/cjk-readme"
cat > "$work/cjk-readme/README.txt" <<README
com.pdfjet:pdfjet-fonts-cjk $version has no code: it is the Chinese, Japanese
and Korean fonts PDFjet carries, IBM Plex Sans JP, KR, SC and TC, Regular,
SemiBold and Bold, with their licenses (SIL Open Font License 1.1), as
resources at com/pdfjet/fonts/cjk. PDFjet, com.pdfjet:pdfjet, finds them on
the classpath: a path such as IBMPlexSansJP.Regular works when this jar is
next to it. https://github.com/edragoev1/pdfjet
README
jar --create --date="$date" --file "$cjkBase-sources.jar" -C "$work/cjk-readme" README.txt
jar --create --date="$date" --file "$cjkBase-javadoc.jar" -C "$work/cjk-readme" README.txt

# The API reference of the release, as the javadoc jar
unzip -q -o "$zip" "$top/docs/java/*" -d "$work/docs"
jar --create --date="$date" --file "$base-javadoc.jar" -C "$work/docs/$top/docs/java" .

# The sources of the tag: the packages build-java.sh builds the jar of
git archive "$ref" com/pdfjet | tar -x -C "$work/sources"
jar --create --date="$date" --file "$base-sources.jar" -C "$work/sources" com

sed -e "s/\$version\\$/$version/g" -e "s/\$commit\\$/$commit/g" maven/pdfjet.pom > "$base.pom"
sed -e "s/\$version\\$/$version/g" -e "s/\$commit\\$/$commit/g" maven/pdfjet-fonts-cjk.pom > "$cjkBase.pom"

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

# PDFjet finds the CJK fonts in the jar of the CJK fonts on the classpath,
# and without it, says what to add, in a folder with no resources/fonts, as
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
cjkJar=$PWD/$cjkBase.jar
without=$(cd "$check" && java -cp "$mainJar:." Check)
with=$(cd "$check" && java -cp "$mainJar:$cjkJar:." Check)
case $without in
    *"<artifactId>pdfjet-fonts-cjk</artifactId>"*) ;;
    *) echo "Without the jar of the CJK fonts: $without" >&2; exit 1 ;;
esac
if [ "$with" != found ]; then
    echo "With the jar of the CJK fonts: $with" >&2; exit 1
fi
echo "Without com.pdfjet:pdfjet-fonts-cjk: $without"

files=("$base.jar" "$base-sources.jar" "$base-javadoc.jar" "$base.pom"
    "$cjkBase.jar" "$cjkBase-sources.jar" "$cjkBase-javadoc.jar" "$cjkBase.pom")
if [ -z "${PDFJET_UNSIGNED:-}" ]; then
    for f in "${files[@]}"; do
        gpg --yes --armor --detach-sign ${GPG_KEY:+--local-user "$GPG_KEY"} "$f"
    done
else
    echo "PDFJET_UNSIGNED: no signatures; this bundle is not for Maven Central." >&2
fi
for f in "$dir"/* "$cjkDir"/*; do
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

bundle=out/maven/pdfjet-$version-bundle.zip
rm -f "$bundle"
(cd out/maven/stage && zip -q -r "../$(basename "$bundle")" com)
echo "$bundle: $(du -h "$bundle" | cut -f1), $(unzip -Z1 "$bundle" | grep -vc '/$') files, from $(basename "$zip"), commit $commit"
echo "  $(basename "$base.jar"): $(du -h "$base.jar" | cut -f1)"
echo "  $(basename "$cjkBase.jar"): $(du -h "$cjkBase.jar" | cut -f1)"
