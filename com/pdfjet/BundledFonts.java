/*
 * BundledFonts.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.InputStream;

/**
 * The fonts PDFjet carries in itself, in resources/fonts, each family in a
 * folder of its name, with its license: IBM Plex Sans
 * and Serif, from Thin to Bold, and Cascadia Mono, from
 * Light to Bold, upright and italic; IBM Plex Sans Arabic, Hebrew and Thai, Thin to
 * Bold; IBM Plex Math; and, in resources/fonts/cjk, IBM Plex Sans JP, KR, SC
 * and TC, Regular, SemiBold and Bold, as .otf. All are under the SIL Open
 * Font License 1.1. A path to one of them, such as
 * fonts/IBMPlexSans/IBMPlexSans-Regular.ttf, the constants of
 * com.pdfjet.fonts, is read from the folder resources/fonts when there is no
 * such file, or else from the classpath, and the text of an SVG image is
 * drawn in them unless a font is set. The jar of Maven Central,
 * com.pdfjet:pdfjet, carries all but the CJK fonts, which are in the jar
 * com.pdfjet:pdfjet-fonts-cjk, at com/pdfjet/fonts/cjk, found on the
 * classpath when a program has it; the PDFjet.jar of the releases on GitHub
 * carries none, and is small, its package having them in resources/fonts.
 */
final class BundledFonts {
    private BundledFonts() {
    }

    // Where the fonts are: the folder resources/fonts of the working
    // directory, as in the repository and the packages of the releases on
    // GitHub, whose PDFjet.jar is small; or else the classpath, as with the
    // jar of Maven Central, which carries them but the CJK ones, and the jar
    // com.pdfjet:pdfjet-fonts-cjk, which carries those.
    static final String FOLDER = "resources/fonts/";
    private static final String RESOURCES = "com/pdfjet/fonts/";

    // The families of the CJK fonts, which are in resources/fonts/cjk and in
    // the jar com.pdfjet:pdfjet-fonts-cjk, apart from the others, as they
    // are about 48 MB.
    private static final String[] CJK_FAMILIES = {"IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC"};

    // Returns the name of a font under resources/fonts, Family/File, of a
    // path to it, such as fonts/IBMPlexSans/IBMPlexSans-Regular.ttf: its
    // last two parts.
    static String nameOf(String fontPath) {
        String path = fontPath.replace('\\', '/');
        int file = path.lastIndexOf('/');
        if (file <= 0) {
            return null;
        }
        int family = path.lastIndexOf('/', file - 1);
        return path.substring(family + 1);
    }

    // Returns the family of a name, Family/File, if it is one of the CJK
    // families, or null.
    private static String cjkFamilyOf(String name) {
        int slash = name.indexOf('/');
        String family = (slash < 0) ? name : name.substring(0, slash);
        for (String cjk : CJK_FAMILIES) {
            if (cjk.equals(family)) {
                return cjk;
            }
        }
        return null;
    }

    // Returns the path of a font under resources/fonts, and under
    // com/pdfjet/fonts in the jars, by its name, Family/File: the name, or
    // cjk/ and the name for a CJK font, such as
    // cjk/IBMPlexSansJP/IBMPlexSansJP-Regular.otf.
    static String folderPathOf(String name) {
        return (cjkFamilyOf(name) != null) ? "cjk/" + name : name;
    }

    // Opens the font of the path that PDFjet carries, or returns null: from
    // the folder resources/fonts, or else from the classpath.
    static InputStream open(String fontPath) {
        return open(fontPath, FOLDER, BundledFonts.class.getClassLoader());
    }

    // Opens the font of the path that PDFjet carries, or returns null: from
    // the folder, or else from the jar of the library, or else, for a CJK
    // font, from the classpath of the loader, which has the jar
    // com.pdfjet:pdfjet-fonts-cjk when the program does.
    static InputStream open(String fontPath, String folder, ClassLoader loader) {
        String name = nameOf(fontPath);
        if (name == null || name.indexOf("..") >= 0) {
            return null;
        }
        String path = folderPathOf(name);
        java.io.File file = new java.io.File(folder + path);
        if (file.isFile()) {
            try {
                return new java.io.FileInputStream(file);
            } catch (java.io.FileNotFoundException e) {
                // Read from the jar then
            }
        }
        // The resources of a jar of a named module are read through the
        // class loader, as they are not in the module of PDFjet.
        InputStream stream = BundledFonts.class.getResourceAsStream("/" + RESOURCES + path);
        if (stream == null && loader != null) {
            stream = loader.getResourceAsStream(RESOURCES + path);
        }
        return stream;
    }

    // Returns what to do when the font of the path is one of the CJK fonts
    // PDFjet carries, Regular, SemiBold or Bold, which is in neither the
    // folder resources/fonts/cjk nor the classpath, or null for any other
    // font: add the jar com.pdfjet:pdfjet-fonts-cjk.
    static String cjkMissing(String fontPath) {
        String name = nameOf(fontPath);
        if (name == null) {
            return null;
        }
        String family = cjkFamilyOf(name);
        if (family == null) {
            return null;
        }
        String file = name.substring(family.length() + 1);
        if (!file.equals(family + "-Regular.otf") && !file.equals(family + "-SemiBold.otf")
                && !file.equals(family + "-Bold.otf")) {
            return null;
        }
        return fontPath + " is not a file, nor in resources/fonts/cjk. "
                + "IBM Plex Sans " + family.substring("IBMPlexSans".length())
                + " is in the package com.pdfjet:pdfjet-fonts-cjk. Add it next to com.pdfjet:pdfjet: "
                + "<dependency><groupId>com.pdfjet</groupId><artifactId>pdfjet-fonts-cjk</artifactId>"
                + "<version>" + PDF.VERSION + "</version></dependency> "
                + "(Gradle: implementation(\"com.pdfjet:pdfjet-fonts-cjk:" + PDF.VERSION + "\"))";
    }

