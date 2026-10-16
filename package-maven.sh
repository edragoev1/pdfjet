#!/bin/bash
# Makes the Maven Central bundle of com.pdfjet:pdfjet of a release, from the
# PDFjet.jar and the API reference of its PDFjet-ForJava-Core zip, as
# released, not built again, and the Java sources of its tag: the jar, the
# sources jar, the javadoc jar and the POM, each signed with the owner's GPG
# key and with its MD5, SHA-1, SHA-256 and SHA-512 checksums, in the layout
# of the repository, zipped.
#
#   ./package-maven.sh 9.0.9 ~/Projects/PDFjet-ForJava-Core-v9.0.9.zip
#
# It writes out/maven/pdfjet-9.0.9-bundle.zip. gpg asks the owner for the
# key's passphrase, which never goes in a file or a command; GPG_KEY chooses
# a key other than the default one. Uploading the bundle is the owner's, on
# https://central.sonatype.com/publishing, Publish Component.

set -euo pipefail
cd "$(dirname "$0")"
version=${1:?the version, such as 9.0.9}
zip=${2:?the PDFjet-ForJava-Core zip of the release}
commit=$(git rev-list -n1 "v$version")
top="PDFjet-ForJava-Core-v$version"
work=out/maven/work
dir=out/maven/stage/com/pdfjet/pdfjet/$version
base=$dir/pdfjet-$version
rm -rf out/maven/stage "$work" && mkdir -p "$dir" "$work/docs" "$work/sources"

# The jar, as released
unzip -q -j -o "$zip" "$top/PDFjet.jar" -d "$work"
cp "$work/PDFjet.jar" "$base.jar"

# The API reference of the release, as the javadoc jar
unzip -q -o "$zip" "$top/docs/java/*" -d "$work/docs"
jar cf "$base-javadoc.jar" -C "$work/docs/$top/docs/java" .

# The sources of the tag: the packages build-java.sh builds the jar of
git archive "v$version" com/pdfjet | tar -x -C "$work/sources"
jar cf "$base-sources.jar" -C "$work/sources" com

sed -e "s/\$version\\$/$version/g" -e "s/\$commit\\$/$commit/g" maven/pdfjet.pom > "$base.pom"

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

for f in "$base.jar" "$base-sources.jar" "$base-javadoc.jar" "$base.pom"; do
    gpg --yes --armor --detach-sign ${GPG_KEY:+--local-user "$GPG_KEY"} "$f"
done
for f in "$dir"/*; do
    case $f in *.asc|*.md5|*.sha1|*.sha256|*.sha512) continue ;; esac
    md5sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.md5"
    sha1sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.sha1"
    sha256sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.sha256"
    sha512sum "$f" | cut -d' ' -f1 | tr -d '\n' > "$f.sha512"
done
for f in "$base.jar" "$base-sources.jar" "$base-javadoc.jar" "$base.pom"; do
    gpg --verify "$f.asc" "$f" 2> /dev/null || { echo "The signature of $f does not verify." >&2; exit 1; }
done

bundle=out/maven/pdfjet-$version-bundle.zip
rm -f "$bundle"
(cd out/maven/stage && zip -q -r "../$(basename "$bundle")" com)
echo "$bundle: $(du -h "$bundle" | cut -f1), $(unzip -Z1 "$bundle" | grep -vc '/$') files, from $(basename "$zip"), commit $commit"
