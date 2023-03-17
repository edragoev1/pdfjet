package pdfjet

type ColorCSS struct {
	transparent          uint32
	aliceblue            uint32
	antiquewhite         uint32
	aqua                 uint32
	aquamarine           uint32
	azure                uint32
	beige                uint32
	bisque               uint32
	black                uint32
	blanchedalmond       uint32
	blue                 uint32
	blueviolet           uint32
	brown                uint32
	burlywood            uint32
	cadetblue            uint32
	chartreuse           uint32
	chocolate            uint32
	coral                uint32
	cornflowerblue       uint32
	cornsilk             uint32
	crimson              uint32
	cyan                 uint32
	darkblue             uint32
	darkcyan             uint32
	darkgoldenrod        uint32
	darkgray             uint32
	darkgreen            uint32
	darkgrey             uint32
	darkkhaki            uint32
	darkmagenta          uint32
	darkolivegreen       uint32
	darkorange           uint32
	darkorchid           uint32
	darkred              uint32
	darksalmon           uint32
	darkseagreen         uint32
	darkslateblue        uint32
	darkslategray        uint32
	darkslategrey        uint32
	darkturquoise        uint32
	darkviolet           uint32
	deeppink             uint32
	deepskyblue          uint32
	dimgray              uint32
	dimgrey              uint32
	dodgerblue           uint32
	firebrick            uint32
	floralwhite          uint32
	forestgreen          uint32
	fuchsia              uint32
	gainsboro            uint32
	ghostwhite           uint32
	gold                 uint32
	goldenrod            uint32
	gray                 uint32
	green                uint32
	greenyellow          uint32
	grey                 uint32
	honeydew             uint32
	hotpink              uint32
	indianred            uint32
	indigo               uint32
	ivory                uint32
	khaki                uint32
	lavender             uint32
	lavenderblush        uint32
	lawngreen            uint32
	lemonchiffon         uint32
	lightblue            uint32
	lightcoral           uint32
	lightcyan            uint32
	lightgoldenrodyellow uint32
	lightgray            uint32
	lightgreen           uint32
	lightgrey            uint32
	lightpink            uint32
	lightsalmon          uint32
	lightseagreen        uint32
	lightskyblue         uint32
	lightslategray       uint32
	lightslategrey       uint32
	lightsteelblue       uint32
	lightyellow          uint32
	lime                 uint32
	limegreen            uint32
	linen                uint32
	magenta              uint32
	maroon               uint32
	mediumaquamarine     uint32
	mediumblue           uint32
	mediumorchid         uint32
	mediumpurple         uint32
	mediumseagreen       uint32
	mediumslateblue      uint32
	mediumspringgreen    uint32
	mediumturquoise      uint32
	mediumvioletred      uint32
	midnightblue         uint32
	mintcream            uint32
	mistyrose            uint32
	moccasin             uint32
	navajowhite          uint32
	navy                 uint32
	oldlace              uint32
	olive                uint32
	olivedrab            uint32
	orange               uint32
	orangered            uint32
	orchid               uint32
	palegoldenrod        uint32
	palegreen            uint32
	paleturquoise        uint32
	palevioletred        uint32
	papayawhip           uint32
	peachpuff            uint32
	peru                 uint32
	pink                 uint32
	plum                 uint32
	powderblue           uint32
	purple               uint32
	red                  uint32
	rosybrown            uint32
	royalblue            uint32
	saddlebrown          uint32
	salmon               uint32
	sandybrown           uint32
	seagreen             uint32
	seashell             uint32
	sienna               uint32
	silver               uint32
	skyblue              uint32
	slateblue            uint32
	slategray            uint32
	slategrey            uint32
	snow                 uint32
	springgreen          uint32
	steelblue            uint32
	tan                  uint32
	teal                 uint32
	thistle              uint32
	tomato               uint32
	turquoise            uint32
	violet               uint32
	wheat                uint32
	white                uint32
	whitesmoke           uint32
	yellow               uint32
	yellowgreen          uint32
	oldgloryred          uint32
	oldgloryblue         uint32
}

