/**
 * RSBlock.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Kazuhiko Arase, 2009
 * URL: http://www.d-project.com/
 * Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
 *
 * The word "QR Code" is a registered trademark of
 * DENSO WAVE INCORPORATED
 * http://www.denso-wave.com/qrcode/faqpatent-e.html
 *
 * Modified and adapted for use in PDFjet by PDFjet Software
 */
import Foundation

class RSBlock {
    private var totalCount = 0
    private var dataCount = 0

    init(_ totalCount: Int, _ dataCount: Int) {
        self.totalCount = totalCount
        self.dataCount  = dataCount
    }

    func getDataCount() -> Int {
        return self.dataCount
    }

    func getTotalCount() -> Int {
        return self.totalCount
    }

    static func getRSBlocks(
            _ errorCorrectLevel: Int) -> [RSBlock] {
        let rsBlock = getRsBlockTable(errorCorrectLevel)
        let length = rsBlock.count / 3
        var list = [RSBlock]()
        for i in 0..<length {
            let count = rsBlock[3*i]
            let totalCount = rsBlock[3*i + 1]
            let dataCount  = rsBlock[3*i + 2]
            for _ in 0..<count {
                list.append(RSBlock(totalCount, dataCount))
            }
        }
        return list
    }

    static func getRsBlockTable(
            _ errorCorrectLevel: Int) -> [Int] {
        if errorCorrectLevel == ErrorCorrectLevel.L {
            return [1, 100, 80]
        } else if errorCorrectLevel == ErrorCorrectLevel.M {
            return [2, 50, 32]
        } else if errorCorrectLevel == ErrorCorrectLevel.Q {
            return [2, 50, 24]
        } else if errorCorrectLevel == ErrorCorrectLevel.H {
            return [4, 25, 9]
        }
        return []
    }
}
