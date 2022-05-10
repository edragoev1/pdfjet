/**
 *
Copyright 2009 Kazuhiko Arase

URL: http://www.d-project.com/

Licensed under the MIT license:
  http://www.opensource.org/licenses/mit-license.php

The word "QR Code" is registered trademark of
DENSO WAVE INCORPORATED
  http://www.denso-wave.com/qrcode/faqpatent-e.html
*/
import Foundation


/**
 * Polynomial
 * @author Kazuhiko Arase
 */
class Polynomial {

    private var num: [Int]

    init(_ num: [Int], _ shift: Int) {
        var offset = 0
        while offset < num.count && num[offset] == 0 {
            offset += 1
        }
        self.num = [Int](repeating: 0, count: num.count - offset + shift)
        for i in 0..<(num.count - offset) {
            self.num[i] = num[offset + i]
        }
    }

    func get(_ index: Int) -> Int {
        return self.num[index]
    }

    func getLength() -> Int {
        return self.num.count
    }

    func multiply(_ polynomial: Polynomial) -> Polynomial {
        var num = [Int](repeating: 0, count: ((getLength() + polynomial.getLength()) - 1))
        for i in 0..<getLength() {
            for j in 0..<polynomial.getLength() {
                num[i + j] ^=
                        QRMath.singleton.gexp(QRMath.singleton.glog(get(i)) +
                        QRMath.singleton.glog(polynomial.get(j)))
            }
        }
        return Polynomial(num, 0)
    }

    func mod(_ polynomial: Polynomial) -> Polynomial {
        if (getLength() - polynomial.getLength()) < 0 {
            return self
        }

        let ratio = QRMath.singleton.glog(get(0)) - QRMath.singleton.glog(polynomial.get(0))
        var num = [Int](repeating: 0, count: getLength())
        for i in 0..<getLength() {
            num[i] = get(i)
        }

        for i in 0..<polynomial.getLength() {
            num[i] ^= QRMath.singleton.gexp(
                    QRMath.singleton.glog(polynomial.get(i)) + ratio)
        }

        return Polynomial(num, 0).mod(polynomial)
    }
}