    static final int SANS = 0;
    static final int SERIF = 1;
    static final int MONO = 2;
    private static final String[] FAMILIES = {"IBMPlexSans", "IBMPlexSerif", "CascadiaMono"};
    // The CSS weights the faces of each family stand for, and their styles.
    // Cascadia Mono's strokes are heavier than IBM Plex Sans's at the same
    // weight: its SemiLight is as thick as Plex Sans Regular, so it stands
    // for 400, and its Regular for 500, as in PDFjet Forms (the owner, 29
    // October 2026); its SemiBold and Bold match Plex Sans's as they are.
    private static final int[][] WEIGHTS = {
        {100, 300, 400, 450, 500, 600, 700},
        {100, 300, 400, 450, 500, 600, 700},
        {300, 400, 500, 600, 700},
    };
    private static final String[][] STYLES = {
        {"Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"},
        {"Thin", "Light", "Regular", "Text", "Medium", "SemiBold", "Bold"},
        {"Light", "SemiLight", "Regular", "SemiBold", "Bold"},
    };

    // Returns the name of a face of an SVG family, Family/File: the weight
    // CSS matches to the one asked for among those the family has, and
    // italic or upright.
    static String fileName(int family, int weight, boolean italic) {
        int w = match(WEIGHTS[family], weight);
        String style = "Regular";
        for (int i = 0; i < WEIGHTS[family].length; i++) {
            if (WEIGHTS[family][i] == w) {
                style = STYLES[family][i];
            }
        }
        if (italic) {
            style = style.equals("Regular") ? "Italic" : style + "Italic";
        }
        return FAMILIES[family] + "/" + FAMILIES[family] + "-" + style + ".ttf";
    }

    // Returns the weight CSS matches to the one asked for, of the weights a
    // family has in order (CSS Fonts 4, 5.2): that weight; from 400 to 500,
    // the weights up to 500, then the lighter ones, then the heavier ones;
    // below 400, the lighter ones, then the heavier ones; above 500, the
    // heavier ones, then the lighter ones; each nearest first.
    static int match(int[] weights, int weight) {
        int lighter = -1;     // The nearest lighter weight
        int heavier = -1;     // The nearest heavier weight
        int upTo500 = -1;     // The nearest heavier weight up to 500
        for (int w : weights) {
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

    // Returns the face for the document, read once for it, or null when the
    // library was built without it.
    static Font get(PDF pdf, int family, int weight, boolean italic) {
        String name = fileName(family, weight, italic);
        Font font = pdf.bundledFonts.get(name);
        if (font != null) {
            return font;
        }
        try (InputStream stream = open(name)) {
            if (stream == null) {
                return null;
            }
            font = new Font(pdf, stream);
        } catch (Exception e) {
            throw new IllegalStateException("Reading the font " + name + ": " + e.getMessage(), e);
        }
        pdf.bundledFonts.put(name, font);
        return font;
    }

    // Returns the family of a font-family list of an SVG file: the first of
    // its names that PDFjet knows, as serif, monospace or sans-serif, and
    // sans-serif for none.
    static int familyOf(String fontFamily) {
        for (String name : fontFamily.split(",", -1)) {
            name = SVGState.toLower(SVGState.trim(name));
            if (name.length() >= 2 && (name.charAt(0) == '"' || name.charAt(0) == '\'')
                    && name.charAt(name.length() - 1) == name.charAt(0)) {
                name = SVGState.trim(name.substring(1, name.length() - 1));
            }
            for (int i = 0; i < NAMES.length; i++) {
                for (String known : NAMES[i]) {
                    if (name.equals(known)) {
                        return i;
                    }
                }
            }
        }
        return SANS;
    }

    // The names of the fonts of each family, in lower case: the generic one,
    // the core fonts and those that systems and charts name most.
    private static final String[][] NAMES = {
        {"sans-serif", "helvetica", "arial", "system-ui", "-apple-system", "blinkmacsystemfont",
                "segoe ui", "roboto", "verdana", "tahoma", "trebuchet ms", "open sans", "noto sans",
                "liberation sans", "dejavu sans", "ibm plex sans", "inter"},
        {"serif", "times", "times new roman", "georgia", "garamond", "cambria", "palatino",
                "book antiqua", "liberation serif", "dejavu serif", "noto serif", "source serif 4",
                "source serif pro", "ibm plex serif"},
        {"monospace", "courier", "courier new", "consolas", "menlo", "monaco", "ui-monospace",
                "sfmono-regular", "lucida console", "liberation mono", "dejavu sans mono",
                "jetbrains mono", "ibm plex mono", "source code pro", "cascadia mono", "cascadia code"},
    };
}   // End of BundledFonts.java
