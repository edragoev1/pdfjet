/*
 * Booklet.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import java.io.BufferedOutputStream;
import java.io.FileOutputStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Writes the PDFjet booklet with PDFjet: an introduction and a guide to the
 * API, class by class, each feature with the smallest code that shows it, as
 * a PDF/UA document with bookmarks and a linked table of contents. It writes
 * one booklet for each port, with the code of that port, and one with the
 * code of the four ports.
 *
 * The text is in booklet/content.txt, and the code is read from the snippet
 * programs in booklet/snippets, one for each port, which
 * booklet/check-snippets.sh builds, runs and checks, so the booklet shows
 * code that works. Run it from the root of the repository, where the fonts
 * are:
 *
 *     booklet/build.sh
 */
public class Booklet {
    // The ports, in the order the all-languages booklet shows them.
    static final String[] LANGUAGES = {"java", "csharp", "go", "swift"};
    static final Map<String, String> NAMES = new HashMap<String, String>();
    static {
        NAMES.put("java", "Java");
        NAMES.put("csharp", "C#");
        NAMES.put("go", "Go");
        NAMES.put("swift", "Swift");
    }

    // The page, in points: Letter, with its margins.
    static final float LEFT = 60f;
    static final float WIDTH = 492f;
    static final float TOP = 64f;
    static final float BOTTOM = 736f;

    static final int ACCENT = 0x1F4E79;         // Headings: a deep blue
    static final int GRAY = 0x5A6270;
    static final int CODE_BACKGROUND = 0xF3F5F8;
    static final int KEYWORD = 0x0033B3;
    static final int COMMENT = 0x6A737D;

    static final float BODY_SIZE = 10.5f;
    static final float CODE_SIZE = 7.6f;

    // One item of the content: a heading, a paragraph, a bullet, code, text
    // for one language, or a snippet.
    static class Item {
        String kind;        // "h1", "h2", "p", "li", "code", "lang", "snippet"
        String language;    // Of "code" and "lang" items
        String text;        // The heading, paragraph or bullet, or the title of a snippet
        String name;        // Of a snippet: its name in the snippet programs
        List<String> lines = new ArrayList<String>();   // Of "code" and "lang" items
        List<Item> items = new ArrayList<Item>();       // Of "lang" items and snippets
    }

    // An entry of the table of contents.
    static class Entry {
        int level;
        String text;
        String destination;
        int pageNumber;
    }

    final String edition;       // "java", "csharp", "go", "swift" or "all"
    final List<Item> content;
    final Map<String, Map<String, List<String>>> snippets =
            new HashMap<String, Map<String, List<String>>>();   // By language and name
    PDF pdf;
    Font body;
    Font bold;
    Font italic;
    Font mono;
    Font monoBold;
    Bookmark outline;
    Bookmark chapter;

    boolean dry;                // True for the first pass, which counts the pages
    Page page;
    int pageNumber;
    float y;
    List<Entry> entries = new ArrayList<Entry>();
    int entryIndex;

    Booklet(String edition, List<Item> content) throws Exception {
        this.edition = edition;
        this.content = content;
        for (String language : LANGUAGES) {
            snippets.put(language, snippets(language));
        }
    }

    public static void main(String[] args) throws Exception {
        List<Item> content = parse(Files.readAllLines(Paths.get("booklet/content.txt"), StandardCharsets.UTF_8));
        checkSnippets(content);
        List<String> editions = (args.length > 0) ? Arrays.asList(args) :
                Arrays.asList("java", "csharp", "go", "swift", "all");
        for (String edition : editions) {
            long time0 = System.currentTimeMillis();
            String fileName = new Booklet(edition, content).write();
            long time1 = System.currentTimeMillis();
            System.out.printf("%s => %4d ms%n", fileName, time1 - time0);
        }
    }

    String editionName() {
        return edition.equals("all") ? "Java, C#, Go and Swift" : NAMES.get(edition);
    }

