/**
 * DictionaryBreaks.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * The break engine is a port of ICU's dictionary break engines
 * (icu4c/source/common/dictbe.cpp and dictionarydata.cpp), Copyright (C) 2016
 * and later: Unicode, Inc. and others, under the Unicode License, as are the
 * dictionaries it reads: see resources/fonts/world/ICUDictionaries/license.txt.
 */
import Foundation

///
/// Thai, Lao, Khmer and Burmese are written without spaces between their
/// words, so where a line may break is found with a dictionary of each, as ICU
/// finds it: wordEnds ports ICU's dictionary break engines, which look three
/// words ahead for the choice that lets dictionary words follow, join a run of
/// characters that is not a word to the word before it, and never end a word
/// before a combining mark. The dictionaries are ICU's, carried with the world
/// fonts (resources/fonts/world/ICUDictionaries, and the product
/// PDFjetFontsWorld), and read when text of their script is first broken;
/// without them no break is found inside such text, as before.
///
enum DictionaryBreaks {
    // What an engine of ICU's knows of its script
    final class Script: Sendable {
        let file: String                    // The dictionary's file, in ICUDictionaries
        let thai: Bool                      // Thai's suffixes, PAIYANNOI and MAIYAMOK, and its shortest span
        let word: @Sendable (Int) -> Bool           // [[:Script:]&[:LineBreak=SA:]]: the characters it breaks
        let beginWord: @Sendable (Int) -> Bool      // The characters a word may begin with
        let endWord: @Sendable (Int) -> Bool        // The characters a word may end with

        init(_ file: String, thai: Bool = false, word: @escaping @Sendable (Int) -> Bool,
                beginWord: @escaping @Sendable (Int) -> Bool, endWord: @escaping @Sendable (Int) -> Bool) {
            self.file = file
            self.thai = thai
            self.word = word
            self.beginWord = beginWord
            self.endWord = endWord
        }
    }

    // The characters of each script of line break class SA (LineBreak.txt of
    // Unicode 18.0.0)
    private static func inRanges(_ c: Int, _ ranges: [ClosedRange<Int>]) -> Bool {
        for range in ranges where range.contains(c) {
            return true
        }
        return false
    }

    static func thaiWord(_ c: Int) -> Bool {
        return inRanges(c, [0x0E01...0x0E3A, 0x0E40...0x0E4E])
    }

    private static func laoWord(_ c: Int) -> Bool {
        return inRanges(c, [0x0E81...0x0E82, 0x0E84...0x0E84, 0x0E86...0x0E8A, 0x0E8C...0x0EA3, 0x0EA5...0x0EA5,
                0x0EA7...0x0EBD, 0x0EC0...0x0EC4, 0x0EC6...0x0EC6, 0x0EC8...0x0ECE, 0x0EDC...0x0EDF])
    }

    private static func khmerWord(_ c: Int) -> Bool {
        return inRanges(c, [0x1780...0x17D3, 0x17D7...0x17D7, 0x17DC...0x17DD])
    }

    private static func burmeseWord(_ c: Int) -> Bool {
        return inRanges(c, [0x1000...0x103F, 0x1050...0x108F, 0x109A...0x109F, 0xA9E0...0xA9EF, 0xA9FA...0xA9FE,
                0xAA60...0xAA7F])
    }

    static let thai = Script("thaidict.txt", thai: true, word: thaiWord,
            // KO KAI through HO NOKHUK, SARA E through SARA AI MAIMALAI
            beginWord: { c in (0x0E01...0x0E2E).contains(c) || (0x0E40...0x0E44).contains(c) },
            // MAI HAN-AKAT and SARA E through SARA AI MAIMALAI end no word
            endWord: { c in thaiWord(c) && c != 0x0E31 && !(0x0E40...0x0E44).contains(c) })

    private static let scripts: [Script] = [
        thai,
        Script("laodict.txt", word: laoWord,
                // The consonants, the digraph consonants, the prefix vowels
                beginWord: { c in (0x0E81...0x0EAE).contains(c) || (0x0EDC...0x0EDD).contains(c)
                        || (0x0EC0...0x0EC4).contains(c) },
                // The prefix vowels end no word
                endWord: { c in laoWord(c) && !(0x0EC0...0x0EC4).contains(c) }),
        Script("khmerdict.txt", word: khmerWord,
                beginWord: { c in (0x1780...0x17B3).contains(c) },
                // KHMER SIGN COENG, which joins the next consonant, ends no word
                endWord: { c in khmerWord(c) && c != 0x17D2 }),
        Script("burmesedict.txt", word: burmeseWord,
                // The consonants and the independent vowels
                beginWord: { c in (0x1000...0x102A).contains(c) },
                endWord: burmeseWord),
    ]

