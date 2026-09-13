// rsblock.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//
// Original author: Kazuhiko Arase, 2009
// URL: http://www.d-project.com/
// Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
//
// The word "QR Code" is a registered trademark of
// DENSO WAVE INCORPORATED
// http://www.denso-wave.com/qrcode/faqpatent-e.html
//
// Modified and adapted for use in PDFjet by PDFjet Software

package qrcode

// qrRSBlock describes the qrRSBlock object.
type qrRSBlock struct {
	totalCount int
	dataCount  int
}

// newQRRSBlock constructs qrRSBlock objects.
func newQRRSBlock(totalCount, dataCount int) *qrRSBlock {
	rsblock := new(qrRSBlock)
	rsblock.totalCount = totalCount
	rsblock.dataCount = dataCount
	return rsblock
}

func (rsblock *qrRSBlock) getDataCount() int {
	return rsblock.dataCount
}

func (rsblock *qrRSBlock) getTotalCount() int {
	return rsblock.totalCount
}

func (rsblock *qrRSBlock) getRSBlocks(errorCorrectionLevel ErrorCorrectionLevel) []*qrRSBlock {
	rsBlock := rsblock.getRsBlockTable(errorCorrectionLevel)
	length := len(rsBlock) / 3
	list := make([]*qrRSBlock, 0)
	for i := 0; i < length; i++ {
		count := rsBlock[3*i]
		totalCount := rsBlock[3*i+1]
		dataCount := rsBlock[3*i+2]
		for j := 0; j < count; j++ {
			list = append(list, newQRRSBlock(totalCount, dataCount))
		}
	}
	return list
}

func (rsblock *qrRSBlock) getRsBlockTable(errorCorrectionLevel ErrorCorrectionLevel) []int {
	buf := make([]int, 0)
	if errorCorrectionLevel == ErrorCorrectionLevelL {
		buf = append(buf, 1)
		buf = append(buf, 100)
		buf = append(buf, 80)
		return buf
	} else if errorCorrectionLevel == ErrorCorrectionLevelM {
		buf = append(buf, 2)
		buf = append(buf, 50)
		buf = append(buf, 32)
		return buf
	} else if errorCorrectionLevel == ErrorCorrectionLevelQ {
		buf = append(buf, 2)
		buf = append(buf, 50)
		buf = append(buf, 24)
		return buf
	} else if errorCorrectionLevel == ErrorCorrectionLevelH {
		buf = append(buf, 4)
		buf = append(buf, 25)
		buf = append(buf, 9)
		return buf
	}
	return nil
}
