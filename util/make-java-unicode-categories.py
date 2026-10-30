#!/usr/bin/env python3
# Writes com/pdfjet/UnicodeCategories.java, the groups of the general categories
# that the Java port asks about, of every code point, as Unicode 15.0 has them,
# as Go and .NET 8 do: Java's Character.getType has the Unicode of the runtime,
# 6.2 on Java 8, where a mark newer than that is laid out as another character.
# Python 3.12's unicodedata is of Unicode 15.0 too. All the letters are one
# group, which leaves a tenth of the runs, as no code tells them apart.
#
#     python3 util/make-java-unicode-categories.py
import os
import unicodedata

if unicodedata.unidata_version != "15.0.0":
    raise SystemExit("Unicode 15.0 is needed; this Python has " + unicodedata.unidata_version)

# The number of Character.getType of each group, of the letters OTHER_LETTER
GROUPS = {"Mn": 6, "Me": 7, "Mc": 8, "Nd": 9, "Zs": 12, "Zl": 13, "Zp": 14, "Cf": 16, "Sc": 26}


def group(c):
    category = unicodedata.category(chr(c))
    return 5 if category[0] == "L" else GROUPS.get(category, 0)


# Each run as its length and its group, in two characters: a run longer than
# a character holds is two runs
units = []
start, current = 0, group(0)
for c in range(1, 0x110001):
    value = group(c) if c < 0x110000 else -1
    if value != current:
        length = c - start
        while length > 0xFFFF:
            units += [0xFFFF, current]
            length -= 0xFFFF
        units += [length, current]
        start, current = c, value

# What Java reads before it reads the text of the source
ESCAPES = {0x0A: "\\n", 0x0D: "\\r", 0x22: '\\"', 0x5C: "\\\\"}
lines = []
for i in range(0, len(units), 16):
    row = "".join(ESCAPES.get(unit, "\\u%04X" % unit) for unit in units[i:i + 16])
    lines.append('            + "' + row + '"')
table = "\n".join(lines).lstrip()[2:]

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
with open(os.path.join(root, "com", "pdfjet", "UnicodeCategories.java"), "w", encoding="utf-8", newline="\n") as out:
    out.write(f"""/*
 * UnicodeCategories.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

// Written by util/make-java-unicode-categories.py: do not edit.

/**
 * The general category of a code point as Unicode 15.0 has it, as Go and .NET 8
 * do, whatever the runtime: Character.getType has the Unicode of the runtime,
 * 6.2 on Java 8, where a mark newer than that is laid out as another character
 * and U+180E is a space. Only the categories that the library asks about are
 * told apart, and the letters are one group.
 */
final class UnicodeCategories {{
    private UnicodeCategories() {{
    }}

    // The length of each run of code points of one group and its group, two
    // characters a run, in a string, which takes less of the class file than
    // an array: {len(units) // 2} runs.
    private static final String RUNS = {table};

    // Where each run starts, and its group.
    private static final int[] STARTS = new int[RUNS.length() / 2];
    private static final byte[] TYPES = new byte[RUNS.length() / 2];
    // The group of the code points that text has most, found at once.
    private static final byte[] LOW = new byte[0x800];

    static {{
        int start = 0;
        for (int i = 0; i < STARTS.length; i++) {{
            STARTS[i] = start;
            TYPES[i] = (byte) RUNS.charAt(2 * i + 1);
            start += RUNS.charAt(2 * i);
        }}
        for (int c = 0; c < LOW.length; c++) {{
            LOW[c] = search(c);
        }}
    }}

    /**
     * Returns the category of the code point as Character.getType does: one of
     * NON_SPACING_MARK, ENCLOSING_MARK, COMBINING_SPACING_MARK,
     * DECIMAL_DIGIT_NUMBER, SPACE_SEPARATOR, LINE_SEPARATOR,
     * PARAGRAPH_SEPARATOR, FORMAT and CURRENCY_SYMBOL, or OTHER_LETTER for any
     * letter, or UNASSIGNED for all the others.
     *
     * @param codePoint the code point.
     * @return the category.
     */
    static int of(int codePoint) {{
        if (codePoint >= 0 && codePoint < LOW.length) {{
            return LOW[codePoint];
        }}
        return (codePoint < 0 || codePoint > 0x10FFFF) ? Character.UNASSIGNED : search(codePoint);
    }}

    private static byte search(int codePoint) {{
        int lo = 0;
        int hi = STARTS.length - 1;
        while (lo < hi) {{
            int mid = (lo + hi + 1) >>> 1;
            if (STARTS[mid] <= codePoint) {{
                lo = mid;
            }} else {{
                hi = mid - 1;
            }}
        }}
        return TYPES[lo];
    }}

    /**
     * Returns true for a letter or a decimal digit, as Character.isLetterOrDigit
     * does.
     *
     * @param codePoint the code point.
     * @return true for a letter or a digit.
     */
    static boolean isLetterOrDigit(int codePoint) {{
        int type = of(codePoint);
        return type == Character.OTHER_LETTER || type == Character.DECIMAL_DIGIT_NUMBER;
    }}
}}
""")