func NewColorCSS() *ColorCSS {
	color := new(ColorCSS)
	color.transparent = 0x80000000
	color.aliceblue = 0xf0f8ff
	color.antiquewhite = 0xfaebd7
	color.aqua = 0x00ffff
	color.aquamarine = 0x7fffd4
	color.azure = 0xf0ffff
	color.beige = 0xf5f5dc
	color.bisque = 0xffe4c4
	color.black = 0x000000
	color.blanchedalmond = 0xffebcd
	color.blue = 0x0000ff
	color.blueviolet = 0x8a2be2
	color.brown = 0xa52a2a
	color.burlywood = 0xdeb887
	color.cadetblue = 0x5f9ea0
	color.chartreuse = 0x7fff00
	color.chocolate = 0xd2691e
	color.coral = 0xff7f50
	color.cornflowerblue = 0x6495ed
	color.cornsilk = 0xfff8dc
	color.crimson = 0xdc143c
	color.cyan = 0x00ffff
	color.darkblue = 0x00008b
	color.darkcyan = 0x008b8b
	color.darkgoldenrod = 0xb8860b
	color.darkgray = 0xa9a9a9
	color.darkgreen = 0x006400
	color.darkgrey = 0xa9a9a9
	color.darkkhaki = 0xbdb76b
	color.darkmagenta = 0x8b008b
	color.darkolivegreen = 0x556b2f
	color.darkorange = 0xff8c00
	color.darkorchid = 0x9932cc
	color.darkred = 0x8b0000
	color.darksalmon = 0xe9967a
	color.darkseagreen = 0x8fbc8f
	color.darkslateblue = 0x483d8b
	color.darkslategray = 0x2f4f4f
	color.darkslategrey = 0x2f4f4f
	color.darkturquoise = 0x00ced1
	color.darkviolet = 0x9400d3
	color.deeppink = 0xff1493
	color.deepskyblue = 0x00bfff
	color.dimgray = 0x696969
	color.dimgrey = 0x696969
	color.dodgerblue = 0x1e90ff
	color.firebrick = 0xb22222
	color.floralwhite = 0xfffaf0
	color.forestgreen = 0x228b22
	color.fuchsia = 0xff00ff
	color.gainsboro = 0xdcdcdc
	color.ghostwhite = 0xf8f8ff
	color.gold = 0xffd700
	color.goldenrod = 0xdaa520
	color.gray = 0x808080
	color.green = 0x008000
	color.greenyellow = 0xadff2f
	color.grey = 0x808080
	color.honeydew = 0xf0fff0
	color.hotpink = 0xff69b4
	color.indianred = 0xcd5c5c
	color.indigo = 0x4b0082
	color.ivory = 0xfffff0
	color.khaki = 0xf0e68c
	color.lavender = 0xe6e6fa
	color.lavenderblush = 0xfff0f5
	color.lawngreen = 0x7cfc00
	color.lemonchiffon = 0xfffacd
	color.lightblue = 0xadd8e6
	color.lightcoral = 0xf08080
	color.lightcyan = 0xe0ffff
	color.lightgoldenrodyellow = 0xfafad2
	color.lightgray = 0xd3d3d3
	color.lightgreen = 0x90ee90
	color.lightgrey = 0xd3d3d3
	color.lightpink = 0xffb6c1
	color.lightsalmon = 0xffa07a
	color.lightseagreen = 0x20b2aa
	color.lightskyblue = 0x87cefa
	color.lightslategray = 0x778899
	color.lightslategrey = 0x778899
	color.lightsteelblue = 0xb0c4de
	color.lightyellow = 0xffffe0
	color.lime = 0x00ff00
	color.limegreen = 0x32cd32
	color.linen = 0xfaf0e6
	color.magenta = 0xff00ff
	color.maroon = 0x800000
	color.mediumaquamarine = 0x66cdaa
	color.mediumblue = 0x0000cd
	color.mediumorchid = 0xba55d3
	color.mediumpurple = 0x9370db
	color.mediumseagreen = 0x3cb371
	color.mediumslateblue = 0x7b68ee
	color.mediumspringgreen = 0x00fa9a
	color.mediumturquoise = 0x48d1cc
	color.mediumvioletred = 0xc71585
	color.midnightblue = 0x191970
	color.mintcream = 0xf5fffa
	color.mistyrose = 0xffe4e1
	color.moccasin = 0xffe4b5
	color.navajowhite = 0xffdead
	color.navy = 0x000080
	color.oldlace = 0xfdf5e6
	color.olive = 0x808000
	color.olivedrab = 0x6b8e23
	color.orange = 0xffa500
	color.orangered = 0xff4500
	color.orchid = 0xda70d6
	color.palegoldenrod = 0xeee8aa
	color.palegreen = 0x98fb98
	color.paleturquoise = 0xafeeee
	color.palevioletred = 0xdb7093
	color.papayawhip = 0xffefd5
	color.peachpuff = 0xffdab9
	color.peru = 0xcd853f
	color.pink = 0xffc0cb
	color.plum = 0xdda0dd
	color.powderblue = 0xb0e0e6
	color.purple = 0x800080
	color.red = 0xff0000
	color.rosybrown = 0xbc8f8f
	color.royalblue = 0x4169e1
	color.saddlebrown = 0x8b4513
	color.salmon = 0xfa8072
	color.sandybrown = 0xf4a460
	color.seagreen = 0x2e8b57
	color.seashell = 0xfff5ee
	color.sienna = 0xa0522d
	color.silver = 0xc0c0c0
	color.skyblue = 0x87ceeb
	color.slateblue = 0x6a5acd
	color.slategray = 0x708090
	color.slategrey = 0x708090
	color.snow = 0xfffafa
	color.springgreen = 0x00ff7f
	color.steelblue = 0x4682b4
	color.tan = 0xd2b48c
	color.teal = 0x008080
	color.thistle = 0xd8bfd8
	color.tomato = 0xff6347
	color.turquoise = 0x40e0d0
	color.violet = 0xee82ee
	color.wheat = 0xf5deb3
	color.white = 0xffffff
	color.whitesmoke = 0xf5f5f5
	color.yellow = 0xffff00
	color.yellowgreen = 0x9acd32
	color.oldgloryred = 0xb22234
	color.oldgloryblue = 0x3c3b6e
	return color
}
