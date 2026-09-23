/*
 * GenerateFontMetricsFiles.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;

/**
 * This program generates font metrics .ts file from .otf and .ttf fonts.
 * These font metrics files are used by the TypeScript pdfjet-client, which
 * measures text as Font.stringWidth does, so they hold what that needs: the
 * advance width of each character, the line gap, and the width of .notdef.
 * It is in the package of OTF, which is not public.
 */
public class GenerateFontMetricsFiles {
    private GenerateFontMetricsFiles() {
    }

    /**
     * Writes a TypeScript font metrics file for the specified font.
     *
     * @param path the directory that contains the font, ending with a separator.
     * @param fileName the font file name. The output file has the same name with a .ts extension.
     * @param outDir the directory the output file is written to, ending with a separator.
     * @throws Exception if the font cannot be read or the output file cannot be written.
     */
    public static void generateFontMetricsFiles(String path, String fileName, String outDir) throws Exception {
        BufferedOutputStream fos = new BufferedOutputStream(
                new FileOutputStream(outDir + fileName.substring(0, fileName.lastIndexOf(".")) + ".ts"));

        OTF otf = new OTF(new FileInputStream(path + fileName));
        StringBuilder sb1 = new StringBuilder();
        sb1.append("import { BaseFont } from \"../BaseFont.js\";\n");
        sb1.append("export class ");
        sb1.append(fileName.substring(0, fileName.lastIndexOf(".")).replaceAll("-", ""));
        sb1.append(" extends BaseFont {\n");
        sb1.append("    constructor() {\n");
        sb1.append("        super();\n");
        // The name of the file, which the @font-face rules of pdfjet-client
        // give the font as its family, and SVG text uses.
        sb1.append("        this.fontName = \"" + fileName.substring(0, fileName.lastIndexOf(".")) + "\";\n");
        sb1.append("        this.unitsPerEm = " + otf.unitsPerEm + ";\n");
        sb1.append("        this.bBoxLLx = " + otf.bBoxLLx + ";\n");
        sb1.append("        this.bBoxLLy = " + otf.bBoxLLy + ";\n");
        sb1.append("        this.bBoxURx = " + otf.bBoxURx + ";\n");
        sb1.append("        this.bBoxURy = " + otf.bBoxURy + ";\n");
        sb1.append("        this.ascent = " + otf.ascent + ";\n");
        sb1.append("        this.descent = " + otf.descent + ";\n");
        sb1.append("        this.lineGap = " + otf.lineGap + ";\n");
        sb1.append("        this.firstChar = " + otf.firstChar + ";\n");
        sb1.append("        this.lastChar = " + otf.lastChar + ";\n");
        sb1.append("        this.capHeight = " + otf.capHeight + ";\n");
        sb1.append("        this.underlinePosition = " + otf.underlinePosition + ";\n");
        sb1.append("        this.underlineThickness = " + otf.underlineThickness + ";\n");
        // The width of .notdef, glyph 0, which a character the font does not
        // have is drawn with in a document of no compliance.
        sb1.append("        this.notdefWidth = " + otf.advanceWidth[0] + ";\n");

        // Here we create an array [delta, width, delta, width, delta, width, ... delta, width,]
        // where the delta is between the last unicode character and the current one.
        sb1.append("        this.advanceWidth = [\n");
        sb1.append("            ");
        int numberOfPairs = 0;
        int ch = 0; // the last unicode character
        for (int i = 0; i < otf.unicodeToGID.length; i++) {
            int gid = otf.unicodeToGID[i];
            if (gid != 0) {
                sb1.append((i - ch) + ", ");
                // A font can list fewer advance widths than it has glyphs, and
                // the glyphs past the end have the width of the last one.
                sb1.append(otf.advanceWidth[Math.min(gid, otf.advanceWidth.length - 1)]);
                numberOfPairs++;
                if (numberOfPairs < 10) {
                    sb1.append(", ");
                } else {
                    sb1.append(",\n            ");
                    numberOfPairs = 0;
                }
                ch = i;
            }
        }
        StringBuilder sb2 = new StringBuilder();
        sb2.append(sb1.toString().trim());
        sb2.append("];\n");

        sb2.append("    }\n");
        sb2.append("}\n");

        fos.write(sb2.toString().getBytes(StandardCharsets.UTF_8));
        fos.close();
    }

    /**
     * Generates the font metrics files for the fonts in a directory.
     *
     * @param args the command line arguments: the directory with the fonts, and
     *     the directory to write the files to, the current one by default.
     * @throws Exception if a font cannot be read or an output file cannot be written.
     */
    public static void main(String[] args) throws Exception {
        if (args.length < 1) {
            System.err.println("Usage:");
            System.err.println("    util/generate-font-metrics-files.sh <directory> [<output directory>]");
            System.err.println("Examples:");
            System.err.println("    util/generate-font-metrics-files.sh fonts/IBMPlexSans");
            System.err.println("    util/generate-font-metrics-files.sh fonts/IBMPlexSans ../pdfjet-client/src/fonts/IBMPlexSans");
            System.exit(1);
        }
        File file = new File(args[0]);
        String outDir = args.length > 1 ? args[1] + File.separator : "";
        if (file.isDirectory()) {
            String path = file.getPath();
            String[] list = file.list();
            for (String fileName : list) {
                if (fileName.endsWith(".ttf") || fileName.endsWith(".otf")) {
                    System.out.println("Reading: " + fileName);
                    generateFontMetricsFiles(path + File.separator, fileName, outDir);
                    System.out.println("Writing: " + outDir + fileName.substring(0, fileName.lastIndexOf(".")) + ".ts");
                }
            }
        } else {
            System.err.println("Usage:");
            System.err.println("    util/generate-font-metrics-files.sh <directory> [<output directory>]");
            System.err.println("Examples:");
            System.err.println("    util/generate-font-metrics-files.sh fonts/IBMPlexSans");
            System.err.println("    util/generate-font-metrics-files.sh fonts/IBMPlexSans ../pdfjet-client/src/fonts/IBMPlexSans");
            System.exit(1);
        }
    }
}
