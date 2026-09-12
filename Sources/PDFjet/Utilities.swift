extension String {
    /// Returns this string without leading and trailing characters with a code
    /// point of U+0020 or below, like String.trim in Java. Unlike
    /// CharacterSet.whitespacesAndNewlines it leaves a no-break space and the
    /// other Unicode spaces in place.
    public func trim() -> String {
        let scalars = self.unicodeScalars
        var start = scalars.startIndex
        var end = scalars.endIndex
        while start < end && scalars[start].value <= 0x20 {
            start = scalars.index(after: start)
        }
        while end > start && scalars[scalars.index(before: end)].value <= 0x20 {
            end = scalars.index(before: end)
        }
        return String(scalars[start..<end])
    }

    /// Splits this string on the ASCII whitespace that Java's \s matches: space,
    /// tab, line feed, vertical tab, form feed and carriage return. Empty tokens
    /// are dropped, and a no-break space does not split.
    func splitOnWhitespace() -> [String] {
        var tokens = [String]()
        var token = String.UnicodeScalarView()
        for scalar in self.unicodeScalars {
            if String.isASCIIWhitespace(scalar) {
                if !token.isEmpty {
                    tokens.append(String(token))
                    token = String.UnicodeScalarView()
                }
            } else {
                token.append(scalar)
            }
        }
        if !token.isEmpty {
            tokens.append(String(token))
        }
        return tokens
    }

    /// Returns true for the six ASCII whitespace scalars. It walks scalars rather
    /// than Characters because Swift folds "\r\n" into a single Character.
    static func isASCIIWhitespace(_ scalar: Unicode.Scalar) -> Bool {
        let value = scalar.value
        return value == 0x20 || (value >= 0x09 && value <= 0x0D)
    }

    /// Returns true if the scalar is whitespace as Character.isWhitespace in
    /// Java defines it: U+0009 to U+000D, U+001C to U+001F, and the space,
    /// line and paragraph separators, except the no-break spaces U+00A0,
    /// U+2007 and U+202F.
    static func isJavaWhitespace(_ scalar: Unicode.Scalar) -> Bool {
        let value = scalar.value
        if (value >= 0x09 && value <= 0x0D) || (value >= 0x1C && value <= 0x1F) {
            return true
        }
        if value == 0x00A0 || value == 0x2007 || value == 0x202F {
            return false
        }
        switch scalar.properties.generalCategory {
        case .spaceSeparator, .lineSeparator, .paragraphSeparator:
            return true
        default:
            return false
        }
    }

    /// Returns true if more than half of the unicode scalars of this string are
    /// CJK: CJK Unified Ideographs (4E00-9FD5), Hiragana (3040-309F),
    /// Katakana (30A0-30FF) or Hangul Jamo (1100-11FF).
    func isCJK() -> Bool {
        var numOfCJK = 0
        var count = 0
        for scalar in self.unicodeScalars {
            count += 1
            let ch = scalar.value
            if (ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF) {
                numOfCJK += 1
            }
        }
        return numOfCJK > (count / 2)
    }
}
