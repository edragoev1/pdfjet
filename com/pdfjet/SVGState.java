/*
 * SVGState.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Pattern;

/**
 * What an element of an SVG file draws with: the properties it inherits from
 * its parent, changed by its own attributes, the rules of the style sheet for
 * its classes and its style attribute, in that order, and the transform from
 * its user space to that of the svg element.
 */
class SVGState {
    int fill = Color.black;             // Color.transparent is none
    int stroke = Color.transparent;
    boolean fillCurrent = false;        // currentColor: the color property, as it is where it is used
    boolean strokeCurrent = false;
    int color = Color.black;            // The color property
    float fillAlpha = 1f;               // The alpha of the fill color, as #f008 or rgba() has one
    float strokeAlpha = 1f;
    float colorAlpha = 1f;
    float strokeWidth = 1f;             // In the user space of the element
    boolean evenOdd = false;            // fill-rule="evenodd"
    boolean clipEvenOdd = false;        // clip-rule="evenodd"
    CapStyle lineCap = CapStyle.BUTT;
    JoinStyle lineJoin = JoinStyle.MITER;
    float miterLimit = 4f;
    float fillOpacity = 1f;
    float strokeOpacity = 1f;
    float opacity = 1f;                 // The opacities of the element and its ancestors, multiplied
    float ownOpacity = 1f;              // The opacity of the element itself, not inherited
    double[] matrix = IDENTITY;         // Never changed in place: a new transform is a new array
    double[] viewport = {0.0, 0.0};     // The width and the height of the viewport, in user units; replaced too
    boolean hidden = false;             // In <defs> and the like, or under display="none": not drawn
    boolean invisible = false;          // visibility: not painted, but its children may be
    float[] dashArray = null;           // stroke-dasharray, in the user space; null is a solid line
    float dashOffset = 0f;
    boolean strokeFirst = false;        // paint-order: the stroke drawn before the fill
    float fontSize = 16f;               // In the user space; medium, as a browser has it
    int fontWeight = 400;               // normal 400, bold 700
    boolean italic = false;             // A font-style of italic or oblique
    int fontFamily = BundledFonts.SANS; // The family of the bundled fonts its names stand for
    int textAnchor = 0;                 // 0 start, 1 middle, 2 end
    int baseline = 0;                   // dominant-baseline: 0 the alphabetic, 1 the middle, 2 the hanging
    String markerStart = null;          // The ids of the markers, inherited
    String markerMid = null;
    String markerEnd = null;
    String clipPath = null;             // The id of the clip path of the element itself, not inherited
    String overflow = null;             // The overflow of the element itself, not inherited
    List<SVGPath> clips = null;         // The clip paths of the element and of its ancestors
    List<SVGImage.Choice> choices = null;   // The children of the switches it is in; never changed in place
    Map<String, Integer> paints = null; // The colors of the gradients, by id, shared by every state

    /**
     * The state of the svg element before its attributes: an SVG fills black,
     * strokes nothing, and a stroke is one unit wide.
     */
    SVGState() {
    }

    /** A copy of the state, for a child element or a shape. */
    SVGState(SVGState parent) {
        this.fill = parent.fill;
        this.stroke = parent.stroke;
        this.fillCurrent = parent.fillCurrent;
        this.strokeCurrent = parent.strokeCurrent;
        this.color = parent.color;
        this.fillAlpha = parent.fillAlpha;
        this.strokeAlpha = parent.strokeAlpha;
        this.colorAlpha = parent.colorAlpha;
        this.strokeWidth = parent.strokeWidth;
        this.evenOdd = parent.evenOdd;
        this.clipEvenOdd = parent.clipEvenOdd;
        this.lineCap = parent.lineCap;
        this.lineJoin = parent.lineJoin;
        this.miterLimit = parent.miterLimit;
        this.fillOpacity = parent.fillOpacity;
        this.strokeOpacity = parent.strokeOpacity;
        this.opacity = parent.opacity;
        this.ownOpacity = parent.ownOpacity;
        this.matrix = parent.matrix;
        this.viewport = parent.viewport;
        this.hidden = parent.hidden;
        this.invisible = parent.invisible;
        this.dashArray = parent.dashArray;
        this.dashOffset = parent.dashOffset;
        this.strokeFirst = parent.strokeFirst;
        this.fontSize = parent.fontSize;
        this.fontWeight = parent.fontWeight;
        this.italic = parent.italic;
        this.fontFamily = parent.fontFamily;
        this.textAnchor = parent.textAnchor;
        this.baseline = parent.baseline;
        this.markerStart = parent.markerStart;
        this.markerMid = parent.markerMid;
        this.markerEnd = parent.markerEnd;
        this.clips = parent.clips;
        this.choices = parent.choices;
        this.paints = parent.paints;
    }

    /**
     * A rule of the style sheet of an SVG file for one class: .name followed
     * by its declarations. Selectors of other kinds are left out.
     */
    static class Rule {
        final String className;
        final List<String[]> declarations;

