import Foundation

public class HuffmanCodes {
    //
    // Lit Value    Bits        Codes
    // ---------    ----        -----
    //   0 - 143     8          00110000 through
    //                          10111111
    // 144 - 255     9          110010000 through
    //                          111111111
    // 256 - 279     7          0000000 through
    //                          0010111
    // 280 - 287     8          11000000 through
    //                          11000111
    //
    var codes = [UInt32](repeating: 0, count: 288)
    var nbits = [UInt32](repeating: 0, count: 288)

    init() {
        var index = 0
        var i = 0
        var code: UInt32 = 0b00110000
        while i < 144 {
            codes[index] = code
            nbits[index] = 8
            index += 1
            code += 1
            i += 1
        }
        code = 0b110010000
        while i < 256 {
            codes[index] = code
            nbits[index] = 9
            index += 1
            code += 1
            i += 1
        }
        code = 0b0000000
        while i < 280 {
            codes[index] = code
            nbits[index] = 7
            index += 1
            code += 1
            i += 1
        }
        code = 0b11000000
        while i < 288 {
            codes[index] = code
            nbits[index] = 8
            index += 1
            code += 1
            i += 1
        }
    }
}
