/*
 * HBShape.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * A Shaper for ComplexText that runs hb-shape, the program of HarfBuzz, the
 * text engine of Chrome and Android: the same shaping on every server, with
 * nothing linked into the program. Lines break at spaces and, in Thai, Lao,
 * Khmer and Burmese, between words, by ICU's dictionaries of the world fonts.
 * hb-shape is installed on the server, HarfBuzz 15 or newer:
 * <pre>
 * Ubuntu, Debian: apt install libharfbuzz-bin
 * Fedora, RHEL:   dnf install harfbuzz-utils
 * macOS, Linux:   brew install harfbuzz
 * Windows:        harfbuzz-win64-&lt;version&gt;.zip of github.com/harfbuzz/harfbuzz/releases
 * </pre>
 * It talks with hb-shape through files, not pipes, the same on every system:
 * the text of each font in a temporary file, the glyphs back in another.
 * <pre>
 * new ComplexText(hindi, text).setFallbackFont(latin).setShaper(new HBShape())
 * </pre>
 */
public class HBShape implements Shaper {
    private final String program;

    /** An HBShape that runs hb-shape of the PATH. */
    public HBShape() {
        this("hb-shape");
    }

    /**
     * An HBShape that runs the hb-shape of the path.
     *
     * @param program hb-shape's path, or its name looked for on the PATH.
     */
    public HBShape(String program) {
        this.program = program;
    }

    /**
     * What hb-shape --version prints first, such as "hb-shape (HarfBuzz) 15.2.0".
     *
     * @return the version.
     * @throws Exception if hb-shape cannot be run.
     */
    public String getVersion() throws Exception {
        File out = File.createTempFile("pdfjet-hbshape-", ".txt");
        try {
            run(new String[] {program, "--version"}, out);
            String text = new String(Files.readAllBytes(out.toPath()), StandardCharsets.UTF_8);
            int end = text.indexOf('\n');
            return (end >= 0 ? text.substring(0, end) : text).trim();
        } finally {
            out.delete();
        }
    }

    // Runs the command, its output to the file, its errors to another, and
    // waits for it to end: files, not pipes, so that nothing waits on a full pipe
    private void run(String[] command, File out) throws Exception {
        File err = File.createTempFile("pdfjet-hbshape-", ".err");
        try {
            Process process;
            try {
                process = new ProcessBuilder(command)
                        .redirectOutput(out).redirectError(err).redirectInput(ProcessBuilder.Redirect.PIPE)
                        .start();
            } catch (IOException e) {
                throw new Exception("ComplexText's HBShape could not run hb-shape, the program of HarfBuzz: "
                        + "install it (Ubuntu: apt install libharfbuzz-bin; macOS: brew install harfbuzz; "
                        + "Windows: HarfBuzz's harfbuzz-win64 zip), or give its path to HBShape. " + e.getMessage(), e);
            }
            process.getOutputStream().close();
            int exit = process.waitFor();
            if (exit != 0) {
                String message = new String(Files.readAllBytes(err.toPath()), StandardCharsets.UTF_8).trim();
                throw new Exception("hb-shape: exit " + exit + (message.isEmpty() ? "" : ": " + message));
            }
        } finally {
            err.delete();
        }
    }

    // A glyph shaped, in points from the start of the text
    private static final class Glyph {
        int id;
        int font;
        int cluster;    // The index in the text, in code points, of its cluster's first character
        float x;
        float y;
        float advance;
    }

    // hb-shape's line of JSON, [{"g":1,"cl":0,"dx":0,"dy":0,"ax":259,"ay":0},...]:
    // for each glyph its g, cl, ax, dx and dy, in font units
    private static List<int[]> readGlyphs(String line) throws Exception {
        List<int[]> glyphs = new ArrayList<>();
        int at = line.indexOf('{');
        while (at >= 0) {
            int end = line.indexOf('}', at);
            if (end < 0) {
                throw new Exception("hb-shape's output: " + line);
            }
            Map<String, Integer> values = new HashMap<>();
            for (String pair : line.substring(at + 1, end).split(",")) {
                int colon = pair.indexOf(':');
                String key = pair.substring(0, colon).trim().replace("\"", "");
                values.put(key, Integer.parseInt(pair.substring(colon + 1).trim()));
            }
            Integer g = values.get("g");
            Integer cl = values.get("cl");
            Integer ax = values.get("ax");
            if (g == null || cl == null || ax == null) {
                throw new Exception("hb-shape's output: " + line);
            }
            glyphs.add(new int[] {g, cl, ax, values.getOrDefault("dx", 0), values.getOrDefault("dy", 0)});
            at = line.indexOf('{', end);
        }
        return glyphs;
    }

