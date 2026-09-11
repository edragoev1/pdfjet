/**
 * Predictor.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Undoes the predictor of the /DecodeParms of a stream: 2 is the TIFF
/// predictor, and 10 to 15 are the PNG predictors, where each row starts with
/// the type of the PNG filter of that row.
///
func applyPredictor(
        _ data: [UInt8],
        _ predictor: Int,
        _ colors: Int,
        _ bitsPerComponent: Int,
        _ columns: Int) -> [UInt8] {
    // Larger values are not in real files, and would overflow the row length.
    if colors < 1 || colors > 256 ||
            bitsPerComponent < 1 || bitsPerComponent > 16 ||
            columns < 1 || columns > (1 << 18) {
        return data
    }
    if predictor == 2 {
        return applyTIFFPredictor(data, colors, bitsPerComponent, columns)
    } else if predictor >= 10 {
        return applyPNGPredictor(
                data,
                (colors * bitsPerComponent + 7) / 8,
                (colors * bitsPerComponent * columns + 7) / 8)
    }
    return data
}

// Each sample adds the sample of the same color to its left.
private func applyTIFFPredictor(
        _ data: [UInt8], _ colors: Int, _ bitsPerComponent: Int, _ columns: Int) -> [UInt8] {
    var decoded = data
    let rowLength = (colors * bitsPerComponent * columns + 7) / 8
    for row in stride(from: 0, to: decoded.count, by: rowLength) {
        if bitsPerComponent == 8 {
            let end = min(row + rowLength, decoded.count)
            for i in stride(from: row + colors, to: end, by: 1) {
                decoded[i] = decoded[i] &+ decoded[i - colors]
            }
        } else {
            let samples = min(colors * columns, (decoded.count - row) * 8 / bitsPerComponent)
            for i in stride(from: colors, to: samples, by: 1) {
                let left = getSample(decoded, row, (i - colors) * bitsPerComponent, bitsPerComponent)
                let sample = getSample(decoded, row, i * bitsPerComponent, bitsPerComponent)
                setSample(&decoded, row, i * bitsPerComponent, bitsPerComponent, left + sample)
            }
        }
    }
    return decoded
}

private func getSample(_ data: [UInt8], _ row: Int, _ bit: Int, _ bitsPerComponent: Int) -> Int {
    var sample = 0
    for i in bit..<(bit + bitsPerComponent) {
        sample = (sample << 1) | Int((data[row + (i >> 3)] >> (7 - (i & 7))) & 1)
    }
    return sample
}

// Sets the low bitsPerComponent bits of the sample.
private func setSample(
        _ data: inout [UInt8], _ row: Int, _ bit: Int, _ bitsPerComponent: Int, _ sample: Int) {
    var sample = sample
    for i in stride(from: bit + bitsPerComponent - 1, through: bit, by: -1) {
        let k = row + (i >> 3)
        let mask = UInt8(1 << (7 - (i & 7)))
        if sample & 1 != 0 {
            data[k] |= mask
        } else {
            data[k] &= ~mask
        }
        sample >>= 1
    }
}

// Only the last row can be shorter than rowLength.
private func applyPNGPredictor(_ data: [UInt8], _ bytesPerPixel: Int, _ rowLength: Int) -> [UInt8] {
    var decoded = [UInt8]()
    decoded.reserveCapacity(data.count)
    for i in stride(from: 0, to: data.count, by: rowLength + 1) {
        let filter = data[i]
        let n = min(rowLength, data.count - i - 1)
        for x in 0..<n {
            let j = decoded.count
            let left = (x >= bytesPerPixel) ? Int(decoded[j - bytesPerPixel]) : 0
            let up = (j >= rowLength) ? Int(decoded[j - rowLength]) : 0
            let upLeft = (x >= bytesPerPixel && j >= rowLength) ?
                    Int(decoded[j - rowLength - bytesPerPixel]) : 0
            var value = Int(data[i + 1 + x])
            if filter == 1 {            // Sub
                value += left
            } else if filter == 2 {     // Up
                value += up
            } else if filter == 3 {     // Average
                value += (left + up) / 2
            } else if filter == 4 {     // Paeth
                value += paeth(left, up, upLeft)
            }                           // 0 is None, and so are unknown types.
            decoded.append(UInt8(value & 0xFF))
        }
    }
    return decoded
}

private func paeth(_ left: Int, _ up: Int, _ upLeft: Int) -> Int {
    let p = left + up - upLeft
    let pLeft = abs(p - left)
    let pUp = abs(p - up)
    let pUpLeft = abs(p - upLeft)
    if pLeft <= pUp && pLeft <= pUpLeft {
        return left
    }
    return (pUp <= pUpLeft) ? up : upLeft
}
