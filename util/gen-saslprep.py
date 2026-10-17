#!/usr/bin/env python3
"""Writes the tables of SASLprep, RFC 4013, for the four ports: the mappings
and the prohibited characters of stringprep, RFC 3454, and the data of
Unicode 3.2 that normalization form KC needs, the version both RFCs fix.

    python3 util/gen-saslprep.py DIR

DIR holds the files it reads, which are not kept in the repository:

    https://www.rfc-editor.org/rfc/rfc3454.txt
    https://www.unicode.org/Public/3.2-Update/UnicodeData-3.2.0.txt
    https://www.unicode.org/Public/3.2-Update/CompositionExclusions-3.2.0.txt

It writes src/internal/saslprep/tables.go, com/pdfjet/SASLprepTables.java,
net/pdfjet/SASLprepTables.cs and Sources/PDFjet/SASLprepTables.swift. Go and
C# have the tables as arrays; Java, whose class initializer may not pass
64 KB, and Swift, which compiles a large array literal slowly, have them as
strings of hexadecimal numbers that they read once.

The tables are:
  space      C.1.2, the non-ASCII spaces, mapped to a space and prohibited
  nothing    B.1, the characters mapped to nothing
  prohibited C.1.2, C.2.1, C.2.2, C.3 to C.9, the characters prohibited
  randal     D.1, the characters of bidirectional category R or AL
  lcat       D.2, those of bidirectional category L
             each as ranges, first and last, in order
  ccc        the canonical combining class of each character that has one,
             as pairs of the character and its class, in order
  decomp     the full compatibility decomposition of each character that has
             one, as the characters in order, the start of each one's
             decomposition in the flat list of their decompositions, and
             that list, its end the start of the next one
  compose    the canonical compositions, the pairs that compose, keyed by
             first * 0x110000 + second, in order, and what each composes to
The Hangul syllables are composed and decomposed by arithmetic, as Unicode
says, and are in none of the tables.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = sys.argv[1]

# The tables of RFC 3454, by name: a list of (first, last).
rfc = open(os.path.join(src, "rfc3454.txt"), encoding="utf-8").read()
tables = {}
for name, body in re.findall(r"----- Start Table (\S+) -----\n(.*?)----- End Table \1 -----", rfc, re.S):
    ranges = []
    for line in body.splitlines():
        line = line.strip()
        if not line or line.startswith("Hoffman") or line.startswith("RFC 3454"):
            continue
        code = line.split(";")[0].strip()
        if "-" in code:
            first, last = code.split("-")
        else:
            first = last = code
        if not re.fullmatch(r"[0-9A-F]{4,6}", first):
            continue
        ranges.append((int(first, 16), int(last, 16)))
    tables[name] = ranges


def merged(ranges):
    out = []
    for first, last in sorted(ranges):
        if out and first <= out[-1][1] + 1:
            out[-1] = (out[-1][0], max(out[-1][1], last))
        else:
            out.append((first, last))
    return out


space = merged(tables["C.1.2"])
nothing = merged(tables["B.1"])
prohibited = merged(sum((tables[t] for t in
                         ["C.1.2", "C.2.1", "C.2.2", "C.3", "C.4", "C.5", "C.6", "C.7", "C.8", "C.9"]), []))
randal = merged(tables["D.1"])
lcat = merged(tables["D.2"])

# Unicode 3.2: the combining classes and the decompositions.
ccc = {}
canonical = {}
compat = {}
for line in open(os.path.join(src, "UnicodeData-3.2.0.txt"), encoding="utf-8"):
    fields = line.split(";")
    code = int(fields[0], 16)
    if int(fields[3]):
        ccc[code] = int(fields[3])
    if fields[5]:
        parts = fields[5].split()
        if parts[0].startswith("<"):
            compat[code] = [int(p, 16) for p in parts[1:]]
        else:
            canonical[code] = [int(p, 16) for p in parts]
            compat[code] = canonical[code]

exclusions = set()
for line in open(os.path.join(src, "CompositionExclusions-3.2.0.txt"), encoding="utf-8"):
    line = line.split("#")[0].strip()
    if line:
        exclusions.add(int(line.split()[0], 16))


def full(code):
    """The full compatibility decomposition of the character."""
    if code not in compat:
        return [code]
    return [d for c in compat[code] for d in full(c)]


decomp = {code: full(code) for code in compat}
# A canonical decomposition of two characters composes, but for the
# exclusions and those whose first character is not a starter.
compose = {}
for code, parts in canonical.items():
    if len(parts) == 2 and code not in exclusions and ccc.get(parts[0], 0) == 0:
        compose[parts[0] * 0x110000 + parts[1]] = code

flat_ranges = {name: [n for r in value for n in r] for name, value in
               [("space", space), ("nothing", nothing), ("prohibited", prohibited),
                ("randal", randal), ("lcat", lcat)]}
ccc_pairs = [n for code in sorted(ccc) for n in (code, ccc[code])]
decomp_codes = sorted(decomp)
decomp_starts = []
decomp_values = []
for code in decomp_codes:
    decomp_starts.append(len(decomp_values))
    decomp_values.extend(decomp[code])
decomp_starts.append(len(decomp_values))
compose_keys = sorted(compose)
compose_values = [compose[k] for k in compose_keys]

lists = [
    ("space", flat_ranges["space"], "C.1.2: the non-ASCII spaces, mapped to a space"),
    ("nothing", flat_ranges["nothing"], "B.1: the characters mapped to nothing"),
    ("prohibited", flat_ranges["prohibited"], "C.1.2, C.2.1, C.2.2 and C.3 to C.9: the characters prohibited"),
    ("randal", flat_ranges["randal"], "D.1: the characters of bidirectional category R or AL"),
    ("lcat", flat_ranges["lcat"], "D.2: the characters of bidirectional category L"),
    ("ccc", ccc_pairs, "The canonical combining classes, as pairs of a character and its class"),
    ("decompCodes", decomp_codes, "The characters that decompose, in order"),
    ("decompStarts", decomp_starts, "Where the decomposition of each starts in decompValues, and its end"),
    ("decompValues", decomp_values, "The full compatibility decompositions"),
    ("composeKeys", compose_keys, "The pairs that compose, as first * 0x110000 + second, in order"),
    ("composeValues", compose_values, "What each pair composes to"),
]

HEADER = """The tables of SASLprep, RFC 4013, written by util/gen-saslprep.py from the
tables of stringprep, RFC 3454, and the data of Unicode 3.2,
UnicodeData-3.2.0.txt and CompositionExclusions-3.2.0.txt, Copyright (c)
1991-2002 Unicode, Inc., used under the terms THIRD-PARTIES.TXT gives. Do not
edit: change the script and run it again."""


def numbers(values, per_line, indent, wide=False):
    lines = []
    for i in range(0, len(values), per_line):
        lines.append(indent + ", ".join(f"0x{v:X}" for v in values[i:i + per_line]) + ",")
    return "\n".join(lines)


def hex_chunks(values, size=8000):
    # Numbers in hexadecimal, separated by spaces, in strings of at most size
    # characters, each well below the 65,535 bytes of a Java string constant.
    text = " ".join(f"{v:X}" for v in values)
    chunks = []
    while text:
        if len(text) <= size:
            chunks.append(text)
            break
        cut = text.rindex(" ", 0, size)
        chunks.append(text[:cut])
        text = text[cut + 1:]
    return chunks


def write(path, text):
    with open(os.path.join(ROOT, path), "w", encoding="utf-8") as f:
        f.write(text)


# Go
out = ["// " + line if line else "//" for line in HEADER.splitlines()]
out += ["", "package saslprep", ""]
for name, values, comment in lists:
    kind = "int64" if name == "composeKeys" else "int32"
    out.append(f"// {comment}")
    out.append(f"var {name} = []{kind}{{")
    out.append(numbers(values, 8, "\t"))
    out.append("}")
    out.append("")
write("src/internal/saslprep/tables.go", "\n".join(out))

# C#
out = ["// " + line if line else "//" for line in HEADER.splitlines()]
out += ["", "namespace PDFjet.NET {", "", "internal static class SASLprepTables {"]
for name, values, comment in lists:
    kind = "long" if name == "composeKeys" else "int"
    out.append(f"    // {comment}")
    out.append(f"    internal static readonly {kind}[] {name} = {{")
    out.append(numbers(values, 8, "        "))
    out.append("    };")
    out.append("")
out[-1:] = ["}", "", "}  // End of namespace PDFjet.NET", ""]
write("net/pdfjet/SASLprepTables.cs", "\n".join(out))

# Java
out = ["/*"] + [" * " + line if line else " *" for line in HEADER.splitlines()] + [" */"]
out += ["package com.pdfjet;", "", "final class SASLprepTables {", "    private SASLprepTables() {", "    }", ""]
for name, values, comment in lists:
    out.append(f"    // {comment}, in hexadecimal")
    out.append(f"    static final String[] {name} = {{")
    for chunk in hex_chunks(values):
        out.append(f'        "{chunk}",')
    out.append("    };")
    out.append("")
out[-1:] = ["}", ""]
write("com/pdfjet/SASLprepTables.java", "\n".join(out))

# Swift
out = ["// " + line if line else "//" for line in HEADER.splitlines()]
out += ["", "enum SASLprepTables {"]
for name, values, comment in lists:
    out.append(f"    // {comment}, in hexadecimal")
    out.append(f"    static let {name}: [String] = [")
    for chunk in hex_chunks(values):
        out.append(f'        "{chunk}",')
    out.append("    ]")
    out.append("")
out[-1:] = ["}", ""]
write("Sources/PDFjet/SASLprepTables.swift", "\n".join(out))

print(f"space {len(space)}, nothing {len(nothing)}, prohibited {len(prohibited)}, randal {len(randal)}, "
      f"lcat {len(lcat)} ranges; ccc {len(ccc)}, decomp {len(decomp)} ({len(decomp_values)} values), "
      f"compose {len(compose)}")
