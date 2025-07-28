/**
 *  GenerateFontMetricsFiles.java
 *
©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;

/**
 * This program generates font metrics .ts file from .otf and .ttf fonts.
 * These font metrics files are used by the TypeScript pdfjet-builder.
 */
public class GenerateFontMetricsFiles {
    public static void generateFontMetricsFiles(String path, String fileName) throws Exception {
        BufferedOutputStream fos = new BufferedOutputStream(
                new FileOutputStream(fileName.substring(0, fileName.lastIndexOf(".")) + ".ts"));

        OTF otf = new OTF(new FileInputStream(path + fileName));
        StringBuilder sb1 = new StringBuilder();
        sb1.append("import { BaseFont } from \"../BaseFont.js\";\n");
        sb1.append("export class ");
        sb1.append(fileName.substring(0, fileName.lastIndexOf(".")).replaceAll("-", ""));
        sb1.append(" extends BaseFont {\n");
        sb1.append("    constructor() {\n");
        sb1.append("        super();\n");
        sb1.append("        this.fontName = \"" + otf.fontName + "\";\n");
        sb1.append("        this.unitsPerEm = " + otf.unitsPerEm + ";\n");
        sb1.append("        this.bBoxLLx = " + otf.bBoxLLx + ";\n");
        sb1.append("        this.bBoxLLy = " + otf.bBoxLLy + ";\n");
        sb1.append("        this.bBoxURx = " + otf.bBoxURx + ";\n");
        sb1.append("        this.bBoxURy = " + otf.bBoxURy + ";\n");
        sb1.append("        this.ascent = " + otf.ascent + ";\n");
        sb1.append("        this.descent = " + otf.descent + ";\n");
        sb1.append("        this.firstChar = " + otf.firstChar + ";\n");
        sb1.append("        this.lastChar = " + otf.lastChar + ";\n");
        sb1.append("        this.capHeight = " + otf.capHeight + ";\n");
        sb1.append("        this.underlinePosition = " + otf.underlinePosition + ";\n");
        sb1.append("        this.underlineThickness = " + otf.underlineThickness + ";\n");

        // Here we create an array [delta, width, delta, width, delta, width, ... delta, width,]
        // where the delta is between the last unicode character and the current one.
        sb1.append("        this.advanceWidth = [\n");
        sb1.append("            ");
        int numberOfPairs = 0;
        int ch = 0; // the last unicode character
        for (int i = 0; i < otf.unicodeToGID.length; i++) {
            int gid = otf.unicodeToGID[i];
            if (gid != 0 && gid < otf.advanceWidth.length) {
                sb1.append((i - ch) + ", ");
                sb1.append(otf.advanceWidth[gid]);
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

    public static void main(String[] args) throws Exception {
        if (args.length < 1) {
            System.err.println("Usage:");
            System.err.println("    ./generate-font-metrics-files.sh <directory>");
            System.err.println("Examples:");
            System.err.println("    ./generate-font-metrics-files.sh fonts/RedHatText/");
            System.err.println("    ./generate-font-metrics-files.sh fonts/SourceCodePro/");
            System.exit(1);
        }
        File file = new File(args[0]);
        if (file.isDirectory()) {
            String path = file.getPath();
            String[] list = file.list();
            for (String fileName : list) {
                if (fileName.endsWith(".ttf") || fileName.endsWith(".otf")) {
                    System.out.println("Reading: " + fileName);
                    generateFontMetricsFiles(path + File.separator, fileName);
                    System.out.println("Writing: " + fileName.substring(0, fileName.lastIndexOf(".")) + ".ts");
                }
            }
        } else {
            System.err.println("Usage:");
            System.err.println("    ./generate-font-metrics-files.sh <directory>");
            System.err.println("Examples:");
            System.err.println("    ./generate-font-metrics-files.sh fonts/RedHatText/");
            System.err.println("    ./generate-font-metrics-files.sh fonts/SourceCodePro/");
            System.exit(1);
        }
    }
}
