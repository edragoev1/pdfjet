/*
 * SVGState.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
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
    internal float fillAlpha = 1f;              // The alpha of the fill color, as #f008 or rgba() has one
    internal float strokeAlpha = 1f;
    internal float colorAlpha = 1f;
    internal float strokeWidth = 1f;            // In the user space of the element
    internal bool evenOdd = false;              // fill-rule="evenodd"
    internal bool clipEvenOdd = false;          // clip-rule="evenodd"
    internal CapStyle lineCap = CapStyle.BUTT;
    internal JoinStyle lineJoin = JoinStyle.MITER;
    internal float miterLimit = 4f;
    internal float fillOpacity = 1f;
    internal float strokeOpacity = 1f;
    internal float opacity = 1f;                // The opacities of the element and its ancestors, multiplied
    internal float ownOpacity = 1f;             // The opacity of the element itself, not inherited
    internal double[] matrix = IDENTITY;        // Replaced, never changed in place
    internal double[] viewport = {0.0, 0.0};    // The width and the height of the viewport, in user units; replaced too
    internal bool hidden = false;               // In <defs> and the like, or under display="none": not drawn
    internal bool invisible = false;            // visibility: not painted, but its children may be
    internal float[] dashArray = null;          // stroke-dasharray, in the user space; null is a solid line
    internal float dashOffset = 0f;
    internal bool strokeFirst = false;          // paint-order: the stroke drawn before the fill
    internal float fontSize = 16f;              // In the user space; medium, as a browser has it
    internal int fontWeight = 400;              // normal 400, bold 700
    internal bool italic = false;               // A font-style of italic or oblique
    internal int fontFamily = BundledFonts.SANS;    // The family of the bundled fonts its names stand for
    internal int textAnchor = 0;                // 0 start, 1 middle, 2 end
    internal int baseline = 0;                  // dominant-baseline: 0 the alphabetic, 1 the middle, 2 the hanging
    internal String markerStart = null;         // The ids of the markers, inherited
    internal String markerMid = null;
    internal String markerEnd = null;
    internal String clipPath = null;            // The id of the clip path of the element itself, not inherited
    internal List<SVGPath> clips = null;        // The clip paths of the element and of its ancestors
    internal Dictionary<String, int> paints = null;     // The colors of the gradients, by id, shared by every state

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
                float paintAlpha;
                bool current;
                if (!ParsePaint(paints, name, value, out paint, out paintAlpha, out current)) {
                    return;
                }
                if (name.Equals("fill")) {
                    fill = paint;
                    fillAlpha = paintAlpha;
                    fillCurrent = current;
                } else {
                    stroke = paint;
                    strokeAlpha = paintAlpha;
                    strokeCurrent = current;
                }
                break;
            case "color":
                int c;
                float cAlpha;
                if (ParseColor(name, value, out c, out cAlpha)) {
                    color = c;
                    colorAlpha = cAlpha;
                }
                break;
            case "stroke-width":
                if (value.EndsWith("%", StringComparison.Ordinal)) {
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
            case "clip-rule":
                if (value.Equals("evenodd")) {
                    clipEvenOdd = true;
                } else if (value.Equals("nonzero")) {
                    clipEvenOdd = false;
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
            case "stroke-miterlimit":
                // Below 1 is an error in SVG, and a PDF has to be able to hold it
                float limit;
                if (ParseLength(value, out limit) && limit >= 1f && FastFloat.IsWritable(limit)) {
                    miterLimit = limit;
                }
                break;
            case "visibility":
                if (value.Equals("hidden") || value.Equals("collapse")) {
                    invisible = true;
                } else if (value.Equals("visible")) {
                    invisible = false;
                }
                break;
            case "stroke-dasharray":
                dashArray = ParseDashArray(value);
                break;
            case "stroke-dashoffset":
                float offset;
                if (!value.EndsWith("%", StringComparison.Ordinal) && ParseLength(value, out offset)) {
                    dashOffset = offset;
                }
                break;
            case "paint-order":
                // The stroke is drawn first when it comes before the fill, and
                // the markers, which PDFjet draws last, are left out.
                List<String> order = Fields(value);
                int strokeIndex = order.IndexOf("stroke");
                int fillIndex = order.IndexOf("fill");
                strokeFirst = strokeIndex >= 0 && (fillIndex < 0 || strokeIndex < fillIndex);
                break;
            case "font-size":
                float size;
                if (ParseFontSize(value, fontSize, out size)) {
                    fontSize = size;
                }
                break;
            case "font-weight":
                int weight;
                if (ParseFontWeight(value, fontWeight, out weight)) {
                    fontWeight = weight;
                }
                break;
            case "font-style":
                if (value.Equals("italic") || value.StartsWith("oblique", StringComparison.Ordinal)) {
                    italic = true;
                } else if (value.Equals("normal")) {
                    italic = false;
                }
                break;
            case "font-family":
                fontFamily = BundledFonts.FamilyOf(value);
                break;
            case "font":
                SetFont(value);
                break;
            case "text-anchor":
                if (value.Equals("start")) {
                    textAnchor = 0;
                } else if (value.Equals("middle")) {
                    textAnchor = 1;
                } else if (value.Equals("end")) {
                    textAnchor = 2;
                }
                break;
            case "dominant-baseline":
            case "alignment-baseline":
                if (value.Equals("middle") || value.Equals("central")) {
                    baseline = 1;
                } else if (value.Equals("hanging") || value.Equals("text-before-edge")) {
                    baseline = 2;
                } else if (value.Equals("auto") || value.Equals("alphabetic") || value.Equals("baseline")) {
                    baseline = 0;
                }
                break;
            case "marker-start":
                markerStart = UrlId(value);
                break;
            case "marker-mid":
                markerMid = UrlId(value);
                break;
            case "marker-end":
                markerEnd = UrlId(value);
                break;
            case "marker":
                markerStart = UrlId(value);
                markerMid = markerStart;
                markerEnd = markerStart;
                break;
            case "clip-path":
                clipPath = UrlId(value);
                break;
        }
    }

    // Returns the id of a reference, url(#id), with or without quotes, and
    // null for none or for a value of another kind.
    internal static String UrlId(String value) {
        value = Trim(value);
        if (!value.StartsWith("url(", StringComparison.Ordinal)) {
            return null;
        }
        int end = value.IndexOf(')');
        if (end < 0) {
            return null;
        }
        String id = Trim(value.Substring(4, end - 4));
        if (id.Length >= 2 && (id[0] == '"' || id[0] == '\'') && id[id.Length - 1] == id[0]) {
            id = Trim(id.Substring(1, id.Length - 2));
        }
        if (id.Length < 2 || id[0] != '#') {
            return null;
        }
        return id.Substring(1);
    }

    // Reads a stroke-dasharray: the lengths of the dashes and the gaps, as
    // many again when they are an odd number. It returns null, a solid line,
    // for none, for a length below zero or one it cannot read, and for
    // lengths that are all zero.
    internal static float[] ParseDashArray(String value) {
        if (value.Equals("none")) {
            return null;
        }
        List<float> lengths = new List<float>();
        foreach (String field in Fields(value.Replace(',', ' '))) {
            float length;
            if (field.EndsWith("%", StringComparison.Ordinal) || !ParseLength(field, out length)
                    || length < 0f || float.IsNaN(length) || float.IsInfinity(length)) {
                return null;
            }
            lengths.Add(length);
        }
        float sum = 0f;
        foreach (float length in lengths) {
            sum += length;
        }
        if (lengths.Count == 0 || sum == 0f) {
            return null;
        }
        int count = lengths.Count;
        float[] dashes = new float[(count % 2 == 0) ? count : 2*count];
        for (int i = 0; i < dashes.Length; i++) {
            dashes[i] = lengths[i % count];
        }
        return dashes;
    }

    // The font sizes of the keywords, as a browser has them.
    private static readonly String[] SIZE_NAMES = {
        "xx-small", "x-small", "small", "medium", "large", "x-large", "xx-large"};
    private static readonly float[] SIZES = {9f, 10f, 13f, 16f, 18f, 24f, 32f};

    // Reads a font-size: a length, a number of em or a percentage of the
    // font size of the parent, or a keyword. It returns false for a size it
    // cannot read or one below zero.
    internal static bool ParseFontSize(String value, float parentSize, out float size) {
        for (int i = 0; i < SIZE_NAMES.Length; i++) {
            if (value.Equals(SIZE_NAMES[i])) {
                size = SIZES[i];
                return true;
            }
        }
        bool ok;
        if (value.EndsWith("em", StringComparison.Ordinal) && !value.EndsWith("rem", StringComparison.Ordinal)) {
            ok = ParseFactor(value.Substring(0, value.Length - 2), out size);
            size = size*parentSize;
        } else if (value.EndsWith("%", StringComparison.Ordinal)) {
            ok = ParseFactor(value.Substring(0, value.Length - 1), out size);
            size = size*parentSize/100f;
        } else {
            ok = ParseLength(value, out size);
        }
        return ok && size >= 0f && !float.IsNaN(size) && !float.IsInfinity(size);
    }

    // Reads a number, or returns false.
    private static bool ParseFactor(String text, out float factor) {
        double number;
        bool ok = ParseNumber(Trim(text), true, out number);
        factor = ok ? (float) number : 0f;
        return ok;
    }

    // Reads a font-weight, from 1 to 1000: normal is 400 and bold 700, and
    // bolder and lighter are those of CSS, from the weight of the parent. It
    // returns false for a weight it cannot read.
    internal static bool ParseFontWeight(String value, int parentWeight, out int weight) {
        weight = 0;
        if (value.Equals("normal")) {
            weight = 400;
            return true;
        } else if (value.Equals("bold")) {
            weight = 700;
            return true;
        } else if (value.Equals("bolder")) {
            weight = (parentWeight < 350) ? 400 : (parentWeight < 550) ? 700 : 900;
            return true;
        } else if (value.Equals("lighter")) {
            weight = (parentWeight < 550) ? 100 : (parentWeight < 750) ? 400 : 700;
            return true;
        }
        float number;
        if (!ParseFactor(value, out number) || number < 1f || number > 1000f) {
            return false;
        }
        // Rounded half up, as Java's Math.round
        weight = (int) Math.Floor((double) number + 0.5);
        return true;
    }

    // Sets the size, the weight, the style and the family of the font
    // shorthand, such as "bold 10px sans-serif": the words before the size,
    // then the size, which may have a line height after a slash, then the
    // family. A shorthand without a weight is normal.
    private void SetFont(String value) {
        int weight = 400;
        bool isItalic = false;
        List<String> fields = Fields(value);
        for (int i = 0; i < fields.Count; i++) {
            String field = fields[i];
            int slash = field.IndexOf('/');
            String size = (slash < 0) ? field : field.Substring(0, slash);
            // A size has a unit or is a keyword; a number alone is a weight.
            if (size.Length > 0 && !char.IsDigit(size[size.Length - 1])) {
                float newSize;
                if (ParseFontSize(size, fontSize, out newSize)) {
                    fontSize = newSize;
                    fontWeight = weight;
                    italic = isItalic;
                    // The family after the size, and a line height after a
                    // slash written apart from it
                    int from = i + 1;
                    if (slash < 0 && from < fields.Count && fields[from].StartsWith("/", StringComparison.Ordinal)) {
                        from += fields[from].Equals("/") ? 2 : 1;
                    }
                    StringBuilder family = new StringBuilder();
                    for (int j = from; j < fields.Count; j++) {
                        family.Append(j > from ? " " : "").Append(fields[j]);
                    }
                    fontFamily = BundledFonts.FamilyOf(family.ToString());
                    return;
                }
            }
            if (field.Equals("italic") || field.Equals("oblique")) {
                isItalic = true;
                continue;
            }
            int w;
            if (ParseFontWeight(field, fontWeight, out w)) {
                weight = w;
            }
        }
    }

    // Returns the text in lower case, a code point at a time, whatever the
    // culture, as the Java port's Character.toLowerCase and the Go port's
    // strings.ToLower do: İ is i, its simple lower case, which .NET's
    // invariant culture leaves as it is.
    internal static String ToLower(String text) {
        StringBuilder buf = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++) {
            char ch = text[i];
            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) {
                buf.Append(text.Substring(i, 2).ToLowerInvariant());
                i++;
            } else {
                buf.Append((ch == '\u0130') ? 'i' : char.ToLowerInvariant(ch));
            }
        }
        return buf.ToString();
    }

    // Reads the value of a fill or a stroke: none, currentColor, a color, or a
    // url of a gradient, which PDFjet draws in the mean color of its stops. A
    // url of anything else, a pattern or an element that is not there, is the
    // color after it, which SVG draws when it cannot draw the url, or none, as
    // a browser draws it. Returns false for a value that leaves the paint as
    // it is inherited.
    private static bool ParsePaint(Dictionary<String, int> paints, String name, String value,
            out int paint, out float alpha, out bool current) {
        paint = 0;
        alpha = 1f;
        current = false;
        if (value.StartsWith("url(", StringComparison.Ordinal)) {
            int end = value.IndexOf(')');
            if (end < 0) {
                return false;
            }
            String id = UrlId(value.Substring(0, end + 1));
            if (id != null && paints != null && paints.TryGetValue(id, out paint)) {
                return true;
            }
            value = Trim(value.Substring(end + 1));
            if (value.Equals("")) {
                value = "none";
            }
        }
        String keyword = value.ToLowerInvariant();
        if (keyword.Equals("none")) {
            paint = Color.transparent;
            return true;
        }
        if (keyword.Equals("currentcolor")) {
            current = true;
            return true;
        }
        return ParseColor(name, value, out paint, out alpha);
    }

    // The alpha the fill is drawn with: its opacities and the alpha of its
    // color.
    internal float DrawnFillAlpha() {
        return fillOpacity * opacity * (fillCurrent ? colorAlpha : fillAlpha);
    }

    // The alpha the stroke is drawn with.
    internal float DrawnStrokeAlpha() {
        return strokeOpacity * opacity * (strokeCurrent ? colorAlpha : strokeAlpha);
    }

    // Reads a color: #rgb, #rrggbb, rgb() of three numbers or percentages, or
    // a name. Returns false for a color it does not know, and throws for one
    // that starts with # but is not hexadecimal.
    private static bool ParseColor(String name, String value, out int color, out float alpha) {
        color = 0;
        alpha = 1f;
        if (value.StartsWith("#", StringComparison.Ordinal)) {
            // The digits are counted and read in the bytes of UTF-8, one char
            // for each byte, as the Go port reads them.
            String hex = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(value.Substring(1)));
            if (hex.Length == 3 || hex.Length == 4) {
                StringBuilder doubled = new StringBuilder();
                foreach (char digit in hex) {
                    doubled.Append(digit).Append(digit);
                }
                hex = doubled.ToString();
            }
            if (hex.Length != 6 && hex.Length != 8) {
                return false;
            }
            int a = 255;
            if (!ParseHex(hex.Substring(0, 6), out color)
                    || (hex.Length == 8 && !ParseHex(hex.Substring(6), out a))) {
                throw new InvalidDataException("Invalid SVG " + name + ": invalid color \"" + value + "\".");
            }
            alpha = ((float) Math.Max(a, 0))/255f;
            return true;
        }
        // İ is i, its simple lower case, as in the other ports: .NET's
        // invariant culture left it as it was (review of 17 October 2026)
        String lower = value.Replace('\u0130', 'i').ToLowerInvariant();
        bool hsl = lower.StartsWith("hsl(", StringComparison.Ordinal) || lower.StartsWith("hsla(", StringComparison.Ordinal);
        if (hsl || lower.StartsWith("rgb(", StringComparison.Ordinal) || lower.StartsWith("rgba(", StringComparison.Ordinal)) {
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
            float opacity;
            if (parts.Length > 3 && ParseOpacity(parts[3], out opacity)) {
                alpha = opacity;
            }
            if (hsl) {
                return ParseHSL(parts, out color);
            }
            int rgb = 0;
            for (int i = 0; i < 3; i++) {
                String part = parts[i];
                double scale = 1.0;
                if (part.EndsWith("%", StringComparison.Ordinal)) {
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

    // Returns the color of the hue, the saturation and the lightness of
    // hsl(), by the formula of CSS Color 4, or false for one it cannot read.
    private static bool ParseHSL(String[] parts, out int color) {
        color = 0;
        double hue;
        double saturation;
        double lightness;
        if (!ParseHue(parts[0], out hue) || !ParsePercentage(parts[1], out saturation)
                || !ParsePercentage(parts[2], out lightness)) {
            return false;
        }
        double a = saturation*Math.Min(lightness, 1.0 - lightness);
        foreach (double n in new double[] {0.0, 8.0, 4.0}) {
            double k = (n + hue/30.0) % 12.0;
            double channel = lightness - a*Math.Max(-1.0, Math.Min(Math.Min(k - 3.0, 9.0 - k), 1.0));
            color = color << 8
                    | (int) Math.Max(0.0, Math.Min(255.0, Math.Round(channel*255.0, MidpointRounding.AwayFromZero)));
        }
        return true;
    }

    // Reads a hue, in degrees unless it has a unit, and returns it from 0 to
    // 360, or false for one it cannot read.
    private static bool ParseHue(String text, out double hue) {
        hue = 0.0;
        double scale = 1.0;
        if (text.EndsWith("deg", StringComparison.Ordinal)) {
            text = text.Substring(0, text.Length - 3);
        } else if (text.EndsWith("grad", StringComparison.Ordinal)) {
            text = text.Substring(0, text.Length - 4);
            scale = 0.9;
        } else if (text.EndsWith("rad", StringComparison.Ordinal)) {
            text = text.Substring(0, text.Length - 3);
            scale = 57.29577951308232;
        } else if (text.EndsWith("turn", StringComparison.Ordinal)) {
            text = text.Substring(0, text.Length - 4);
            scale = 360.0;
        }
        double number;
        if (!ParseDouble(text, out number) || double.IsNaN(number) || double.IsInfinity(number)) {
            return false;
        }
        hue = (number*scale) % 360.0;
        if (hue < 0.0) {
            hue += 360.0;
        }
        return true;
    }

    // Reads a saturation or a lightness, with or without its percent sign,
    // and returns it between 0 and 1, or false for one it cannot read.
    private static bool ParsePercentage(String text, out double value) {
        value = 0.0;
        double number;
        if (text.EndsWith("%", StringComparison.Ordinal)) {
            text = text.Substring(0, text.Length - 1);
        }
        if (!ParseDouble(text, out number) || double.IsNaN(number) || double.IsInfinity(number)) {
            return false;
        }
        value = Math.Max(0.0, Math.Min(1.0, number/100.0));
        return true;
    }

    // Reads hexadecimal digits; a sign is read as the sign of the number, as
    // in the Go port.
    private static bool ParseHex(String hex, out int number) {
        number = 0;
        for (int i = (hex[0] == '+' || hex[0] == '-') ? 1 : 0; i < hex.Length; i++) {
            int digit = HexDigit(hex[i]);
            if (digit < 0) {
                return false;
            }
            number = number*16 + digit;
        }
        if (hex[0] == '-') {
            number = -number;
        }
        return true;
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
        if (value.EndsWith("%", StringComparison.Ordinal)) {
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
        if (rest.StartsWith("+", StringComparison.Ordinal) || rest.StartsWith("-", StringComparison.Ordinal)) {
            sign = rest.StartsWith("-", StringComparison.Ordinal) ? -1.0 : 1.0;
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
            if (text.EndsWith(UNITS[i], StringComparison.Ordinal)) {
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
        if (!ParseLength(value, out length) || value.Trim().EndsWith("%", StringComparison.Ordinal)) {
            return 0.0;
        }
        return (double) length;
    }

    private static bool IsSpace(char ch) {
        return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
    }

    // Returns the text without the white space of XML at its ends: spaces,
    // tabs and line breaks, and no other characters.
    internal static String Trim(String text) {
        int start = 0;
        int end = text.Length;
        while (start < end && IsSpace(text[start])) {
            start++;
        }
        while (end > start && IsSpace(text[end - 1])) {
            end--;
        }
        return text.Substring(start, end - start);
    }

    // Returns the words of the text, separated by the white space of XML.
    internal static List<String> Fields(String text) {
        List<String> fields = new List<String>();
        int start = -1;
        for (int i = 0; i <= text.Length; i++) {
            if (i == text.Length || IsSpace(text[i])) {
                if (start >= 0) {
                    fields.Add(text.Substring(start, i - start));
                    start = -1;
                }
            } else if (start < 0) {
                start = i;
            }
        }
        return fields;
    }

    // Reads a number as the Java port's parseNumber does: inf or infinity
    // with or without a sign, nan without one, whatever the case of their
    // letters, or digits with a sign, a point and an exponent, read as a
    // float when single is true. Returns false for anything else and for a
    // number too large.
    internal static bool ParseNumber(String text, bool single, out double number) {
        number = 0.0;
        int start = (text.StartsWith("+", StringComparison.Ordinal) || text.StartsWith("-", StringComparison.Ordinal)) ? 1 : 0;
        StringBuilder buf = new StringBuilder(text.Length);
        for (int i = start; i < text.Length; i++) {
            char ch = text[i];
            buf.Append((ch >= 'A' && ch <= 'Z') ? (char) (ch + 32) : ch);
        }
        String name = buf.ToString();
        if (name.Equals("inf") || name.Equals("infinity")) {
            number = text.StartsWith("-", StringComparison.Ordinal) ? double.NegativeInfinity : double.PositiveInfinity;
            return true;
        }
        if (start == 0 && name.Equals("nan")) {
            number = double.NaN;
            return true;
        }
        if (text.Length == 0) {
            return false;
        }
        foreach (char ch in text) {
            if ("0123456789.eE+-".IndexOf(ch) < 0) {
                return false;
            }
        }
        if (single) {
            float f;
            if (!ParseFloat(text, out f)) {
                return false;
            }
            number = f;
            return true;
        }
        return ParseDouble(text, out number);
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

/// <summary>
/// The rules of the style elements of an SVG file, in their order, and where
/// the rules of each class are among them, so that an element finds the rules
/// of its classes without going through all of them.
/// </summary>
internal class SVGStyleSheet {
    private readonly List<SVGRule> rules = new List<SVGRule>();
    private readonly Dictionary<String, List<int>> byClass = new Dictionary<String, List<int>>();

    // Adds the rules of a <style> element after those before it.
    internal void Add(List<SVGRule> added) {
        foreach (SVGRule rule in added) {
            List<int> indexes;
            if (!byClass.TryGetValue(rule.name, out indexes)) {
                indexes = new List<int>();
                byClass[rule.name] = indexes;
            }
            indexes.Add(rules.Count);
            rules.Add(rule);
        }
    }

    // Returns the rules for any of the classes, in the order of the style
    // sheet, each rule once.
    // The declarations its rules have applied, to elements, MAX_APPLIED at
    // most: N copies of a rule for a class and N elements of it were N x N
    // declarations, 20 s for 416 KB (review of 17 October 2026)
    internal const int MAX_APPLIED = 1000000;
    private long applied = 0;

    internal List<SVGRule> ForClasses(String[] classes) {
        List<int> indexes = new List<int>();
        // A set, not the classes before each: one element of N classes was
        // N x N
        HashSet<String> seen = new HashSet<String>(StringComparer.Ordinal);
        for (int i = 0; i < classes.Length; i++) {
            if (!seen.Add(classes[i])) {
                continue;   // A class named twice has its rules once.
            }
            List<int> found;
            if (byClass.TryGetValue(classes[i], out found)) {
                indexes.AddRange(found);
            }
        }
        indexes.Sort();
        List<SVGRule> list = new List<SVGRule>(indexes.Count);
        foreach (int index in indexes) {
            SVGRule rule = rules[index];
            applied += rule.declarations.Count;
            if (applied > MAX_APPLIED) {
                throw new ArgumentException(
                        "The style sheet of the SVG applies more than " + MAX_APPLIED + " declarations.");
            }
            list.Add(rule);
        }
        return list;
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
        // Comments are left out first; they may hold braces. A comment that
        // is not closed runs to the end of the text.
        StringBuilder uncommented = new StringBuilder();
        int from = 0;
        while (true) {
            int start = text.IndexOf("/*", from, StringComparison.Ordinal);
            if (start < 0) {
                uncommented.Append(text, from, text.Length - from);
                break;
            }
            uncommented.Append(text, from, start - from);
            int end = text.IndexOf("*/", start + 2, StringComparison.Ordinal);
            if (end < 0) {
                break;
            }
            uncommented.Append(' ');
            from = end + 2;
        }
        text = uncommented.ToString();
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
            if (value.EndsWith("!important", StringComparison.Ordinal)) {
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
