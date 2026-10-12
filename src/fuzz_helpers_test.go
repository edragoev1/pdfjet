// fuzz_helpers_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"compress/zlib"
	"math"
	"runtime"
	"runtime/debug"
	"strconv"
	"testing"
	"time"
)

// The checks of the fuzz targets. Any input either works, or panics with a
// message of PDFjet's own: an index out of range, a nil pointer, a hang or
// gigabytes of memory is a bug.

// fuzzMaxTime is much longer than any input PDFjet ships or tests with takes,
// which is a few milliseconds.
const fuzzMaxTime = 10 * time.Second

// fuzzMaxAlloc is more memory than any font or image PDFjet ships needs to be
// loaded, drawn with and written.
const fuzzMaxAlloc = 256 * 1024 * 1024

// fuzzRun runs the function on an input of the size. It fails when that ends
// in a runtime error, not a panic of PDFjet's own, or allocates more than
// fuzzMaxAlloc, or takes longer than fuzzMaxTime, which ends the fuzzing
// process so that the fuzzer keeps the input.
func fuzzRun(t *testing.T, size int, fn func()) {
	fuzzRunWithin(t, size, fuzzMaxAlloc, fn)
}

// fuzzRunWithin is fuzzRun with a limit of allocation of its own.
func fuzzRunWithin(t *testing.T, size int, maxAlloc uint64, fn func()) {
	timer := time.AfterFunc(fuzzMaxTime, func() {
		panic("an input of " + strconv.Itoa(size) + " bytes took longer than " + fuzzMaxTime.String())
	})
	defer timer.Stop()
	var before runtime.MemStats
	runtime.ReadMemStats(&before)
	defer func() {
		if r := recover(); r != nil {
			if err, ok := r.(runtime.Error); ok {
				t.Fatalf("runtime error: %v\n%s", err, debug.Stack())
			}
		}
		var after runtime.MemStats
		runtime.ReadMemStats(&after)
		if allocated := after.TotalAlloc - before.TotalAlloc; allocated > maxAlloc {
			t.Fatalf("%d bytes allocated for an input of %d bytes", allocated, size)
		}
	}()
	fn()
}

// fuzzShortest runs the function, and again, up to three times, while each
// run takes longer than the limit, and returns the shortest time: a run made
// slow by the load of the computer is run again, and an input that takes time
// much worse than linear is slow each time. The checks of time gave false
// failures under load when they timed one run.
func fuzzShortest(limit time.Duration, fn func()) time.Duration {
	shortest := time.Duration(math.MaxInt64)
	for run := 0; run < 3 && shortest > limit; run++ {
		start := time.Now()
		fn()
		shortest = min(shortest, time.Since(start))
	}
	return shortest
}

// fuzzFontText has letters, marks that go on letters and on other marks,
// right to left text, ideographs, a character past the Basic Multilingual
// Plane and ones that are not drawn.
const fuzzFontText = "Ab1 กิ่ ñ é\u0301 שָׁלוֹם مَرْحَبًا 日本 😀 \u200D\uFEFF"

func fuzzCompress(data []byte) []byte {
	var buf bytes.Buffer
	w := zlib.NewWriter(&buf)
	_, _ = w.Write(data)
	_ = w.Close()
	return buf.Bytes()
}
