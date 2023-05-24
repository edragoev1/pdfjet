using System.Collections.Generic;

namespace PDFjet.NET {
/**
 * RSBlock
 * @author Kazuhiko Arase
 */
class RSBlock {
    private int totalCount;
    private int dataCount;

    private RSBlock(int totalCount, int dataCount) {
        this.totalCount = totalCount;
        this.dataCount  = dataCount;
    }

    public int GetDataCount() {
        return dataCount;
    }

    public int GetTotalCount() {
        return totalCount;
    }

    public static RSBlock[] GetRSBlocks(int errorCorrectLevel) {
        int[] rsBlock = GetRsBlockTable(errorCorrectLevel);
        int length = rsBlock.Length / 3;

        List<RSBlock> list = new List<RSBlock>();
        for (int i = 0; i < length; i++) {
            int count = rsBlock[3*i];
            int totalCount = rsBlock[3*i + 1];
            int dataCount  = rsBlock[3*i + 2];

            for (int j = 0; j < count; j++) {
                list.Add(new RSBlock(totalCount, dataCount));
            }
        }

        return list.ToArray();
    }

    private static int[] GetRsBlockTable(int errorCorrectLevel) {
        switch(errorCorrectLevel) {
        case ErrorCorrectLevel.L :
            return new int[] {1, 100, 80};
        case ErrorCorrectLevel.M :
            return new int[] {2, 50, 32};
        case ErrorCorrectLevel.Q :
            return new int[] {2, 50, 24};
        case ErrorCorrectLevel.H :
            return new int[] {4, 25, 9};
        }
        return null;
    }
}
}   // End of namespace PDFjet.NET
