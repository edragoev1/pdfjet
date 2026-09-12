// datamatrix.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package datamatrix creates Data Matrix barcodes.
package datamatrix

import (
	"log"
	"strconv"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
)

const (
	// Square selects the square symbols, from 10x10 to 144x144 modules.
	Square = 0
	// Rectangle selects the rectangular symbols, from 8x18 to 16x48 modules.
	// Data that does not fit in a rectangular symbol gets a square symbol.
	Rectangle = 1
)

// The symbols, smallest first: the rows and columns of modules, the rows and
// columns of modules in each data region, the data codewords, the error
// correction codewords and the number of Reed-Solomon blocks.
var squares = [][7]int{
	{10, 10, 8, 8, 3, 5, 1},
	{12, 12, 10, 10, 5, 7, 1},
	{14, 14, 12, 12, 8, 10, 1},
	{16, 16, 14, 14, 12, 12, 1},
	{18, 18, 16, 16, 18, 14, 1},
	{20, 20, 18, 18, 22, 18, 1},
	{22, 22, 20, 20, 30, 20, 1},
	{24, 24, 22, 22, 36, 24, 1},
	{26, 26, 24, 24, 44, 28, 1},
	{32, 32, 14, 14, 62, 36, 1},
	{36, 36, 16, 16, 86, 42, 1},
	{40, 40, 18, 18, 114, 48, 1},
	{44, 44, 20, 20, 144, 56, 1},
	{48, 48, 22, 22, 174, 68, 1},
	{52, 52, 24, 24, 204, 84, 2},
	{64, 64, 14, 14, 280, 112, 2},
	{72, 72, 16, 16, 368, 144, 4},
	{80, 80, 18, 18, 456, 192, 4},
	{88, 88, 20, 20, 576, 224, 4},
	{96, 96, 22, 22, 696, 272, 4},
	{104, 104, 24, 24, 816, 336, 6},
	{120, 120, 18, 18, 1050, 408, 6},
	{132, 132, 20, 20, 1304, 496, 8},
	{144, 144, 22, 22, 1558, 620, 10},
}

var rectangles = [][7]int{
	{8, 18, 6, 16, 5, 7, 1},
	{8, 32, 6, 14, 10, 11, 1},
	{12, 26, 10, 24, 16, 14, 1},
	{12, 36, 10, 16, 22, 18, 1},
	{16, 36, 14, 16, 32, 24, 1},
	{16, 48, 14, 22, 49, 28, 1},
}

const (
	padCodeword  = 129
	base256Latch = 231
	upperShift   = 235
	eci          = 241
	eciUTF8      = 26
)

// Powers and logarithms of 2 in GF(256) with the prime polynomial 301.
var gfExp [255]int
var gfLog [256]int

func init() {
	value := 1
	for i := 0; i < 255; i++ {
		gfExp[i] = value
		gfLog[value] = i
		value <<= 1
		if value > 255 {
			value ^= 301
		}
	}
}

// DataMatrix is used to create 2D Data Matrix barcodes: ECC 200 symbols as
// specified in ISO/IEC 16022. The text is encoded as UTF-8 in the smallest
// symbol that holds it. Please see Example_51.
type DataMatrix struct {
	modules [][]bool
	x, y    float32
	m1      float32 // Module length
	color   int32

	// The codewords being placed in the mapping matrix, and the matrix.
	codewords    []int
	nrow, ncol   int
	dark, placed [][]bool
}

// NewDataMatrix creates a square Data Matrix barcode. It exits the program if
// the text does not fit in the largest symbol.
func NewDataMatrix(str string) *DataMatrix {
	return NewDataMatrixWithShape(str, Square)
}

// NewDataMatrixWithShape creates a Data Matrix barcode with the shape Square or
// Rectangle. It exits the program if the text does not fit in the largest
// symbol.
func NewDataMatrixWithShape(str string, shape int) *DataMatrix {
	dm := &DataMatrix{m1: 2.0, color: color.Black}
	data := encode([]byte(str))
	symbol := selectSymbol(len(data), shape)
	allCodewords := addErrorCorrection(pad(data, symbol[4]), symbol)
	rows := symbol[0]
	cols := symbol[1]
	regionRows := symbol[2]
	regionCols := symbol[3]
	dm.placeCodewords(allCodewords, (rows/(regionRows+2))*regionRows, (cols/(regionCols+2))*regionCols)

	dm.modules = newMatrix(rows, cols)
	// Each data region has a solid line on its left and bottom sides and a
	// line of alternating modules on its top and right sides.
	for row := 0; row < rows; row += regionRows + 2 {
		for col := 0; col < cols; col++ {
			dm.modules[row][col] = col%2 == 0
			dm.modules[row+regionRows+1][col] = true
		}
	}
	for col := 0; col < cols; col += regionCols + 2 {
		for row := 0; row < rows; row++ {
			dm.modules[row][col] = true
			dm.modules[row][col+regionCols+1] = row%2 == 1
		}
	}
	for row := 0; row < dm.nrow; row++ {
		for col := 0; col < dm.ncol; col++ {
			dm.modules[row+1+2*(row/regionRows)][col+1+2*(col/regionCols)] = dm.dark[row][col]
		}
	}
	return dm
}