    /**
     * Shapes the text with hb-shape, each run of one font in that font, and
     * breaks it into lines no wider than the width.
     *
     * @param request the text, its language, the font files, the font size
     *     and the width of the lines.
     * @return the lines, in order.
     * @throws Exception if hb-shape cannot be run or a font cannot be read.
     */
    @Override
    public List<ShapedLine> shape(ShapingRequest request) throws Exception {
        String text = request.getText();
        if (text.indexOf('\n') >= 0 || text.indexOf('\r') >= 0) {
            throw new Exception("HBShape shapes a paragraph: a text without line feeds");
        }
        byte[][] files = request.getFontFiles();
        if (files.length == 0) {
            throw new Exception("HBShape needs a font file");
        }
        OTF[] fonts = new OTF[files.length];
        for (int i = 0; i < files.length; i++) {
            fonts[i] = new OTF(new ByteArrayInputStream(files[i]));
        }
        int[] cps = text.codePoints().toArray();
        // The index in the text, in chars, of each code point, and of the end
        int[] offsets = new int[cps.length + 1];
        for (int i = 0, at = 0; i < cps.length; i++) {
            offsets[i] = at;
            at += Character.charCount(cps[i]);
            offsets[i + 1] = at;
        }

        // Each character in the first font that has it, else in the first; a
        // mark in the font of the character it is on
        int[] fontOf = new int[cps.length];
        for (int i = 0; i < cps.length; i++) {
            int c = cps[i];
            int type = Character.getType(c);
            if (i > 0 && (type == Character.NON_SPACING_MARK || type == Character.COMBINING_SPACING_MARK
                    || type == Character.ENCLOSING_MARK)) {
                fontOf[i] = fontOf[i - 1];
                continue;
            }
            for (int k = 0; k < fonts.length; k++) {
                OTF otf = fonts[k];
                if (Character.isISOControl(c)
                        || (c >= otf.firstChar && c <= otf.lastChar && otf.unicodeToGID.get(c) != 0)) {
                    fontOf[i] = k;
                    break;
                }
            }
        }
        // The runs of one font: start and end
        List<int[]> runs = new ArrayList<>();
        for (int start = 0; start < cps.length;) {
            int end = start + 1;
            while (end < cps.length && fontOf[end] == fontOf[start]) {
                end++;
            }
            runs.add(new int[] {start, end});
            start = end;
        }

        // Each font's runs shaped in one run of hb-shape, a run a line
        File dir = Files.createTempDirectory("pdfjet-hbshape-").toFile();
        Map<Integer, List<int[]>> shapedRuns = new HashMap<>();   // By the start of the run
        try {
            for (int k = 0; k < fonts.length; k++) {
                List<int[]> list = new ArrayList<>();
                StringBuilder input = new StringBuilder();
                for (int[] run : runs) {
                    if (fontOf[run[0]] == k) {
                        list.add(run);
                        input.append(new String(cps, run[0], run[1] - run[0])).append('\n');
                    }
                }
                if (list.isEmpty()) {
                    continue;
                }
                File font = new File(dir, "font" + k);
                File in = new File(dir, "in" + k + ".txt");
                File out = new File(dir, "out" + k + ".txt");
                Files.write(font.toPath(), files[k]);
                Files.write(in.toPath(), input.toString().getBytes(StandardCharsets.UTF_8));
                List<String> command = new ArrayList<>();
                command.add(program);
                command.add("--font-file=" + font.getPath());
                command.add("--text-file=" + in.getPath());
                command.add("--no-glyph-names");
                command.add("--output-format=json");
                String language = request.getLanguage();
                if (language != null && !language.isEmpty()) {
                    command.add("--language=" + language);
                }
                run(command.toArray(new String[0]), out);
                List<String> lines = new ArrayList<>();
                for (String line : new String(Files.readAllBytes(out.toPath()), StandardCharsets.UTF_8).split("\n")) {
                    line = line.replace("\r", "");
                    if (!line.isEmpty()) {
                        lines.add(line);
                    }
                }
                if (lines.size() != list.size()) {
                    throw new Exception("hb-shape gave " + lines.size() + " lines for " + list.size() + " runs of text");
                }
                for (int i = 0; i < lines.size(); i++) {
                    shapedRuns.put(list.get(i)[0], readGlyphs(lines.get(i)));
                }
            }
        } finally {
            File[] left = dir.listFiles();
            if (left != null) {
                for (File f : left) {
                    f.delete();
                }
            }
            dir.delete();
        }

        // The glyphs of the runs in order, in points
        List<Glyph> glyphs = new ArrayList<>();
        float pen = 0f;
        for (int[] run : runs) {
            int k = fontOf[run[0]];
            float scale = request.getFontSize() / fonts[k].unitsPerEm;
            for (int[] g : shapedRuns.get(run[0])) {
                if (run[0] + g[1] >= run[1]) {
                    throw new Exception("hb-shape gave a glyph of a character past its run");
                }
                Glyph glyph = new Glyph();
                glyph.id = g[0];
                glyph.font = k;
                glyph.cluster = run[0] + g[1];
                glyph.x = pen + g[3] * scale;
                glyph.y = -g[4] * scale;
                glyph.advance = g[2] * scale;
                glyphs.add(glyph);
                pen += g[2] * scale;
            }
        }
        return lines(glyphs, cps, offsets, text.length(), request.getWidth());
    }

