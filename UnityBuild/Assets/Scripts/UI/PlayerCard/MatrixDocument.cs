using System.Collections.Generic;

namespace GameManagement.Data
{
    [System.Serializable]
    public class MatrixDocumentWrapper
    {
        public List<MatrixDocument> data;
    }
    [System.Serializable]
    public class MatrixDocument
    {
        public string id;
        public string type;
        public List<int> cardPool;
        public Dictionary<string, List<List<int>>> matrixMap;
        public int version;
    }
}