        Rule(String className, List<String[]> declarations) {
            this.className = className;
            this.declarations = declarations;
        }
    }

    // Splits the text at every separator, keeping the empty parts, as the
    // Go port's strings.Split does.
    private static String[] split(String text, String separator) {
        return text.split(Pattern.quote(separator), -1);
    }

    /**
     * The rules of the style elements of an SVG file, in their order, and
     * where the rules of each class are among them, so that an element finds
     * the rules of its classes without going through all of them.
     */
    static class StyleSheet {
        private final List<Rule> rules = new ArrayList<Rule>();
        private final Map<String, List<Integer>> byClass = new HashMap<String, List<Integer>>();
        // The declarations its rules have applied, to elements, MAX_APPLIED at
        // most: N copies of a rule for a class and N elements of it were N x N
        // declarations, 20 s for 416 KB (review of 17 October 2026)
        static final int MAX_APPLIED = 1000000;
        private long applied = 0;

        // Adds the rules of a style element after those before it.
        void add(List<Rule> newRules) {
            for (Rule rule : newRules) {
                List<Integer> indexes = byClass.get(rule.className);
                if (indexes == null) {
                    indexes = new ArrayList<Integer>();
                    byClass.put(rule.className, indexes);
                }
                indexes.add(rules.size());
                rules.add(rule);
            }
        }

        // Returns the rules for any of the classes, in the order of the style
        // sheet, each rule once.
        List<Rule> forClasses(List<String> classes) {
            List<Integer> indexes = new ArrayList<Integer>();
            // A set, not the classes before each: one element of N classes
            // was N x N
            java.util.Set<String> seen = new java.util.HashSet<String>();
            for (int i = 0; i < classes.size(); i++) {
                if (!seen.add(classes.get(i))) {
                    continue;   // A class named twice has its rules once.
                }
                List<Integer> ofClass = byClass.get(classes.get(i));
                if (ofClass != null) {
                    indexes.addAll(ofClass);
                }
            }
            Collections.sort(indexes);
            List<Rule> found = new ArrayList<Rule>(indexes.size());
            for (int index : indexes) {
                Rule rule = rules.get(index);
                applied += rule.declarations.size();
                if (applied > MAX_APPLIED) {
                    throw new IllegalArgumentException(
                            "The style sheet of the SVG applies more than " + MAX_APPLIED + " declarations.");
                }
                found.add(rule);
            }
            return found;
        }
    }

    // Returns the rules of the text of a <style> element that are for a
    // class, in the order of the text.
    static List<Rule> parseStyleSheet(String text) {
        // Comments are left out first; they may hold braces. A comment that
        // is not closed runs to the end of the text.
        StringBuilder uncommented = new StringBuilder();
        int from = 0;
        while (true) {
            int start = text.indexOf("/*", from);
            if (start < 0) {
                uncommented.append(text, from, text.length());
                break;
            }
            uncommented.append(text, from, start);
            int end = text.indexOf("*/", start + 2);
            if (end < 0) {
                break;
            }
            uncommented.append(' ');
            from = end + 2;
        }
        text = uncommented.toString();
        List<Rule> rules = new ArrayList<Rule>();
        for (String block : split(text, "}")) {
            int open = block.indexOf('{');
            if (open < 0) {
                continue;
            }
            List<String[]> declarations = parseDeclarations(block.substring(open + 1));
            for (String selector : split(block.substring(0, open), ",")) {
                selector = trim(selector);
                if (selector.length() > 1 && selector.charAt(0) == '.' && isName(selector.substring(1))) {
                    rules.add(new Rule(selector.substring(1), declarations));
                }
            }
        }
        return rules;
    }

    // Returns true when the text is a name of a class alone, without the
    // combinators, the pseudo-classes and the like of other selectors.
    private static boolean isName(String text) {
        for (int i = 0; i < text.length(); i++) {
            char ch = text.charAt(i);
            if (!(ch == '-' || ch == '_' || ch >= '0' && ch <= '9' ||
                    ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch > 127)) {
                return false;
            }
        }
        return true;
    }

    // Returns the declarations of a style attribute or of a rule, name: value;
    // each, without !important.
    static List<String[]> parseDeclarations(String text) {
        List<String[]> declarations = new ArrayList<String[]>();
        for (String declaration : split(text, ";")) {
            int colon = declaration.indexOf(':');
            if (colon < 0) {
                continue;
            }
            String name = trim(declaration.substring(0, colon));
            String value = trim(declaration.substring(colon + 1));
            if (value.endsWith("!important")) {
                value = trim(value.substring(0, value.length() - "!important".length()));
            }
            if (!name.isEmpty() && !value.isEmpty()) {
                declarations.add(new String[] {name, value});
            }
        }
        return declarations;
    }

