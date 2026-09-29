#!/bin/bash
# Lists the public API of the four ports at a tag, v9.0.1 unless another is
# given, and in this checkout, and prints what was added and what was removed
# in each, as goal 6 of TODO.md asks before each tag: nothing removed, no
# signature changed, and nothing added but the members goal 6 lists.
#
#   ./check-api.sh            against v9.0.1
#   ./check-api.sh v9.0.2     against another tag
#
# Java: javap -public of every class; Go: the exported declarations of every
# package but internal ones and the examples, of util/goapi; C#: the public types and members of the
# assembly, by reflection; Swift: the public symbols of the symbol graph. The
# lists are kept in build/check-api, a directory of each tree, to read whole.
# It needs the four toolchains, as the build-*.sh scripts do.
#
# It prints the differences and leaves the judging of them to the one who
# reads them, against the list of goal 6: a public API is words, and whether
# an added member is one that goal 6 lists is not a thing a diff can say.

cd "$(dirname "$0")" || exit 1
BASE=${1:-v9.0.1}
WORK=$(pwd)/build/check-api
rm -rf "$WORK"
mkdir -p "$WORK/base" "$WORK/head"
git archive "$BASE" | tar -x -C "$WORK/base" || exit 1
# The checkout as it is, uncommitted changes and all
git ls-files -z | tar --null -T - -c | tar -x -C "$WORK/head" || exit 1

# The C# lister, a console program that loads the assembly by reflection.
LISTER=$WORK/lister
mkdir -p "$LISTER"
cat > "$LISTER/Lister.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
  </PropertyGroup>
</Project>
EOF
cat > "$LISTER/Program.cs" <<'EOF'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Every public type of the assembly and its public members, one to a line,
// sorted, with [Obsolete] before what is deprecated.
class Program {
    static void Main(string[] args) {
        Assembly assembly = Assembly.LoadFrom(args[0]);
        List<string> lines = new List<string>();
        foreach (Type type in assembly.GetExportedTypes()) {
            string kind = type.IsEnum ? "enum" : type.IsInterface ? "interface" : type.IsValueType ? "struct" : "class";
            lines.Add(Obsolete(type) + kind + " " + type.FullName);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (MemberInfo member in type.GetMembers(flags)) {
                if (member is MethodInfo method && method.IsSpecialName) {
                    continue; // The accessors of properties, which are listed as properties
                }
                lines.Add(Obsolete(member) + type.FullName + " :: " + member.MemberType + " " + member);
            }
        }
        foreach (string line in lines.Distinct().OrderBy(l => l, StringComparer.Ordinal)) {
            Console.WriteLine(line);
        }
    }

    static string Obsolete(MemberInfo member) {
        return member.IsDefined(typeof(ObsoleteAttribute), false) ? "[Obsolete] " : "";
    }
}
EOF
dotnet build "$LISTER/Lister.csproj" -c release -o "$LISTER/bin" > "$WORK/lister.log" 2>&1 || {
    cat "$WORK/lister.log"
    exit 1
}

# The Go lister, of this checkout, which reads the source of either tree.
go build -o "$WORK/goapi" ./util/goapi || exit 1

for TREE in base head; do
    DIR=$WORK/$TREE
    OUT=$WORK/$TREE-api
    mkdir -p "$OUT"
    echo "Listing the API of the $TREE tree"

    # Java
    mkdir -p "$DIR/classes"
    find "$DIR/com/pdfjet" -name '*.java' > "$DIR/java-sources.txt"
    javac -nowarn -encoding utf-8 -d "$DIR/classes" @"$DIR/java-sources.txt" > "$OUT/javac.log" 2>&1 || {
        cat "$OUT/javac.log"
        exit 1
    }
    # com.pdfjet.internal is public only because Java has no internal access
    # across packages; it is not API
    (cd "$DIR/classes" && find com -name '*.class' -not -path 'com/pdfjet/internal/*' |
        sed 's/\.class$//; s#/#.#g' | sort) > "$DIR/java-classes.txt"
    # javap prints every class, and the public members of each: the public
    # classes and their members are kept, each member after its class
    javap -public -cp "$DIR/classes" $(cat "$DIR/java-classes.txt") 2> /dev/null | python3 -c '
import re, sys
current = None
for line in sys.stdin:
    line = line.rstrip()
    if line.startswith("Compiled from") or line == "}":
        continue
    if not line.startswith(" "):
        current = line if re.match(r"public ", line) else None
        if current:
            print(current)
    elif current:
        print(current.rstrip(" {") + " :: " + line.strip())
' > "$OUT/java.txt"

    # Go: the exported declarations of every package, read from the source by
    # util/goapi with Go's own parser, each on one line and without comments
    (cd "$DIR" && for dir in $(go list -f '{{.Dir}}' ./src/... | grep -v -e /internal -e /examples); do
        "$WORK/goapi" "$dir"
    done) > "$OUT/go.txt" 2> "$OUT/go.log" || {
        cat "$OUT/go.log"
        exit 1
    }

    # C#
    dotnet build "$DIR/PDFjet.csproj" -c release -o "$DIR/dotnet" > "$OUT/dotnet.log" 2>&1 || {
        cat "$OUT/dotnet.log"
        exit 1
    }
    dotnet "$LISTER/bin/Lister.dll" "$DIR/dotnet/PDFjet.dll" > "$OUT/dotnet.txt"

    # Swift
    (cd "$DIR" && swift build --target PDFjet -Xswiftc -emit-symbol-graph \
        -Xswiftc -emit-symbol-graph-dir -Xswiftc "$DIR/symbols") > "$OUT/swift.log" 2>&1 || {
        cat "$OUT/swift.log"
        exit 1
    }
    python3 - "$DIR/symbols/PDFjet.symbols.json" > "$OUT/swift.txt" <<'EOF'
import json, sys
graph = json.load(open(sys.argv[1]))
lines = set()
for symbol in graph["symbols"]:
    if symbol.get("accessLevel") not in ("public", "open"):
        continue
    declaration = "".join(f["spelling"] for f in symbol.get("declarationFragments", []))
    path = ".".join(symbol["pathComponents"])
    deprecated = "[deprecated] " if any(a.get("isUnconditionallyDeprecated") or "deprecated" in a
                                        for a in symbol.get("availability", [])) else ""
    lines.add(deprecated + path + " :: " + declaration)
print("\n".join(sorted(lines)))
EOF
done

python3 util/compare-api.py "$WORK" "$BASE"