// SetLocation sets the location of the top left corner of this barcode.
func (dm *DataMatrix) SetLocation(x, y float32) pdfjet.Drawable {
	dm.x = x
	dm.y = y
	return dm
}

// SetModuleLength sets the module length of this barcode. The default value is
// 2.0. Leave a margin of at least one module around the barcode.
func (dm *DataMatrix) SetModuleLength(moduleLength float32) *DataMatrix {
	dm.m1 = moduleLength
	return dm
}

// SetColor sets the color of the barcode as a 0xRRGGBB value.
func (dm *DataMatrix) SetColor(color int32) *DataMatrix {
	dm.color = color
	return dm
}

// GetData returns the modules of this barcode, by row and column; true is dark.
func (dm *DataMatrix) GetData() [][]bool {
	return dm.modules
}

// DrawOn draws this barcode on the specified page and returns the x and y
// coordinates of the bottom right corner of the barcode. With no page nothing
// is drawn.
func (dm *DataMatrix) DrawOn(page *pdfjet.Page) [2]float32 {
	rows := len(dm.modules)
	cols := len(dm.modules[0])
	if page != nil {
		page.SetBrushColor(dm.color)
		for row := 0; row < rows; row++ {
			col := 0
			for col < cols {
				if !dm.modules[row][col] {
					col++
					continue
				}
				start := col
				for col < cols && dm.modules[row][col] {
					col++
				}
				page.FillRect(
					dm.x+float32(start)*dm.m1,
					dm.y+float32(row)*dm.m1,
					float32(col-start)*dm.m1,
					dm.m1)
			}
		}
	}
	return [2]float32{dm.x + float32(cols)*dm.m1, dm.y + float32(rows)*dm.m1}
}

// encode encodes the bytes in ASCII encodation, or in Base 256 encodation when
// that takes fewer codewords. Text that is not all ASCII starts with the ECI
// that tells the reader the bytes are UTF-8.
func encode(bytes []byte) []int {
	data := make([]int, 0, 2*len(bytes)+5)
	ascii := true
	for _, b := range bytes {
		if b > 127 {
			ascii = false
		}
	}
	if !ascii {
		data = append(data, eci, eciUTF8+1)
	}
	base256Length := 1 + 1 + len(bytes)
	if len(bytes) > 249 {
		base256Length++
	}
	if asciiLength(bytes) <= base256Length {
		i := 0
		for i < len(bytes) {
			c := int(bytes[i])
			if isDigit(c) && i+1 < len(bytes) && isDigit(int(bytes[i+1])) {
				data = append(data, 130+10*(c-'0')+(int(bytes[i+1])-'0'))
				i += 2
			} else if c < 128 {
				data = append(data, c+1)
				i++
			} else {
				data = append(data, upperShift, c-127)
				i++
			}
		}
	} else {
		data = append(data, base256Latch)
		if len(bytes) <= 249 {
			data = append(data, randomize255(len(bytes), len(data)+1))
		} else {
			data = append(data, randomize255(len(bytes)/250+249, len(data)+1))
			data = append(data, randomize255(len(bytes)%250, len(data)+1))
		}
		for _, b := range bytes {
			data = append(data, randomize255(int(b), len(data)+1))
		}
	}
	return data
}

// asciiLength returns the number of codewords of the bytes in ASCII encodation.
func asciiLength(bytes []byte) int {
	length := 0
	i := 0
	for i < len(bytes) {
		c := int(bytes[i])
		if isDigit(c) && i+1 < len(bytes) && isDigit(int(bytes[i+1])) {
			length++
			i += 2
		} else {
			if c < 128 {
				length++
			} else {
				length += 2
			}
			i++
		}
	}
	return length
}

func isDigit(c int) bool {
	return c >= '0' && c <= '9'
}

