package dproject

/**
 *
Copyright (c) 2009 Kazuhiko Arase

URL: http://www.d-project.com/

Licensed under the MIT license:
  http://www.opensource.org/licenses/mit-license.php

The word "QR Code" is registered trademark of
DENSO WAVE INCORPORATED
  http://www.denso-wave.com/qrcode/faqpatent-e.html
*/

// RSBlock describes the RSBlock object.
type RSBlock struct {
	totalCount int
	dataCount  int
}

// NewRSBlock constructs RSBlock objects.
func NewRSBlock(totalCount, dataCount int) *RSBlock {
	rsblock := new(RSBlock)
	rsblock.totalCount = totalCount
	rsblock.dataCount = dataCount
	return rsblock
}

func (rsblock *RSBlock) getDataCount() int {
	return rsblock.dataCount
}

func (rsblock *RSBlock) getTotalCount() int {
	return rsblock.totalCount
}

func (rsblock *RSBlock) getRSBlocks(errorCorrectLevel int) []*RSBlock {
	rsBlock := rsblock.getRsBlockTable(errorCorrectLevel)
	length := len(rsBlock) / 3
	list := make([]*RSBlock, 0)
	for i := 0; i < length; i++ {
		count := rsBlock[3*i]
		totalCount := rsBlock[3*i+1]
		dataCount := rsBlock[3*i+2]
		for j := 0; j < count; j++ {
			list = append(list, NewRSBlock(totalCount, dataCount))
		}
	}
	return list
}

func (rsblock *RSBlock) getRsBlockTable(errorCorrectLevel int) []int {
	buf := make([]int, 0)
	if errorCorrectLevel == ErrorCorrectLevelL {
		buf = append(buf, 1)
		buf = append(buf, 100)
		buf = append(buf, 80)
		return buf
	} else if errorCorrectLevel == ErrorCorrectLevelM {
		buf = append(buf, 2)
		buf = append(buf, 50)
		buf = append(buf, 32)
		return buf
	} else if errorCorrectLevel == ErrorCorrectLevelQ {
		buf = append(buf, 2)
		buf = append(buf, 50)
		buf = append(buf, 24)
		return buf
	} else if errorCorrectLevel == ErrorCorrectLevelH {
		buf = append(buf, 4)
		buf = append(buf, 25)
		buf = append(buf, 9)
		return buf
	}
	return nil
}
