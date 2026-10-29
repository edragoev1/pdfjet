// swift-tools-version: 6.1
import PackageDescription

let package = Package(
    name: "PDFjet",
    // Other packages depend on the library with
    // .product(name: "PDFjet", package: "pdfjet"); the examples are
    // executable targets that only this package builds.
    products: [
        .library(name: "PDFjet", targets: ["PDFjet"]),
    ],
    targets: [
        // The fonts PDFjet carries, resources/fonts, each family in a folder
        // with its license: the one copy of them in the repository, which
        // the other ports read too. SwiftPM takes the resources of a target in
        // its folder alone, so they are a target of their own, whose
        // PDFjetFonts.swift finds them; fonts.go is the Go port's.
        .target(
            name: "PDFjetFonts",
            path: "resources/fonts",
            exclude: ["fonts.go"],
            sources: ["PDFjetFonts.swift"],
            resources: [
                .copy("CascadiaMono"),
                .copy("IBMPlexMath"),
                .copy("IBMPlexSans"),
                .copy("IBMPlexSansArabic"),
                .copy("IBMPlexSansHebrew"),
                .copy("IBMPlexSansJP"),
                .copy("IBMPlexSansKR"),
                .copy("IBMPlexSansSC"),
                .copy("IBMPlexSansTC"),
                .copy("IBMPlexSansThai"),
                .copy("IBMPlexSerif"),
                .copy("SourceSerif4"),
            ]
        ),
        .target(name: "PDFjet", dependencies: ["PDFjetFonts"]),
        .executableTarget(name: "Example_01", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_02", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_03", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_04", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_05", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_06", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_07", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_08", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_09", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_10", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_11", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_12", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_13", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_14", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_15", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_16", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_17", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_18", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_19", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_20", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_21", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_22", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_23", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_24", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_25", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_26", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_27", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_28", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_29", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_30", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_31", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_32", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_33", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_34", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_35", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_36", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_37", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_38", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_39", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_40", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_41", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_42", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_43", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_44", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_45", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_46", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_47", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_48", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_49", dependencies: ["PDFjet"]),
        .executableTarget(name: "Example_50", dependencies: ["PDFjet"]),
        .executableTarget(name: "BookletSnippets", dependencies: ["PDFjet"], path: "booklet/snippets/swift"),
        // The harness of tests/corpus/check-corpus.py --port swift.
        .executableTarget(name: "CorpusSwift", dependencies: ["PDFjet"], path: "tests/corpus/swift"),
        // The Swift side of tests/crossport/check-crossport.sh.
        .executableTarget(name: "CrossPortSwift", dependencies: ["PDFjet"], path: "tests/crossport/swift"),
        // The unit tests, run with ./test-swift.sh or swift test.
        .testTarget(name: "PDFjetTests", dependencies: ["PDFjet"], path: "tests/swift"),
    ]
)