    // What ICU's engines share: how many words in a row are good enough; the
    // longest dictionary word a non-word is joined to; the prefix a non-word
    // may share with a dictionary word and still be joined; the shortest span
    // of two words
    private static let lookahead = 3
    private static let rootCombineThreshold = 3
    private static let prefixCombineThreshold = 3
    private static let minWordSpan = 2 * 2
    private static let possibleWordListMax = 20
    private static let thaiPaiyannoi = 0x0E2F
    private static let thaiMaiyamok = 0x0E46

    // A mark of the script's characters, or a space: a word never ends before one
    private static func mark(_ script: Script, _ c: Int) -> Bool {
        if c == 0x20 {
            return true
        }
        guard script.word(c), let scalar = Unicode.Scalar(UInt32(c)) else {
            return false
        }
        switch scalar.properties.generalCategory {
        case .nonspacingMark, .spacingMark, .enclosingMark:
            return true
        default:
            return false
        }
    }

    // A dictionary's words, sorted, for ICU's trie's answers: whether a
    // string is a word, and whether a longer word begins with it. The words
    // are kept as arrays of their code points, compared code point by code
    // point.
    final class Dictionary {
        private let words: [[Int]]
        private let isWord: Set<[Int]>

        // A dictionary of ICU's: a word a line, after an optional tab its
        // value, which is not used; comments begin with #
        init(_ data: String) {
            var seen = Set<[Int]>()
            var list = [[Int]]()
            for row in data.split(separator: "\n", omittingEmptySubsequences: false) {
                var word = row.split(separator: "\t", maxSplits: 1, omittingEmptySubsequences: false).first
                        .map(String.init) ?? ""
                word = word.trimmingCharacters(in: .whitespacesAndNewlines)
                if word.hasPrefix("\u{FEFF}") {
                    word.removeFirst()
                    word = word.trimmingCharacters(in: .whitespacesAndNewlines)
                }
                if word.isEmpty || word.hasPrefix("#") {
                    continue
                }
                let cps = word.unicodeScalars.map { Int($0.value) }
                if seen.insert(cps).inserted {
                    list.append(cps)
                }
            }
            words = list.sorted { $0.lexicographicallyPrecedes($1) }
            isWord = seen
        }

        // Tells if a word begins with the prefix and is longer than it, as
        // ICU's trie can go on from the prefix
        private func continues(_ prefix: [Int]) -> Bool {
            var lo = 0
            var hi = words.count
            while lo < hi {     // The first word not before the prefix
                let mid = lo + (hi - lo) / 2
                if words[mid].lexicographicallyPrecedes(prefix) {
                    lo = mid + 1
                } else {
                    hi = mid
                }
            }
            if lo < words.count && words[lo] == prefix {
                lo += 1
            }
            return lo < words.count && words[lo].starts(with: prefix)
        }

        // ICU's DictionaryMatcher.matches: the lengths, shortest first, of
        // the words the text begins with at the position, no longer than
        // maxLength; and the characters the trie took, the one it refused
        // among them
        func matches(_ text: [Int], _ at: Int, _ maxLength: Int, _ lengths: inout [Int]) -> (count: Int, prefix: Int) {
            var s = [Int]()
            var count = 0
            var taken = 0
            var i = at
            while i < text.count {
                s.append(text[i])
                let lengthMatched = i + 1 - at
                taken += 1
                let word = isWord.contains(s)
                let more = continues(s)
                if word {
                    if count < lengths.count {
                        lengths[count] = lengthMatched
                        count += 1
                    }
                    if !more {      // A final value
                        break
                    }
                } else if !more {   // No match
                    break
                }
                if lengthMatched >= maxLength {
                    break
                }
                i += 1
            }
            return (count, taken)
        }
    }

    // ICU's UText as the engines use it: the text, in code points, and a position
    final class Text {
        let cps: [Int]
        var pos = 0

        init(_ cps: [Int]) {
            self.cps = cps
        }

        func current32() -> Int {
            return (pos < cps.count) ? cps[pos] : -1
        }

        func next32() -> Int {
            if pos < cps.count {
                pos += 1
                return cps[pos - 1]
            }
            return -1
        }

        func previous32() -> Int {
            if pos > 0 {
                pos -= 1
                return cps[pos]
            }
            return -1
        }
    }

    // ICU's PossibleWord: the words of the dictionary at a position of the
    // text, the one chosen, the one looked at
    final class PossibleWord {
        var count = 0
        var prefix = 0
        var offset = -1
        var mark = 0
        var current = 0
        var lengths = [Int](repeating: 0, count: DictionaryBreaks.possibleWordListMax)

