/**
 * FastFloatTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

/// The numbers written in content streams, which must be the same bytes in the four ports.
@Suite struct FastFloatTests {
    private func format(_ value: Float) -> String {
        return TestSupport.latin1(FastFloat.toByteArray(value))
    }

    @Test func writesWholeNumbersWithoutDecimals() {
        #expect(format(0) == "0")
        #expect(format(1) == "1")
        #expect(format(-3) == "-3")
        #expect(format(100) == "100")
    }

    @Test func leavesOutTrailingZeros() {
        #expect(format(0.5) == "0.5")
        #expect(format(1.25) == "1.25")
        #expect(format(0.1) == "0.1")
        #expect(format(1.1) == "1.1")
    }

    @Test func roundsHundredthsHalfAwayFromZero() {
        #expect(format(1.125) == "1.13")
        #expect(format(-1.125) == "-1.13")
        #expect(format(2.375) == "2.38")
        #expect(format(0.125) == "0.13")
        #expect(format(-0.125) == "-0.13")
        #expect(format(0.006) == "0.01")
        #expect(format(99.995) == "100")
    }

    @Test func writesZeroForValuesThatRoundToZero() {
        #expect(format(-0.0) == "0")
        #expect(format(-0.001) == "0")
        #expect(format(0.004) == "0")
    }

    @Test func writesLargeNumbersWithAllTheirDigits() {
        #expect(format(8388607.5) == "8388607.5")
        #expect(format(8388608) == "8388608")
        #expect(format(21600000) == "21600000")
        #expect(format(1e9) == "1000000000")
        #expect(format(-1e9) == "-1000000000")
        #expect(format(2147483520) == "2147483520")
    }

    @Test func refusesNumbersAPdfCannotHold() {
        let values: [Float] = [.nan, .infinity, -.infinity, 2147483648, -3.4e38]
        for value in values {
            #expect(!FastFloat.isWritable(value))
            #expect(format(value) == "0")
        }
        #expect(FastFloat.isWritable(2147483520))
    }
}
