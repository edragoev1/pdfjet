/**
 *  Color.swift
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
public class Color {
    public static let transparent: Int32 = -1

    public static let aliceblue: Int32 = 0xf0f8ff
    public static let antiquewhite: Int32 = 0xfaebd7
    public static let aqua: Int32 = 0x00ffff
    public static let aquamarine: Int32 = 0x7fffd4
    public static let azure: Int32 = 0xf0ffff
    public static let beige: Int32 = 0xf5f5dc
    public static let bisque: Int32 = 0xffe4c4
    public static let black: Int32 = 0x000000
    public static let blanchedalmond: Int32 = 0xffebcd
    public static let blue: Int32 = 0x0000ff
    public static let blueviolet: Int32 = 0x8a2be2
    public static let brown: Int32 = 0xa52a2a
    public static let burlywood: Int32 = 0xdeb887
    public static let cadetblue: Int32 = 0x5f9ea0
    public static let chartreuse: Int32 = 0x7fff00
    public static let chocolate: Int32 = 0xd2691e
    public static let coral: Int32 = 0xff7f50
    public static let cornflowerblue: Int32 = 0x6495ed
    public static let cornsilk: Int32 = 0xfff8dc
    public static let crimson: Int32 = 0xdc143c
    public static let cyan: Int32 = 0x00ffff
    public static let darkblue: Int32 = 0x00008b
    public static let darkcyan: Int32 = 0x008b8b
    public static let darkgoldenrod: Int32 = 0xb8860b
    public static let darkgray: Int32 = 0xa9a9a9
    public static let darkgreen: Int32 = 0x006400
    public static let darkgrey: Int32 = 0xa9a9a9
    public static let darkkhaki: Int32 = 0xbdb76b
    public static let darkmagenta: Int32 = 0x8b008b
    public static let darkolivegreen: Int32 = 0x556b2f
    public static let darkorange: Int32 = 0xff8c00
    public static let darkorchid: Int32 = 0x9932cc
    public static let darkred: Int32 = 0x8b0000
    public static let darksalmon: Int32 = 0xe9967a
    public static let darkseagreen: Int32 = 0x8fbc8f
    public static let darkslateblue: Int32 = 0x483d8b
    public static let darkslategray: Int32 = 0x2f4f4f
    public static let darkslategrey: Int32 = 0x2f4f4f
    public static let darkturquoise: Int32 = 0x00ced1
    public static let darkviolet: Int32 = 0x9400d3
    public static let deeppink: Int32 = 0xff1493
    public static let deepskyblue: Int32 = 0x00bfff
    public static let dimgray: Int32 = 0x696969
    public static let dimgrey: Int32 = 0x696969
    public static let dodgerblue: Int32 = 0x1e90ff
    public static let firebrick: Int32 = 0xb22222
    public static let floralwhite: Int32 = 0xfffaf0
    public static let forestgreen: Int32 = 0x228b22
    public static let fuchsia: Int32 = 0xff00ff
    public static let gainsboro: Int32 = 0xdcdcdc
    public static let ghostwhite: Int32 = 0xf8f8ff
    public static let gold: Int32 = 0xffd700
    public static let goldenrod: Int32 = 0xdaa520
    public static let gray: Int32 = 0x808080
    public static let green: Int32 = 0x008000
    public static let greenyellow: Int32 = 0xadff2f
    public static let grey: Int32 = 0x808080
    public static let honeydew: Int32 = 0xf0fff0
    public static let hotpink: Int32 = 0xff69b4
    public static let indianred: Int32 = 0xcd5c5c
    public static let indigo: Int32 = 0x4b0082
    public static let ivory: Int32 = 0xfffff0
    public static let khaki: Int32 = 0xf0e68c
    public static let lavender: Int32 = 0xe6e6fa
    public static let lavenderblush: Int32 = 0xfff0f5
    public static let lawngreen: Int32 = 0x7cfc00
    public static let lemonchiffon: Int32 = 0xfffacd
    public static let lightblue: Int32 = 0xadd8e6
    public static let lightcoral: Int32 = 0xf08080
    public static let lightcyan: Int32 = 0xe0ffff
    public static let lightgoldenrodyellow: Int32 = 0xfafad2
    public static let lightgray: Int32 = 0xd3d3d3
    public static let lightgreen: Int32 = 0x90ee90
    public static let lightgrey: Int32 = 0xd3d3d3
    public static let lightpink: Int32 = 0xffb6c1
    public static let lightsalmon: Int32 = 0xffa07a
    public static let lightseagreen: Int32 = 0x20b2aa
    public static let lightskyblue: Int32 = 0x87cefa
    public static let lightslategray: Int32 = 0x778899
    public static let lightslategrey: Int32 = 0x778899
    public static let lightsteelblue: Int32 = 0xb0c4de
    public static let lightyellow: Int32 = 0xffffe0
    public static let lime: Int32 = 0x00ff00
    public static let limegreen: Int32 = 0x32cd32
    public static let linen: Int32 = 0xfaf0e6
    public static let magenta: Int32 = 0xff00ff
    public static let maroon: Int32 = 0x800000
    public static let mediumaquamarine: Int32 = 0x66cdaa
    public static let mediumblue: Int32 = 0x0000cd
    public static let mediumorchid: Int32 = 0xba55d3
    public static let mediumpurple: Int32 = 0x9370db
    public static let mediumseagreen: Int32 = 0x3cb371
    public static let mediumslateblue: Int32 = 0x7b68ee
    public static let mediumspringgreen: Int32 = 0x00fa9a
    public static let mediumturquoise: Int32 = 0x48d1cc
    public static let mediumvioletred: Int32 = 0xc71585
    public static let midnightblue: Int32 = 0x191970
    public static let mintcream: Int32 = 0xf5fffa
    public static let mistyrose: Int32 = 0xffe4e1
    public static let moccasin: Int32 = 0xffe4b5
    public static let navajowhite: Int32 = 0xffdead
    public static let navy: Int32 = 0x000080
    public static let oldlace: Int32 = 0xfdf5e6
    public static let olive: Int32 = 0x808000
    public static let olivedrab: Int32 = 0x6b8e23
    public static let orange: Int32 = 0xffa500
    public static let orangered: Int32 = 0xff4500
    public static let orchid: Int32 = 0xda70d6
    public static let palegoldenrod: Int32 = 0xeee8aa
    public static let palegreen: Int32 = 0x98fb98
    public static let paleturquoise: Int32 = 0xafeeee
    public static let palevioletred: Int32 = 0xdb7093
    public static let papayawhip: Int32 = 0xffefd5
    public static let peachpuff: Int32 = 0xffdab9
    public static let peru: Int32 = 0xcd853f
    public static let pink: Int32 = 0xffc0cb
    public static let plum: Int32 = 0xdda0dd
    public static let powderblue: Int32 = 0xb0e0e6
    public static let purple: Int32 = 0x800080
    public static let red: Int32 = 0xff0000
    public static let rosybrown: Int32 = 0xbc8f8f
    public static let royalblue: Int32 = 0x4169e1
    public static let saddlebrown: Int32 = 0x8b4513
    public static let salmon: Int32 = 0xfa8072
    public static let sandybrown: Int32 = 0xf4a460
    public static let seagreen: Int32 = 0x2e8b57
    public static let seashell: Int32 = 0xfff5ee
    public static let sienna: Int32 = 0xa0522d
    public static let silver: Int32 = 0xc0c0c0
    public static let skyblue: Int32 = 0x87ceeb
    public static let slateblue: Int32 = 0x6a5acd
    public static let slategray: Int32 = 0x708090
    public static let slategrey: Int32 = 0x708090
    public static let snow: Int32 = 0xfffafa
    public static let springgreen: Int32 = 0x00ff7f
    public static let steelblue: Int32 = 0x4682b4
    public static let tan: Int32 = 0xd2b48c
    public static let teal: Int32 = 0x008080
    public static let thistle: Int32 = 0xd8bfd8
    public static let tomato: Int32 = 0xff6347
    public static let turquoise: Int32 = 0x40e0d0
    public static let violet: Int32 = 0xee82ee
    public static let wheat: Int32 = 0xf5deb3
    public static let white: Int32 = 0xffffff
    public static let whitesmoke: Int32 = 0xf5f5f5
    public static let yellow: Int32 = 0xffff00
    public static let yellowgreen: Int32 = 0x9acd32

    public static let oldgloryred: Int32 = 0xb22234
    public static let oldgloryblue: Int32 = 0x3c3b6e
}
