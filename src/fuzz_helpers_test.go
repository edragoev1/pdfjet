// fuzz_helpers_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
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
		if allocated := after.TotalAlloc - before.TotalAlloc; allocated > fuzzMaxAlloc {
			t.Fatalf("%d bytes allocated for an input of %d bytes", allocated, size)
		}
	}()
	fn()
}
