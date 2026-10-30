// swift-tools-version: 6.1
import PackageDescription

let package = Package(
    name: "PDFjet",
    // Other packages depend on the library with
    // .product(name: "PDFjet", package: "pdfjet"); the examples are
    // executable targets that only this package builds.
    products: [
        // PDFjet, with the fonts PDFjet carries for every European language,
        // in Latin, Greek and Cyrillic, about 11 MB, in the bundle of its
        // target PDFjetFonts.
        .library(name: "PDFjet", targets: ["PDFjet"]),
        // The fonts of every other script, IBM Plex Sans JP, KR, SC and TC,
        // and IBM Plex Sans Arabic, Hebrew and Thai, about 52 MB, for an app
        // that draws in them:
        // .product(name: "PDFjetFontsWorld", package: "pdfjet").
        .library(name: "PDFjetFontsWorld", targets: ["PDFjetFontsWorld"]),
        // The European fonts, which the product PDFjet has from 9.1.7: kept
        // so that an app that listed it still builds; it adds nothing to
        // PDFjet.
        .library(name: "PDFjetFonts", targets: ["PDFjetFonts"]),
    ],
    targets: [
        // The fonts PDFjet carries, resources/fonts, each family in a folder
        // with its license: the one copy of them in the repository, which
        // the other ports read too. SwiftPM takes the resources of a target in
        // its folder alone, so they are targets of their own: PDFjetFonts,
        // the European fonts, which PDFjet depends on, and PDFjetFontsWorld,
        // those of the other scripts, the folder world, which PDFjet does not
        // depend on: it finds their bundle, PDFjet_PDFjetFontsWorld, by its
        // name, in an app that has it (the owner, 30 October 2026). .process
        // puts the files of the family folders in the bundle side by side,
        // and leaves out what exclude names, as .copy of a folder does not:
        // the .go files, the Go port's, the folder cjk, its deprecated
        // package, and the license of IBM Plex but one, the same text in
        // each IBM Plex folder.
        .target(
            name: "PDFjetFonts",
            path: "resources/fonts",
            exclude: [
                "fonts.go",
                "cjk",
                "world",
                "IBMPlexSerif/license.txt",
            ],
            sources: ["PDFjetFonts.swift"],
            resources: [
                .process("CascadiaMono"),
                .process("IBMPlexSans"),
                .process("IBMPlexSerif"),
            ]
        ),
        .target(
            name: "PDFjetFontsWorld",
            path: "resources/fonts/world",
            exclude: [
                "world.go",
                "IBMPlexSansKR/license.txt",
                "IBMPlexSansSC/license.txt",
                "IBMPlexSansTC/license.txt",
                "IBMPlexSansArabic/license.txt",
                "IBMPlexSansHebrew/license.txt",
                "IBMPlexSansThai/license.txt",
            ],
            sources: ["PDFjetFontsWorld.swift"],
            resources: [
                .process("IBMPlexSansJP"),
                .process("IBMPlexSansKR"),
                .process("IBMPlexSansSC"),
                .process("IBMPlexSansTC"),
                .process("IBMPlexSansArabic"),
                .process("IBMPlexSansHebrew"),
                .process("IBMPlexSansThai"),
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
        // The unit tests, run with ./test-swift.sh or swift test, which read
        // the fonts from resources/fonts and from the bundles of PDFjetFonts
        // and PDFjetFontsWorld. The examples run in the repository, and read
        // resources/fonts.
        .testTarget(name: "PDFjetTests", dependencies: ["PDFjet", "PDFjetFontsWorld"], path: "tests/swift"),
    ]
)
