/*
 * BundledFonts.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;

namespace PDFjet.NET {
/// <summary>
/// The fonts PDFjet carries in itself, those of PDFjet Forms: IBM Plex Sans,
/// Source Serif 4 and JetBrains Mono, each regular, semibold and bold, upright
/// and italic, under the SIL Open Font License 1.1. An SVG image draws its
/// text in them unless a font is set: embedded, as PDF/A and PDF/UA ask, where
/// the core fonts are not. They are resources of the assembly, named
/// PDFjet.BundledFonts. and the name of the file.
/// </summary>
internal static class BundledFonts {
    internal const int SANS = 0;
    internal const int SERIF = 1;
    internal const int MONO = 2;
    private static readonly String[] FAMILIES = {"IBMPlexSans", "SourceSerif4", "JetBrainsMono"};

    // The prefix of the names of the resources, before the name of the file.
    internal const String RESOURCE_PREFIX = "PDFjet.BundledFonts.";

    // The name of the file of a face: a weight below 600 is regular, from 600
    // to 699 semibold, and 700 or more bold.
    internal static String FileName(int family, int weight, bool italic) {
        String style = (weight >= 700) ? "Bold" : (weight >= 600) ? "SemiBold" : "";
        if (italic) {
            style += "Italic";
        }
        return FAMILIES[family] + "-" + ((style.Length == 0) ? "Regular" : style) + ".ttf";
    }

    // Returns the face for the document, read once for it, or null when the
    // library was built without the fonts.
    internal static Font Get(PDF pdf, int family, int weight, bool italic) {
        String name = FileName(family, weight, italic);
        Font font;
        if (pdf.bundledFonts.TryGetValue(name, out font)) {
            return font;
        }
        using (Stream stream = typeof(BundledFonts).Assembly.GetManifestResourceStream(RESOURCE_PREFIX + name)) {
            if (stream == null) {
                return null;
            }
            try {
                font = new Font(pdf, stream);
            } catch (Exception e) {
                throw new InvalidOperationException("Reading the bundled font " + name + ": " + e.Message, e);
            }
        }
        pdf.bundledFonts[name] = font;
        return font;
    }

    // Returns the family of a font-family list of an SVG file: the first of
    // its names that PDFjet knows, as serif, monospace or sans-serif, and
    // sans-serif for none.
    internal static int FamilyOf(String fontFamily) {
        foreach (String part in fontFamily.Split(',')) {
            String name = SVGState.ToLower(SVGState.Trim(part));
            if (name.Length >= 2 && (name[0] == '"' || name[0] == '\'') && name[name.Length - 1] == name[0]) {
                name = SVGState.Trim(name.Substring(1, name.Length - 2));
            }
            for (int i = 0; i < NAMES.Length; i++) {
                foreach (String known in NAMES[i]) {
                    if (name.Equals(known)) {
                        return i;
                    }
                }
            }
        }
        return SANS;
    }

    // The names of the fonts of each family, in lower case: the generic one,
    // the core fonts and those that systems and charts name most.
    private static readonly String[][] NAMES = {
        new String[] {"sans-serif", "helvetica", "arial", "system-ui", "-apple-system", "blinkmacsystemfont",
                "segoe ui", "roboto", "verdana", "tahoma", "trebuchet ms", "open sans", "noto sans",
                "liberation sans", "dejavu sans", "ibm plex sans", "inter"},
        new String[] {"serif", "times", "times new roman", "georgia", "garamond", "cambria", "palatino",
                "book antiqua", "liberation serif", "dejavu serif", "noto serif", "source serif 4",
                "source serif pro"},
        new String[] {"monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
                "sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
                "jetbrains mono", "ibm plex mono", "source code pro"},
    };
}   // End of BundledFonts.cs
}   // End of PDFjet.NET namespace