    // Sets a property of the state, of a presentation attribute, a rule or a
    // style attribute. Properties that PDFjet does not draw, and values it
    // cannot read, are left as they are inherited; it throws only for a color
    // that starts with # and is not hexadecimal, as it always has.
    void setProperty(ColorMap colorMap, String name, String value) {
        value = trim(value);
        if (value.equals("inherit")) {
            return;
        }
        if (name.equals("fill") || name.equals("stroke")) {
            setPaint(colorMap, name, value);
        } else if (name.equals("color")) {
            float[] alpha = {1f};
            Integer c = parseColor(colorMap, name, value, alpha);
            if (c != null) {
                this.color = c;
                this.colorAlpha = alpha[0];
            }
        } else if (name.equals("stroke-width")) {
            if (value.endsWith("%")) {
                return;
            }
            Float width = parseLength(value);
            if (width != null && width >= 0f) {
                this.strokeWidth = width;
            }
        } else if (name.equals("fill-rule")) {
            if (value.equals("evenodd")) {
                this.evenOdd = true;
            } else if (value.equals("nonzero")) {
                this.evenOdd = false;
            }
        } else if (name.equals("clip-rule")) {
            if (value.equals("evenodd")) {
                this.clipEvenOdd = true;
            } else if (value.equals("nonzero")) {
                this.clipEvenOdd = false;
            }
        } else if (name.equals("stroke-linecap")) {
            if (value.equals("butt")) {
                this.lineCap = CapStyle.BUTT;
            } else if (value.equals("round")) {
                this.lineCap = CapStyle.ROUND;
            } else if (value.equals("square")) {
                this.lineCap = CapStyle.PROJECTING_SQUARE;
            }
        } else if (name.equals("stroke-linejoin")) {
            if (value.equals("miter") || value.equals("miter-clip") || value.equals("arcs")) {
                this.lineJoin = JoinStyle.MITER;
            } else if (value.equals("round")) {
                this.lineJoin = JoinStyle.ROUND;
            } else if (value.equals("bevel")) {
                this.lineJoin = JoinStyle.BEVEL;
            }
        } else if (name.equals("stroke-miterlimit")) {
            // Below 1 is an error in SVG, and a PDF has to be able to hold it
            Float limit = parseLength(value);
            if (limit != null && limit >= 1f && FastFloat.isWritable(limit)) {
                this.miterLimit = limit;
            }
        } else if (name.equals("opacity") || name.equals("fill-opacity") || name.equals("stroke-opacity")) {
            Float alpha = parseOpacity(value);
            if (alpha == null) {
                return;
            }
            if (name.equals("opacity")) {
                this.ownOpacity = alpha;
            } else if (name.equals("fill-opacity")) {
                this.fillOpacity = alpha;
            } else {
                this.strokeOpacity = alpha;
            }
        } else if (name.equals("display")) {
            if (value.equals("none")) {
                this.hidden = true;
            }
        } else if (name.equals("visibility")) {
            if (value.equals("hidden") || value.equals("collapse")) {
                this.invisible = true;
            } else if (value.equals("visible")) {
                this.invisible = false;
            }
        } else if (name.equals("stroke-dasharray")) {
            this.dashArray = parseDashArray(value);
        } else if (name.equals("stroke-dashoffset")) {
            Float offset = value.endsWith("%") ? null : parseLength(value);
            if (offset != null) {
                this.dashOffset = offset;
            }
        } else if (name.equals("paint-order")) {
            // The stroke is drawn first when it comes before the fill, and
            // the markers, which PDFjet draws last, are left out.
            List<String> order = fields(value);
            int stroke = order.indexOf("stroke");
            int fill = order.indexOf("fill");
            this.strokeFirst = stroke >= 0 && (fill < 0 || stroke < fill);
        } else if (name.equals("font-size")) {
            Float size = parseFontSize(value, this.fontSize);
            if (size != null) {
                this.fontSize = size;
            }
        } else if (name.equals("font-weight")) {
            Integer weight = parseFontWeight(value, this.fontWeight);
            if (weight != null) {
                this.fontWeight = weight;
            }
        } else if (name.equals("font-style")) {
            if (value.equals("italic") || value.startsWith("oblique")) {
                this.italic = true;
            } else if (value.equals("normal")) {
                this.italic = false;
            }
        } else if (name.equals("font-family")) {
            this.fontFamily = BundledFonts.familyOf(value);
        } else if (name.equals("font")) {
            setFont(value);
        } else if (name.equals("text-anchor")) {
            if (value.equals("start")) {
                this.textAnchor = 0;
            } else if (value.equals("middle")) {
                this.textAnchor = 1;
            } else if (value.equals("end")) {
                this.textAnchor = 2;
            }
        } else if (name.equals("dominant-baseline") || name.equals("alignment-baseline")) {
            if (value.equals("middle") || value.equals("central")) {
                this.baseline = 1;
            } else if (value.equals("hanging") || value.equals("text-before-edge")) {
                this.baseline = 2;
            } else if (value.equals("auto") || value.equals("alphabetic") || value.equals("baseline")) {
                this.baseline = 0;
            }
        } else if (name.equals("marker-start")) {
            this.markerStart = urlId(value);
        } else if (name.equals("marker-mid")) {
            this.markerMid = urlId(value);
        } else if (name.equals("marker-end")) {
            this.markerEnd = urlId(value);
        } else if (name.equals("marker")) {
            this.markerStart = urlId(value);
            this.markerMid = this.markerStart;
            this.markerEnd = this.markerStart;
        } else if (name.equals("clip-path")) {
            this.clipPath = urlId(value);
        } else if (name.equals("overflow")) {
            this.overflow = toLower(value);
        }
    }

