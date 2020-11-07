namespace System.DJ.DJson
{
    public struct DJsonItem
    {
        public string key;
        public object value;
        public int index;
        public int count;
        public bool isJsonOfValue;
        public bool isArrayItemOfValue;
        public DJsonChildren children;
    }
}
