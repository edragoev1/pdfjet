/**
 * ColorMap.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/**
 * Used to specify the pen and brush colors.
 * @see <a href="http://www.w3.org/TR/css3-color/#svg-color">http://www.w3.org/TR/css3-color/#svg-color</a>
 *
 * NOTE: We need this class in addition to Color.swift because the Swift reflection does not work with static variables.
 */
public class ColorMap {
    /// The color named "transparent"
    public let transparent: Int32 = -1
    /// The color named "none"
    public let none: Int32 = -1

    /// The color named "aliceblue"
    public let aliceblue: Int32 = 0xf0f8ff
    /// The color named "antiquewhite"
    public let antiquewhite: Int32 = 0xfaebd7
    /// The color named "aqua"
    public let aqua: Int32 = 0x00ffff
    /// The color named "aquamarine"
    public let aquamarine: Int32 = 0x7fffd4
    /// The color named "azure"
    public let azure: Int32 = 0xf0ffff
    /// The color named "beige"
    public let beige: Int32 = 0xf5f5dc
    /// The color named "bisque"
    public let bisque: Int32 = 0xffe4c4
    /// The color named "black"
    public let black: Int32 = 0x000000
    /// The color named "blanchedalmond"
    public let blanchedalmond: Int32 = 0xffebcd
    /// The color named "blue"
    public let blue: Int32 = 0x0000ff
    /// The color named "blueviolet"
    public let blueviolet: Int32 = 0x8a2be2
    /// The color named "brown"
    public let brown: Int32 = 0xa52a2a
    /// The color named "burlywood"
    public let burlywood: Int32 = 0xdeb887
    /// The color named "cadetblue"
    public let cadetblue: Int32 = 0x5f9ea0
    /// The color named "chartreuse"
    public let chartreuse: Int32 = 0x7fff00
    /// The color named "chocolate"
    public let chocolate: Int32 = 0xd2691e
    /// The color named "coral"
    public let coral: Int32 = 0xff7f50
    /// The color named "cornflowerblue"
    public let cornflowerblue: Int32 = 0x6495ed
    /// The color named "cornsilk"
    public let cornsilk: Int32 = 0xfff8dc
    /// The color named "crimson"
    public let crimson: Int32 = 0xdc143c
    /// The color named "cyan"
    public let cyan: Int32 = 0x00ffff
    /// The color named "darkblue"
    public let darkblue: Int32 = 0x00008b
    /// The color named "darkcyan"
    public let darkcyan: Int32 = 0x008b8b
    /// The color named "darkgoldenrod"
    public let darkgoldenrod: Int32 = 0xb8860b
    /// The color named "darkgray"
    public let darkgray: Int32 = 0xa9a9a9
    /// The color named "darkgreen"
    public let darkgreen: Int32 = 0x006400
    /// The color named "darkgrey"
    public let darkgrey: Int32 = 0xa9a9a9
    /// The color named "darkkhaki"
    public let darkkhaki: Int32 = 0xbdb76b
    /// The color named "darkmagenta"
    public let darkmagenta: Int32 = 0x8b008b
    /// The color named "darkolivegreen"
    public let darkolivegreen: Int32 = 0x556b2f
    /// The color named "darkorange"
    public let darkorange: Int32 = 0xff8c00
    /// The color named "darkorchid"
    public let darkorchid: Int32 = 0x9932cc
    /// The color named "darkred"
    public let darkred: Int32 = 0x8b0000
    /// The color named "darksalmon"
    public let darksalmon: Int32 = 0xe9967a
    /// The color named "darkseagreen"
    public let darkseagreen: Int32 = 0x8fbc8f
    /// The color named "darkslateblue"
    public let darkslateblue: Int32 = 0x483d8b
    /// The color named "darkslategray"
    public let darkslategray: Int32 = 0x2f4f4f
    /// The color named "darkslategrey"
    public let darkslategrey: Int32 = 0x2f4f4f
    /// The color named "darkturquoise"
    public let darkturquoise: Int32 = 0x00ced1
    /// The color named "darkviolet"
    public let darkviolet: Int32 = 0x9400d3
    /// The color named "deeppink"
    public let deeppink: Int32 = 0xff1493
    /// The color named "deepskyblue"
    public let deepskyblue: Int32 = 0x00bfff
    /// The color named "dimgray"
    public let dimgray: Int32 = 0x696969
    /// The color named "dimgrey"
    public let dimgrey: Int32 = 0x696969
    /// The color named "dodgerblue"
    public let dodgerblue: Int32 = 0x1e90ff
    /// The color named "firebrick"
    public let firebrick: Int32 = 0xb22222
    /// The color named "floralwhite"
    public let floralwhite: Int32 = 0xfffaf0
    /// The color named "forestgreen"
    public let forestgreen: Int32 = 0x228b22
    /// The color named "fuchsia"
    public let fuchsia: Int32 = 0xff00ff
    /// The color named "gainsboro"
    public let gainsboro: Int32 = 0xdcdcdc
    /// The color named "ghostwhite"
    public let ghostwhite: Int32 = 0xf8f8ff
    /// The color named "gold"
    public let gold: Int32 = 0xffd700
    /// The color named "goldenrod"
    public let goldenrod: Int32 = 0xdaa520
    /// The color named "gray"
    public let gray: Int32 = 0x808080
    /// The color named "green"
    public let green: Int32 = 0x008000
    /// The color named "greenyellow"
    public let greenyellow: Int32 = 0xadff2f
    /// The color named "grey"
    public let grey: Int32 = 0x808080
    /// The color named "honeydew"
    public let honeydew: Int32 = 0xf0fff0
    /// The color named "hotpink"
    public let hotpink: Int32 = 0xff69b4
    /// The color named "indianred"
    public let indianred: Int32 = 0xcd5c5c
    /// The color named "indigo"
    public let indigo: Int32 = 0x4b0082
    /// The color named "ivory"
    public let ivory: Int32 = 0xfffff0
    /// The color named "khaki"
    public let khaki: Int32 = 0xf0e68c
    /// The color named "lavender"
    public let lavender: Int32 = 0xe6e6fa
    /// The color named "lavenderblush"
    public let lavenderblush: Int32 = 0xfff0f5
    /// The color named "lawngreen"
    public let lawngreen: Int32 = 0x7cfc00
    /// The color named "lemonchiffon"
    public let lemonchiffon: Int32 = 0xfffacd
    /// The color named "lightblue"
    public let lightblue: Int32 = 0xadd8e6
    /// The color named "lightcoral"
    public let lightcoral: Int32 = 0xf08080
    /// The color named "lightcyan"
    public let lightcyan: Int32 = 0xe0ffff
    /// The color named "lightgoldenrodyellow"
    public let lightgoldenrodyellow: Int32 = 0xfafad2
    /// The color named "lightgray"
    public let lightgray: Int32 = 0xd3d3d3
    /// The color named "lightgreen"
    public let lightgreen: Int32 = 0x90ee90
    /// The color named "lightgrey"
    public let lightgrey: Int32 = 0xd3d3d3
    /// The color named "lightpink"
    public let lightpink: Int32 = 0xffb6c1
    /// The color named "lightsalmon"
    public let lightsalmon: Int32 = 0xffa07a
    /// The color named "lightseagreen"
    public let lightseagreen: Int32 = 0x20b2aa
    /// The color named "lightskyblue"
    public let lightskyblue: Int32 = 0x87cefa
    /// The color named "lightslategray"
    public let lightslategray: Int32 = 0x778899
    /// The color named "lightslategrey"
    public let lightslategrey: Int32 = 0x778899
    /// The color named "lightsteelblue"
    public let lightsteelblue: Int32 = 0xb0c4de
    /// The color named "lightyellow"
    public let lightyellow: Int32 = 0xffffe0
    /// The color named "lime"
    public let lime: Int32 = 0x00ff00
    /// The color named "limegreen"
    public let limegreen: Int32 = 0x32cd32
    /// The color named "linen"
    public let linen: Int32 = 0xfaf0e6
    /// The color named "magenta"
    public let magenta: Int32 = 0xff00ff
    /// The color named "maroon"
    public let maroon: Int32 = 0x800000
    /// The color named "mediumaquamarine"
    public let mediumaquamarine: Int32 = 0x66cdaa
    /// The color named "mediumblue"
    public let mediumblue: Int32 = 0x0000cd
    /// The color named "mediumorchid"
    public let mediumorchid: Int32 = 0xba55d3
    /// The color named "mediumpurple"
    public let mediumpurple: Int32 = 0x9370db
    /// The color named "mediumseagreen"
    public let mediumseagreen: Int32 = 0x3cb371
    /// The color named "mediumslateblue"
    public let mediumslateblue: Int32 = 0x7b68ee
    /// The color named "mediumspringgreen"
    public let mediumspringgreen: Int32 = 0x00fa9a
    /// The color named "mediumturquoise"
    public let mediumturquoise: Int32 = 0x48d1cc
    /// The color named "mediumvioletred"
    public let mediumvioletred: Int32 = 0xc71585
    /// The color named "midnightblue"
    public let midnightblue: Int32 = 0x191970
    /// The color named "mintcream"
    public let mintcream: Int32 = 0xf5fffa
    /// The color named "mistyrose"
    public let mistyrose: Int32 = 0xffe4e1
    /// The color named "moccasin"
    public let moccasin: Int32 = 0xffe4b5
    /// The color named "navajowhite"
    public let navajowhite: Int32 = 0xffdead
    /// The color named "navy"
    public let navy: Int32 = 0x000080
    /// The color named "oldlace"
    public let oldlace: Int32 = 0xfdf5e6
    /// The color named "olive"
    public let olive: Int32 = 0x808000
    /// The color named "olivedrab"
    public let olivedrab: Int32 = 0x6b8e23
    /// The color named "orange"
    public let orange: Int32 = 0xffa500
    /// The color named "orangered"
    public let orangered: Int32 = 0xff4500
    /// The color named "orchid"
    public let orchid: Int32 = 0xda70d6
    /// The color named "palegoldenrod"
    public let palegoldenrod: Int32 = 0xeee8aa
    /// The color named "palegreen"
    public let palegreen: Int32 = 0x98fb98
    /// The color named "paleturquoise"
    public let paleturquoise: Int32 = 0xafeeee
    /// The color named "palevioletred"
    public let palevioletred: Int32 = 0xdb7093
    /// The color named "papayawhip"
    public let papayawhip: Int32 = 0xffefd5
    /// The color named "peachpuff"
    public let peachpuff: Int32 = 0xffdab9
    /// The color named "peru"
    public let peru: Int32 = 0xcd853f
    /// The color named "pink"
    public let pink: Int32 = 0xffc0cb
    /// The color named "plum"
    public let plum: Int32 = 0xdda0dd
    /// The color named "powderblue"
    public let powderblue: Int32 = 0xb0e0e6
    /// The color named "purple"
    public let purple: Int32 = 0x800080
    /// The color named "red"
    public let red: Int32 = 0xff0000
    /// The color named "rosybrown"
    public let rosybrown: Int32 = 0xbc8f8f
    /// The color named "royalblue"
    public let royalblue: Int32 = 0x4169e1
    /// The color named "saddlebrown"
    public let saddlebrown: Int32 = 0x8b4513
    /// The color named "salmon"
    public let salmon: Int32 = 0xfa8072
    /// The color named "sandybrown"
    public let sandybrown: Int32 = 0xf4a460
    /// The color named "seagreen"
    public let seagreen: Int32 = 0x2e8b57
    /// The color named "seashell"
    public let seashell: Int32 = 0xfff5ee
    /// The color named "sienna"
    public let sienna: Int32 = 0xa0522d
    /// The color named "silver"
    public let silver: Int32 = 0xc0c0c0
    /// The color named "skyblue"
    public let skyblue: Int32 = 0x87ceeb
    /// The color named "slateblue"
    public let slateblue: Int32 = 0x6a5acd
    /// The color named "slategray"
    public let slategray: Int32 = 0x708090
    /// The color named "slategrey"
    public let slategrey: Int32 = 0x708090
    /// The color named "snow"
    public let snow: Int32 = 0xfffafa
    /// The color named "springgreen"
    public let springgreen: Int32 = 0x00ff7f
    /// The color named "steelblue"
    public let steelblue: Int32 = 0x4682b4
    /// The color named "tan"
    public let tan: Int32 = 0xd2b48c
    /// The color named "teal"
    public let teal: Int32 = 0x008080
    /// The color named "thistle"
    public let thistle: Int32 = 0xd8bfd8
    /// The color named "tomato"
    public let tomato: Int32 = 0xff6347
    /// The color named "turquoise"
    public let turquoise: Int32 = 0x40e0d0
    /// The color named "violet"
    public let violet: Int32 = 0xee82ee
    /// The color named "wheat"
    public let wheat: Int32 = 0xf5deb3
    /// The color named "white"
    public let white: Int32 = 0xffffff
    /// The color named "whitesmoke"
    public let whitesmoke: Int32 = 0xf5f5f5
    /// The color named "yellow"
    public let yellow: Int32 = 0xffff00
    /// The color named "yellowgreen"
    public let yellowgreen: Int32 = 0x9acd32

    /// The color named "oldgloryred"
    public let oldgloryred: Int32 = 0xb22234
    /// The color named "oldgloryblue"
    public let oldgloryblue: Int32 = 0x3c3b6e
}
