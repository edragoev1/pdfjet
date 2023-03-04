/**
 *  Color.java
 *
Copyright 2023 Innovatics Inc.

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

/**
 * Used to specify the pen and brush colors.
 * @see <a href="http://www.w3.org/TR/css3-color/#svg-color">http://www.w3.org/TR/css3-color/#svg-color</a>
 *
 */
 public class Color {
    /** The color named "aliceblue" */
    public static final int aliceblue = 0xf0f8ff;
    /** The color named "antiquewhite" */
    public static final int antiquewhite = 0xfaebd7;
    /** The color named "aqua" */
    public static final int aqua = 0x00ffff;
    /** The color named "aquamarine" */
    public static final int aquamarine = 0x7fffd4;
    /** The color named "azure" */
    public static final int azure = 0xf0ffff;
    /** The color named "beige" */
    public static final int beige = 0xf5f5dc;
    /** The color named "bisque" */
    public static final int bisque = 0xffe4c4;
    /** The color named "black" */
    public static final int black = 0x000000;
    /** The color named "blanchedalmond" */
    public static final int blanchedalmond = 0xffebcd;
    /** The color named "blue" */
    public static final int blue = 0x0000ff;
    /** The color named "blueviolet" */
    public static final int blueviolet = 0x8a2be2;
    /** The color named "brown" */
    public static final int brown = 0xa52a2a;
    /** The color named "burlywood" */
    public static final int burlywood = 0xdeb887;
    /** The color named "cadetblue" */
    public static final int cadetblue = 0x5f9ea0;
    /** The color named "chartreuse" */
    public static final int chartreuse = 0x7fff00;
    /** The color named "chocolate" */
    public static final int chocolate = 0xd2691e;
    /** The color named "coral" */
    public static final int coral = 0xff7f50;
    /** The color named "cornflowerblue" */
    public static final int cornflowerblue = 0x6495ed;
    /** The color named "cornsilk" */
    public static final int cornsilk = 0xfff8dc;
    /** The color named "crimson" */
    public static final int crimson = 0xdc143c;
    /** The color named "cyan" */
    public static final int cyan = 0x00ffff;
    /** The color named "darkblue" */
    public static final int darkblue = 0x00008b;
    /** The color named "darkcyan" */
    public static final int darkcyan = 0x008b8b;
    /** The color named "darkgoldenrod" */
    public static final int darkgoldenrod = 0xb8860b;
    /** The color named "darkgray" */
    public static final int darkgray = 0xa9a9a9;
    /** The color named "darkgreen" */
    public static final int darkgreen = 0x006400;
    /** The color named "darkgrey" */
    public static final int darkgrey = 0xa9a9a9;
    /** The color named "darkkhaki" */
    public static final int darkkhaki = 0xbdb76b;
    /** The color named "darkmagenta" */
    public static final int darkmagenta = 0x8b008b;
    /** The color named "darkolivegreen" */
    public static final int darkolivegreen = 0x556b2f;
    /** The color named "darkorange" */
    public static final int darkorange = 0xff8c00;
    /** The color named "darkorchid" */
    public static final int darkorchid = 0x9932cc;
    /** The color named "darkred" */
    public static final int darkred = 0x8b0000;
    /** The color named "darksalmon" */
    public static final int darksalmon = 0xe9967a;
    /** The color named "darkseagreen" */
    public static final int darkseagreen = 0x8fbc8f;
    /** The color named "darkslateblue" */
    public static final int darkslateblue = 0x483d8b;
    /** The color named "darkslategray" */
    public static final int darkslategray = 0x2f4f4f;
    /** The color named "darkslategrey" */
    public static final int darkslategrey = 0x2f4f4f;
    /** The color named "darkturquoise" */
    public static final int darkturquoise = 0x00ced1;
    /** The color named "darkviolet" */
    public static final int darkviolet = 0x9400d3;
    /** The color named "deeppink" */
    public static final int deeppink = 0xff1493;
    /** The color named "deepskyblue" */
    public static final int deepskyblue = 0x00bfff;
    /** The color named "dimgray" */
    public static final int dimgray = 0x696969;
    /** The color named "dimgrey" */
    public static final int dimgrey = 0x696969;
    /** The color named "dodgerblue" */
    public static final int dodgerblue = 0x1e90ff;
    /** The color named "firebrick" */
    public static final int firebrick = 0xb22222;
    /** The color named "floralwhite" */
    public static final int floralwhite = 0xfffaf0;
    /** The color named "forestgreen" */
    public static final int forestgreen = 0x228b22;
    /** The color named "fuchsia" */
    public static final int fuchsia = 0xff00ff;
    /** The color named "gainsboro" */
    public static final int gainsboro = 0xdcdcdc;
    /** The color named "ghostwhite" */
    public static final int ghostwhite = 0xf8f8ff;
    /** The color named "gold" */
    public static final int gold = 0xffd700;
    /** The color named "goldenrod" */
    public static final int goldenrod = 0xdaa520;
    /** The color named "gray" */
    public static final int gray = 0x808080;
    /** The color named "green" */
    public static final int green = 0x008000;
    /** The color named "greenyellow" */
    public static final int greenyellow = 0xadff2f;
    /** The color named "grey" */
    public static final int grey = 0x808080;
    /** The color named "honeydew" */
    public static final int honeydew = 0xf0fff0;
    /** The color named "hotpink" */
    public static final int hotpink = 0xff69b4;
    /** The color named "indianred" */
    public static final int indianred = 0xcd5c5c;
    /** The color named "indigo" */
    public static final int indigo = 0x4b0082;
    /** The color named "ivory" */
    public static final int ivory = 0xfffff0;
    /** The color named "khaki" */
    public static final int khaki = 0xf0e68c;
    /** The color named "lavender" */
    public static final int lavender = 0xe6e6fa;
    /** The color named "lavenderblush" */
    public static final int lavenderblush = 0xfff0f5;
    /** The color named "lawngreen" */
    public static final int lawngreen = 0x7cfc00;
    /** The color named "lemonchiffon" */
    public static final int lemonchiffon = 0xfffacd;
    /** The color named "lightblue" */
    public static final int lightblue = 0xadd8e6;
    /** The color named "lightcoral" */
    public static final int lightcoral = 0xf08080;
    /** The color named "lightcyan" */
    public static final int lightcyan = 0xe0ffff;
    /** The color named "lightgoldenrodyellow" */
    public static final int lightgoldenrodyellow = 0xfafad2;
    /** The color named "lightgray" */
    public static final int lightgray = 0xd3d3d3;
    /** The color named "lightgreen" */
    public static final int lightgreen = 0x90ee90;
    /** The color named "lightgrey" */
    public static final int lightgrey = 0xd3d3d3;
    /** The color named "lightpink" */
    public static final int lightpink = 0xffb6c1;
    /** The color named "lightsalmon" */
    public static final int lightsalmon = 0xffa07a;
    /** The color named "lightseagreen" */
    public static final int lightseagreen = 0x20b2aa;
    /** The color named "lightskyblue" */
    public static final int lightskyblue = 0x87cefa;
    /** The color named "lightslategray" */
    public static final int lightslategray = 0x778899;
    /** The color named "lightslategrey" */
    public static final int lightslategrey = 0x778899;
    /** The color named "lightsteelblue" */
    public static final int lightsteelblue = 0xb0c4de;
    /** The color named "lightyellow" */
    public static final int lightyellow = 0xffffe0;
    /** The color named "lime" */
    public static final int lime = 0x00ff00;
    /** The color named "limegreen" */
    public static final int limegreen = 0x32cd32;
    /** The color named "linen" */
    public static final int linen = 0xfaf0e6;
    public static final int magenta = 0xff00ff;
    public static final int maroon = 0x800000;
    public static final int mediumaquamarine = 0x66cdaa;
    public static final int mediumblue = 0x0000cd;
    public static final int mediumorchid = 0xba55d3;
    public static final int mediumpurple = 0x9370db;
    public static final int mediumseagreen = 0x3cb371;
    public static final int mediumslateblue = 0x7b68ee;
    public static final int mediumspringgreen = 0x00fa9a;
    public static final int mediumturquoise = 0x48d1cc;
    public static final int mediumvioletred = 0xc71585;
    public static final int midnightblue = 0x191970;
    public static final int mintcream = 0xf5fffa;
    public static final int mistyrose = 0xffe4e1;
    public static final int moccasin = 0xffe4b5;
    public static final int navajowhite = 0xffdead;
    public static final int navy = 0x000080;
    public static final int oldlace = 0xfdf5e6;
    public static final int olive = 0x808000;
    public static final int olivedrab = 0x6b8e23;
    public static final int orange = 0xffa500;
    public static final int orangered = 0xff4500;
    public static final int orchid = 0xda70d6;
    public static final int palegoldenrod = 0xeee8aa;
    public static final int palegreen = 0x98fb98;
    public static final int paleturquoise = 0xafeeee;
    public static final int palevioletred = 0xdb7093;
    public static final int papayawhip = 0xffefd5;
    public static final int peachpuff = 0xffdab9;
    public static final int peru = 0xcd853f;
    public static final int pink = 0xffc0cb;
    public static final int plum = 0xdda0dd;
    public static final int powderblue = 0xb0e0e6;
    public static final int purple = 0x800080;
    public static final int red = 0xff0000;
    public static final int rosybrown = 0xbc8f8f;
    public static final int royalblue = 0x4169e1;
    public static final int saddlebrown = 0x8b4513;
    public static final int salmon = 0xfa8072;
    public static final int sandybrown = 0xf4a460;
    public static final int seagreen = 0x2e8b57;
    public static final int seashell = 0xfff5ee;
    public static final int sienna = 0xa0522d;
    public static final int silver = 0xc0c0c0;
    public static final int skyblue = 0x87ceeb;
    public static final int slateblue = 0x6a5acd;
    public static final int slategray = 0x708090;
    public static final int slategrey = 0x708090;
    public static final int snow = 0xfffafa;
    public static final int springgreen = 0x00ff7f;
    public static final int steelblue = 0x4682b4;
    public static final int tan = 0xd2b48c;
    public static final int teal = 0x008080;
    public static final int thistle = 0xd8bfd8;
    public static final int tomato = 0xff6347;
    public static final int turquoise = 0x40e0d0;
    public static final int violet = 0xee82ee;
    public static final int wheat = 0xf5deb3;
    public static final int white = 0xffffff;
    public static final int whitesmoke = 0xf5f5f5;
    public static final int yellow = 0xffff00;
    public static final int yellowgreen = 0x9acd32;

    public static final int oldgloryred = 0xb22234;
    public static final int oldgloryblue = 0x3c3b6e;

    public static final int transparent = -1;
}