    String write() throws Exception {
        String fileName = "booklet/PDFjet-Booklet-" +
                (edition.equals("all") ? "All" : edition.equals("csharp") ? "CSharp" : NAMES.get(edition)) + ".pdf";
        pdf = new PDF(new BufferedOutputStream(new FileOutputStream(fileName)));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("PDFjet, " + editionName() + " edition");
        pdf.setSubject("An introduction to PDFjet and a guide to its API, with code in " + editionName());
        pdf.setAuthor("PDFjet Software");
        pdf.setLanguage("en-US");

        body = new Font(pdf, IBMPlexSans.Regular);
        bold = new Font(pdf, IBMPlexSans.SemiBold);
        italic = new Font(pdf, IBMPlexSans.Italic);
        mono = new Font(pdf, JetBrainsMono.Regular);
        monoBold = new Font(pdf, JetBrainsMono.Bold);

        // The first pass draws on pages that are never added, to find the
        // page of every heading for the table of contents.
        collectEntries();
        int contentsPages = contentsPages();
        dry = true;
        pageNumber = 1 + contentsPages;
        entryIndex = 0;
        drawContent();

        dry = false;
        pageNumber = 0;
        entryIndex = 0;
        outline = new Bookmark(pdf);
        drawTitlePage();
        drawContents(contentsPages);
        drawContent();
        pdf.complete();
        return fileName;
    }

    // --- The content --------------------------------------------------------

    static List<Item> parse(List<String> lines) {
        List<Item> items = new ArrayList<Item>();
        List<Item> target = items;
        StringBuilder paragraph = new StringBuilder();
        for (int i = 0; i < lines.size(); i++) {
            String line = lines.get(i);
            if (line.startsWith("%")) {
                continue;
            }
            if (line.trim().isEmpty() || line.startsWith("#") || line.startsWith("- ") || line.startsWith("@")) {
                addParagraph(target, paragraph);
            }
            if (line.startsWith("# ") || line.startsWith("## ")) {
                Item item = new Item();
                item.kind = line.startsWith("# ") ? "h1" : "h2";
                item.text = line.substring(line.indexOf(' ') + 1).trim();
                items.add(item);
                target = items;
            } else if (line.startsWith("@snippet ")) {
                String[] parts = line.substring(9).trim().split(" ", 2);
                Item snippet = new Item();
                snippet.kind = "snippet";
                snippet.name = parts[0];
                snippet.text = parts[1].trim();
                items.add(snippet);
                target = snippet.items;
            } else if (line.startsWith("@code ") || line.startsWith("@lang ")) {
                Item item = new Item();
                item.kind = line.startsWith("@code ") ? "code" : "lang";
                item.language = line.substring(6).trim();
                for (i++; !lines.get(i).equals("@end"); i++) {
                    item.lines.add(lines.get(i));
                }
                if (item.kind.equals("lang")) {
                    item.items = parseLang(item.lines);
                }
                target.add(item);
            } else if (line.startsWith("- ")) {
                Item item = new Item();
                item.kind = "li";
                item.text = line.substring(2).trim();
                target.add(item);
            } else if (!line.trim().isEmpty()) {
                if (paragraph.length() > 0) {
                    paragraph.append(' ');
                }
                paragraph.append(line.trim());
            }
        }
        addParagraph(target, paragraph);
        return items;
    }

    // The text of a "lang" item: paragraphs, and code where the lines are
    // indented 4 spaces.
    static List<Item> parseLang(List<String> lines) {
        List<Item> items = new ArrayList<Item>();
        StringBuilder paragraph = new StringBuilder();
        Item code = null;
        for (String line : lines) {
            if (line.startsWith("    ")) {
                addParagraph(items, paragraph);
                if (code == null) {
                    code = new Item();
                    code.kind = "code";
                    items.add(code);
                }
                code.lines.add(line.substring(4));
            } else if (line.trim().isEmpty()) {
                addParagraph(items, paragraph);
                code = null;
            } else {
                code = null;
                if (paragraph.length() > 0) {
                    paragraph.append(' ');
                }
                paragraph.append(line.trim());
            }
        }
        addParagraph(items, paragraph);
        return items;
    }

    static void addParagraph(List<Item> items, StringBuilder paragraph) {
        if (paragraph.length() > 0) {
            Item item = new Item();
            item.kind = "p";
            item.text = paragraph.toString();
            items.add(item);
            paragraph.setLength(0);
        }
    }

    // The snippet programs, by language.
    static String snippetPath(String language) {
        if (language.equals("java")) {
            return "booklet/snippets/java/Snippets.java";
        } else if (language.equals("csharp")) {
            return "booklet/snippets/csharp/Snippets.cs";
        } else if (language.equals("go")) {
            return "booklet/snippets/go/main.go";
        }
        return "booklet/snippets/swift/main.swift";
    }

