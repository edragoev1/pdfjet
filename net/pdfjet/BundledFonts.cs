/*
 * BundledFonts.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Reflection;

namespace PDFjet.NET {
/// <summary>
/// The fonts PDFjet carries in itself, in resources/fonts, each family in a
/// folder of its name, with its license: IBM Plex Sans
/// and Serif, from Thin to Bold, and Cascadia Mono, from
/// Light to Bold, upright and italic; IBM Plex Sans Arabic, Hebrew and Thai,
/// Thin to Bold; IBM Plex Math; and, in resources/fonts/cjk, IBM Plex Sans JP,
/// KR, SC and TC, Regular, SemiBold and Bold, as .otf. All are under the SIL
/// Open Font License 1.1. A path to one of them, such as
/// fonts/IBMPlexSans/IBMPlexSans-Regular.ttf, the constants of the font
/// classes, is read from the folder resources/fonts when there is no such
/// file, or else from the resources of the assembly, named PDFjet.Fonts/ and
/// their path under resources/fonts, and the text of an SVG image is drawn in
/// them unless a font is set. The PDFjet.dll of the NuGet package PDFjet
/// carries all but the CJK fonts, which are in the assembly
/// PDFjet.Fonts.CJK of the NuGet package of that name, loaded by its name
/// when a CJK font is asked for, without a reference to it; the PDFjet.dll of
/// the releases on GitHub carries none, and is small, its package having them
/// in resources/fonts.
/// </summary>
internal static class BundledFonts {
    // Where the fonts are: the folder resources/fonts of the working
    // directory, as in the repository and the packages of the releases on
    // GitHub, whose PDFjet.dll is small; or else the resources of the
    // assembly, as in the PDFjet.dll of NuGet, built with -p:PDFjetFonts=true,
    // and those of the assembly PDFjet.Fonts.CJK for the CJK fonts.
    internal const String FOLDER = "resources/fonts/";
    // The prefix of the names of the resources, before their path under
    // resources/fonts, Family/File or cjk/Family/File.
    internal const String RESOURCE_PREFIX = "PDFjet.Fonts/";
    // The assembly of the NuGet package PDFjet.Fonts.CJK, which has the CJK
    // fonts as resources of the same names.
    internal const String CJK_ASSEMBLY = "PDFjet.Fonts.CJK";

    // The families of the CJK fonts, which are in resources/fonts/cjk and in
    // the package PDFjet.Fonts.CJK, apart from the others, as they are about
    // 48 MB.
    private static readonly String[] CJK_FAMILIES = {"IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC"};

    // Returns the name of a font under resources/fonts, Family/File, of a
    // path to it, such as fonts/IBMPlexSans/IBMPlexSans-Regular.ttf: its
    // last two parts.
    internal static String NameOf(String fontPath) {
        String path = fontPath.Replace('\\', '/');
        int file = path.LastIndexOf('/');
        if (file <= 0) {
            return null;
        }
        int family = path.LastIndexOf('/', file - 1);
        return path.Substring(family + 1);
    }

    // Returns the family of a name, Family/File, if it is one of the CJK
    // families, or null.
    private static String CjkFamilyOf(String name) {
        int slash = name.IndexOf('/');
        String family = (slash < 0) ? name : name.Substring(0, slash);
        foreach (String cjk in CJK_FAMILIES) {
            if (cjk.Equals(family)) {
                return cjk;
            }
        }
        return null;
    }

    // Returns the path of a font under resources/fonts, and after
    // PDFjet.Fonts/ among the resources, by its name, Family/File: the name,
    // or cjk/ and the name for a CJK font, such as
    // cjk/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.
    internal static String FolderPathOf(String name) {
        return (CjkFamilyOf(name) != null) ? "cjk/" + name : name;
    }

    // The assembly PDFjet.Fonts.CJK, loaded once, when a CJK font is first
    // asked for, or null when the program does not have it.
    private static readonly Lazy<Assembly> cjkAssembly = new Lazy<Assembly>(LoadCjkAssembly);

    internal static Assembly CjkAssembly {
        get { return cjkAssembly.Value; }
    }

    private static Assembly LoadCjkAssembly() {
        try {
            return Assembly.Load(new AssemblyName(CJK_ASSEMBLY));
        } catch (FileNotFoundException) {
            return null;
        } catch (FileLoadException) {
            return null;
        } catch (BadImageFormatException) {
            return null;
        }
    }

    // Opens the font of the path that PDFjet carries, or returns null: from
    // the folder resources/fonts, or else from the assembly, or else, for a
    // CJK font, from the assembly PDFjet.Fonts.CJK.
    internal static Stream Open(String fontPath) {
        return Open(fontPath, FOLDER, null, true);
    }

    // Opens the font of the path that PDFjet carries, or returns null: from
    // the folder, or else from the assembly, or else, for a CJK font, from
    // the assembly cjk, or PDFjet.Fonts.CJK when loadCjk.
    internal static Stream Open(String fontPath, String folder, Assembly cjk, bool loadCjk) {
        String name = NameOf(fontPath);
        if (name == null || name.IndexOf("..", StringComparison.Ordinal) >= 0) {
            return null;
        }
        String file = folder + FolderPathOf(name);
        if (File.Exists(file)) {
            try {
                return new FileStream(file, FileMode.Open, FileAccess.Read);
            } catch (IOException) {
                // Read from the assembly then
            } catch (UnauthorizedAccessException) {
                // Read from the assembly then
            }
        }
        Stream stream = OpenResource(name);
        if (stream == null && CjkFamilyOf(name) != null) {
            if (cjk == null && loadCjk) {
                cjk = CjkAssembly;
            }
            if (cjk != null) {
                stream = cjk.GetManifestResourceStream(RESOURCE_PREFIX + FolderPathOf(name));
            }
        }
        return stream;
    }

    // Opens the font of the name, Family/File, among the resources of the
    // assembly, or returns null when it was built without the fonts.
    internal static Stream OpenResource(String name) {
        return typeof(BundledFonts).Assembly.GetManifestResourceStream(RESOURCE_PREFIX + FolderPathOf(name));
    }

    // Returns what to do when the font of the path is one of the CJK fonts
    // PDFjet carries, Regular, SemiBold or Bold, which is in neither the
    // folder resources/fonts/cjk nor the program, or null for any other
    // font: add the NuGet package PDFjet.Fonts.CJK.
    internal static String CjkMissing(String fontPath) {
        String name = NameOf(fontPath);
        if (name == null) {
            return null;
        }
        String family = CjkFamilyOf(name);
        if (family == null) {
            return null;
        }
        String file = name.Substring(family.Length + 1);
        if (!file.Equals(family + "-Regular.otf") && !file.Equals(family + "-SemiBold.otf")
                && !file.Equals(family + "-Bold.otf")) {
            return null;
        }
        return fontPath + " is not a file, nor in resources/fonts/cjk. "
                + "IBM Plex Sans " + family.Substring("IBMPlexSans".Length)
                + " is in the NuGet package " + CJK_ASSEMBLY + ": dotnet add package " + CJK_ASSEMBLY
                + " --version " + PDF.VERSION
                + " (in the project file: <PackageReference Include=\"" + CJK_ASSEMBLY + "\" Version=\""
                + PDF.VERSION + "\" />)";
    }

    internal const int SANS = 0;
    internal const int SERIF = 1;
    internal const int MONO = 2;
    private static readonly String[] FAMILIES = {"IBMPlexSans", "IBMPlexSerif", "CascadiaMono"};
    // The CSS weights the faces of each family stand for, and their styles.
    // Cascadia Mono's strokes are heavier than IBM Plex Sans's at the same
    // weight: its SemiLight is as thick as Plex Sans Regular, so it stands
    // for 400, and its Regular for 500, as in PDFjet Forms (the owner, 29
    // October 2026); its SemiBold and Bold match Plex Sans's as they are.
    private static readonly int[][] WEIGHTS = {
        new int[] {100, 300, 400, 450, 500, 600, 700},
        new int[] {100, 300, 400, 450, 500, 600, 700},
        new int[] {300, 400, 500, 600, 700},
    };
    private static readonly String[][] STYLES = {
        new String[] {"Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"},
        new String[] {"Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"},
        new String[] {"Light", "SemiLight", "Regular", "SemiBold", "Bold"},
    };

    // Returns the name of a face of an SVG family, Family/File: the weight
    // CSS matches to the one asked for among those the family has, and
    // italic or upright.
    internal static String FileName(int family, int weight, bool italic) {
        int w = Match(WEIGHTS[family], weight);
        String style = "Regular";
        for (int i = 0; i < WEIGHTS[family].Length; i++) {
            if (WEIGHTS[family][i] == w) {
                style = STYLES[family][i];
            }
        }
        if (italic) {
            style = style.Equals("Regular") ? "Italic" : style + "Italic";
        }
        return FAMILIES[family] + "/" + FAMILIES[family] + "-" + style + ".ttf";
    }

    // Returns the weight CSS matches to the one asked for, of the weights a
    // family has in order (CSS Fonts 4, 5.2): that weight; from 400 to 500,
    // the weights up to 500, then the lighter ones, then the heavier ones;
    // below 400, the lighter ones, then the heavier ones; above 500, the
    // heavier ones, then the lighter ones; each nearest first.
    internal static int Match(int[] weights, int weight) {
        int lighter = -1;     // The nearest lighter weight
        int heavier = -1;     // The nearest heavier weight
        int upTo500 = -1;     // The nearest heavier weight up to 500
        foreach (int w in weights) {
            if (w == weight) {
                return w;
            } else if (w < weight) {
                lighter = w;
            } else if (heavier < 0) {
                heavier = w;
            }
            if (w > weight && w <= 500 && upTo500 < 0) {
                upTo500 = w;
            }
        }
        if (weight >= 400 && weight <= 500 && upTo500 >= 0) {
            return upTo500;
        }
        if (weight <= 500) {
            return (lighter >= 0) ? lighter : heavier;
        }
        return (heavier >= 0) ? heavier : lighter;
    }

    // Returns the face for the document, read once for it, or null when it is
    // neither in resources/fonts nor in the library.
    internal static Font Get(PDF pdf, int family, int weight, bool italic) {
        String name = FileName(family, weight, italic);
        Font font;
        if (pdf.bundledFonts.TryGetValue(name, out font)) {
            return font;
        }
        using (Stream stream = Open(name)) {
            if (stream == null) {
                return null;
            }
            try {
                font = new Font(pdf, stream);
            } catch (Exception e) {
                throw new InvalidOperationException("Reading the font " + name + ": " + e.Message, e);
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
                "source serif pro", "ibm plex serif"},
        new String[] {"monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
                "sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
                "jetbrains mono", "ibm plex mono", "source code pro", "cascadia mono", "cascadia code"},
    };
}   // End of BundledFonts.cs
}   // End of PDFjet.NET namespace
