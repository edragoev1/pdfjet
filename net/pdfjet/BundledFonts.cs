/*
 * BundledFonts.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PDFjet.NET {
/// <summary>
/// The fonts PDFjet carries in itself, in resources/fonts, each family in a
/// folder of its name, with its license: IBM Plex Sans and Serif, from Thin
/// to Bold, and Cascadia Mono, from Light to Bold, upright and italic, for
/// every European language, in Latin, Greek and Cyrillic; and, in
/// resources/fonts/world, IBM Plex Sans Arabic, Hebrew and Thai, Thin to Bold,
/// and IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold and Bold, as .otf.
/// All are under the SIL Open Font License 1.1. A path to one of them, such
/// as fonts/IBMPlexSans/IBMPlexSans-Regular.ttf, the constants of the font
/// classes, is read from the folder resources/fonts when there is no such
/// file, or else from the resources of the assembly, named PDFjet.Fonts/ and
/// their path under resources/fonts, and the text of an SVG image is drawn in
/// them unless a font is set. The PDFjet.dll of the NuGet package PDFjet
/// carries the European fonts; the others are in the assembly
/// PDFjet.Fonts.World of the NuGet package of that name, loaded by its name
/// when one of them is asked for, without a reference to it. The PDFjet.dll
/// of the releases on GitHub carries none, and is small, its package having
/// them in resources/fonts.
/// </summary>
internal static class BundledFonts {
    // Where the fonts are: the folder resources/fonts of the working
    // directory, as in the repository and the packages of the releases on
    // GitHub, whose PDFjet.dll is small; or else the resources of the
    // assembly, as in the PDFjet.dll of NuGet, built with -p:PDFjetFonts=true,
    // and those of the assembly PDFjet.Fonts.World for the other scripts.
    internal const String FOLDER = "resources/fonts/";
    // The prefix of the names of the resources, before their path under
    // resources/fonts, Family/File or world/Family/File.
    internal const String RESOURCE_PREFIX = "PDFjet.Fonts/";
    // The assembly of the NuGet package PDFjet.Fonts.World, which has the
    // fonts of resources/fonts/world as resources of the same names.
    internal const String WORLD_ASSEMBLY = "PDFjet.Fonts.World";

    // The families of the fonts of resources/fonts/world and the package
    // PDFjet.Fonts.World, apart from the European ones, as they are about 52
    // MB, and the files of the faces of each that PDFjet carries: IBM Plex
    // Sans JP, KR, SC and TC as .otf, the others as .ttf.
    private static readonly Dictionary<String, List<String>> WORLD_FACES = WorldFaces();

    private static Dictionary<String, List<String>> WorldFaces() {
        Dictionary<String, List<String>> faces = new Dictionary<String, List<String>>();
        foreach (String family in new String[] {"IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC"}) {
            List<String> files = new List<String>();
            foreach (String style in new String[] {"Regular", "SemiBold", "Bold"}) {
                files.Add(family + "-" + style + ".otf");
            }
            faces[family] = files;
        }
        foreach (String family in new String[] {"IBMPlexSansArabic", "IBMPlexSansHebrew", "IBMPlexSansThai"}) {
            List<String> files = new List<String>();
            foreach (String style in new String[] {"Thin", "Light", "Text", "Regular", "Medium", "SemiBold", "Bold"}) {
                files.Add(family + "-" + style + ".ttf");
            }
            faces[family] = files;
        }
        return faces;
    }

    // What the fonts of the package PDFjet.Fonts.World are for.
    internal const String WORLD_SCRIPTS = "with PDFjet's fonts for Chinese, Japanese, Korean, Arabic, Hebrew and Thai";

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

    // Returns the family of a name, Family/File, if it is one of the
    // families of resources/fonts/world, or null.
    private static String WorldFamilyOf(String name) {
        int slash = name.IndexOf('/');
        String family = (slash < 0) ? name : name.Substring(0, slash);
        return WORLD_FACES.ContainsKey(family) ? family : null;
    }

    // Returns the name of a family as people write it, such as IBM Plex Sans
    // Arabic of IBMPlexSansArabic.
    internal static String FamilyName(String family) {
        String name = "IBM Plex " + family.Substring("IBMPlex".Length);
        int sans = name.IndexOf("Sans", StringComparison.Ordinal);
        return (sans < 0 || name.Length == sans + 4) ? name : name.Insert(sans + 4, " ");
    }

    // Returns the path of a font under resources/fonts, and after
    // PDFjet.Fonts/ among the resources, by its name, Family/File: the name,
    // or world/ and the name for a font of resources/fonts/world, such as
    // world/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.
    internal static String FolderPathOf(String name) {
        return (WorldFamilyOf(name) != null) ? "world/" + name : name;
    }

    // The assembly PDFjet.Fonts.World, loaded once, when one of its fonts is
    // first asked for, or null when the program does not have it.
    private static readonly Lazy<Assembly> worldAssembly = new Lazy<Assembly>(LoadWorldAssembly);

    internal static Assembly WorldAssembly {
        get { return worldAssembly.Value; }
    }

    private static Assembly LoadWorldAssembly() {
        try {
            return Assembly.Load(new AssemblyName(WORLD_ASSEMBLY));
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
    // font of resources/fonts/world, from the assembly PDFjet.Fonts.World.
    internal static Stream Open(String fontPath) {
        return Open(fontPath, FOLDER);
    }

    // Opens the font of the path that PDFjet carries, or returns null: from
    // the folder, or else from the assembly, or else, for a font of
    // resources/fonts/world, from the assembly PDFjet.Fonts.World.
    internal static Stream Open(String fontPath, String folder) {
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
        if (stream == null && WorldFamilyOf(name) != null && WorldAssembly != null) {
            stream = WorldAssembly.GetManifestResourceStream(RESOURCE_PREFIX + FolderPathOf(name));
        }
        return stream;
    }

    // Opens the font of the name, Family/File, among the resources of the
    // assembly, or returns null when it was built without the fonts.
    internal static Stream OpenResource(String name) {
        return typeof(BundledFonts).Assembly.GetManifestResourceStream(RESOURCE_PREFIX + FolderPathOf(name));
    }

    // Returns what to do when the font of the path is one of the fonts of
    // resources/fonts/world that PDFjet carries, which is in neither that
    // folder nor the program, or null for any other font: add the NuGet
    // package PDFjet.Fonts.World.
    internal static String WorldMissing(String fontPath) {
        String name = NameOf(fontPath);
        if (name == null) {
            return null;
        }
        String family = WorldFamilyOf(name);
        if (family == null) {
            return null;
        }
        String file = name.Substring(family.Length + 1);
        List<String> faces = WORLD_FACES[family];
        // A path written for the .stream files PDFjet shipped before is the same font
        foreach (String stream in new String[] {".ttf.stream", ".otf.stream"}) {
            if (file.EndsWith(stream, StringComparison.Ordinal)) {
                String first = faces[0];
                file = file.Substring(0, file.Length - stream.Length) + first.Substring(first.LastIndexOf('.'));
            }
        }
        if (!faces.Contains(file)) {
            return null;
        }
        return fontPath + " is not a file, nor in resources/fonts/world. "
                + FamilyName(family) + " is in the NuGet package " + WORLD_ASSEMBLY + ", " + WORLD_SCRIPTS
                + ": dotnet add package " + WORLD_ASSEMBLY + " --version " + PDF.VERSION
                + " (in the project file: <PackageReference Include=\"" + WORLD_ASSEMBLY + "\" Version=\""
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
