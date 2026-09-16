/**
 * FlateHuffman.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

// The code lengths of one dynamic block, written the way RFC 1951 3.2.7 asks
// for them: the literal lengths and the distance lengths as a single run of
// symbols, with repeats folded into the symbols 16, 17 and 18. Each symbol
// keeps the extra bits that belong to it alongside it.
internal struct FlateCodeLengths {
    var symbols = [UInt8]()
    var extra = [UInt32]()
    var extraBits = [UInt8]()

    mutating func add(_ symbol: UInt8, _ value: UInt32, _ bits: UInt8) {
        symbols.append(symbol)
        extra.append(value)
        extraBits.append(bits)
    }
}

internal enum FlateHuffman {
    // The code length of every symbol, zero for the ones the block never
    // uses, with no code longer than maxBits.
    static func codeLengths(_ frequency: [Int32], _ maxBits: Int) -> [UInt8] {
        var weights = frequency
        while true {
            let depths = huffmanDepths(weights)
            var deepest = 0
            for depth in depths where depth > deepest {
                deepest = depth
            }
            if deepest <= maxBits {
                var lengths = [UInt8](repeating: 0, count: depths.count)
                for symbol in 0..<depths.count {
                    lengths[symbol] = UInt8(depths[symbol])
                }
                return lengths
            }
            // Only a very lopsided block reaches here. Halving every weight
            // brings the rarest symbols closer to the commonest, which makes
            // the deepest code shallower; repeating it ends with every weight
            // 1 and a balanced tree, so the loop always finishes.
            for symbol in 0..<weights.count where weights[symbol] > 0 {
                weights[symbol] = (weights[symbol] + 1) / 2
            }
        }
    }

    // The depth of every symbol in the Huffman tree of these weights, which
    // is the length its code would have. Unused symbols come back at zero.
    private static func huffmanDepths(_ weights: [Int32]) -> [Int] {
        var depths = [Int](repeating: 0, count: weights.count)
        var symbols = [Int]()
        for symbol in 0..<weights.count where weights[symbol] > 0 {
            symbols.append(symbol)
        }
        if symbols.count < 2 {
            // A block can use a single symbol, or none at all when it holds
            // no matches and so no distances. Two codes of one bit are handed
            // out either way: a lone code leaves the tree incomplete, which
            // not every decoder accepts.
            let used = symbols.first ?? 0
            depths[used] = 1
            depths[used == 0 ? 1 : 0] = 1
            return depths
        }
        symbols.sort {
            weights[$0] != weights[$1] ? weights[$0] < weights[$1] : $0 < $1
        }

        // The leaves come first, in order of weight, and every node merged
        // from them is appended after. Both sequences then only grow at the
        // end and are read from the front, and because a merged node is never
        // lighter than the one merged before it, the lighter of the two heads
        // is always the lightest node left. That is Huffman's algorithm
        // without a heap.
        let leaves = symbols.count
        var weight = [Int64](repeating: 0, count: 2 * leaves)
        var left = [Int](repeating: -1, count: 2 * leaves)
        var right = [Int](repeating: -1, count: 2 * leaves)
        for i in 0..<leaves {
            weight[i] = Int64(weights[symbols[i]])
        }
        var nextLeaf = 0
        var nextMerged = leaves
        var end = leaves
        func takeLightest() -> Int {
            if nextLeaf < leaves
                    && (nextMerged == end || weight[nextLeaf] <= weight[nextMerged]) {
                nextLeaf += 1
                return nextLeaf - 1
            }
            nextMerged += 1
            return nextMerged - 1
        }
        while (leaves - nextLeaf) + (end - nextMerged) > 1 {
            let a = takeLightest()
            let b = takeLightest()
            weight[end] = weight[a] + weight[b]
            left[end] = a
            right[end] = b
            end += 1
        }

        var depth = [Int](repeating: 0, count: end)
        var stack = [end - 1]
        while let node = stack.popLast() {
            if left[node] < 0 {
                depths[symbols[node]] = depth[node]
            } else {
                depth[left[node]] = depth[node] + 1
                depth[right[node]] = depth[node] + 1
                stack.append(left[node])
                stack.append(right[node])
            }
        }
        return depths
    }

    // Folds the runs in a sequence of code lengths into the symbols 16, 17
    // and 18, by RFC 1951 3.2.7.
    static func runLengthEncode(_ lengths: [UInt8]) -> FlateCodeLengths {
        var encoded = FlateCodeLengths()
        var i = 0
        while i < lengths.count {
            let value = lengths[i]
            var run = 1
            while i + run < lengths.count && lengths[i + run] == value {
                run += 1
            }
            i += run
            if value == 0 {
                while run >= 11 {
                    let taken = min(run, 138)
                    encoded.add(18, UInt32(taken - 11), 7)
                    run -= taken
                }
                while run >= 3 {
                    let taken = min(run, 10)
                    encoded.add(17, UInt32(taken - 3), 3)
                    run -= taken
                }
            } else {
                // Symbol 16 repeats what came before it, so the first of a run
                // is always written out as itself.
                encoded.add(value, 0, 0)
                run -= 1
                while run >= 3 {
                    let taken = min(run, 6)
                    encoded.add(16, UInt32(taken - 3), 2)
                    run -= taken
                }
            }
            while run > 0 {
                encoded.add(value, 0, 0)
                run -= 1
            }
        }
        return encoded
    }
}