// randomize255 scrambles a Base 256 codeword at the position, counted from 1,
// in the data.
func randomize255(value, position int) int {
	result := value + ((149 * position) % 255) + 1
	if result <= 255 {
		return result
	}
	return result - 256
}

// selectSymbol returns the smallest symbol of the shape that holds the data
// codewords.
func selectSymbol(length, shape int) [7]int {
	if shape == Rectangle {
		for _, symbol := range rectangles {
			if symbol[4] >= length {
				return symbol
			}
		}
	}
	for _, symbol := range squares {
		if symbol[4] >= length {
			return symbol
		}
	}
	log.Fatal("The text takes " + strconv.Itoa(length) +
		" codewords; a Data Matrix symbol holds 1558 at most.")
	return squares[len(squares)-1]
}

// pad fills the rest of the symbol's data capacity with pad codewords: the
// first one as it is, and the others scrambled by their position.
func pad(data []int, capacity int) []int {
	padded := make([]int, capacity)
	copy(padded, data)
	for i := len(data); i < capacity; i++ {
		if i == len(data) {
			padded[i] = padCodeword
		} else {
			result := padCodeword + ((149 * (i + 1)) % 253) + 1
			if result > 254 {
				result -= 254
			}
			padded[i] = result
		}
	}
	return padded
}

// addErrorCorrection appends the error correction codewords. The codewords are
// spread over the blocks in turn, the error correction codewords continuing
// where the data codewords stop, and each block gets its own error correction
// codewords.
func addErrorCorrection(data []int, symbol [7]int) []int {
	blocks := symbol[6]
	eccPerBlock := symbol[5] / blocks
	gen := generator(eccPerBlock)
	ecc := make([][]int, blocks)
	for block := 0; block < blocks; block++ {
		blockData := make([]int, 0, (len(data)-block+blocks-1)/blocks)
		for i := block; i < len(data); i += blocks {
			blockData = append(blockData, data[i])
		}
		ecc[block] = reedSolomon(blockData, gen)
	}
	codewords := make([]int, len(data)+symbol[5])
	copy(codewords, data)
	next := make([]int, blocks)
	for i := len(data); i < len(codewords); i++ {
		block := i % blocks
		codewords[i] = ecc[block][next[block]]
		next[block]++
	}
	return codewords
}

// generator returns the coefficients, lowest degree first, of the generator
// polynomial (x + 2^1)(x + 2^2)...(x + 2^degree).
func generator(degree int) []int {
	g := make([]int, degree+1)
	g[0] = 1
	for j := 1; j <= degree; j++ {
		root := gfExp[j]
		for i := j; i > 0; i-- {
			g[i] = g[i-1] ^ multiply(g[i], root)
		}
		g[0] = multiply(g[0], root)
	}
	return g
}

// reedSolomon returns the error correction codewords of the data: the
// remainder of the data polynomial times x^degree divided by the generator,
// highest degree first.
func reedSolomon(data, generator []int) []int {
	degree := len(generator) - 1
	remainder := make([]int, degree)
	for _, codeword := range data {
		feedback := codeword ^ remainder[degree-1]
		for i := degree - 1; i > 0; i-- {
			remainder[i] = remainder[i-1] ^ multiply(feedback, generator[i])
		}
		remainder[0] = multiply(feedback, generator[0])
	}
	ecc := make([]int, degree)
	for i := 0; i < degree; i++ {
		ecc[i] = remainder[degree-1-i]
	}
	return ecc
}

func multiply(a, b int) int {
	if a == 0 || b == 0 {
		return 0
	}
	return gfExp[(gfLog[a]+gfLog[b])%255]
}

func newMatrix(rows, cols int) [][]bool {
	matrix := make([][]bool, rows)
	for row := range matrix {
		matrix[row] = make([]bool, cols)
	}
	return matrix
}

