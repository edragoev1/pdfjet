/**
 *  ColorCSS.swift
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


/**
 * Used to specify the pen and brush colors.
 * @see <a href="http://www.w3.org/TR/css3-color/#svg-color">http://www.w3.org/TR/css3-color/#svg-color</a>
 *
 */
public class ColorCSS {
    public let transparent: UInt32 = 0x80000000

    public let aliceblue: UInt32 = 0xf0f8ff
    public let antiquewhite: UInt32 = 0xfaebd7
    public let aqua: UInt32 = 0x00ffff
    public let aquamarine: UInt32 = 0x7fffd4
    public let azure: UInt32 = 0xf0ffff
    public let beige: UInt32 = 0xf5f5dc
    public let bisque: UInt32 = 0xffe4c4
    public let black: UInt32 = 0x000000
    public let blanchedalmond: UInt32 = 0xffebcd
    public let blue: UInt32 = 0x0000ff
    public let blueviolet: UInt32 = 0x8a2be2
    public let brown: UInt32 = 0xa52a2a
    public let burlywood: UInt32 = 0xdeb887
    public let cadetblue: UInt32 = 0x5f9ea0
    public let chartreuse: UInt32 = 0x7fff00
    public let chocolate: UInt32 = 0xd2691e
    public let coral: UInt32 = 0xff7f50
    public let cornflowerblue: UInt32 = 0x6495ed
    public let cornsilk: UInt32 = 0xfff8dc
    public let crimson: UInt32 = 0xdc143c
    public let cyan: UInt32 = 0x00ffff
    public let darkblue: UInt32 = 0x00008b
    public let darkcyan: UInt32 = 0x008b8b
    public let darkgoldenrod: UInt32 = 0xb8860b
    public let darkgray: UInt32 = 0xa9a9a9
    public let darkgreen: UInt32 = 0x006400
    public let darkgrey: UInt32 = 0xa9a9a9
    public let darkkhaki: UInt32 = 0xbdb76b
    public let darkmagenta: UInt32 = 0x8b008b
    public let darkolivegreen: UInt32 = 0x556b2f
    public let darkorange: UInt32 = 0xff8c00
    public let darkorchid: UInt32 = 0x9932cc
    public let darkred: UInt32 = 0x8b0000
    public let darksalmon: UInt32 = 0xe9967a
    public let darkseagreen: UInt32 = 0x8fbc8f
    public let darkslateblue: UInt32 = 0x483d8b
    public let darkslategray: UInt32 = 0x2f4f4f
    public let darkslategrey: UInt32 = 0x2f4f4f
    public let darkturquoise: UInt32 = 0x00ced1
    public let darkviolet: UInt32 = 0x9400d3
    public let deeppink: UInt32 = 0xff1493
    public let deepskyblue: UInt32 = 0x00bfff
    public let dimgray: UInt32 = 0x696969
    public let dimgrey: UInt32 = 0x696969
    public let dodgerblue: UInt32 = 0x1e90ff
    public let firebrick: UInt32 = 0xb22222
    public let floralwhite: UInt32 = 0xfffaf0
    public let forestgreen: UInt32 = 0x228b22
    public let fuchsia: UInt32 = 0xff00ff
    public let gainsboro: UInt32 = 0xdcdcdc
    public let ghostwhite: UInt32 = 0xf8f8ff
    public let gold: UInt32 = 0xffd700
    public let goldenrod: UInt32 = 0xdaa520
    public let gray: UInt32 = 0x808080
    public let green: UInt32 = 0x008000
    public let greenyellow: UInt32 = 0xadff2f
    public let grey: UInt32 = 0x808080
    public let honeydew: UInt32 = 0xf0fff0
    public let hotpink: UInt32 = 0xff69b4
    public let indianred: UInt32 = 0xcd5c5c
    public let indigo: UInt32 = 0x4b0082
    public let ivory: UInt32 = 0xfffff0
    public let khaki: UInt32 = 0xf0e68c
    public let lavender: UInt32 = 0xe6e6fa
    public let lavenderblush: UInt32 = 0xfff0f5
    public let lawngreen: UInt32 = 0x7cfc00
    public let lemonchiffon: UInt32 = 0xfffacd
    public let lightblue: UInt32 = 0xadd8e6
    public let lightcoral: UInt32 = 0xf08080
    public let lightcyan: UInt32 = 0xe0ffff
    public let lightgoldenrodyellow: UInt32 = 0xfafad2
    public let lightgray: UInt32 = 0xd3d3d3
    public let lightgreen: UInt32 = 0x90ee90
    public let lightgrey: UInt32 = 0xd3d3d3
    public let lightpink: UInt32 = 0xffb6c1
    public let lightsalmon: UInt32 = 0xffa07a
    public let lightseagreen: UInt32 = 0x20b2aa
    public let lightskyblue: UInt32 = 0x87cefa
    public let lightslategray: UInt32 = 0x778899
    public let lightslategrey: UInt32 = 0x778899
    public let lightsteelblue: UInt32 = 0xb0c4de
    public let lightyellow: UInt32 = 0xffffe0
    public let lime: UInt32 = 0x00ff00
    public let limegreen: UInt32 = 0x32cd32
    public let linen: UInt32 = 0xfaf0e6
    public let magenta: UInt32 = 0xff00ff
    public let maroon: UInt32 = 0x800000
    public let mediumaquamarine: UInt32 = 0x66cdaa
    public let mediumblue: UInt32 = 0x0000cd
    public let mediumorchid: UInt32 = 0xba55d3
    public let mediumpurple: UInt32 = 0x9370db
    public let mediumseagreen: UInt32 = 0x3cb371
    public let mediumslateblue: UInt32 = 0x7b68ee
    public let mediumspringgreen: UInt32 = 0x00fa9a
    public let mediumturquoise: UInt32 = 0x48d1cc
    public let mediumvioletred: UInt32 = 0xc71585
    public let midnightblue: UInt32 = 0x191970
    public let mintcream: UInt32 = 0xf5fffa
    public let mistyrose: UInt32 = 0xffe4e1
    public let moccasin: UInt32 = 0xffe4b5
    public let navajowhite: UInt32 = 0xffdead
    public let navy: UInt32 = 0x000080
    public let oldlace: UInt32 = 0xfdf5e6
    public let olive: UInt32 = 0x808000
    public let olivedrab: UInt32 = 0x6b8e23
    public let orange: UInt32 = 0xffa500
    public let orangered: UInt32 = 0xff4500
    public let orchid: UInt32 = 0xda70d6
    public let palegoldenrod: UInt32 = 0xeee8aa
    public let palegreen: UInt32 = 0x98fb98
    public let paleturquoise: UInt32 = 0xafeeee
    public let palevioletred: UInt32 = 0xdb7093
    public let papayawhip: UInt32 = 0xffefd5
    public let peachpuff: UInt32 = 0xffdab9
    public let peru: UInt32 = 0xcd853f
    public let pink: UInt32 = 0xffc0cb
    public let plum: UInt32 = 0xdda0dd
    public let powderblue: UInt32 = 0xb0e0e6
    public let purple: UInt32 = 0x800080
    public let red: UInt32 = 0xff0000
    public let rosybrown: UInt32 = 0xbc8f8f
    public let royalblue: UInt32 = 0x4169e1
    public let saddlebrown: UInt32 = 0x8b4513
    public let salmon: UInt32 = 0xfa8072
    public let sandybrown: UInt32 = 0xf4a460
    public let seagreen: UInt32 = 0x2e8b57
    public let seashell: UInt32 = 0xfff5ee
    public let sienna: UInt32 = 0xa0522d
    public let silver: UInt32 = 0xc0c0c0
    public let skyblue: UInt32 = 0x87ceeb
    public let slateblue: UInt32 = 0x6a5acd
    public let slategray: UInt32 = 0x708090
    public let slategrey: UInt32 = 0x708090
    public let snow: UInt32 = 0xfffafa
    public let springgreen: UInt32 = 0x00ff7f
    public let steelblue: UInt32 = 0x4682b4
    public let tan: UInt32 = 0xd2b48c
    public let teal: UInt32 = 0x008080
    public let thistle: UInt32 = 0xd8bfd8
    public let tomato: UInt32 = 0xff6347
    public let turquoise: UInt32 = 0x40e0d0
    public let violet: UInt32 = 0xee82ee
    public let wheat: UInt32 = 0xf5deb3
    public let white: UInt32 = 0xffffff
    public let whitesmoke: UInt32 = 0xf5f5f5
    public let yellow: UInt32 = 0xffff00
    public let yellowgreen: UInt32 = 0x9acd32

    public let oldgloryred: UInt32 = 0xb22234
    public let oldgloryblue: UInt32 = 0x3c3b6e
}