        func candidates(_ t: Text, _ d: Dictionary, _ rangeEnd: Int) -> Int {
            let start = t.pos
            if start != offset {
                offset = start
                (count, prefix) = d.matches(t.cps, start, rangeEnd - start, &lengths)
                if count <= 0 {
                    t.pos = start
                }
            }
            if count > 0 {
                t.pos = start + lengths[count - 1]
            }
            current = count - 1
            mark = current
            return count
        }

        func acceptMarked(_ t: Text) -> Int {
            t.pos = offset + lengths[mark]
            return lengths[mark]
        }

        func backUp(_ t: Text) -> Bool {
            if current > 0 {
                current -= 1
                t.pos = offset + lengths[current]
                return true
            }
            return false
        }
    }

    // Of more candidate words at the position, the one that lets the most
    // words follow, marked in the word
    private static func findBest(_ words: [PossibleWord], _ wordsFound: Int, _ word: PossibleWord,
            _ t: Text, _ d: Dictionary, _ rangeEnd: Int) {
        if t.pos >= rangeEnd {
            return
        }
        repeat {
            if words[(wordsFound + 1) % lookahead].candidates(t, d, rangeEnd) > 0 {
                // Followed by another word: a good candidate
                word.mark = word.current
                if t.pos >= rangeEnd {
                    return
                }
                // A third word after any of the second words: done
                repeat {
                    if words[(wordsFound + 2) % lookahead].candidates(t, d, rangeEnd) > 0 {
                        word.mark = word.current
                        return
                    }
                } while words[(wordsFound + 1) % lookahead].backUp(t)
            }
        } while word.backUp(t)
    }

    // ICU's engine on a run of the script's characters, from rangeStart to
    // rangeEnd: the positions where its words end, but its end
    private static func divideUpDictionaryRange(_ script: Script, _ d: Dictionary, _ t: Text,
            _ rangeStart: Int, _ rangeEnd: Int, _ breaks: inout [Int]) {
        // Not enough characters for two words: Thai's engine moves four
        // characters on and wants the end beyond them, the others want four
        if (script.thai && rangeEnd - rangeStart <= minWordSpan)
                || (!script.thai && rangeEnd - rangeStart < minWordSpan) {
            return
        }
        let first = breaks.count
        var wordsFound = 0
        let words = (0..<lookahead).map { _ in PossibleWord() }
        t.pos = rangeStart
        while t.pos < rangeEnd {
            let current = t.pos
            var wordLength = 0
            var cpWordLength = 0
            let word = words[wordsFound % lookahead]

            // Candidate words at the position: one is taken; of more, the one
            // that lets the most words follow
            let candidates = word.candidates(t, d, rangeEnd)
            if candidates == 1 {
                wordLength = word.acceptMarked(t)
                cpWordLength = word.lengths[word.mark]
                wordsFound += 1
            } else if candidates > 1 {
                findBest(words, wordsFound, word, t, d, rangeEnd)
                wordLength = word.acceptMarked(t)
                cpWordLength = word.lengths[word.mark]
                wordsFound += 1
            }

            // A non-word after the word is joined to it, if the word is short
            // and the non-word shares too little with a dictionary word
            var uc = 0
            if t.pos < rangeEnd && cpWordLength < rootCombineThreshold {
                let next = words[wordsFound % lookahead]
                if next.candidates(t, d, rangeEnd) <= 0
                        && (wordLength == 0 || next.prefix < prefixCombineThreshold) {
                    // A plausible word boundary
                    var remaining = rangeEnd - (current + wordLength)
                    var chars = 0
                    while true {
                        let pcIndex = t.pos
                        let pc = t.next32()
                        let pcSize = t.pos - pcIndex
                        chars += pcSize
                        remaining -= pcSize
                        if remaining <= 0 {
                            break
                        }
                        uc = t.current32()
                        if script.endWord(pc) && script.beginWord(uc) {
                            let n = words[(wordsFound + 1) % lookahead].candidates(t, d, rangeEnd)
                            t.pos = current + wordLength + chars
                            if n > 0 {
                                break
                            }
                        }
                    }
                    if wordLength <= 0 {
                        wordsFound += 1
                    }
                    wordLength += chars
                } else {
                    t.pos = current + wordLength
                }
            }

            // Never a break before a combining mark
            while t.pos < rangeEnd && mark(script, t.current32()) {
                let currPos = t.pos
                _ = t.next32()
                wordLength += t.pos - currPos
            }

            // Thai's suffixes, when no word follows
            if script.thai && t.pos < rangeEnd && wordLength > 0 {
                uc = t.current32()
                if words[wordsFound % lookahead].candidates(t, d, rangeEnd) <= 0
                        && (uc == thaiPaiyannoi || uc == thaiMaiyamok) {
                    if uc == thaiPaiyannoi {
                        let prev = t.previous32()
                        if prev != thaiPaiyannoi && prev != thaiMaiyamok {
                            _ = t.next32()
                            let paiyannoiIndex = t.pos
                            _ = t.next32()
                            wordLength += t.pos - paiyannoiIndex
                            uc = t.current32()
                        } else {
                            _ = t.next32()
                        }
                    }
                    if uc == thaiMaiyamok {
                        if t.previous32() != thaiMaiyamok {
                            _ = t.next32()
                            let maiyamokIndex = t.pos
                            _ = t.next32()
                            wordLength += t.pos - maiyamokIndex
                        } else {
                            _ = t.next32()
                        }
                    }
                } else {
                    t.pos = current + wordLength
                }
            }

            if wordLength > 0 {
                breaks.append(current + wordLength)
            }
        }
        // No break at the end of the run
        if breaks.count > first, let last = breaks.last, last >= rangeEnd {
            breaks.removeLast()
        }
    }

