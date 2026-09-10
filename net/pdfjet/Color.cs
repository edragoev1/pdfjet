/*
 * Color.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
/// <summary>
/// Used to specify the pen and brush colors.
/// </summary>
/// <seealso href="http://www.w3.org/TR/css3-color/#svg-color">http://www.w3.org/TR/css3-color/#svg-color</seealso>
public class Color {
    /// <summary>The color named "transparent"</summary>
    public const int transparent = -1;

    /// <summary>The color named "aliceblue"</summary>
    public const int aliceblue = 0xf0f8ff;
    /// <summary>The color named "antiquewhite"</summary>
    public const int antiquewhite = 0xfaebd7;
    /// <summary>The color named "aqua"</summary>
    public const int aqua = 0x00ffff;
    /// <summary>The color named "aquamarine"</summary>
    public const int aquamarine = 0x7fffd4;
    /// <summary>The color named "azure"</summary>
    public const int azure = 0xf0ffff;
    /// <summary>The color named "beige"</summary>
    public const int beige = 0xf5f5dc;
    /// <summary>The color named "bisque"</summary>
    public const int bisque = 0xffe4c4;
    /// <summary>The color named "black"</summary>
    public const int black = 0x000000;
    /// <summary>The color named "blanchedalmond"</summary>
    public const int blanchedalmond = 0xffebcd;
    /// <summary>The color named "blue"</summary>
    public const int blue = 0x0000ff;
    /// <summary>The color named "blueviolet"</summary>
    public const int blueviolet = 0x8a2be2;
    /// <summary>The color named "brown"</summary>
    public const int brown = 0xa52a2a;
    /// <summary>The color named "burlywood"</summary>
    public const int burlywood = 0xdeb887;
    /// <summary>The color named "cadetblue"</summary>
    public const int cadetblue = 0x5f9ea0;
    /// <summary>The color named "chartreuse"</summary>
    public const int chartreuse = 0x7fff00;
    /// <summary>The color named "chocolate"</summary>
    public const int chocolate = 0xd2691e;
    /// <summary>The color named "coral"</summary>
    public const int coral = 0xff7f50;
    /// <summary>The color named "cornflowerblue"</summary>
    public const int cornflowerblue = 0x6495ed;
    /// <summary>The color named "cornsilk"</summary>
    public const int cornsilk = 0xfff8dc;
    /// <summary>The color named "crimson"</summary>
    public const int crimson = 0xdc143c;
    /// <summary>The color named "cyan"</summary>
    public const int cyan = 0x00ffff;
    /// <summary>The color named "darkblue"</summary>
    public const int darkblue = 0x00008b;
    /// <summary>The color named "darkcyan"</summary>
    public const int darkcyan = 0x008b8b;
    /// <summary>The color named "darkgoldenrod"</summary>
    public const int darkgoldenrod = 0xb8860b;
    /// <summary>The color named "darkgray"</summary>
    public const int darkgray = 0xa9a9a9;
    /// <summary>The color named "darkgreen"</summary>
    public const int darkgreen = 0x006400;
    /// <summary>The color named "darkgrey"</summary>
    public const int darkgrey = 0xa9a9a9;
    /// <summary>The color named "darkkhaki"</summary>
    public const int darkkhaki = 0xbdb76b;
    /// <summary>The color named "darkmagenta"</summary>
    public const int darkmagenta = 0x8b008b;
    /// <summary>The color named "darkolivegreen"</summary>
    public const int darkolivegreen = 0x556b2f;
    /// <summary>The color named "darkorange"</summary>
    public const int darkorange = 0xff8c00;
    /// <summary>The color named "darkorchid"</summary>
    public const int darkorchid = 0x9932cc;
    /// <summary>The color named "darkred"</summary>
    public const int darkred = 0x8b0000;
    /// <summary>The color named "darksalmon"</summary>
    public const int darksalmon = 0xe9967a;
    /// <summary>The color named "darkseagreen"</summary>
    public const int darkseagreen = 0x8fbc8f;
    /// <summary>The color named "darkslateblue"</summary>
    public const int darkslateblue = 0x483d8b;
    /// <summary>The color named "darkslategray"</summary>
    public const int darkslategray = 0x2f4f4f;
    /// <summary>The color named "darkslategrey"</summary>
    public const int darkslategrey = 0x2f4f4f;
    /// <summary>The color named "darkturquoise"</summary>
    public const int darkturquoise = 0x00ced1;
    /// <summary>The color named "darkviolet"</summary>
    public const int darkviolet = 0x9400d3;
    /// <summary>The color named "deeppink"</summary>
    public const int deeppink = 0xff1493;
    /// <summary>The color named "deepskyblue"</summary>
    public const int deepskyblue = 0x00bfff;
    /// <summary>The color named "dimgray"</summary>
    public const int dimgray = 0x696969;
    /// <summary>The color named "dimgrey"</summary>
    public const int dimgrey = 0x696969;
    /// <summary>The color named "dodgerblue"</summary>
    public const int dodgerblue = 0x1e90ff;
    /// <summary>The color named "firebrick"</summary>
    public const int firebrick = 0xb22222;
    /// <summary>The color named "floralwhite"</summary>
    public const int floralwhite = 0xfffaf0;
    /// <summary>The color named "forestgreen"</summary>
    public const int forestgreen = 0x228b22;
    /// <summary>The color named "fuchsia"</summary>
    public const int fuchsia = 0xff00ff;
    /// <summary>The color named "gainsboro"</summary>
    public const int gainsboro = 0xdcdcdc;
    /// <summary>The color named "ghostwhite"</summary>
    public const int ghostwhite = 0xf8f8ff;
    /// <summary>The color named "gold"</summary>
    public const int gold = 0xffd700;
    /// <summary>The color named "goldenrod"</summary>
    public const int goldenrod = 0xdaa520;
    /// <summary>The color named "gray"</summary>
    public const int gray = 0x808080;
    /// <summary>The color named "green"</summary>
    public const int green = 0x008000;
    /// <summary>The color named "greenyellow"</summary>
    public const int greenyellow = 0xadff2f;
    /// <summary>The color named "grey"</summary>
    public const int grey = 0x808080;
    /// <summary>The color named "honeydew"</summary>
    public const int honeydew = 0xf0fff0;
    /// <summary>The color named "hotpink"</summary>
    public const int hotpink = 0xff69b4;
    /// <summary>The color named "indianred"</summary>
    public const int indianred = 0xcd5c5c;
    /// <summary>The color named "indigo"</summary>
    public const int indigo = 0x4b0082;
    /// <summary>The color named "ivory"</summary>
    public const int ivory = 0xfffff0;
    /// <summary>The color named "khaki"</summary>
    public const int khaki = 0xf0e68c;
    /// <summary>The color named "lavender"</summary>
    public const int lavender = 0xe6e6fa;
    /// <summary>The color named "lavenderblush"</summary>
    public const int lavenderblush = 0xfff0f5;
    /// <summary>The color named "lawngreen"</summary>
    public const int lawngreen = 0x7cfc00;
    /// <summary>The color named "lemonchiffon"</summary>
    public const int lemonchiffon = 0xfffacd;
    /// <summary>The color named "lightblue"</summary>
    public const int lightblue = 0xadd8e6;
    /// <summary>The color named "lightcoral"</summary>
    public const int lightcoral = 0xf08080;
    /// <summary>The color named "lightcyan"</summary>
    public const int lightcyan = 0xe0ffff;
    /// <summary>The color named "lightgoldenrodyellow"</summary>
    public const int lightgoldenrodyellow = 0xfafad2;
    /// <summary>The color named "lightgray"</summary>
    public const int lightgray = 0xd3d3d3;
    /// <summary>The color named "lightgreen"</summary>
    public const int lightgreen = 0x90ee90;
    /// <summary>The color named "lightgrey"</summary>
    public const int lightgrey = 0xd3d3d3;
    /// <summary>The color named "lightpink"</summary>
    public const int lightpink = 0xffb6c1;
    /// <summary>The color named "lightsalmon"</summary>
    public const int lightsalmon = 0xffa07a;
    /// <summary>The color named "lightseagreen"</summary>
    public const int lightseagreen = 0x20b2aa;
    /// <summary>The color named "lightskyblue"</summary>
    public const int lightskyblue = 0x87cefa;
    /// <summary>The color named "lightslategray"</summary>
    public const int lightslategray = 0x778899;
    /// <summary>The color named "lightslategrey"</summary>
    public const int lightslategrey = 0x778899;
    /// <summary>The color named "lightsteelblue"</summary>
    public const int lightsteelblue = 0xb0c4de;
    /// <summary>The color named "lightyellow"</summary>
    public const int lightyellow = 0xffffe0;
    /// <summary>The color named "lime"</summary>
    public const int lime = 0x00ff00;
    /// <summary>The color named "limegreen"</summary>
    public const int limegreen = 0x32cd32;
    /// <summary>The color named "linen"</summary>
    public const int linen = 0xfaf0e6;
    /// <summary>The color named "magenta"</summary>
    public const int magenta = 0xff00ff;
    /// <summary>The color named "maroon"</summary>
    public const int maroon = 0x800000;
    /// <summary>The color named "mediumaquamarine"</summary>
    public const int mediumaquamarine = 0x66cdaa;
    /// <summary>The color named "mediumblue"</summary>
    public const int mediumblue = 0x0000cd;
    /// <summary>The color named "mediumorchid"</summary>
    public const int mediumorchid = 0xba55d3;
    /// <summary>The color named "mediumpurple"</summary>
    public const int mediumpurple = 0x9370db;
    /// <summary>The color named "mediumseagreen"</summary>
    public const int mediumseagreen = 0x3cb371;
    /// <summary>The color named "mediumslateblue"</summary>
    public const int mediumslateblue = 0x7b68ee;
    /// <summary>The color named "mediumspringgreen"</summary>
    public const int mediumspringgreen = 0x00fa9a;
    /// <summary>The color named "mediumturquoise"</summary>
    public const int mediumturquoise = 0x48d1cc;
    /// <summary>The color named "mediumvioletred"</summary>
    public const int mediumvioletred = 0xc71585;
    /// <summary>The color named "midnightblue"</summary>
    public const int midnightblue = 0x191970;
    /// <summary>The color named "mintcream"</summary>
    public const int mintcream = 0xf5fffa;
    /// <summary>The color named "mistyrose"</summary>
    public const int mistyrose = 0xffe4e1;
    /// <summary>The color named "moccasin"</summary>
    public const int moccasin = 0xffe4b5;
    /// <summary>The color named "navajowhite"</summary>
    public const int navajowhite = 0xffdead;
    /// <summary>The color named "navy"</summary>
    public const int navy = 0x000080;
    /// <summary>The color named "oldlace"</summary>
    public const int oldlace = 0xfdf5e6;
    /// <summary>The color named "olive"</summary>
    public const int olive = 0x808000;
    /// <summary>The color named "olivedrab"</summary>
    public const int olivedrab = 0x6b8e23;
    /// <summary>The color named "orange"</summary>
    public const int orange = 0xffa500;
    /// <summary>The color named "orangered"</summary>
    public const int orangered = 0xff4500;
    /// <summary>The color named "orchid"</summary>
    public const int orchid = 0xda70d6;
    /// <summary>The color named "palegoldenrod"</summary>
    public const int palegoldenrod = 0xeee8aa;
    /// <summary>The color named "palegreen"</summary>
    public const int palegreen = 0x98fb98;
    /// <summary>The color named "paleturquoise"</summary>
    public const int paleturquoise = 0xafeeee;
    /// <summary>The color named "palevioletred"</summary>
    public const int palevioletred = 0xdb7093;
    /// <summary>The color named "papayawhip"</summary>
    public const int papayawhip = 0xffefd5;
    /// <summary>The color named "peachpuff"</summary>
    public const int peachpuff = 0xffdab9;
    /// <summary>The color named "peru"</summary>
    public const int peru = 0xcd853f;
    /// <summary>The color named "pink"</summary>
    public const int pink = 0xffc0cb;
    /// <summary>The color named "plum"</summary>
    public const int plum = 0xdda0dd;
    /// <summary>The color named "powderblue"</summary>
    public const int powderblue = 0xb0e0e6;
    /// <summary>The color named "purple"</summary>
    public const int purple = 0x800080;
    /// <summary>The color named "red"</summary>
    public const int red = 0xff0000;
    /// <summary>The color named "rosybrown"</summary>
    public const int rosybrown = 0xbc8f8f;
    /// <summary>The color named "royalblue"</summary>
    public const int royalblue = 0x4169e1;
    /// <summary>The color named "saddlebrown"</summary>
    public const int saddlebrown = 0x8b4513;
    /// <summary>The color named "salmon"</summary>
    public const int salmon = 0xfa8072;
    /// <summary>The color named "sandybrown"</summary>
    public const int sandybrown = 0xf4a460;
    /// <summary>The color named "seagreen"</summary>
    public const int seagreen = 0x2e8b57;
    /// <summary>The color named "seashell"</summary>
    public const int seashell = 0xfff5ee;
    /// <summary>The color named "sienna"</summary>
    public const int sienna = 0xa0522d;
    /// <summary>The color named "silver"</summary>
    public const int silver = 0xc0c0c0;
    /// <summary>The color named "skyblue"</summary>
    public const int skyblue = 0x87ceeb;
    /// <summary>The color named "slateblue"</summary>
    public const int slateblue = 0x6a5acd;
    /// <summary>The color named "slategray"</summary>
    public const int slategray = 0x708090;
    /// <summary>The color named "slategrey"</summary>
    public const int slategrey = 0x708090;
    /// <summary>The color named "snow"</summary>
    public const int snow = 0xfffafa;
    /// <summary>The color named "springgreen"</summary>
    public const int springgreen = 0x00ff7f;
    /// <summary>The color named "steelblue"</summary>
    public const int steelblue = 0x4682b4;
    /// <summary>The color named "tan"</summary>
    public const int tan = 0xd2b48c;
    /// <summary>The color named "teal"</summary>
    public const int teal = 0x008080;
    /// <summary>The color named "thistle"</summary>
    public const int thistle = 0xd8bfd8;
    /// <summary>The color named "tomato"</summary>
    public const int tomato = 0xff6347;
    /// <summary>The color named "turquoise"</summary>
    public const int turquoise = 0x40e0d0;
    /// <summary>The color named "violet"</summary>
    public const int violet = 0xee82ee;
    /// <summary>The color named "wheat"</summary>
    public const int wheat = 0xf5deb3;
    /// <summary>The color named "white"</summary>
    public const int white = 0xffffff;
    /// <summary>The color named "whitesmoke"</summary>
    public const int whitesmoke = 0xf5f5f5;
    /// <summary>The color named "yellow"</summary>
    public const int yellow = 0xffff00;
    /// <summary>The color named "yellowgreen"</summary>
    public const int yellowgreen = 0x9acd32;

    /// <summary>The color named "oldgloryred"</summary>
    public const int oldgloryred = 0xb22234;
    /// <summary>The color named "oldgloryblue"</summary>
    public const int oldgloryblue = 0x3c3b6e;
}
}    // End of namespace PDFjet.NET