    // Returns the id of a reference, url(#id), with or without quotes, and
    // null for none or for a value of another kind.
    static String urlId(String value) {
        value = trim(value);
        if (!value.startsWith("url(")) {
            return null;
        }
        int end = value.indexOf(')');
        if (end < 0) {
            return null;
        }
        String id = trim(value.substring(4, end));
        if (id.length() >= 2 && (id.charAt(0) == '"' || id.charAt(0) == '\'')
                && id.charAt(id.length() - 1) == id.charAt(0)) {
            id = trim(id.substring(1, id.length() - 1));
        }
        if (id.length() < 2 || id.charAt(0) != '#') {
            return null;
        }
        return id.substring(1);
    }

    // Reads a stroke-dasharray: the lengths of the dashes and the gaps, as
    // many again when they are an odd number. It returns null, a solid line,
    // for none, for a length below zero or one it cannot read, and for
    // lengths that are all zero.
    static float[] parseDashArray(String value) {
        if (value.equals("none")) {
            return null;
        }
        List<Float> lengths = new ArrayList<Float>();
        for (String field : fields(value.replace(',', ' '))) {
            Float length = field.endsWith("%") ? null : parseLength(field);
            if (length == null || length < 0f || Float.isNaN(length) || Float.isInfinite(length)) {
                return null;
            }
            lengths.add(length);
        }
        float sum = 0f;
        for (float length : lengths) {
            sum += length;
        }
        if (lengths.isEmpty() || sum == 0f) {
            return null;
        }
        int count = lengths.size();
        float[] dashes = new float[(count % 2 == 0) ? count : 2*count];
        for (int i = 0; i < dashes.length; i++) {
            dashes[i] = lengths.get(i % count);
        }
        return dashes;
    }

    // The font sizes of the keywords, as a browser has them.
    private static final String[] SIZE_NAMES = {
            "xx-small", "x-small", "small", "medium", "large", "x-large", "xx-large"};
    private static final float[] SIZES = {9f, 10f, 13f, 16f, 18f, 24f, 32f};

    // Reads a font-size: a length, a number of em or a percentage of the
    // font size of the parent, or a keyword. It returns null for a size it
    // cannot read or one below zero.
    static Float parseFontSize(String value, float parentSize) {
        for (int i = 0; i < SIZE_NAMES.length; i++) {
            if (value.equals(SIZE_NAMES[i])) {
                return SIZES[i];
            }
        }
        Float size;
        if (value.endsWith("em") && !value.endsWith("rem")) {
            size = parseFactor(value.substring(0, value.length() - 2));
            size = (size == null) ? null : size*parentSize;
        } else if (value.endsWith("%")) {
            size = parseFactor(value.substring(0, value.length() - 1));
            size = (size == null) ? null : size*parentSize/100f;
        } else {
            size = parseLength(value);
        }
        if (size == null || size < 0f || Float.isNaN(size) || Float.isInfinite(size)) {
            return null;
        }
        return size;
    }

    // Reads a number, or returns null.
    private static Float parseFactor(String text) {
        try {
            return (float) parseNumber(trim(text), true);
        } catch (NumberFormatException e) {
            return null;
        }
    }

    // Reads a font-weight, from 1 to 1000: normal is 400 and bold 700, and
    // bolder and lighter are those of CSS, from the weight of the parent. It
    // returns null for a weight it cannot read.
    static Integer parseFontWeight(String value, int parentWeight) {
        if (value.equals("normal")) {
            return 400;
        } else if (value.equals("bold")) {
            return 700;
        } else if (value.equals("bolder")) {
            return (parentWeight < 350) ? 400 : (parentWeight < 550) ? 700 : 900;
        } else if (value.equals("lighter")) {
            return (parentWeight < 550) ? 100 : (parentWeight < 750) ? 400 : 700;
        }
        Float weight = parseFactor(value);
        if (weight == null || weight < 1f || weight > 1000f) {
            return null;
        }
        return Math.round(weight);
    }

