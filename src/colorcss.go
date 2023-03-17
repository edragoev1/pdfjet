package pdfjet

/**
 * colorcss.go
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

func NewColorCSS() map[string]uint32 {
	color := make(map[string]uint32)
	color["aliceblue"] = 0xf0f8ff
	color["transparent"] = 0x80000000
	color["aliceblue"] = 0xf0f8ff
	color["antiquewhite"] = 0xfaebd7
	color["aqua"] = 0x00ffff
	color["aquamarine"] = 0x7fffd4
	color["azure"] = 0xf0ffff
	color["beige"] = 0xf5f5dc
	color["bisque"] = 0xffe4c4
	color["black"] = 0x000000
	color["blanchedalmond"] = 0xffebcd
	color["blue"] = 0x0000ff
	color["blueviolet"] = 0x8a2be2
	color["brown"] = 0xa52a2a
	color["burlywood"] = 0xdeb887
	color["cadetblue"] = 0x5f9ea0
	color["chartreuse"] = 0x7fff00
	color["chocolate"] = 0xd2691e
	color["coral"] = 0xff7f50
	color["cornflowerblue"] = 0x6495ed
	color["cornsilk"] = 0xfff8dc
	color["crimson"] = 0xdc143c
	color["cyan"] = 0x00ffff
	color["darkblue"] = 0x00008b
	color["darkcyan"] = 0x008b8b
	color["darkgoldenrod"] = 0xb8860b
	color["darkgray"] = 0xa9a9a9
	color["darkgreen"] = 0x006400
	color["darkgrey"] = 0xa9a9a9
	color["darkkhaki"] = 0xbdb76b
	color["darkmagenta"] = 0x8b008b
	color["darkolivegreen"] = 0x556b2f
	color["darkorange"] = 0xff8c00
	color["darkorchid"] = 0x9932cc
	color["darkred"] = 0x8b0000
	color["darksalmon"] = 0xe9967a
	color["darkseagreen"] = 0x8fbc8f
	color["darkslateblue"] = 0x483d8b
	color["darkslategray"] = 0x2f4f4f
	color["darkslategrey"] = 0x2f4f4f
	color["darkturquoise"] = 0x00ced1
	color["darkviolet"] = 0x9400d3
	color["deeppink"] = 0xff1493
	color["deepskyblue"] = 0x00bfff
	color["dimgray"] = 0x696969
	color["dimgrey"] = 0x696969
	color["dodgerblue"] = 0x1e90ff
	color["firebrick"] = 0xb22222
	color["floralwhite"] = 0xfffaf0
	color["forestgreen"] = 0x228b22
	color["fuchsia"] = 0xff00ff
	color["gainsboro"] = 0xdcdcdc
	color["ghostwhite"] = 0xf8f8ff
	color["gold"] = 0xffd700
	color["goldenrod"] = 0xdaa520
	color["gray"] = 0x808080
	color["green"] = 0x008000
	color["greenyellow"] = 0xadff2f
	color["grey"] = 0x808080
	color["honeydew"] = 0xf0fff0
	color["hotpink"] = 0xff69b4
	color["indianred"] = 0xcd5c5c
	color["indigo"] = 0x4b0082
	color["ivory"] = 0xfffff0
	color["khaki"] = 0xf0e68c
	color["lavender"] = 0xe6e6fa
	color["lavenderblush"] = 0xfff0f5
	color["lawngreen"] = 0x7cfc00
	color["lemonchiffon"] = 0xfffacd
	color["lightblue"] = 0xadd8e6
	color["lightcoral"] = 0xf08080
	color["lightcyan"] = 0xe0ffff
	color["lightgoldenrodyellow"] = 0xfafad2
	color["lightgray"] = 0xd3d3d3
	color["lightgreen"] = 0x90ee90
	color["lightgrey"] = 0xd3d3d3
	color["lightpink"] = 0xffb6c1
	color["lightsalmon"] = 0xffa07a
	color["lightseagreen"] = 0x20b2aa
	color["lightskyblue"] = 0x87cefa
	color["lightslategray"] = 0x778899
	color["lightslategrey"] = 0x778899
	color["lightsteelblue"] = 0xb0c4de
	color["lightyellow"] = 0xffffe0
	color["lime"] = 0x00ff00
	color["limegreen"] = 0x32cd32
	color["linen"] = 0xfaf0e6
	color["magenta"] = 0xff00ff
	color["maroon"] = 0x800000
	color["mediumaquamarine"] = 0x66cdaa
	color["mediumblue"] = 0x0000cd
	color["mediumorchid"] = 0xba55d3
	color["mediumpurple"] = 0x9370db
	color["mediumseagreen"] = 0x3cb371
	color["mediumslateblue"] = 0x7b68ee
	color["mediumspringgreen"] = 0x00fa9a
	color["mediumturquoise"] = 0x48d1cc
	color["mediumvioletred"] = 0xc71585
	color["midnightblue"] = 0x191970
	color["mintcream"] = 0xf5fffa
	color["mistyrose"] = 0xffe4e1
	color["moccasin"] = 0xffe4b5
	color["navajowhite"] = 0xffdead
	color["navy"] = 0x000080
	color["oldlace"] = 0xfdf5e6
	color["olive"] = 0x808000
	color["olivedrab"] = 0x6b8e23
	color["orange"] = 0xffa500
	color["orangered"] = 0xff4500
	color["orchid"] = 0xda70d6
	color["palegoldenrod"] = 0xeee8aa
	color["palegreen"] = 0x98fb98
	color["paleturquoise"] = 0xafeeee
	color["palevioletred"] = 0xdb7093
	color["papayawhip"] = 0xffefd5
	color["peachpuff"] = 0xffdab9
	color["peru"] = 0xcd853f
	color["pink"] = 0xffc0cb
	color["plum"] = 0xdda0dd
	color["powderblue"] = 0xb0e0e6
	color["purple"] = 0x800080
	color["red"] = 0xff0000
	color["rosybrown"] = 0xbc8f8f
	color["royalblue"] = 0x4169e1
	color["saddlebrown"] = 0x8b4513
	color["salmon"] = 0xfa8072
	color["sandybrown"] = 0xf4a460
	color["seagreen"] = 0x2e8b57
	color["seashell"] = 0xfff5ee
	color["sienna"] = 0xa0522d
	color["silver"] = 0xc0c0c0
	color["skyblue"] = 0x87ceeb
	color["slateblue"] = 0x6a5acd
	color["slategray"] = 0x708090
	color["slategrey"] = 0x708090
	color["snow"] = 0xfffafa
	color["springgreen"] = 0x00ff7f
	color["steelblue"] = 0x4682b4
	color["tan"] = 0xd2b48c
	color["teal"] = 0x008080
	color["thistle"] = 0xd8bfd8
	color["tomato"] = 0xff6347
	color["turquoise"] = 0x40e0d0
	color["violet"] = 0xee82ee
	color["wheat"] = 0xf5deb3
	color["white"] = 0xffffff
	color["whitesmoke"] = 0xf5f5f5
	color["yellow"] = 0xffff00
	color["yellowgreen"] = 0x9acd32
	color["oldgloryred"] = 0xb22234
	color["oldgloryblue"] = 0x3c3b6e
	return color
}
