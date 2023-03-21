/**
 *  ColorMap.swift
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
 * NOTE: We need this class in addition to Color.swift because the Swift reflection does not work with static variables.
 */
public class ColorMap {
    public let transparent: Int32 = -1
    public let none: Int32 = -1

    public let aliceblue: Int32 = 0xf0f8ff
    public let antiquewhite: Int32 = 0xfaebd7
    public let aqua: Int32 = 0x00ffff
    public let aquamarine: Int32 = 0x7fffd4
    public let azure: Int32 = 0xf0ffff
    public let beige: Int32 = 0xf5f5dc
    public let bisque: Int32 = 0xffe4c4
    public let black: Int32 = 0x000000
    public let blanchedalmond: Int32 = 0xffebcd
    public let blue: Int32 = 0x0000ff
    public let blueviolet: Int32 = 0x8a2be2
    public let brown: Int32 = 0xa52a2a
    public let burlywood: Int32 = 0xdeb887
    public let cadetblue: Int32 = 0x5f9ea0
    public let chartreuse: Int32 = 0x7fff00
    public let chocolate: Int32 = 0xd2691e
    public let coral: Int32 = 0xff7f50
    public let cornflowerblue: Int32 = 0x6495ed
    public let cornsilk: Int32 = 0xfff8dc
    public let crimson: Int32 = 0xdc143c
    public let cyan: Int32 = 0x00ffff
    public let darkblue: Int32 = 0x00008b
    public let darkcyan: Int32 = 0x008b8b
    public let darkgoldenrod: Int32 = 0xb8860b
    public let darkgray: Int32 = 0xa9a9a9
    public let darkgreen: Int32 = 0x006400
    public let darkgrey: Int32 = 0xa9a9a9
    public let darkkhaki: Int32 = 0xbdb76b
    public let darkmagenta: Int32 = 0x8b008b
    public let darkolivegreen: Int32 = 0x556b2f
    public let darkorange: Int32 = 0xff8c00
    public let darkorchid: Int32 = 0x9932cc
    public let darkred: Int32 = 0x8b0000
    public let darksalmon: Int32 = 0xe9967a
    public let darkseagreen: Int32 = 0x8fbc8f
    public let darkslateblue: Int32 = 0x483d8b
    public let darkslategray: Int32 = 0x2f4f4f
    public let darkslategrey: Int32 = 0x2f4f4f
    public let darkturquoise: Int32 = 0x00ced1
    public let darkviolet: Int32 = 0x9400d3
    public let deeppink: Int32 = 0xff1493
    public let deepskyblue: Int32 = 0x00bfff
    public let dimgray: Int32 = 0x696969
    public let dimgrey: Int32 = 0x696969
    public let dodgerblue: Int32 = 0x1e90ff
    public let firebrick: Int32 = 0xb22222
    public let floralwhite: Int32 = 0xfffaf0
    public let forestgreen: Int32 = 0x228b22
    public let fuchsia: Int32 = 0xff00ff
    public let gainsboro: Int32 = 0xdcdcdc
    public let ghostwhite: Int32 = 0xf8f8ff
    public let gold: Int32 = 0xffd700
    public let goldenrod: Int32 = 0xdaa520
    public let gray: Int32 = 0x808080
    public let green: Int32 = 0x008000
    public let greenyellow: Int32 = 0xadff2f
    public let grey: Int32 = 0x808080
    public let honeydew: Int32 = 0xf0fff0
    public let hotpink: Int32 = 0xff69b4
    public let indianred: Int32 = 0xcd5c5c
    public let indigo: Int32 = 0x4b0082
    public let ivory: Int32 = 0xfffff0
    public let khaki: Int32 = 0xf0e68c
    public let lavender: Int32 = 0xe6e6fa
    public let lavenderblush: Int32 = 0xfff0f5
    public let lawngreen: Int32 = 0x7cfc00
    public let lemonchiffon: Int32 = 0xfffacd
    public let lightblue: Int32 = 0xadd8e6
    public let lightcoral: Int32 = 0xf08080
    public let lightcyan: Int32 = 0xe0ffff
    public let lightgoldenrodyellow: Int32 = 0xfafad2
    public let lightgray: Int32 = 0xd3d3d3
    public let lightgreen: Int32 = 0x90ee90
    public let lightgrey: Int32 = 0xd3d3d3
    public let lightpink: Int32 = 0xffb6c1
    public let lightsalmon: Int32 = 0xffa07a
    public let lightseagreen: Int32 = 0x20b2aa
    public let lightskyblue: Int32 = 0x87cefa
    public let lightslategray: Int32 = 0x778899
    public let lightslategrey: Int32 = 0x778899
    public let lightsteelblue: Int32 = 0xb0c4de
    public let lightyellow: Int32 = 0xffffe0
    public let lime: Int32 = 0x00ff00
    public let limegreen: Int32 = 0x32cd32
    public let linen: Int32 = 0xfaf0e6
    public let magenta: Int32 = 0xff00ff
    public let maroon: Int32 = 0x800000
    public let mediumaquamarine: Int32 = 0x66cdaa
    public let mediumblue: Int32 = 0x0000cd
    public let mediumorchid: Int32 = 0xba55d3
    public let mediumpurple: Int32 = 0x9370db
    public let mediumseagreen: Int32 = 0x3cb371
    public let mediumslateblue: Int32 = 0x7b68ee
    public let mediumspringgreen: Int32 = 0x00fa9a
    public let mediumturquoise: Int32 = 0x48d1cc
    public let mediumvioletred: Int32 = 0xc71585
    public let midnightblue: Int32 = 0x191970
    public let mintcream: Int32 = 0xf5fffa
    public let mistyrose: Int32 = 0xffe4e1
    public let moccasin: Int32 = 0xffe4b5
    public let navajowhite: Int32 = 0xffdead
    public let navy: Int32 = 0x000080
    public let oldlace: Int32 = 0xfdf5e6
    public let olive: Int32 = 0x808000
    public let olivedrab: Int32 = 0x6b8e23
    public let orange: Int32 = 0xffa500
    public let orangered: Int32 = 0xff4500
    public let orchid: Int32 = 0xda70d6
    public let palegoldenrod: Int32 = 0xeee8aa
    public let palegreen: Int32 = 0x98fb98
    public let paleturquoise: Int32 = 0xafeeee
    public let palevioletred: Int32 = 0xdb7093
    public let papayawhip: Int32 = 0xffefd5
    public let peachpuff: Int32 = 0xffdab9
    public let peru: Int32 = 0xcd853f
    public let pink: Int32 = 0xffc0cb
    public let plum: Int32 = 0xdda0dd
    public let powderblue: Int32 = 0xb0e0e6
    public let purple: Int32 = 0x800080
    public let red: Int32 = 0xff0000
    public let rosybrown: Int32 = 0xbc8f8f
    public let royalblue: Int32 = 0x4169e1
    public let saddlebrown: Int32 = 0x8b4513
    public let salmon: Int32 = 0xfa8072
    public let sandybrown: Int32 = 0xf4a460
    public let seagreen: Int32 = 0x2e8b57
    public let seashell: Int32 = 0xfff5ee
    public let sienna: Int32 = 0xa0522d
    public let silver: Int32 = 0xc0c0c0
    public let skyblue: Int32 = 0x87ceeb
    public let slateblue: Int32 = 0x6a5acd
    public let slategray: Int32 = 0x708090
    public let slategrey: Int32 = 0x708090
    public let snow: Int32 = 0xfffafa
    public let springgreen: Int32 = 0x00ff7f
    public let steelblue: Int32 = 0x4682b4
    public let tan: Int32 = 0xd2b48c
    public let teal: Int32 = 0x008080
    public let thistle: Int32 = 0xd8bfd8
    public let tomato: Int32 = 0xff6347
    public let turquoise: Int32 = 0x40e0d0
    public let violet: Int32 = 0xee82ee
    public let wheat: Int32 = 0xf5deb3
    public let white: Int32 = 0xffffff
    public let whitesmoke: Int32 = 0xf5f5f5
    public let yellow: Int32 = 0xffff00
    public let yellowgreen: Int32 = 0x9acd32

    public let oldgloryred: Int32 = 0xb22234
    public let oldgloryblue: Int32 = 0x3c3b6e
}
