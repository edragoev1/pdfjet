import Foundation

class BufferedOutputStream {
    private let outputStream: OutputStream
    private var buffer = [UInt8]()
    private let bufferSize: Int
    private(set) var byteCount = 0
    // The first error writing to the stream, thrown by flush and close. The
    // bytes written after it are dropped.
    private var writeError: Error?
    private var closed = false

    /// Initializes with an existing OutputStream (file, socket, etc.)
    init(_ outputStream: OutputStream, bufferSize: Int = 2*4096) {
        self.outputStream = outputStream
        self.bufferSize = bufferSize
        self.outputStream.open()
    }

    /// Appends full buffer of data
    func write(_ data: [UInt8]) {
        write(data, 0, data.count)
    }

    /// Appends partial buffer of data. An error writing the full buffer to
    /// the stream is thrown by the next flush or close.
    func write(_ data: [UInt8], _ offset: Int, _ length: Int) {
        guard offset >= 0, length >= 0, offset + length <= data.count else {
            fatalError("Invalid offset or length in write()")
        }

        byteCount += length
        if writeError != nil {
            return
        }
        buffer.append(contentsOf: data[offset ..< offset + length])
        if buffer.count >= bufferSize {
            writeBuffer()
        }
    }

    /// Writes the buffer contents to the underlying stream
    /// - Throws: the stream's error, or PDFjetError, if the stream fails or
    ///   accepts no more bytes, now or in an earlier write.
    func flush() throws {
        writeBuffer()
        if let error = writeError {
            throw error
        }
    }

    /// Flushes the buffer and closes the stream
    /// - Throws: the error of flush.
    func close() throws {
        if closed {
            return
        }
        closed = true
        defer { outputStream.close() }
        try flush()
    }

    // Writes the buffer to the stream and keeps the first error.
    private func writeBuffer() {
        var totalWritten = 0
        while writeError == nil && totalWritten < buffer.count {
            let written = buffer.withUnsafeBufferPointer {
                outputStream.write($0.baseAddress! + totalWritten, maxLength: $0.count - totalWritten)
            }
            if written <= 0 {
                writeError = outputStream.streamError ?? PDFjetError(message: "Cannot write to the stream.")
            } else {
                totalWritten += written
            }
        }
        buffer.removeAll(keepingCapacity: true)
    }

    deinit {
        try? close()
    }
}