    // The lines no wider than the width: broken after a space, or between two
    // words of Thai, Lao, Khmer or Burmese, the last place that fits; a word
    // wider than a line alone on its line
    private static List<ShapedLine> lines(List<Glyph> glyphs, int[] cps, int[] offsets, int end, float width) {
        Set<Integer> wordEnd = new HashSet<>(DictionaryBreaks.wordEnds(cps));
        List<ShapedLine> lines = new ArrayList<>();
        for (int first = 0; first < glyphs.size();) {
            float lineStart = glyphs.get(first).x;
            int last = glyphs.size();
            int lastBreak = -1;
            for (int i = first; i < glyphs.size(); i++) {
                Glyph g = glyphs.get(i);
                if (i > first && glyphs.get(i - 1).cluster != g.cluster
                        && (cps[g.cluster - 1] == ' ' || (wordEnd.contains(g.cluster) && cps[g.cluster] != ' '))) {
                    lastBreak = i;
                }
                if (cps[g.cluster] != ' ' && g.x + g.advance - lineStart > width && lastBreak > first) {
                    last = lastBreak;
                    break;
                }
            }
            int lineEnd = (last < glyphs.size()) ? offsets[glyphs.get(last).cluster] : end;
            // The width of the line without the spaces at its end
            int visible = last;
            while (visible > first && cps[glyphs.get(visible - 1).cluster] == ' ') {
                visible--;
            }
            float lineWidth = 0f;
            if (visible > first) {
                Glyph g = glyphs.get(visible - 1);
                lineWidth = g.x + g.advance - lineStart;
            }
            int n = last - first;
            int[] fontIndexes = new int[n];
            int[] ids = new int[n];
            float[] positions = new float[2 * n];
            for (int i = 0; i < n; i++) {
                Glyph g = glyphs.get(first + i);
                fontIndexes[i] = g.font;
                ids[i] = g.id;
                positions[2 * i] = g.x - lineStart;
                positions[2 * i + 1] = g.y;
            }
            lines.add(new ShapedLine(offsets[glyphs.get(first).cluster], lineEnd, fontIndexes, ids, positions, lineWidth));
            first = last;
        }
        return lines;
    }
}   // End of HBShape.java
