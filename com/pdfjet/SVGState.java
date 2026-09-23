/*
 * SVGState.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
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
    float strokeWidth = 1f;             // In the user space of the element
    boolean evenOdd = false;            // fill-rule="evenodd"
    CapStyle lineCap = CapStyle.BUTT;
    JoinStyle lineJoin = JoinStyle.MITER;
    float fillOpacity = 1f;
    float strokeOpacity = 1f;
    float opacity = 1f;                 // The opacities of the element and its ancestors, multiplied
    float ownOpacity = 1f;              // The opacity of the element itself, not inherited
    double[] matrix = IDENTITY;         // Never changed in place: a new transform is a new array
    boolean hidden = false;             // In <defs> and the like, or under display="none": not drawn

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
        this.strokeWidth = parent.strokeWidth;
        this.evenOdd = parent.evenOdd;
        this.lineCap = parent.lineCap;
        this.lineJoin = parent.lineJoin;
        this.fillOpacity = parent.fillOpacity;
        this.strokeOpacity = parent.strokeOpacity;
        this.opacity = parent.opacity;
        this.ownOpacity = parent.ownOpacity;
        this.matrix = parent.matrix;
        this.hidden = parent.hidden;
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

    // Returns the rules of the text of a <style> element that are for a
    // class, in the order of the text.
    static List<Rule> parseStyleSheet(String text) {
        // Comments are left out first; they may hold braces.
        while (true) {
            int start = text.indexOf("/*");
            if (start < 0) {
                break;
            }
            int end = text.indexOf("*/", start + 2);
            if (end < 0) {
                text = text.substring(0, start);
                break;
            }
            text = text.substring(0, start) + " " + text.substring(end + 2);
        }
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
            Integer c = parseColor(colorMap, name, value);
            if (c != null) {
                this.color = c;
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
        }
    }

    // Sets the fill or the stroke: none, currentColor, a color, or a url of a
    // gradient or a pattern, which PDFjet does not draw, and the color after
    // it, if any, which SVG draws when it cannot. A value it cannot read
    // leaves the paint as it is inherited.
    private void setPaint(ColorMap colorMap, String name, String value) {
        if (value.startsWith("url(")) {
            int end = value.indexOf(')');
            if (end < 0) {
                return;
            }
            value = trim(value.substring(end + 1));
            if (value.isEmpty()) {
                return;
            }
        }
        int paint = 0;
        boolean current = false;
        if (value.equals("none")) {
            paint = Color.transparent;
        } else if (value.equals("currentColor")) {
            current = true;
        } else {
            Integer c = parseColor(colorMap, name, value);
            if (c == null) {
                return;
            }
            paint = c;
        }
        if (name.equals("fill")) {
            this.fill = paint;
            this.fillCurrent = current;
        } else {
            this.stroke = paint;
            this.strokeCurrent = current;
        }
    }

    // Reads a color: #rgb, #rrggbb, rgb() of three numbers or percentages, or
    // a name. It returns null for a color it does not know, and throws for one
    // that starts with # but is not hexadecimal. The digits are counted in
    // bytes of UTF-8, as the Go port counts them.
    static Integer parseColor(ColorMap colorMap, String property, String value) {
        if (value.startsWith("#")) {
            byte[] hex = value.substring(1).getBytes(StandardCharsets.UTF_8);
            if (hex.length == 3) {
                hex = new byte[] {hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]};
            }
            if (hex.length != 6) {
                return null;
            }
            try {
                for (byte b : hex) {
                    if (b < 0) {
                        throw new NumberFormatException();  // Not ASCII, so not a digit
                    }
                }
                return Integer.parseInt(new String(hex, StandardCharsets.US_ASCII), 16);
            } catch (NumberFormatException e) {
                throw new NumberFormatException(
                        "Invalid SVG " + property + ": invalid color \"" + value + "\"");
            }
        }
        String lower = toLower(value);
        if (lower.startsWith("rgb(") || lower.startsWith("rgba(")) {
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
        for (int i = 0; i < text.length(); i++) {
            if ("0123456789.eE+-xXpPabcdefABCDEF_".indexOf(text.charAt(i)) < 0) {
                throw new NumberFormatException("\"" + text + "\" is not a number");
            }
        }
        String digits = text;
        if (text.indexOf('_') >= 0) {
            if (!underscoresOK(text)) {
                throw new NumberFormatException("\"" + text + "\" is not a number");
            }
            digits = text.replace("_", "");
        }
        double number = single ? Float.parseFloat(digits) : Double.parseDouble(digits);
        if (Double.isInfinite(number)) {
            throw new NumberFormatException("\"" + text + "\" is out of range");
        }
        return number;
    }

    // Returns true when every underscore of the number is between two digits,
    // or between the 0x of a hexadecimal number and a digit, as Go writes
    // them: 1_000 is 1000.
    private static boolean underscoresOK(String text) {
        char saw = '^';     // The start, 0 for a digit, _ or ! for anything else
        int i = (text.startsWith("+") || text.startsWith("-")) ? 1 : 0;
        String lower = asciiLower(text);
        boolean hex = false;
        if (lower.length() >= i + 2 && lower.charAt(i) == '0' &&
                (lower.charAt(i + 1) == 'b' || lower.charAt(i + 1) == 'o' || lower.charAt(i + 1) == 'x')) {
            hex = lower.charAt(i + 1) == 'x';
            saw = '0';
            i += 2;
        }
        for (; i < lower.length(); i++) {
            char ch = lower.charAt(i);
            if (ch >= '0' && ch <= '9' || hex && ch >= 'a' && ch <= 'f') {
                saw = '0';
            } else if (ch == '_') {
                if (saw != '0') {
                    return false;
                }
                saw = '_';
            } else if (saw == '_') {
                return false;
            } else {
                saw = '!';
            }
        }
        return saw != '_';
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
        if (ch == '\t' || ch == '\n' || ch == '\u000B' || ch == '\f' || ch == '\r' || ch == '\u0085') {
            return true;
        }
        return ch != '᠎' && Character.isSpaceChar(ch);
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