    // The dictionaries, each read once, for every thread: the lock guards the map
    private final class Dictionaries: @unchecked Sendable {
        static let shared = Dictionaries()
        private var map = [String: Dictionary?]()
        private let lock = NSLock()

        // The dictionary of the script, or nil when it is not carried: from
        // the folder resources/fonts/world of the working directory, else
        // from the bundle of PDFjetFontsWorld
        func of(_ script: Script) -> Dictionary? {
            lock.lock()
            defer { lock.unlock() }
            if let known = map[script.file] {
                return known
            }
            var url: URL?
            let path = BundledFonts.FOLDER + "world/ICUDictionaries/" + script.file
            var isDirectory: ObjCBool = false
            if FileManager.default.fileExists(atPath: path, isDirectory: &isDirectory), !isDirectory.boolValue {
                url = URL(fileURLWithPath: path)
            } else if let bundle = BundledFonts.worldBundle {
                let base = script.file as NSString
                url = bundle.url(forResource: base.deletingPathExtension, withExtension: base.pathExtension)
            }
            var d: Dictionary?
            if let url, let data = try? Data(contentsOf: url), let text = String(data: data, encoding: .utf8) {
                d = Dictionary(text)
            }
            map[script.file] = d
            return d
        }
    }

    /// The code points of the text.
    static func codePoints(_ text: String) -> [Int] {
        return text.unicodeScalars.map { Int($0.value) }
    }

    ///
    /// The indexes in the code points where a word of Thai, Lao, Khmer or
    /// Burmese ends inside a run of the script, where a line may break: in
    /// increasing order, none at a run's end. Of the runs of one script, or of
    /// all four when it is nil.
    ///
    static func wordEnds(_ cps: [Int], _ only: Script? = nil) -> [Int] {
        var ends = [Int]()
        let t = Text(cps)
        var i = 0
        while i < cps.count {
            guard let script = scripts.first(where: { $0.word(cps[i]) }), only == nil || script === only else {
                i += 1
                continue
            }
            let start = i
            while i < cps.count && script.word(cps[i]) {
                i += 1
            }
            if let d = Dictionaries.shared.of(script) {
                divideUpDictionaryRange(script, d, t, start, i, &ends)
            }
        }
        // The ends before a mark left out, as ICU's rules never break a letter
        // from its marks: a mark after a sign that takes none, as ฯ then ุ, ended
        // a word before it (found by FuzzDictionaryWordEnds)
        return ends.filter { end in
            guard end < cps.count, let scalar = Unicode.Scalar(UInt32(cps[end])) else {
                return true
            }
            switch scalar.properties.generalCategory {
            case .nonspacingMark, .spacingMark, .enclosingMark:
                return false
            default:
                return true
            }
        }
    }

    ///
    /// The line with a zero width space at each end of a Thai word inside a
    /// run of Thai, where TextBlock may break it, when the line has no zero
    /// width space of its own; else, or without the dictionary, the line as it
    /// is. A zero width space is not drawn.
    ///
    static func thaiWordBreaks(_ line: String) -> String {
        if line.unicodeScalars.contains("\u{200B}") || !line.unicodeScalars.contains(where: { thaiWord(Int($0.value)) }) {
            return line
        }
        let cps = codePoints(line)
        let ends = wordEnds(cps, thai)
        if ends.isEmpty {
            return line
        }
        var scalars = String.UnicodeScalarView()
        var next = 0
        for (i, c) in cps.enumerated() {
            if next < ends.count && ends[next] == i {
                scalars.append("\u{200B}")
                next += 1
            }
            if let scalar = Unicode.Scalar(UInt32(c)) {
                scalars.append(scalar)
            }
        }
        return String(scalars)
    }
}
