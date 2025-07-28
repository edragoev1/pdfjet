/**
 *  Color.cs
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

/**
 * Used to specify the pen and brush colors.
 * @see <a href="http://www.w3.org/TR/css3-color/#svg-color">http://www.w3.org/TR/css3-color/#svg-color</a>
 *
 */
namespace PDFjet.NET {
public class Color {
    public const int transparent = -1;
    public const int none = -1;

    public const int aliceblue = 0xf0f8ff;
    public const int antiquewhite = 0xfaebd7;
    public const int aqua = 0x00ffff;
    public const int aquamarine = 0x7fffd4;
    public const int azure = 0xf0ffff;
    public const int beige = 0xf5f5dc;
    public const int bisque = 0xffe4c4;
    public const int black = 0x000000;
    public const int blanchedalmond = 0xffebcd;
    public const int blue = 0x0000ff;
    public const int blueviolet = 0x8a2be2;
    public const int brown = 0xa52a2a;
    public const int burlywood = 0xdeb887;
    public const int cadetblue = 0x5f9ea0;
    public const int chartreuse = 0x7fff00;
    public const int chocolate = 0xd2691e;
    public const int coral = 0xff7f50;
    public const int cornflowerblue = 0x6495ed;
    public const int cornsilk = 0xfff8dc;
    public const int crimson = 0xdc143c;
    public const int cyan = 0x00ffff;
    public const int darkblue = 0x00008b;
    public const int darkcyan = 0x008b8b;
    public const int darkgoldenrod = 0xb8860b;
    public const int darkgray = 0xa9a9a9;
    public const int darkgreen = 0x006400;
    public const int darkgrey = 0xa9a9a9;
    public const int darkkhaki = 0xbdb76b;
    public const int darkmagenta = 0x8b008b;
    public const int darkolivegreen = 0x556b2f;
    public const int darkorange = 0xff8c00;
    public const int darkorchid = 0x9932cc;
    public const int darkred = 0x8b0000;
    public const int darksalmon = 0xe9967a;
    public const int darkseagreen = 0x8fbc8f;
    public const int darkslateblue = 0x483d8b;
    public const int darkslategray = 0x2f4f4f;
    public const int darkslategrey = 0x2f4f4f;
    public const int darkturquoise = 0x00ced1;
    public const int darkviolet = 0x9400d3;
    public const int deeppink = 0xff1493;
    public const int deepskyblue = 0x00bfff;
    public const int dimgray = 0x696969;
    public const int dimgrey = 0x696969;
    public const int dodgerblue = 0x1e90ff;
    public const int firebrick = 0xb22222;
    public const int floralwhite = 0xfffaf0;
    public const int forestgreen = 0x228b22;
    public const int fuchsia = 0xff00ff;
    public const int gainsboro = 0xdcdcdc;
    public const int ghostwhite = 0xf8f8ff;
    public const int gold = 0xffd700;
    public const int goldenrod = 0xdaa520;
    public const int gray = 0x808080;
    public const int green = 0x008000;
    public const int greenyellow = 0xadff2f;
    public const int grey = 0x808080;
    public const int honeydew = 0xf0fff0;
    public const int hotpink = 0xff69b4;
    public const int indianred = 0xcd5c5c;
    public const int indigo = 0x4b0082;
    public const int ivory = 0xfffff0;
    public const int khaki = 0xf0e68c;
    public const int lavender = 0xe6e6fa;
    public const int lavenderblush = 0xfff0f5;
    public const int lawngreen = 0x7cfc00;
    public const int lemonchiffon = 0xfffacd;
    public const int lightblue = 0xadd8e6;
    public const int lightcoral = 0xf08080;
    public const int lightcyan = 0xe0ffff;
    public const int lightgoldenrodyellow = 0xfafad2;
    public const int lightgray = 0xd3d3d3;
    public const int lightgreen = 0x90ee90;
    public const int lightgrey = 0xd3d3d3;
    public const int lightpink = 0xffb6c1;
    public const int lightsalmon = 0xffa07a;
    public const int lightseagreen = 0x20b2aa;
    public const int lightskyblue = 0x87cefa;
    public const int lightslategray = 0x778899;
    public const int lightslategrey = 0x778899;
    public const int lightsteelblue = 0xb0c4de;
    public const int lightyellow = 0xffffe0;
    public const int lime = 0x00ff00;
    public const int limegreen = 0x32cd32;
    public const int linen = 0xfaf0e6;
    public const int magenta = 0xff00ff;
    public const int maroon = 0x800000;
    public const int mediumaquamarine = 0x66cdaa;
    public const int mediumblue = 0x0000cd;
    public const int mediumorchid = 0xba55d3;
    public const int mediumpurple = 0x9370db;
    public const int mediumseagreen = 0x3cb371;
    public const int mediumslateblue = 0x7b68ee;
    public const int mediumspringgreen = 0x00fa9a;
    public const int mediumturquoise = 0x48d1cc;
    public const int mediumvioletred = 0xc71585;
    public const int midnightblue = 0x191970;
    public const int mintcream = 0xf5fffa;
    public const int mistyrose = 0xffe4e1;
    public const int moccasin = 0xffe4b5;
    public const int navajowhite = 0xffdead;
    public const int navy = 0x000080;
    public const int oldlace = 0xfdf5e6;
    public const int olive = 0x808000;
    public const int olivedrab = 0x6b8e23;
    public const int orange = 0xffa500;
    public const int orangered = 0xff4500;
    public const int orchid = 0xda70d6;
    public const int palegoldenrod = 0xeee8aa;
    public const int palegreen = 0x98fb98;
    public const int paleturquoise = 0xafeeee;
    public const int palevioletred = 0xdb7093;
    public const int papayawhip = 0xffefd5;
    public const int peachpuff = 0xffdab9;
    public const int peru = 0xcd853f;
    public const int pink = 0xffc0cb;
    public const int plum = 0xdda0dd;
    public const int powderblue = 0xb0e0e6;
    public const int purple = 0x800080;
    public const int red = 0xff0000;
    public const int rosybrown = 0xbc8f8f;
    public const int royalblue = 0x4169e1;
    public const int saddlebrown = 0x8b4513;
    public const int salmon = 0xfa8072;
    public const int sandybrown = 0xf4a460;
    public const int seagreen = 0x2e8b57;
    public const int seashell = 0xfff5ee;
    public const int sienna = 0xa0522d;
    public const int silver = 0xc0c0c0;
    public const int skyblue = 0x87ceeb;
    public const int slateblue = 0x6a5acd;
    public const int slategray = 0x708090;
    public const int slategrey = 0x708090;
    public const int snow = 0xfffafa;
    public const int springgreen = 0x00ff7f;
    public const int steelblue = 0x4682b4;
    public const int tan = 0xd2b48c;
    public const int teal = 0x008080;
    public const int thistle = 0xd8bfd8;
    public const int tomato = 0xff6347;
    public const int turquoise = 0x40e0d0;
    public const int violet = 0xee82ee;
    public const int wheat = 0xf5deb3;
    public const int white = 0xffffff;
    public const int whitesmoke = 0xf5f5f5;
    public const int yellow = 0xffff00;
    public const int yellowgreen = 0x9acd32;

    public const int oldgloryred = 0xb22234;
    public const int oldgloryblue = 0x3c3b6e;
}
}    // End of namespace PDFjet.NET
