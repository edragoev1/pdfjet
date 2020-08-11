extension String {
    public func trim() -> String {
        return self.trimmingCharacters(in: .whitespacesAndNewlines)
    }
}