    // The snippets of a snippet program: the body of the function after each
    // "@snippet name" comment, up to the brace that closes it, one indent less.
    static Map<String, List<String>> snippets(String language) throws Exception {
        List<String> lines = Files.readAllLines(Paths.get(snippetPath(language)), StandardCharsets.UTF_8);
        Map<String, List<String>> snippets = new HashMap<String, List<String>>();
        for (int i = 0; i < lines.size(); i++) {
            String line = lines.get(i).trim();
            if (!line.startsWith("// @snippet ")) {
                continue;
            }
            String name = line.substring(12).trim();
            String signature = lines.get(i + 1);
            String indent = signature.substring(0, signature.length() - signature.replaceAll("^\\s+", "").length());
            if (!signature.trim().endsWith("{")) {
                throw new Exception(snippetPath(language) + ": the snippet " + name + " has no body");
            }
            List<String> body = new ArrayList<String>();
            int j = i + 2;
            for (; j < lines.size() && !lines.get(j).equals(indent + "}"); j++) {
                body.add(lines.get(j));
            }
            if (j == lines.size()) {
                throw new Exception(snippetPath(language) + ": the snippet " + name + " does not end");
            }
            snippets.put(name, dedent(body));
            i = j;
        }
        return snippets;
    }

    // The lines without the indent of the least indented of them.
    static List<String> dedent(List<String> lines) {
        int indent = Integer.MAX_VALUE;
        for (String line : lines) {
            if (!line.trim().isEmpty()) {
                indent = Math.min(indent, line.length() - line.replaceAll("^[ \\t]+", "").length());
            }
        }
        List<String> result = new ArrayList<String>();
        for (String line : lines) {
            result.add(line.trim().isEmpty() ? "" : line.substring(indent));
        }
        return result;
    }

    // Every port has every snippet the content shows, and the content shows
    // every snippet of every port, once.
    static void checkSnippets(List<Item> content) throws Exception {
        List<String> shown = new ArrayList<String>();
        for (Item item : content) {
            if (item.kind.equals("snippet")) {
                if (shown.contains(item.name)) {
                    throw new Exception("booklet/content.txt shows the snippet " + item.name + " twice");
                }
                shown.add(item.name);
            }
        }
        for (String language : LANGUAGES) {
            Map<String, List<String>> snippets = snippets(language);
            for (String name : shown) {
                if (!snippets.containsKey(name)) {
                    throw new Exception(snippetPath(language) + " has no snippet " + name);
                }
            }
            for (String name : snippets.keySet()) {
                if (!shown.contains(name)) {
                    throw new Exception("booklet/content.txt does not show the snippet " + name
                            + " of " + snippetPath(language));
                }
            }
        }
    }

    // The languages whose code this edition shows.
    List<String> languages() {
        return edition.equals("all") ? Arrays.asList(LANGUAGES) : Arrays.asList(edition);
    }

    // --- The table of contents ---------------------------------------------

    void collectEntries() {
        for (Item item : content) {
            if (item.kind.equals("h1") || item.kind.equals("h2") || item.kind.equals("snippet")) {
                Entry entry = new Entry();
                entry.level = item.kind.equals("h1") ? 0 : 1;
                entry.text = item.text;
                entry.destination = "entry" + entries.size();
                entries.add(entry);
            }
        }
    }

    static final float CONTENTS_TOP = 120f;
    static final float CONTENTS_LEADING = 15.5f;
    static final float CHAPTER_SPACE = 6f;

    int contentsPages() {
        int pages = 1;
        float y = CONTENTS_TOP;
        for (Entry entry : entries) {
            float h = CONTENTS_LEADING + ((entry.level == 0) ? CHAPTER_SPACE : 0f);
            if (y + h > BOTTOM) {
                pages++;
                y = TOP;
            }
            y += h;
        }
        return pages;
    }

    void drawContents(int contentsPages) throws Exception {
        newPage();
        TextLine heading = new TextLine(bold, "Contents");
        heading.setFontSize(24f).setTextColor(ACCENT).setStructureType(StructElem.H1);
        heading.setLocation(LEFT, TOP + 24f).drawOn(page);
        y = CONTENTS_TOP;
        for (Entry entry : entries) {
            float h = CONTENTS_LEADING + ((entry.level == 0) ? CHAPTER_SPACE : 0f);
            if (y + h > BOTTOM) {
                newPage();
                y = TOP;
            }
            y += h;
            Font font = (entry.level == 0) ? bold : body;
            float size = (entry.level == 0) ? 11f : 10f;
            float x = LEFT + ((entry.level == 0) ? 0f : 16f);
            TextLine text = new TextLine(font, entry.text);
            text.setFontSize(size).setLocation(x, y).setGoToAction(entry.destination);
            text.drawOn(page);
            TextLine number = new TextLine(font, String.valueOf(entry.pageNumber));
            number.setFontSize(size);
            number.setLocation(LEFT + WIDTH - number.getWidth(), y).setGoToAction(entry.destination);
            number.drawOn(page);
            // A dotted leader between the text and the page number, as decoration.
            float x1 = x + text.getWidth() + 6f;
            float x2 = LEFT + WIDTH - number.getWidth() - 6f;
            if (x2 > x1) {
                page.addArtifactBMC();
                page.setPenColor(0xB0B6BF);
                page.setPenWidth(0.6f);
                page.setStrokeDashPattern("[0.6 3] 0");
                page.drawLine(x1, y - 1f, x2, y - 1f);
                page.setStrokeDashPattern("[] 0");
                page.addEMC();
            }
        }
    }

