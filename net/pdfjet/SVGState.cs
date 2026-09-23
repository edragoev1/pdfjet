/*
 * SVGState.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// What an element of an SVG file draws with: the properties it inherits from
/// its parent, changed by its own attributes, the rules of the style sheet for
/// its classes and its style attribute, in that order, and the transform from
/// its user space to that of the svg element.
/// </summary>
internal class SVGState {
    internal int fill = Color.black;            // Color.transparent is none
    internal int stroke = Color.transparent;
    internal bool fillCurrent = false;          // currentColor: the color property, as it is where it is used
    internal bool strokeCurrent = false;
    internal int color = Color.black;           // The color property
    internal float strokeWidth = 1f;            // In the user space of the element
    internal bool evenOdd = false;              // fill-rule="evenodd"
    internal CapStyle lineCap = CapStyle.BUTT;
    internal JoinStyle lineJoin = JoinStyle.MITER;
    internal float fillOpacity = 1f;
    internal float strokeOpacity = 1f;
    internal float opacity = 1f;                // The opacities of the element and its ancestors, multiplied
    internal float ownOpacity = 1f;             // The opacity of the element itself, not inherited
    internal double[] matrix = IDENTITY;        // Replaced, never changed in place
    internal bool hidden = false;               // In <defs> and the like, or under display="none": not drawn

    // The state of the svg element before its attributes: an SVG fills black,
    // strokes nothing, and a stroke is one unit wide.
    internal SVGState() {
    }

    // Returns a copy of this state, for a child element to change.
    internal SVGState Copy() {
        return (SVGState) MemberwiseClone();
    }

    // The colors of the names of CSS, lower case, and none.
    private static readonly Dictionary<String, int> COLORS = NewColorMap();

    private static Dictionary<String, int> NewColorMap() {
        Dictionary<String, int> colors = new Dictionary<String, int>();
        foreach (FieldInfo field in typeof(Color).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (field.IsLiteral && field.FieldType == typeof(int)) {
                colors[field.Name.ToLowerInvariant()] = (int) field.GetRawConstantValue();
            }
        }
        colors["none"] = Color.transparent;
        return colors;
    }

    // Sets a property of the state, of a presentation attribute, a rule or a
    // style attribute. Properties that PDFjet does not draw, and values it
    // cannot read, are left as they are inherited; it throws only for a color
    // that starts with # and is not hexadecimal, as it always has.
    internal void SetProperty(String name, String value) {
        value = value.Trim();
        if (value.Equals("inherit")) {
            return;
        }
        switch (name) {
            case "fill":
            case "stroke":
                int paint;
                bool current;
                if (!ParsePaint(name, value, out paint, out current)) {
                    return;
                }
                if (name.Equals("fill")) {
                    fill = paint;
                    fillCurrent = current;
                } else {
                    stroke = paint;
                    strokeCurrent = current;
                }
                break;
            case "color":
                int c;
                if (ParseColor(name, value, out c)) {
                    color = c;
                }
                break;
            case "stroke-width":
                if (value.EndsWith("%")) {
                    return;
                }
                float width;
                if (ParseLength(value, out width) && width >= 0f) {
                    strokeWidth = width;
                }
                break;
            case "fill-rule":
                if (value.Equals("evenodd")) {
                    evenOdd = true;
                } else if (value.Equals("nonzero")) {
                    evenOdd = false;
                }
                break;
            case "stroke-linecap":
                if (value.Equals("butt")) {
                    lineCap = CapStyle.BUTT;
                } else if (value.Equals("round")) {
                    lineCap = CapStyle.ROUND;
                } else if (value.Equals("square")) {
                    lineCap = CapStyle.PROJECTING_SQUARE;
                }
                break;
            case "stroke-linejoin":
                if (value.Equals("miter") || value.Equals("miter-clip") || value.Equals("arcs")) {
                    lineJoin = JoinStyle.MITER;
                } else if (value.Equals("round")) {
                    lineJoin = JoinStyle.ROUND;
                } else if (value.Equals("bevel")) {
                    lineJoin = JoinStyle.BEVEL;
                }
                break;
            case "opacity":
            case "fill-opacity":
            case "stroke-opacity":
                float alpha;
                if (!ParseOpacity(value, out alpha)) {
                    return;
                }
                if (name.Equals("opacity")) {
                    ownOpacity = alpha;
                } else if (name.Equals("fill-opacity")) {
                    fillOpacity = alpha;
                } else {
                    strokeOpacity = alpha;
                }
                break;
            case "display":
                if (value.Equals("none")) {
                    hidden = true;
                }
                break;
        }
    }

    // Reads the value of a fill or a stroke: none, currentColor, a color, or a
    // url of a gradient or a pattern, which PDFjet does not draw, and the color
    // after it, if any, which SVG draws when it cannot. Returns false for a
    // value that leaves the paint as it is inherited.
    private static bool ParsePaint(String name, String value, out int paint, out bool current) {
        paint = 0;
        current = false;
        if (value.StartsWith("url(")) {
            int end = value.IndexOf(')');
            if (end < 0) {
                return false;
            }
            value = value.Substring(end + 1).Trim();
            if (value.Equals("")) {
                return false;
            }
        }
        if (value.Equals("none")) {
            paint = Color.transparent;
            return true;
        }
        if (value.Equals("currentColor")) {
            current = true;
            return true;
        }
        return ParseColor(name, value, out paint);
    }

    // Reads a color: #rgb, #rrggbb, rgb() of three numbers or percentages, or
    // a name. Returns false for a color it does not know, and throws for one
    // that starts with # but is not hexadecimal.
    private static bool ParseColor(String name, String value, out int color) {
        color = 0;
        if (value.StartsWith("#")) {
            // The digits are counted and read in the bytes of UTF-8, one char
            // for each byte, as the Go port reads them.
            String hex = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(value.Substring(1)));
            if (hex.Length == 3) {
                hex = new String(new char[] {hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]});
            }
            if (hex.Length != 6) {
                return false;
            }
            // A sign is read as the sign of the number, as in the Go port.
            int start = (hex[0] == '+' || hex[0] == '-') ? 1 : 0;
            int number = 0;
            for (int i = start; i < hex.Length; i++) {
                int digit = HexDigit(hex[i]);
                if (digit < 0) {
                    throw new Exception("Invalid SVG " + name + ": invalid color \"" + value + "\".");
                }
                number = number*16 + digit;
            }
            color = (hex[0] == '-') ? -number : number;
            return true;
        }
        String lower = value.ToLowerInvariant();
        if (lower.StartsWith("rgb(") || lower.StartsWith("rgba(")) {
            int open = lower.IndexOf('(');
            int end = lower.IndexOf(')');
            if (end < open) {
                return false;
            }
            String[] parts = lower.Substring(open + 1, end - (open + 1)).Split(
                    new char[] {',', '/', ' ', '\t', '\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) {
                return false;
            }
            int rgb = 0;
            for (int i = 0; i < 3; i++) {
                String part = parts[i];
                double scale = 1.0;
                if (part.EndsWith("%")) {
                    part = part.Substring(0, part.Length - 1);
                    scale = 2.55;
                }
                double number;
                if (!ParseDouble(part, out number) || double.IsNaN(number) || double.IsInfinity(number)) {
                    return false;
                }
                double channel = Math.Round(number*scale, MidpointRounding.AwayFromZero);
                rgb = rgb << 8 | (int) Math.Max(0.0, Math.Min(255.0, channel));
            }
            color = rgb;
            return true;
        }
        return COLORS.TryGetValue(lower, out color);
    }

    private static int HexDigit(char ch) {
        if (ch >= '0' && ch <= '9') {
            return ch - '0';
        }
        if (ch >= 'a' && ch <= 'f') {
            return ch - 'a' + 10;
        }
        if (ch >= 'A' && ch <= 'F') {
            return ch - 'A' + 10;
        }
        return -1;
    }

    // Reads an opacity, a number or a percentage, and returns it between 0 and 1.
    private static bool ParseOpacity(String value, out float alpha) {
        alpha = 0f;
        double scale = 1.0;
        if (value.EndsWith("%")) {
            value = value.Substring(0, value.Length - 1);
            scale = 0.01;
        }
        double number;
        if (!ParseDouble(value.Trim(), out number) || double.IsNaN(number)) {
            return false;
        }
        alpha = (float) Math.Max(0.0, Math.Min(1.0, number*scale));
        return true;
    }

    // Reads a number as the Go port reads one: digits with a sign, a point and
    // an exponent, and no white space, or inf, infinity or nan, whatever their
    // case, which the callers turn down where they need a finite number. A
    // number too large for a double is not read.
    internal static bool ParseDouble(String text, out double number) {
        if (ParseSpecial(text, out number)) {
            return true;
        }
        if (!double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out number)) {
            return false;
        }
        return !double.IsInfinity(number) && !double.IsNaN(number);
    }

    // The same for a float: a number too large for a float is not read.
    internal static bool ParseFloat(String text, out float number) {
        double special;
        if (ParseSpecial(text, out special)) {
            number = (float) special;
            return true;
        }
        if (!float.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out number)) {
            return false;
        }
        return !float.IsInfinity(number) && !float.IsNaN(number);
    }

    // Reads inf or infinity with or without a sign, and nan, whatever the case
    // of their ASCII letters.
    private static bool ParseSpecial(String text, out double number) {
        number = 0.0;
        StringBuilder buf = new StringBuilder(text.Length);
        foreach (char ch in text) {
            buf.Append((ch >= 'A' && ch <= 'Z') ? (char) (ch + 32) : ch);
        }
        String lower = buf.ToString();
        double sign = 1.0;
        String rest = lower;
        if (rest.StartsWith("+") || rest.StartsWith("-")) {
            sign = rest.StartsWith("-") ? -1.0 : 1.0;
            rest = rest.Substring(1);
        }
        if (rest.Equals("inf") || rest.Equals("infinity")) {
            number = sign*double.PositiveInfinity;
            return true;
        }
        if (lower.Equals("nan")) {
            number = double.NaN;
            return true;
        }
        return false;
    }

    // The units of a length of an SVG file, in points. A number without a
    // unit is in the user unit of the file, which PDFjet draws as a point,
    // and so is a number in px.
    internal static readonly String[] UNITS = {"px", "pt", "pc", "in", "mm", "cm"};
    internal static readonly float[] POINTS = {1f, 1f, 12f, 72f, 72f/25.4f, 72f/2.54f};

    // Reads a length in user units, which PDFjet draws as points, with or
    // without a unit; an empty value is 0, as an omitted attribute.
    internal static bool ParseLength(String value, out float length) {
        length = 0f;
        String text = value.Trim();
        float scale = 1f;
        for (int i = 0; i < UNITS.Length; i++) {
            if (text.EndsWith(UNITS[i])) {
                text = text.Substring(0, text.Length - UNITS[i].Length).Trim();
                scale = POINTS[i];
                break;
            }
        }
        float number = 0f;
        if (text.Length > 0 && !ParseFloat(text, out number)) {
            return false;
        }
        if (float.IsInfinity(number) || float.IsNaN(number)) {
            return false;
        }
        length = number*scale;
        return true;
    }

    // Reads a coordinate or a size of a shape, and returns 0 for one it
    // cannot read, a percentage among them.
    internal static double ParseCoordinate(String value) {
        float length;
        if (!ParseLength(value, out length) || value.Trim().EndsWith("%")) {
            return 0.0;
        }
        return (double) length;
    }

    private static bool IsSpace(char ch) {
        return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
    }

    // Reads a list of numbers, as the points of a polygon or the arguments of
    // a transform are written: separated by white space or a comma, or by
    // nothing where a sign or a second point starts the next one. Returns null
    // for a list with a number it cannot read.
    internal static List<double> ParseNumbers(String text) {
        List<double> numbers = new List<double>();
        StringBuilder buf = new StringBuilder();
        foreach (char ch in text) {
            if (IsSpace(ch) || ch == ',') {
                if (!Flush(buf, numbers)) {
                    return null;
                }
            } else if (((ch == '-' || ch == '+') && !AfterExponent(buf)) ||
                    (ch == '.' && buf.ToString().Contains("."))) {
                if (!Flush(buf, numbers)) {
                    return null;
                }
                buf.Append(ch);
            } else {
                buf.Append(ch);
            }
        }
        if (!Flush(buf, numbers)) {
            return null;
        }
        return numbers;
    }

    private static bool AfterExponent(StringBuilder buf) {
        if (buf.Length == 0) {
            return false;
        }
        char last = buf[buf.Length - 1];
        return last == 'e' || last == 'E';
    }

    // Adds the number in the buffer, if any, to the list and empties the
    // buffer; returns false for one that is not a finite number.
    private static bool Flush(StringBuilder buf, List<double> numbers) {
        if (buf.Length == 0) {
            return true;
        }
        double number;
        bool ok = ParseDouble(buf.ToString(), out number) &&
                !double.IsInfinity(number) && !double.IsNaN(number);
        buf.Length = 0;
        if (ok) {
            numbers.Add(number);
        }
        return ok;
    }

    // The matrices of transforms are [a b c d e f], which take x, y to
    // a*x + c*y + e, b*x + d*y + f, as SVG writes them.
    internal static readonly double[] IDENTITY = {1.0, 0.0, 0.0, 1.0, 0.0, 0.0};

    internal static bool IsIdentity(double[] m) {
        for (int i = 0; i < 6; i++) {
            if (m[i] != IDENTITY[i]) {
                return false;
            }
        }
        return true;
    }

    // Returns the transform that is n, then m.
    internal static double[] Multiply(double[] m, double[] n) {
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
    internal static double[] ParseTransform(String text) {
        double[] matrix = IDENTITY;
        String rest = text.Trim();
        while (rest.Length > 0) {
            int open = rest.IndexOf('(');
            int end = rest.IndexOf(')');
            if (open < 0 || end < open) {
                return null;
            }
            String name = rest.Substring(0, open).Trim();
            List<double> args = ParseNumbers(rest.Substring(open + 1, end - (open + 1)));
            if (args == null) {
                return null;
            }
            rest = rest.Substring(end + 1).TrimStart(' ', '\t', '\r', '\n', ',');
            double[] m;
            if (name.Equals("matrix") && args.Count == 6) {
                m = new double[] {args[0], args[1], args[2], args[3], args[4], args[5]};
            } else if (name.Equals("translate") && args.Count == 1) {
                m = new double[] {1.0, 0.0, 0.0, 1.0, args[0], 0.0};
            } else if (name.Equals("translate") && args.Count == 2) {
                m = new double[] {1.0, 0.0, 0.0, 1.0, args[0], args[1]};
            } else if (name.Equals("scale") && args.Count == 1) {
                m = new double[] {args[0], 0.0, 0.0, args[0], 0.0, 0.0};
            } else if (name.Equals("scale") && args.Count == 2) {
                m = new double[] {args[0], 0.0, 0.0, args[1], 0.0, 0.0};
            } else if (name.Equals("rotate") && (args.Count == 1 || args.Count == 3)) {
                double angle = args[0] * Math.PI / 180.0;
                double cos = Math.Cos(angle);
                double sin = Math.Sin(angle);
                m = new double[] {cos, sin, -sin, cos, 0.0, 0.0};
                if (args.Count == 3) {
                    m = Multiply(new double[] {1.0, 0.0, 0.0, 1.0, args[1], args[2]},
                            Multiply(m, new double[] {1.0, 0.0, 0.0, 1.0, -args[1], -args[2]}));
                }
            } else if ((name.Equals("skewX") || name.Equals("skewY")) && args.Count == 1
                    && Math.Abs(args[0]) % 180.0 == 90.0) {
                return null;    // A skew of 90 degrees flattens everything: not a transform
            } else if (name.Equals("skewX") && args.Count == 1) {
                m = new double[] {1.0, 0.0, Math.Tan(args[0] * Math.PI / 180.0), 1.0, 0.0, 0.0};
            } else if (name.Equals("skewY") && args.Count == 1) {
                m = new double[] {1.0, Math.Tan(args[0] * Math.PI / 180.0), 0.0, 1.0, 0.0, 0.0};
            } else {
                return null;
            }
            matrix = Multiply(matrix, m);
        }
        return matrix;
    }
}

/// <summary>A rule of the style sheet of an SVG file for one class: .name followed by its declarations.</summary>
internal class SVGRule {
    internal String name;
    internal List<String[]> declarations;

    internal SVGRule(String name, List<String[]> declarations) {
        this.name = name;
        this.declarations = declarations;
    }

    // Returns the rules of the text of a <style> element that are for a
    // class, in the order of the text. Selectors of other kinds are left out.
    internal static List<SVGRule> ParseStyleSheet(String text) {
        // Comments are left out first; they may hold braces.
        while (true) {
            int start = text.IndexOf("/*", StringComparison.Ordinal);
            if (start < 0) {
                break;
            }
            int end = text.IndexOf("*/", start + 2, StringComparison.Ordinal);
            if (end < 0) {
                text = text.Substring(0, start);
                break;
            }
            text = text.Substring(0, start) + " " + text.Substring(end + 2);
        }
        List<SVGRule> rules = new List<SVGRule>();
        foreach (String block in text.Split('}')) {
            int open = block.IndexOf('{');
            if (open < 0) {
                continue;
            }
            List<String[]> declarations = ParseDeclarations(block.Substring(open + 1));
            foreach (String part in block.Substring(0, open).Split(',')) {
                String selector = part.Trim();
                if (selector.Length > 1 && selector[0] == '.' && IsName(selector.Substring(1))) {
                    rules.Add(new SVGRule(selector.Substring(1), declarations));
                }
            }
        }
        return rules;
    }

    // Returns true when the text is a name of a class alone, without the
    // combinators, the pseudo-classes and the like of other selectors.
    private static bool IsName(String text) {
        foreach (char ch in text) {
            if (!(ch == '-' || ch == '_' || (ch >= '0' && ch <= '9') ||
                    (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch > 127)) {
                return false;
            }
        }
        return true;
    }

    // Returns the declarations of a style attribute or of a rule, name: value;
    // each, without !important.
    internal static List<String[]> ParseDeclarations(String text) {
        List<String[]> declarations = new List<String[]>();
        foreach (String declaration in text.Split(';')) {
            int colon = declaration.IndexOf(':');
            if (colon < 0) {
                continue;
            }
            String name = declaration.Substring(0, colon).Trim();
            String value = declaration.Substring(colon + 1).Trim();
            if (value.EndsWith("!important")) {
                value = value.Substring(0, value.Length - "!important".Length);
            }
            value = value.Trim();
            if (name.Length > 0 && value.Length > 0) {
                declarations.Add(new String[] {name, value});
            }
        }
        return declarations;
    }
}
}   // End of PDFjet.NET namespace
