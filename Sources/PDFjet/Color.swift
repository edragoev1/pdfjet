/**
 *  Color.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


/**
 * Used to specify the pen and brush colors.
 * @see <a href="http://www.w3.org/TR/css3-color/#svg-color">http://www.w3.org/TR/css3-color/#svg-color</a>
 *
 */
public class Color {
    public static let transparent: UInt32 = 0x80000000

    public static let aliceblue: UInt32 = 0xf0f8ff
    public static let antiquewhite: UInt32 = 0xfaebd7
    public static let aqua: UInt32 = 0x00ffff
    public static let aquamarine: UInt32 = 0x7fffd4
    public static let azure: UInt32 = 0xf0ffff
    public static let beige: UInt32 = 0xf5f5dc
    public static let bisque: UInt32 = 0xffe4c4
    public static let black: UInt32 = 0x000000
    public static let blanchedalmond: UInt32 = 0xffebcd
    public static let blue: UInt32 = 0x0000ff
    public static let blueviolet: UInt32 = 0x8a2be2
    public static let brown: UInt32 = 0xa52a2a
    public static let burlywood: UInt32 = 0xdeb887
    public static let cadetblue: UInt32 = 0x5f9ea0
    public static let chartreuse: UInt32 = 0x7fff00
    public static let chocolate: UInt32 = 0xd2691e
    public static let coral: UInt32 = 0xff7f50
    public static let cornflowerblue: UInt32 = 0x6495ed
    public static let cornsilk: UInt32 = 0xfff8dc
    public static let crimson: UInt32 = 0xdc143c
    public static let cyan: UInt32 = 0x00ffff
    public static let darkblue: UInt32 = 0x00008b
    public static let darkcyan: UInt32 = 0x008b8b
    public static let darkgoldenrod: UInt32 = 0xb8860b
    public static let darkgray: UInt32 = 0xa9a9a9
    public static let darkgreen: UInt32 = 0x006400
    public static let darkgrey: UInt32 = 0xa9a9a9
    public static let darkkhaki: UInt32 = 0xbdb76b
    public static let darkmagenta: UInt32 = 0x8b008b
    public static let darkolivegreen: UInt32 = 0x556b2f
    public static let darkorange: UInt32 = 0xff8c00
    public static let darkorchid: UInt32 = 0x9932cc
    public static let darkred: UInt32 = 0x8b0000
    public static let darksalmon: UInt32 = 0xe9967a
    public static let darkseagreen: UInt32 = 0x8fbc8f
    public static let darkslateblue: UInt32 = 0x483d8b
    public static let darkslategray: UInt32 = 0x2f4f4f
    public static let darkslategrey: UInt32 = 0x2f4f4f
    public static let darkturquoise: UInt32 = 0x00ced1
    public static let darkviolet: UInt32 = 0x9400d3
    public static let deeppink: UInt32 = 0xff1493
    public static let deepskyblue: UInt32 = 0x00bfff
    public static let dimgray: UInt32 = 0x696969
    public static let dimgrey: UInt32 = 0x696969
    public static let dodgerblue: UInt32 = 0x1e90ff
    public static let firebrick: UInt32 = 0xb22222
    public static let floralwhite: UInt32 = 0xfffaf0
    public static let forestgreen: UInt32 = 0x228b22
    public static let fuchsia: UInt32 = 0xff00ff
    public static let gainsboro: UInt32 = 0xdcdcdc
    public static let ghostwhite: UInt32 = 0xf8f8ff
    public static let gold: UInt32 = 0xffd700
    public static let goldenrod: UInt32 = 0xdaa520
    public static let gray: UInt32 = 0x808080
    public static let green: UInt32 = 0x008000
    public static let greenyellow: UInt32 = 0xadff2f
    public static let grey: UInt32 = 0x808080
    public static let honeydew: UInt32 = 0xf0fff0
    public static let hotpink: UInt32 = 0xff69b4
    public static let indianred: UInt32 = 0xcd5c5c
    public static let indigo: UInt32 = 0x4b0082
    public static let ivory: UInt32 = 0xfffff0
    public static let khaki: UInt32 = 0xf0e68c
    public static let lavender: UInt32 = 0xe6e6fa
    public static let lavenderblush: UInt32 = 0xfff0f5
    public static let lawngreen: UInt32 = 0x7cfc00
    public static let lemonchiffon: UInt32 = 0xfffacd
    public static let lightblue: UInt32 = 0xadd8e6
    public static let lightcoral: UInt32 = 0xf08080
    public static let lightcyan: UInt32 = 0xe0ffff
    public static let lightgoldenrodyellow: UInt32 = 0xfafad2
    public static let lightgray: UInt32 = 0xd3d3d3
    public static let lightgreen: UInt32 = 0x90ee90
    public static let lightgrey: UInt32 = 0xd3d3d3
    public static let lightpink: UInt32 = 0xffb6c1
    public static let lightsalmon: UInt32 = 0xffa07a
    public static let lightseagreen: UInt32 = 0x20b2aa
    public static let lightskyblue: UInt32 = 0x87cefa
    public static let lightslategray: UInt32 = 0x778899
    public static let lightslategrey: UInt32 = 0x778899
    public static let lightsteelblue: UInt32 = 0xb0c4de
    public static let lightyellow: UInt32 = 0xffffe0
    public static let lime: UInt32 = 0x00ff00
    public static let limegreen: UInt32 = 0x32cd32
    public static let linen: UInt32 = 0xfaf0e6
    public static let magenta: UInt32 = 0xff00ff
    public static let maroon: UInt32 = 0x800000
    public static let mediumaquamarine: UInt32 = 0x66cdaa
    public static let mediumblue: UInt32 = 0x0000cd
    public static let mediumorchid: UInt32 = 0xba55d3
    public static let mediumpurple: UInt32 = 0x9370db
    public static let mediumseagreen: UInt32 = 0x3cb371
    public static let mediumslateblue: UInt32 = 0x7b68ee
    public static let mediumspringgreen: UInt32 = 0x00fa9a
    public static let mediumturquoise: UInt32 = 0x48d1cc
    public static let mediumvioletred: UInt32 = 0xc71585
    public static let midnightblue: UInt32 = 0x191970
    public static let mintcream: UInt32 = 0xf5fffa
    public static let mistyrose: UInt32 = 0xffe4e1
    public static let moccasin: UInt32 = 0xffe4b5
    public static let navajowhite: UInt32 = 0xffdead
    public static let navy: UInt32 = 0x000080
    public static let oldlace: UInt32 = 0xfdf5e6
    public static let olive: UInt32 = 0x808000
    public static let olivedrab: UInt32 = 0x6b8e23
    public static let orange: UInt32 = 0xffa500
    public static let orangered: UInt32 = 0xff4500
    public static let orchid: UInt32 = 0xda70d6
    public static let palegoldenrod: UInt32 = 0xeee8aa
    public static let palegreen: UInt32 = 0x98fb98
    public static let paleturquoise: UInt32 = 0xafeeee
    public static let palevioletred: UInt32 = 0xdb7093
    public static let papayawhip: UInt32 = 0xffefd5
    public static let peachpuff: UInt32 = 0xffdab9
    public static let peru: UInt32 = 0xcd853f
    public static let pink: UInt32 = 0xffc0cb
    public static let plum: UInt32 = 0xdda0dd
    public static let powderblue: UInt32 = 0xb0e0e6
    public static let purple: UInt32 = 0x800080
    public static let red: UInt32 = 0xff0000
    public static let rosybrown: UInt32 = 0xbc8f8f
    public static let royalblue: UInt32 = 0x4169e1
    public static let saddlebrown: UInt32 = 0x8b4513
    public static let salmon: UInt32 = 0xfa8072
    public static let sandybrown: UInt32 = 0xf4a460
    public static let seagreen: UInt32 = 0x2e8b57
    public static let seashell: UInt32 = 0xfff5ee
    public static let sienna: UInt32 = 0xa0522d
    public static let silver: UInt32 = 0xc0c0c0
    public static let skyblue: UInt32 = 0x87ceeb
    public static let slateblue: UInt32 = 0x6a5acd
    public static let slategray: UInt32 = 0x708090
    public static let slategrey: UInt32 = 0x708090
    public static let snow: UInt32 = 0xfffafa
    public static let springgreen: UInt32 = 0x00ff7f
    public static let steelblue: UInt32 = 0x4682b4
    public static let tan: UInt32 = 0xd2b48c
    public static let teal: UInt32 = 0x008080
    public static let thistle: UInt32 = 0xd8bfd8
    public static let tomato: UInt32 = 0xff6347
    public static let turquoise: UInt32 = 0x40e0d0
    public static let violet: UInt32 = 0xee82ee
    public static let wheat: UInt32 = 0xf5deb3
    public static let white: UInt32 = 0xffffff
    public static let whitesmoke: UInt32 = 0xf5f5f5
    public static let yellow: UInt32 = 0xffff00
    public static let yellowgreen: UInt32 = 0x9acd32

    public static let oldgloryred: UInt32 = 0xb22234
    public static let oldgloryblue: UInt32 = 0x3c3b6e
}