    // --- Pages ---------------------------------------------------------------

    void newPage() throws Exception {
        page = dry ? new Page(pdf, Letter.PORTRAIT, Page.DETACHED) : new Page(pdf, Letter.PORTRAIT);
        pageNumber++;
        y = TOP;
        if (pageNumber > 1) {
            drawFooter();
        }
    }

    // The edition and the page number at the foot of the page, as decoration
    // that screen readers skip.
    void drawFooter() throws Exception {
        page.addArtifactBMC();
        page.setPenColor(0xD5D9DF);
        page.setPenWidth(0.5f);
        page.drawLine(LEFT, 752f, LEFT + WIDTH, 752f);
        page.setBrushColor(GRAY);
        page.drawString(body, 8.5f, "PDFjet, " + editionName() + " edition", LEFT, 766f);
        String number = String.valueOf(pageNumber);
        page.drawString(body, 8.5f, number, LEFT + WIDTH - body.stringWidth(8.5f, number), 766f);
        page.addEMC();
    }

    void ensure(float height) throws Exception {
        if (y + height > BOTTOM) {
            newPage();
        }
    }

    void drawTitlePage() throws Exception {
        newPage();
        page.addArtifactBMC();
        page.setBrushColor(ACCENT);
        page.fillRect(0f, 0f, 612f, 300f);
        page.addEMC();

        // The title and the tagline in white on the band.
        centered(bold, 46f, "PDFjet", 0xFFFFFF, 150f, StructElem.H1);
        centered(body, 18f, "Fast, dependency-free PDF generation", 0xD6E4F0, 196f, StructElem.P);
        centered(body, 18f, "for Java, C#, Go and Swift", 0xD6E4F0, 222f, StructElem.P);

        // The logo of pdfjet.com, drawn from its SVG file with PDFjet itself,
        // so it stays sharp at any size the booklet is printed at.
        SVGImage logo = new SVGImage("images/readme/pdfjet-logo.svg");
        logo.setAltDescription("The PDFjet logo");
        logo.scaleBy(320f / logo.getWidth());
        logo.setLocation(306f - logo.getWidth() / 2f, 350f);
        float[] xy = logo.drawOn(page);

        centered(bold, 20f, editionName() + " edition", ACCENT, xy[1] + 80f, StructElem.P);
        centered(body, 12f, "An introduction and a guide to the API, with code for every feature", GRAY,
                xy[1] + 106f, StructElem.P);
        centered(body, 10f, "Version 9 \u00B7 2026 \u00B7 pdfjet.com", GRAY, 730f, StructElem.P);
    }

    float centered(Font font, float size, String text, int color, float yText, StructElem structure)
            throws Exception {
        TextLine line = new TextLine(font, text);
        line.setFontSize(size).setTextColor(color).setStructureType(structure);
        line.setLocation(306f - line.getWidth() / 2f, yText).drawOn(page);
        return yText;
    }

    // --- The body ------------------------------------------------------------

    void drawContent() throws Exception {
        for (Item item : content) {
            if (item.kind.equals("h1")) {
                newPage();
                Entry entry = entries.get(entryIndex++);
                y = TOP + 28f;
                heading(entry, item.text, 24f, StructElem.H1);
                y += 22f;
            } else if (item.kind.equals("h2")) {
                ensure(90f);
                Entry entry = entries.get(entryIndex++);
                y += (y > TOP) ? 16f : 0f;
                y += 16f;
                heading(entry, item.text, 15f, StructElem.H2);
                y += 12f;
            } else if (item.kind.equals("snippet")) {
                drawSnippet(item, entries.get(entryIndex++));
            } else {
                drawItem(item);
            }
        }
    }