    // Sets the size and the weight of the font shorthand, such as "bold 10px
    // sans-serif": the words before the size, then the size, which may have
    // a line height after a slash. A shorthand without a weight is normal.
    private void setFont(String value) {
        int weight = 400;
        boolean italic = false;
        List<String> fields = fields(value);
        for (int i = 0; i < fields.size(); i++) {
            String field = fields.get(i);
            int slash = field.indexOf('/');
            String size = (slash < 0) ? field : field.substring(0, slash);
            // A size has a unit or is a keyword; a number alone is a weight.
            if (!size.isEmpty() && UnicodeCategories.of(size.charAt(size.length() - 1)) != Character.DECIMAL_DIGIT_NUMBER) {
                Float fontSize = parseFontSize(size, this.fontSize);
                if (fontSize != null) {
                    this.fontSize = fontSize;
                    this.fontWeight = weight;
                    this.italic = italic;
                    // The family after the size, and a line height after a
                    // slash written apart from it
                    int from = i + 1;
                    if (slash < 0 && from < fields.size() && fields.get(from).startsWith("/")) {
                        from += fields.get(from).equals("/") ? 2 : 1;
                    }
                    StringBuilder family = new StringBuilder();
                    for (int j = from; j < fields.size(); j++) {
                        family.append(j > from ? " " : "").append(fields.get(j));
                    }
                    this.fontFamily = BundledFonts.familyOf(family.toString());
                    return;
                }
            }
            if (field.equals("italic") || field.equals("oblique")) {
                italic = true;
                continue;
            }
            Integer w = parseFontWeight(field, this.fontWeight);
            if (w != null) {
                weight = w;
            }
        }
    }

    // The alpha the fill is drawn with: its opacities and the alpha of its
    // color.
    float drawnFillAlpha() {
        return fillOpacity*opacity*(fillCurrent ? colorAlpha : fillAlpha);
    }

    // The alpha the stroke is drawn with.
    float drawnStrokeAlpha() {
        return strokeOpacity*opacity*(strokeCurrent ? colorAlpha : strokeAlpha);
    }

    // Sets the fill or the stroke: none, currentColor, a color, or a url of a
    // gradient, which PDFjet draws in the mean color of its stops. A url of
    // anything else, a pattern or an element that is not there, is the color
    // after it, which SVG draws when it cannot draw the url, or none, as a
    // browser draws it. A value it cannot read leaves the paint as it is
    // inherited. none and currentColor are keywords of CSS, in any case.
    private void setPaint(ColorMap colorMap, String name, String value) {
        Integer gradient = null;
        if (value.startsWith("url(")) {
            int end = value.indexOf(')');
            if (end < 0) {
                return;
            }
            String id = urlId(value.substring(0, end + 1));
            gradient = (id == null || paints == null) ? null : paints.get(id);
            value = trim(value.substring(end + 1));
            if (value.isEmpty()) {
                value = "none";
            }
        }
        int paint = 0;
        float[] alpha = {1f};
        boolean current = false;
        String keyword = toLower(value);
        if (gradient != null) {
            paint = gradient;
        } else if (keyword.equals("none")) {
            paint = Color.transparent;
        } else if (keyword.equals("currentcolor")) {
            current = true;
        } else {
            Integer c = parseColor(colorMap, name, value, alpha);
            if (c == null) {
                return;
            }
            paint = c;
        }
        if (name.equals("fill")) {
            this.fill = paint;
            this.fillAlpha = alpha[0];
            this.fillCurrent = current;
        } else {
            this.stroke = paint;
            this.strokeAlpha = alpha[0];
            this.strokeCurrent = current;
        }
    }

    // Reads a color, and its alpha into alpha[0]: #rgb, #rgba, #rrggbb,
    // #rrggbbaa, rgb() of three numbers or percentages, hsl(), with an alpha
    // after them in either, or a name. It returns null for a color it does
    // not know, and throws for one that starts with # but is not hexadecimal.
    // The digits are counted in bytes of UTF-8, as the Go port counts them.
    static Integer parseColor(ColorMap colorMap, String property, String value, float[] alpha) {
        if (value.startsWith("#")) {
            byte[] hex = Util.utf8(value.substring(1));
            if (hex.length == 3 || hex.length == 4) {
                byte[] doubled = new byte[2*hex.length];
                for (int i = 0; i < hex.length; i++) {
                    doubled[2*i] = hex[i];
                    doubled[2*i + 1] = hex[i];
                }
                hex = doubled;
            }
            if (hex.length != 6 && hex.length != 8) {
                return null;
            }
            try {
                for (byte b : hex) {
                    if (b < 0) {
                        throw new NumberFormatException();  // Not ASCII, so not a digit
                    }
                }
                int rgb = Integer.parseInt(new String(hex, 0, 6, StandardCharsets.US_ASCII), 16);
                if (hex.length == 8) {
                    int a = Integer.parseInt(new String(hex, 6, 2, StandardCharsets.US_ASCII), 16);
                    alpha[0] = ((float) Math.max(a, 0))/255f;
                }
                return rgb;
            } catch (NumberFormatException e) {
                throw new NumberFormatException(
                        "Invalid SVG " + property + ": invalid color \"" + value + "\"");
            }
        }
        String lower = toLower(value);
        boolean hsl = lower.startsWith("hsl(") || lower.startsWith("hsla(");
        if (hsl || lower.startsWith("rgb(") || lower.startsWith("rgba(")) {
            int open = lower.indexOf('(');
            int end = lower.indexOf(')');
            if (end < open) {
                return null;
            }
            List<String> parts = new ArrayList<String>();
            StringBuilder part = new StringBuilder();
            for (int i = open + 1; i <= end; i++) {
                char ch = (i < end) ? lower.charAt(i) : ',';
                if (ch == ',' || ch == '/' || isSVGSpace(ch)) {
                    if (part.length() > 0) {
                        parts.add(part.toString());
                        part.setLength(0);
                    }
                } else {
                    part.append(ch);
                }
            }
            if (parts.size() < 3) {
                return null;
            }
            if (parts.size() > 3) {
                Float opacity = parseOpacity(parts.get(3));
                if (opacity != null) {
                    alpha[0] = opacity;
                }
            }
            if (hsl) {
                return parseHSL(parts);
            }
            int rgb = 0;
            for (int i = 0; i < 3; i++) {
                String number = parts.get(i);
                double scale = 1.0;
                if (number.endsWith("%")) {
                    number = number.substring(0, number.length() - 1);
                    scale = 2.55;
                }
                double channel;
                try {
                    channel = parseNumber(number, false);
                } catch (NumberFormatException e) {
                    return null;
                }
                if (Double.isNaN(channel) || Double.isInfinite(channel)) {
                    return null;
                }
                channel = round(channel*scale);
                rgb = rgb << 8 | (int) Math.max(0.0, Math.min(255.0, channel));
            }
            return rgb;
        }
        return colorMap.map.get(lower);
    }

