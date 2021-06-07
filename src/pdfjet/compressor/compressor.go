package compressor

import (
	"bytes"
	"compress/zlib"
)

// Deflate deflates the input data.
func Deflate(buf []byte) []byte {
	var deflated bytes.Buffer
	writer := zlib.NewWriter(&deflated)
	writer.Write(buf)
	writer.Close()
	return deflated.Bytes()
}