    void heading(Entry entry, String text, float size, StructElem structure) throws Exception {
        TextLine line = new TextLine(bold, text);
        line.setFontSize(size).setTextColor(ACCENT).setStructureType(structure);
        line.setLocation(LEFT, y);
        if (dry) {
            entry.pageNumber = pageNumber;
        } else {
            page.addDestination(entry.destination, y - size);
            Title title = new Title(bold, text, LEFT, y);
            if (entry.level == 0) {
                chapter = outline.addBookmark(page, title);
            } else {
                chapter.addBookmark(page, title);
            }
        }
        line.drawOn(page);
    }

    void drawItem(Item item) throws Exception {
        if (item.kind.equals("p")) {
            paragraph(item.text, body, BODY_SIZE, LEFT, WIDTH);
        } else if (item.kind.equals("li")) {
            bullet(item.text);
        } else if (item.kind.equals("code")) {
            if (languages().contains(item.language)) {
                code(item.lines, item.language,
                        edition.equals("all") ? NAMES.get(item.language) : null);
            }
        } else if (item.kind.equals("lang")) {
            if (languages().contains(item.language)) {
                if (edition.equals("all")) {
                    ensure(40f);
                    TextLine label = new TextLine(bold, NAMES.get(item.language));
                    label.setFontSize(11f).setTextColor(ACCENT).setStructureType(StructElem.H3);
                    label.setLocation(LEFT, y + 11f).drawOn(page);
                    y += 20f;
                }
                for (Item part : item.items) {
                    if (part.kind.equals("code")) {
                        code(part.lines, item.language, null);
                    } else {
                        drawItem(part);
                    }
                }
            }
        }
    }

    void paragraph(String text, Font font, float size, float x, float width) throws Exception {
        TextBlock block = new TextBlock(font, text);
        block.setFontSize(size);
        block.setLineSpacing(1.25f);
        block.setTextColor(0x222222);
        block.setWidth(width);
        block.setLocation(x, y);
        float height = block.drawOn(null)[1] - y;
        if (y + height > BOTTOM) {
            newPage();
            block.setLocation(x, y);
        }
        y = block.drawOn(page)[1] + 8f;
    }

    void bullet(String text) throws Exception {
        TextBlock block = new TextBlock(body, text);
        block.setFontSize(BODY_SIZE).setLineSpacing(1.25f).setTextColor(0x222222);
        block.setWidth(WIDTH - 16f).setLocation(LEFT + 16f, y);
        float height = block.drawOn(null)[1] - y;
        if (y + height > BOTTOM) {
            newPage();
            block.setLocation(LEFT + 16f, y);
        }
        // The bullet is decoration: the item is the text next to it.
        page.addArtifactBMC();
        page.setBrushColor(ACCENT);
        page.fillCircle(LEFT + 5f, y + 7.5f, 2f);
        page.addEMC();
        y = block.drawOn(page)[1] + 4f;
    }

    void drawSnippet(Item snippet, Entry entry) throws Exception {
        // The heading, the description and the first lines of the code start
        // on the same page.
        ensure(160f);
        y += (y > TOP) ? 18f : 0f;
        y += 15f;
        heading(entry, snippet.text, 14f, StructElem.H2);
        y += 12f;
        for (Item item : snippet.items) {
            drawItem(item);
        }
        for (String language : languages()) {
            code(snippets.get(language).get(snippet.name), language,
                    edition.equals("all") ? NAMES.get(language) : null);
        }
    }

    // --- Code listings -------------------------------------------------------

    static final float CODE_PADDING = 7f;