// placeCodewords places the codewords in the mapping matrix, the data regions
// of the symbol without their borders, along the diagonals of ISO/IEC 16022
// Annex F.
func (dm *DataMatrix) placeCodewords(codewords []int, nrow, ncol int) {
	dm.codewords = codewords
	dm.nrow = nrow
	dm.ncol = ncol
	dm.dark = newMatrix(nrow, ncol)
	dm.placed = newMatrix(nrow, ncol)
	chr := 0
	row := 4
	col := 0
	for {
		if row == nrow && col == 0 {
			dm.corner1(chr)
			chr++
		}
		if row == nrow-2 && col == 0 && ncol%4 != 0 {
			dm.corner2(chr)
			chr++
		}
		if row == nrow-2 && col == 0 && ncol%8 == 4 {
			dm.corner3(chr)
			chr++
		}
		if row == nrow+4 && col == 2 && ncol%8 == 0 {
			dm.corner4(chr)
			chr++
		}
		// Up and to the right
		for {
			if row < nrow && col >= 0 && !dm.placed[row][col] {
				dm.utah(row, col, chr)
				chr++
			}
			row -= 2
			col += 2
			if !(row >= 0 && col < ncol) {
				break
			}
		}
		row++
		col += 3
		// Down and to the left
		for {
			if row >= 0 && col < ncol && !dm.placed[row][col] {
				dm.utah(row, col, chr)
				chr++
			}
			row += 2
			col -= 2
			if !(row < nrow && col >= 0) {
				break
			}
		}
		row += 3
		col++
		if !(row < nrow || col < ncol) {
			break
		}
	}
	// The bottom right corner is not always used by the codewords.
	if !dm.placed[nrow-1][ncol-1] {
		dm.dark[nrow-1][ncol-1] = true
		dm.dark[nrow-2][ncol-2] = true
	}
}

// module places bit 1 (the most significant) to bit 8 of a codeword at the
// module, wrapping positions outside the matrix around to the other side.
func (dm *DataMatrix) module(row, col, chr, bit int) {
	if row < 0 {
		row += dm.nrow
		col += 4 - ((dm.nrow + 4) % 8)
	}
	if col < 0 {
		col += dm.ncol
		row += 4 - ((dm.ncol + 4) % 8)
	}
	dm.placed[row][col] = true
	dm.dark[row][col] = ((dm.codewords[chr] >> (8 - bit)) & 1) == 1
}

// utah places a codeword in its usual shape, with its last bit at the row and
// column.
func (dm *DataMatrix) utah(row, col, chr int) {
	dm.module(row-2, col-2, chr, 1)
	dm.module(row-2, col-1, chr, 2)
	dm.module(row-1, col-2, chr, 3)
	dm.module(row-1, col-1, chr, 4)
	dm.module(row-1, col, chr, 5)
	dm.module(row, col-2, chr, 6)
	dm.module(row, col-1, chr, 7)
	dm.module(row, col, chr, 8)
}

func (dm *DataMatrix) corner1(chr int) {
	dm.module(dm.nrow-1, 0, chr, 1)
	dm.module(dm.nrow-1, 1, chr, 2)
	dm.module(dm.nrow-1, 2, chr, 3)
	dm.module(0, dm.ncol-2, chr, 4)
	dm.module(0, dm.ncol-1, chr, 5)
	dm.module(1, dm.ncol-1, chr, 6)
	dm.module(2, dm.ncol-1, chr, 7)
	dm.module(3, dm.ncol-1, chr, 8)
}

func (dm *DataMatrix) corner2(chr int) {
	dm.module(dm.nrow-3, 0, chr, 1)
	dm.module(dm.nrow-2, 0, chr, 2)
	dm.module(dm.nrow-1, 0, chr, 3)
	dm.module(0, dm.ncol-4, chr, 4)
	dm.module(0, dm.ncol-3, chr, 5)
	dm.module(0, dm.ncol-2, chr, 6)
	dm.module(0, dm.ncol-1, chr, 7)
	dm.module(1, dm.ncol-1, chr, 8)
}

func (dm *DataMatrix) corner3(chr int) {
	dm.module(dm.nrow-3, 0, chr, 1)
	dm.module(dm.nrow-2, 0, chr, 2)
	dm.module(dm.nrow-1, 0, chr, 3)
	dm.module(0, dm.ncol-2, chr, 4)
	dm.module(0, dm.ncol-1, chr, 5)
	dm.module(1, dm.ncol-1, chr, 6)
	dm.module(2, dm.ncol-1, chr, 7)
	dm.module(3, dm.ncol-1, chr, 8)
}

func (dm *DataMatrix) corner4(chr int) {
	dm.module(dm.nrow-1, 0, chr, 1)
	dm.module(dm.nrow-1, dm.ncol-1, chr, 2)
	dm.module(0, dm.ncol-3, chr, 3)
	dm.module(0, dm.ncol-2, chr, 4)
	dm.module(0, dm.ncol-1, chr, 5)
	dm.module(1, dm.ncol-3, chr, 6)
	dm.module(1, dm.ncol-2, chr, 7)
	dm.module(1, dm.ncol-1, chr, 8)
}