    // Returns the color of the hue, the saturation and the lightness of
    // hsl(), by the formula of CSS Color 4, or null for one it cannot read.
    private static Integer parseHSL(List<String> parts) {
        Double hue = parseHue(parts.get(0));
        Double saturation = parsePercentage(parts.get(1));
        Double lightness = parsePercentage(parts.get(2));
        if (hue == null || saturation == null || lightness == null) {
            return null;
        }
        double a = saturation*Math.min(lightness, 1.0 - lightness);
        int rgb = 0;
        for (double n : new double[] {0.0, 8.0, 4.0}) {
            double k = (n + hue/30.0) % 12.0;
            double channel = lightness - a*Math.max(-1.0, Math.min(Math.min(k - 3.0, 9.0 - k), 1.0));
            rgb = rgb << 8 | (int) Math.max(0.0, Math.min(255.0, round(channel*255.0)));
        }
        return rgb;
    }

    // Reads a hue, in degrees unless it has a unit, and returns it from 0 to
    // 360, or null for one it cannot read.
    private static Double parseHue(String text) {
        double scale = 1.0;
        if (text.endsWith("deg")) {
            text = text.substring(0, text.length() - 3);
        } else if (text.endsWith("grad")) {
            text = text.substring(0, text.length() - 4);
            scale = 0.9;
        } else if (text.endsWith("rad")) {
            text = text.substring(0, text.length() - 3);
            scale = 57.29577951308232;
        } else if (text.endsWith("turn")) {
            text = text.substring(0, text.length() - 4);
            scale = 360.0;
        }
        double number;
        try {
            number = parseNumber(text, false);
        } catch (NumberFormatException e) {
            return null;
        }
        if (Double.isNaN(number) || Double.isInfinite(number)) {
            return null;
        }
        double hue = (number*scale) % 360.0;
        if (Double.isNaN(hue)) {
            return null;    // Too large for its unit
        }
        return (hue < 0.0) ? hue + 360.0 : hue;
    }

    // Reads a saturation or a lightness, with or without its percent sign,
    // and returns it between 0 and 1, or null for one it cannot read.
    private static Double parsePercentage(String text) {
        double number;
        try {
            number = parseNumber(text.endsWith("%") ? text.substring(0, text.length() - 1) : text, false);
        } catch (NumberFormatException e) {
            return null;
        }
        if (Double.isNaN(number) || Double.isInfinite(number)) {
            return null;
        }
        return Math.max(0.0, Math.min(1.0, number/100.0));
    }

    // Rounds half away from zero.
    private static double round(double x) {
        return (x < 0.0) ? -Math.round(-x) : Math.round(x);
    }

    // Reads an opacity, a number or a percentage, and returns it between 0
    // and 1, or null for one it cannot read.
    static Float parseOpacity(String value) {
        double scale = 1.0;
        if (value.endsWith("%")) {
            value = value.substring(0, value.length() - 1);
            scale = 0.01;
        }
        double number;
        try {
            number = parseNumber(trim(value), false);
        } catch (NumberFormatException e) {
            return null;
        }
        if (Double.isNaN(number)) {
            return null;
        }
        return (float) Math.max(0.0, Math.min(1.0, number*scale));
    }

    // The units of a length of an SVG file, in points. A number without a
    // unit is in the user unit of the file, which PDFjet draws as a point,
    // and so is a number in px.
    static final String[] UNITS = {"px", "pt", "pc", "in", "mm", "cm"};
    static final float[] POINTS = {1f, 1f, 12f, 72f, 72f/25.4f, 72f/2.54f};