    // Draws code in JetBrains Mono on a light background, with the keywords of
    // its language in color and its comments in gray, a line of the source at a
    // time. Lines too long for the page wrap, and a listing that goes on to the
    // next page repeats its caption there.
    void code(List<String> source, String language, String caption) throws Exception {
        float leading = mono.getBodyHeight(CODE_SIZE);
        int columns = (int) ((WIDTH - 2 * CODE_PADDING) / mono.stringWidth(CODE_SIZE, "0"));
        List<String> lines = new ArrayList<String>();
        List<Boolean> comments = new ArrayList<Boolean>();
        boolean inComment = false;
        for (String line : source) {
            String text = escape(line.replace("\t", "    "), language);
            String trimmed = text.trim();
            boolean comment = inComment || trimmed.startsWith("//") || trimmed.startsWith("/*");
            if (trimmed.startsWith("/*") && !trimmed.contains("*/")) {
                inComment = true;
            } else if (inComment && trimmed.contains("*/")) {
                inComment = false;
            }
            // A line too long for the page goes on under itself, indented.
            String indent = text.substring(0, text.length() - text.replaceAll("^ +", "").length()) + "    ";
            while (text.length() > columns) {
                int cut = text.lastIndexOf(' ', columns);
                if (cut <= indent.length()) {
                    cut = columns;
                }
                lines.add(text.substring(0, cut));
                comments.add(comment);
                text = indent + text.substring(cut).trim();
            }
            lines.add(text);
            comments.add(comment);
        }
        Map<String, Integer> keywords = keywords(language);

        // A listing starts on the next page unless 8 of its lines fit here.
        ensure(Math.min(8, lines.size()) * leading + 2 * CODE_PADDING + ((caption != null) ? 13f : 0f));
        boolean first = true;
        int i = 0;
        while (i < lines.size()) {
            if (caption != null) {
                TextLine label = new TextLine(monoBold, first ? caption : caption + " (continued)");
                label.setFontSize(7.5f).setTextColor(GRAY);
                label.setLocation(LEFT, y + 7.5f).drawOn(page);
                y += 13f;
            }
            int fit = (int) ((BOTTOM - y - 2 * CODE_PADDING) / leading);
            int count = Math.min(fit, lines.size() - i);
            // The background is decoration, as a rectangle is.
            new Rect(LEFT, y, WIDTH, count * leading + 2 * CODE_PADDING)
                    .setFillColor(CODE_BACKGROUND).drawOn(page);
            float yLine = y + CODE_PADDING + mono.getAscent(CODE_SIZE);
            for (int j = 0; j < count; j++, i++) {
                String text = lines.get(i);
                if (!text.trim().isEmpty()) {
                    TextLine line = new TextLine(mono, text);
                    line.setFontSize(CODE_SIZE);
                    if (comments.get(i)) {
                        line.setTextColor(COMMENT);
                    } else {
                        line.setTextColor(0x1F2328);
                        line.setHighlightColors(keywords);
                    }
                    line.setLocation(LEFT + CODE_PADDING, yLine).drawOn(page);
                }
                yLine += leading;
            }
            y += count * leading + 2 * CODE_PADDING + 12f;
            first = false;
            if (i < lines.size()) {
                newPage();
            }
        }
    }

    // Writes a character that JetBrains Mono has no glyph for, in Chinese,
    // Japanese, Korean, Hebrew, Arabic and emoji text, as a Unicode escape of
    // the language, so the listing draws no missing glyph and stays valid code.
    static String escape(String line, String language) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < line.length(); ) {
            int cp = line.codePointAt(i);
            i += Character.charCount(cp);
            if (cp < 0x0590 || (cp >= 0x2000 && cp <= 0x25FF)) {
                buf.appendCodePoint(cp);
            } else if (language.equals("swift")) {
                buf.append(String.format("\\u{%X}", cp));
            } else if (language.equals("go") && cp > 0xFFFF) {
                buf.append(String.format("\\U%08X", cp));
            } else {
                for (char c : Character.toChars(cp)) {
                    buf.append(String.format("\\u%04X", (int) c));
                }
            }
        }
        return buf.toString();
    }

    // The keywords of each language, which the listings draw in color. The
    // coloring goes by whole words and does not know strings, so keywords that
    // are common English words, such as "is", "in" and "as", are left out.
    static Map<String, Integer> keywords(String language) {
        String words;
        if (language.equals("java")) {
            words = "abstract boolean break byte case catch char class continue default double else "
                    + "enum extends final finally float for if implements import instanceof int interface "
                    + "long new null package private protected public return short static super switch "
                    + "this throw throws true false try void while";
        } else if (language.equals("csharp")) {
            words = "abstract base bool break byte case catch char class const continue default "
                    + "double else false finally float for foreach if int internal long namespace "
                    + "new null out override params private protected public readonly ref return sealed "
                    + "short static string struct switch this throw true try using var virtual void while";
        } else if (language.equals("go")) {
            words = "break case chan const continue default defer else fallthrough false for func go "
                    + "goto if import interface map nil package range return select struct switch true "
                    + "type var";
        } else {
            words = "break case catch class continue default defer else enum extension false "
                    + "fileprivate final for func guard if import init internal let nil override "
                    + "private protocol public repeat return self static struct switch throw throws true "
                    + "try var while";
        }
        Map<String, Integer> map = new HashMap<String, Integer>();
        for (String word : words.split(" ")) {
            map.put(word, KEYWORD);
        }
        return map;
    }
}
