/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// The Swift port's side of the corpus check, tests/corpus/check-corpus.py. It
// reads one PDF, merges the whole of it into a document of its own, and splits
// its first and its last page into two more:
//
//     swift run -c release CorpusSwift IN.pdf PASSWORD OUTDIR
//
// It writes merged.pdf, first.pdf and last.pdf into OUTDIR, the ones it could
// make, and prints what it read as JSON: the error, or the number of pages and
// the size of each page. An error that is thrown is the reader refusing the
// PDF. A trap -- an index out of range, a nil unwrapped, an arithmetic
// overflow -- cannot be caught in Swift, and in a release build it has no
// message: the process dies of a signal. The harness catches the signal and
// prints the crash, the signal and the step it was at, as its JSON, in place
// of the Swift backtracer. To see where the trap is, run the harness alone
// with SWIFT_BACKTRACE=enable=yes, which leaves the signal to the backtracer.
import Foundation
import PDFjet

struct Report: Encodable {
    var error: String?
    var crash: String?
    var pages = 0
    var sizes = [[Float]]()
    var made = [String: String]()
}

// The message of an error that was thrown.
func message(_ error: Error) -> String {
    return String(describing: error)
}

// The end of the JSON that the signal handler prints, which step sets: a
// signal handler can only write what was made before.
nonisolated(unsafe) let crashEnd = UnsafeMutablePointer<UInt8>.allocate(capacity: 1024)
nonisolated(unsafe) var crashEndLength = 0

// Tells the signal handler what the harness does.
func step(_ what: String) {
    let json = Array(" while \(what)\",\"made\":{},\"pages\":0,\"sizes\":[]}\n".utf8.prefix(1024))
    crashEnd.update(from: json, count: json.count)
    crashEndLength = json.count
}

// Prints the crash of a signal, as the JSON of the harness, and exits.
func crashed(_ signal: Int32) {
    let start: StaticString
    switch signal {
    case SIGILL: start = "{\"crash\":\"SIGILL, a trap,"
    case SIGTRAP: start = "{\"crash\":\"SIGTRAP, a trap,"
    case SIGSEGV: start = "{\"crash\":\"SIGSEGV, a stack overflow or a bad pointer,"
    case SIGBUS: start = "{\"crash\":\"SIGBUS,"
    case SIGFPE: start = "{\"crash\":\"SIGFPE,"
    default: start = "{\"crash\":\"SIGABRT,"
    }
    _ = Glibc.write(1, start.utf8Start, start.utf8CodeUnitCount)
    _ = Glibc.write(1, crashEnd, crashEndLength)
    _exit(128 + signal)
}

// Catches the signals of a crash, on a stack of their own, which a stack
// overflow leaves the handler.
func catchCrashes() {
    var stack = stack_t()
    stack.ss_size = 1 << 16
    stack.ss_sp = UnsafeMutableRawPointer.allocate(byteCount: stack.ss_size, alignment: 16)
    sigaltstack(&stack, nil)
    var action = sigaction()
    action.__sigaction_handler.sa_handler = crashed
    action.sa_flags = SA_ONSTACK
    for signal in [SIGILL, SIGTRAP, SIGSEGV, SIGBUS, SIGFPE, SIGABRT] {
        sigaction(signal, &action, nil)
    }
}

// Merges the pages into path, all of them when pages is nil, and returns "ok"
// or the error.
func write(_ objects: [PDFobj], _ path: String, _ pages: [Int]? = nil) -> String {
    step("writing \((path as NSString).lastPathComponent)")
    guard let stream = OutputStream(toFileAtPath: path, append: false) else {
        return "\(path) cannot be written."
    }
    do {
        let pdf = PDF(stream)
        if let pages = pages {
            try pdf.merge(objects, pages)
        } else {
            try pdf.merge(objects)
        }
        try pdf.complete()
    } catch {
        stream.close()
        try? FileManager.default.removeItem(atPath: path)
        return message(error)
    }
    return "ok"
}

// Reads the PDF, merges and splits it into dir, and returns what it read.
func run(_ path: String, _ password: String, _ dir: String) -> Report {
    var out = Report()
    guard let data = FileManager.default.contents(atPath: path) else {
        out.error = "\(path) cannot be read."
        return out
    }
    func read(_ what: String) throws -> [PDFobj] {
        step("reading \(what)")
        return try PDF().read(from: InputStream(data: data), password: password)
    }
    let objects: [PDFobj]
    do {
        objects = try read("the PDF")
    } catch {
        out.error = message(error)
        return out
    }
    step("finding the pages")
    let pages = PDF().getPageObjects(from: objects)
    out.pages = pages.count
    for page in pages {
        let size = page.getPageSize()
        out.sizes.append([size.getWidth(), size.getHeight()])
    }
    out.made["merged"] = write(objects, dir + "/merged.pdf")
    if !pages.isEmpty {
        // Each document is read again: a merge may change what it merges.
        for (name, number) in [("first", 1), ("last", pages.count)] {
            do {
                out.made[name] = write(try read("the PDF again"), dir + "/\(name).pdf", [number])
            } catch {
                out.made[name] = message(error)
            }
        }
    }
    return out
}

let args = CommandLine.arguments
if args.count != 4 {
    FileHandle.standardError.write(Data("usage: CorpusSwift IN.pdf PASSWORD OUTDIR\n".utf8))
    exit(2)
}
if ProcessInfo.processInfo.environment["SWIFT_BACKTRACE"] == nil {
    catchCrashes()
}
let encoder = JSONEncoder()
encoder.outputFormatting = .sortedKeys
FileHandle.standardOutput.write(try encoder.encode(run(args[1], args[2], args[3])) + Data("\n".utf8))
