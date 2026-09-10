extension String {
    /// Returns this string without leading and trailing whitespace and newlines.
    public func trim() -> String {
        return self.trimmingCharacters(in: .whitespacesAndNewlines)
    }
}
