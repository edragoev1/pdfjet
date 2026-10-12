#!/usr/bin/env python3
# Writes net48/Compat/UnicodeCategories.cs, the general category of every code
# point as .NET 8 has it, Unicode 15.0, for the .NET Framework 4.8 build, whose
# own data is of Unicode 8.0: a character newer than that, as the Arabic mark
# U+08D4 of Unicode 9, was laid out as another character there (third review,
# K3). Python 3.12's unicodedata is of Unicode 15.0 too, and
# tests/dotnet/UnicodeCategoriesTest.cs holds the table to .NET 8's own.
#
#     python3 net48/make-unicode-categories.py
import os
import unicodedata

if unicodedata.unidata_version != "15.0.0":
    raise SystemExit("Unicode 15.0 is needed, as .NET 8 has; this Python has "
                     + unicodedata.unidata_version)

# The categories in the order of .NET's UnicodeCategory, so each is its number
ORDER = ["Lu", "Ll", "Lt", "Lm", "Lo", "Mn", "Mc", "Me", "Nd", "Nl", "No",
         "Zs", "Zl", "Zp", "Cc", "Cf", "Cs", "Co", "Pc", "Pd", "Ps", "Pe",
         "Pi", "Pf", "Po", "Sm", "Sc", "Sk", "So", "Cn"]

starts, categories = [], []
for c in range(0x110000):
    category = ORDER.index(unicodedata.category(chr(c)))
    if not categories or categories[-1] != category:
        starts.append(c)
        categories.append(category)


def rows(values, per_line, form):
    lines = []
    for i in range(0, len(values), per_line):
        lines.append("            " + ", ".join(form(v) for v in values[i:i + per_line]) + ",")
    return "\n".join(lines)


path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Compat", "UnicodeCategories.cs")
with open(path, "w", encoding="utf-8", newline="\n") as out:
    out.write(f"""/*
 * UnicodeCategories.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Written by net48/make-unicode-categories.py: do not edit.

using System.Globalization;

namespace PDFjet.NET.Compat {{
// The general category of every code point as .NET 8 has it, Unicode 15.0, for
// the .NET Framework 4.8 build, whose own data is of Unicode 8.0: {len(starts)} runs
// of code points of one category, each from its start up to the next one.
internal static class UnicodeCategories {{
    internal static UnicodeCategory Of(int codePoint) {{
        if (codePoint < 0 || codePoint > 0x10FFFF) {{
            return UnicodeCategory.OtherNotAssigned;
        }}
        int lo = 0;
        int hi = Starts.Length - 1;
        while (lo < hi) {{
            int mid = (lo + hi + 1) >> 1;
            if (Starts[mid] <= codePoint) {{
                lo = mid;
            }} else {{
                hi = mid - 1;
            }}
        }}
        return (UnicodeCategory) Categories[lo];
    }}

    private static readonly int[] Starts = {{
{rows(starts, 10, lambda v: f"0x{v:X}")}
    }};

    private static readonly byte[] Categories = {{
{rows(categories, 20, str)}
    }};
}}
}}
""")
print("wrote", path, len(starts), "runs")
