using System.Collections.Generic;

namespace GameManagement.Data
{
    [System.Serializable]
    public class MatrixDocumentWrapper
    {
        public List<MatrixDocument> Documents;
    }
    [System.Serializable]
    public class MatrixDocument
    {
        public string Id;
        public string Type;
        public List<int> CardPool;
        public Dictionary<string, List<List<int>>> MatrixMap;
        public int Version;
    }
}