    // Reads a length in user units, which PDFjet draws as points, with or
    // without a unit; an empty value is 0, as an omitted attribute. It returns
    // null for a length it cannot read.
    static Float parseLength(String value) {
        value = trim(value);
        float scale = 1f;
        for (int i = 0; i < UNITS.length; i++) {
            if (value.endsWith(UNITS[i])) {
                value = trim(value.substring(0, value.length() - UNITS[i].length()));
                scale = POINTS[i];
                break;
            }
        }
        if (value.isEmpty()) {
            return 0f*scale;
        }
        float number;
        try {
            number = (float) parseNumber(value, true);
        } catch (NumberFormatException e) {
            return null;
        }
        if (Float.isInfinite(number) || Float.isNaN(number)) {
            return null;
        }
        return number*scale;
    }

    // Reads a coordinate or a size of a shape, and returns 0 for one it
    // cannot read, a percentage among them.
    static double parseCoordinate(String value) {
        Float length = parseLength(value);
        if (length == null || trim(value).endsWith("%")) {
            return 0.0;
        }
        return (double) length;
    }

    // Reads a list of numbers, as the points of a polygon or the arguments of
    // a transform are written: separated by white space or a comma, or by
    // nothing where a sign or a second point starts the next one. It returns
    // null for a list with a number it cannot read.
    static double[] parseNumbers(String text) {
        List<Double> numbers = new ArrayList<Double>();
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < text.length(); i++) {
            char ch = text.charAt(i);
            if (isSVGSpace(ch) || ch == ',') {
                if (!flush(buf, numbers)) {
                    return null;
                }
            } else if ((ch == '-' || ch == '+') && !afterExponent(buf) ||
                    ch == '.' && buf.indexOf(".") >= 0) {
                if (!flush(buf, numbers)) {
                    return null;
                }
                buf.append(ch);
            } else {
                buf.append(ch);
            }
        }
        if (!flush(buf, numbers)) {
            return null;
        }
        double[] list = new double[numbers.size()];
        for (int i = 0; i < list.length; i++) {
            list[i] = numbers.get(i);
        }
        return list;
    }

    // Adds the number in the buffer, if any, to the list, and empties the
    // buffer; it returns false for a number it cannot read.
    private static boolean flush(StringBuilder buf, List<Double> numbers) {
        if (buf.length() == 0) {
            return true;
        }
        String text = buf.toString();
        buf.setLength(0);
        double number;
        try {
            number = parseNumber(text, false);
        } catch (NumberFormatException e) {
            return false;
        }
        if (Double.isInfinite(number) || Double.isNaN(number)) {
            return false;
        }
        numbers.add(number);
        return true;
    }

    // Returns true when the number so far ends with the e of an exponent, so
    // that the sign of the exponent belongs to it.
    private static boolean afterExponent(StringBuilder buf) {
        if (buf.length() == 0) {
            return false;
        }
        char last = buf.charAt(buf.length() - 1);
        return last == 'e' || last == 'E';
    }

    // The white space of SVG 1.1 section 8.3.9, which is the white space of XML.
    private static boolean isSVGSpace(char ch) {
        return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
    }

    // Reads a number as the Go port does, with strconv.ParseFloat: no white
    // space around it and no suffix, as Java's parser takes, and inf,
    // infinity and nan in any case, as Java's does not. A number too large
    // for a float, or a double, throws; the infinity it names does not.
    static double parseNumber(String text, boolean single) {
        int start = (text.startsWith("+") || text.startsWith("-")) ? 1 : 0;
        String name = asciiLower(text.substring(start));
        if (name.equals("inf") || name.equals("infinity")) {
            return text.startsWith("-") ? Double.NEGATIVE_INFINITY : Double.POSITIVE_INFINITY;
        }
        if (start == 0 && name.equals("nan")) {
            return Double.NaN;
        }
        if (text.isEmpty() || "fFdD".indexOf(text.charAt(text.length() - 1)) >= 0) {
            throw new NumberFormatException("\"" + text + "\" is not a number");
        }
        // Only a number written in decimal: a hexadecimal number, as 0x1p3, or
        // one with underscores, as 1_000, is none of SVG's, and C# did not
        // read them (review of 17 October 2026)
        for (int i = 0; i < text.length(); i++) {
            if ("0123456789.eE+-".indexOf(text.charAt(i)) < 0) {
                throw new NumberFormatException("\"" + text + "\" is not a number");
            }
        }
        double number = single ? Float.parseFloat(text) : Double.parseDouble(text);
        if (Double.isInfinite(number)) {
            throw new NumberFormatException("\"" + text + "\" is out of range");
        }
        return number;
    }

    // Returns the text with the letters A to Z in lower case, and no other.
    private static String asciiLower(String text) {
        StringBuilder buf = new StringBuilder(text.length());
        for (int i = 0; i < text.length(); i++) {
            char ch = text.charAt(i);
            buf.append((ch >= 'A' && ch <= 'Z') ? (char) (ch + ('a' - 'A')) : ch);
        }
        return buf.toString();
    }

    // Returns the text in lower case, a code point at a time, whatever the
    // locale, as the Go port's strings.ToLower does.
    static String toLower(String text) {
        StringBuilder buf = new StringBuilder(text.length());
        int i = 0;
        while (i < text.length()) {
            int codePoint = text.codePointAt(i);
            buf.appendCodePoint(Character.toLowerCase(codePoint));
            i += Character.charCount(codePoint);
        }
        return buf.toString();
    }

    // Returns true for the white space of Unicode, as the Go port's
    // unicode.IsSpace does, which String.trim does not take.
    static boolean isSpace(char ch) {
        return Util.isSpace(ch);
    }

    // Returns the text without the white space at its ends.
    static String trim(String text) {
        int start = 0;
        int end = text.length();
        while (start < end && isSpace(text.charAt(start))) {
            start++;
        }
        while (end > start && isSpace(text.charAt(end - 1))) {
            end--;
        }
        return text.substring(start, end);
    }

    // Returns the words of the text, separated by white space.
    static List<String> fields(String text) {
        List<String> fields = new ArrayList<String>();
        int start = -1;
        for (int i = 0; i <= text.length(); i++) {
            if (i == text.length() || isSpace(text.charAt(i))) {
                if (start >= 0) {
                    fields.add(text.substring(start, i));
                    start = -1;
                }
            } else if (start < 0) {
                start = i;
            }
        }
        return fields;
    }

    // The matrices of transforms are [a b c d e f], which take x, y to
    // a*x + c*y + e, b*x + d*y + f, as SVG writes them.
    static final double[] IDENTITY = {1, 0, 0, 1, 0, 0};

    // Returns true for the identity, compared as numbers, so that -0 is 0.
    static boolean isIdentity(double[] m) {
        return m[0] == 1.0 && m[1] == 0.0 && m[2] == 0.0 && m[3] == 1.0 && m[4] == 0.0 && m[5] == 0.0;
    }

    // Returns the transform that is n, then m.
    static double[] multiply(double[] m, double[] n) {
        return new double[] {
            m[0]*n[0] + m[2]*n[1],
            m[1]*n[0] + m[3]*n[1],
            m[0]*n[2] + m[2]*n[3],
            m[1]*n[2] + m[3]*n[3],
            m[0]*n[4] + m[2]*n[5] + m[4],
            m[1]*n[4] + m[3]*n[5] + m[5],
        };
    }

    // Returns the matrix of a transform attribute, the list of its matrix,
    // translate, scale, rotate, skewX and skewY, applied from the last to the
    // first, or null for a list PDFjet cannot read, which is drawn as if the
    // element had none.
    static double[] parseTransform(String text) {
        double[] matrix = IDENTITY;
        String rest = trim(text);
        while (!rest.isEmpty()) {
            int open = rest.indexOf('(');
            int end = rest.indexOf(')');
            if (open < 0 || end < open) {
                return null;
            }
            String name = trim(rest.substring(0, open));
            double[] args = parseNumbers(rest.substring(open + 1, end));
            if (args == null) {
                return null;
            }
            int next = end + 1;
            while (next < rest.length() && " \t\r\n,".indexOf(rest.charAt(next)) >= 0) {
                next++;
            }
            rest = rest.substring(next);
            double[] m;
            if (name.equals("matrix") && args.length == 6) {
                m = new double[] {args[0], args[1], args[2], args[3], args[4], args[5]};
            } else if (name.equals("translate") && args.length == 1) {
                m = new double[] {1, 0, 0, 1, args[0], 0};
            } else if (name.equals("translate") && args.length == 2) {
                m = new double[] {1, 0, 0, 1, args[0], args[1]};
            } else if (name.equals("scale") && args.length == 1) {
                m = new double[] {args[0], 0, 0, args[0], 0, 0};
            } else if (name.equals("scale") && args.length == 2) {
                m = new double[] {args[0], 0, 0, args[1], 0, 0};
            } else if (name.equals("rotate") && (args.length == 1 || args.length == 3)) {
                double angle = args[0]*Math.PI/180.0;
                double cos = Math.cos(angle);
                double sin = Math.sin(angle);
                m = new double[] {cos, sin, -sin, cos, 0, 0};
                if (args.length == 3) {
                    m = multiply(new double[] {1, 0, 0, 1, args[1], args[2]},
                            multiply(m, new double[] {1, 0, 0, 1, -args[1], -args[2]}));
                }
            } else if ((name.equals("skewX") || name.equals("skewY")) && args.length == 1
                    && Math.abs(args[0]) % 180.0 == 90.0) {
                return null;    // A skew of 90 degrees flattens everything: not a transform
            } else if (name.equals("skewX") && args.length == 1) {
                m = new double[] {1, 0, Math.tan(args[0]*Math.PI/180.0), 1, 0, 0};
            } else if (name.equals("skewY") && args.length == 1) {
                m = new double[] {1, Math.tan(args[0]*Math.PI/180.0), 0, 1, 0, 0};
            } else {
                return null;
            }
            matrix = multiply(matrix, m);
        }
        return matrix;
    }
}   // End of SVGState.java
