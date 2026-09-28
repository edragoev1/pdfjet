// fastfloat_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package fastfloat

import (
	"math"
	"testing"
)

// The numbers written in content streams, which must be the same bytes in the four ports.

func testFormat(t *testing.T, want string, value float32) {
	t.Helper()
	if got := string(ToByteArray(value)); got != want {
		t.Errorf("%v: want %q, got %q", value, want, got)
	}
}

func TestFastFloatWritesWholeNumbersWithoutDecimals(t *testing.T) {
	testFormat(t, "0", 0)
	testFormat(t, "1", 1)
	testFormat(t, "-3", -3)
	testFormat(t, "100", 100)
}

func TestFastFloatLeavesOutTrailingZeros(t *testing.T) {
	testFormat(t, "0.5", 0.5)
	testFormat(t, "1.25", 1.25)
	testFormat(t, "0.1", 0.1)
	testFormat(t, "1.1", 1.1)
}

func TestFastFloatRoundsHundredthsHalfAwayFromZero(t *testing.T) {
	testFormat(t, "1.13", 1.125)
	testFormat(t, "-1.13", -1.125)
	testFormat(t, "2.38", 2.375)
	testFormat(t, "0.13", 0.125)
	testFormat(t, "-0.13", -0.125)
	testFormat(t, "0.01", 0.006)
	testFormat(t, "100", 99.995)
}

func TestFastFloatWritesZeroForValuesThatRoundToZero(t *testing.T) {
	testFormat(t, "0", float32(math.Copysign(0, -1)))
	testFormat(t, "0", -0.001)
	testFormat(t, "0", 0.004)
}

func TestFastFloatWritesLargeNumbersWithAllTheirDigits(t *testing.T) {
	testFormat(t, "8388607.5", 8388607.5)
	testFormat(t, "8388608", 8388608)
	testFormat(t, "21600000", 21600000)
	testFormat(t, "1000000000", 1e9)
	testFormat(t, "-1000000000", -1e9)
	testFormat(t, "2147483520", 2147483520)
}

func TestFastFloatAppendsAfterWhatTheSliceHolds(t *testing.T) {
	if got := string(Append([]byte("1 "), -2147483520)); got != "1 -2147483520" {
		t.Errorf("want %q, got %q", "1 -2147483520", got)
	}
	if n := len(ToByteArray(-2147483520)); n != MaxLength {
		t.Errorf("want %d bytes, got %d", MaxLength, n)
	}
}

func TestFastFloatRefusesNumbersAPdfCannotHold(t *testing.T) {
	for _, value := range []float32{float32(math.NaN()), float32(math.Inf(1)), float32(math.Inf(-1)), 2147483648, -3.4e38} {
		if IsWritable(value) {
			t.Errorf("%v is writable", value)
		}
		testFormat(t, "0", value)
	}
}

func testPrecise(t *testing.T, want string, value float32) {
	t.Helper()
	if got := string(AppendPrecise(nil, value)); got != want {
		t.Errorf("%v: want %q, got %q", value, want, got)
	}
}

func TestFastFloatWritesTheEntriesOfARotationWithFiveDecimals(t *testing.T) {
	// The sine and the cosine of 1 degree, which hundredths write as those of 1.15 degrees.
	testPrecise(t, "0.01745", float32(math.Sin(math.Pi/180)))
	testPrecise(t, "0.99985", float32(math.Cos(math.Pi/180)))
	testPrecise(t, "-0.70711", float32(-math.Sqrt(0.5)))
	testPrecise(t, "1", 1)
	testPrecise(t, "-1", -1)
	testPrecise(t, "0.5", 0.5)
	testPrecise(t, "0.0001", 0.0001)
	testPrecise(t, "0", float32(math.Cos(math.Pi/2)))
	testPrecise(t, "0", float32(math.Copysign(0, -1)))
	testPrecise(t, "0", -0.000004)
	testPrecise(t, "12.25", 12.25)
	testPrecise(t, "0", float32(math.NaN()))
}
