using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System.DJ.DJson.Commons
{
    public enum OrderBy { asc, desc }

    /// <summary>
    /// 维护及变更： 
    /// 1. 2020-04-27 [查询速度及add速度优化]数据存储机制由单体集合变更为多个集合体,多任务查询,以提高查询速度,由于是多个集合体模式,所以必须重构循环体,重新实现 IEnumerable<T> 接口
    /// Author: 代久 - Allan
    /// QQ: 564343162
    /// Email: 564343162@qq.com
    /// CreateDate: 2020-03-05
    /// </summary>
    public class EList<T> : IEnumerable<T> where T : CKeyValue
    {
        private List<ChildList> cKeyValues = new List<ChildList>();
        private object _obj = new object();

        public EList() : base() { }

        public T this[string key]
        {
            get
            {
                lock (_obj)
                {
                    bool mbool = false;
                    CKeyValue kv = new CKeyValue() { Key = key };
                    getContainer((T)kv, (cKeyValue) => {
                        kv.Value = cKeyValue.Value;
                        kv.other = cKeyValue.other;
                        kv.index = cKeyValue.index;
                        kv.isReset = cKeyValue.isReset;
                        mbool = true;
                    });

                    if (!mbool) return null;
                    return (T)kv;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_obj)
                {
                    IEnumerator<T> ienum = new Enumerator(this, cKeyValues);
                    ((Enumerator)ienum).index = index;
                    ienum.MoveNext();
                    ienum.Reset();
                    object current = ((Enumerator)ienum).current;
                    if (null == current) return default(T);
                    return (T)current;
                }
            }
        }

        public void Add(T cKeyValue)
        {
            lock (_obj)
            {
                ChildList keyValues = getContainer(cKeyValue, null);

                if (null == keyValues)
                {
                    if (0 == cKeyValues.Count)
                    {
                        keyValues = new ChildList(cKeyValues);
                        cKeyValues.Add(keyValues);
                    }
                    else
                    {
                        keyValues = cKeyValues[cKeyValues.Count - 1];
                    }
                }
                else
                {
                    return;
                }

                if (keyValues.Count == UnitMaxNumber)
                {
                    keyValues = new ChildList(cKeyValues);
                    cKeyValues.Add(keyValues);
                }

                keyValues.Add(cKeyValue);
            }
        }

        public int Count
        {
            get
            {
                lock (_obj)
                {
                    int n = 0;
                    foreach (ChildList item in cKeyValues)
                    {
                        n += item.Count;
                    }
                    return n;
                }
            }
        }

        public void Clear(string key)
        {
            lock (_obj)
            {
                CKeyValue kv = new CKeyValue() { Key = key };
                ChildList ts = getContainer((T)kv, null);
                if (null == ts) return;
                ts.EClear(key);
            }
        }

        public void Clear()
        {
            lock (_obj)
            {
                if (0 == cKeyValues.Count) return;
                foreach (ChildList item in cKeyValues)
                {
                    item.Clear();
                }

                cKeyValues.Clear();
            }
        }

        public void SetDataCount(int count)
        {
            int n = count / UnitMaxNumber;
            if (20 < n)
            {
                UnitMaxNumber = count / 20;
            }
        }

        public void ForEach(Func<T, int, bool> func)
        {
            IEnumerable<T> list = this;
            int n = 0;
            bool mbool = false;
            foreach (var item in list)
            {
                mbool = func(item, n);
                if (!mbool) break;
                n++;
            }
        }

        public void ForEach(Action<T> action)
        {
            ForEach((o, index) =>
            {
                action(o);
                return true;
            });
        }

        ChildList getContainer(T cKeyValue, Action<T> action)
        {
            ChildList keyValues = null;
            if (0 == cKeyValues.Count) return keyValues;

            Task[] tasks = new Task[cKeyValues.Count];
            int n = 0;
            foreach (ChildList item in cKeyValues)
            {
                tasks[n] = new Task((o) => {
                    ChildList cKeys = (ChildList)o;
                    CKeyValue kv = cKeys[cKeyValue.Key];
                    if (null != kv)
                    {
                        keyValues = cKeys;
                        if (null != action) action((T)kv);
                    }
                }, item);
                tasks[n].Start();
                n++;
            }
            Task.WaitAll(tasks, -1);

            return keyValues;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this, cKeyValues);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, cKeyValues);
        }

        int UnitMaxNumber { get; set; } = 20000;

        public class Enumerator : IEnumerator<T>, IEnumerator
        {
            private List<ChildList> cKeyValues = null;
            private int ncount = 0;
            private EList<T> elist = null;

            public Enumerator(EList<T> elist, List<ChildList> cKeyValues)
            {
                this.elist = elist;
                this.cKeyValues = cKeyValues;
                if (null == cKeyValues) return;
                ncount = elist.Count;
            }

            object IEnumerator.Current => current;

            T IEnumerator<T>.Current => (T)current;

            void IDisposable.Dispose()
            {
                //执行 foreach 后释放数据集
                //current = null;
                //if (null == cKeyValues) return;
                //elist.Clear();
            }

            bool IEnumerator.MoveNext()
            {
                if (0 == ncount) return false;
                if (index >= ncount) return false;

                int n = 0;
                int n1 = 0;
                int nlen = 0;
                ChildList clist = null;
                foreach (ChildList item in cKeyValues)
                {
                    nlen = item.Count;
                    n += nlen;
                    if ((index < n) && (index >= n1))
                    {
                        clist = item;
                        break;
                    }
                    n1 += nlen;
                }

                n = nlen - (n - index);
                current = clist[n];

                index++;
                return true;
            }

            void IEnumerator.Reset()
            {
                index = 0;
            }

            public int index { get; set; } = 0;

            public object current { get; set; }
        }

        public class ChildList : List<T>
        {
            List<ChildList> cKeyValues = null;
            public ChildList(List<ChildList> cKeyValues) : base()
            {
                this.cKeyValues = cKeyValues;
            }

            void removeMyself()
            {
                IList kvlist = this;
                if (0 < kvlist.Count) return;
                ChildList cTs = kvlist as ChildList;
                if (null == cTs) return;
                cKeyValues.Remove(cTs);
            }

            public void EClear(string key)
            {
                int n = GetIndexByKeyVal(key);
                if (-1 == n) return;

                IList kvlist = this;
                CKeyValue kv = (CKeyValue)kvlist[n];

                if (kv != null)
                {
                    kvlist.Remove(kv);
                    removeMyself();
                }
            }

            public OrderBy orderBy { get; set; }

            public CKeyValue this[string key]
            {
                get
                {
                    int n = GetIndexByKeyVal(key);
                    if (-1 == n) return null;

                    IList kvlist = this;
                    return (CKeyValue)kvlist[n];
                }
            }

            public CKeyValue this[string key, bool isLike]
            {
                get
                {
                    CKeyValue kv = null;
                    CKeyValue kv1 = null;
                    IList kvlist = this;
                    whileNum = 0;

                    foreach (object item in kvlist)
                    {
                        whileNum++;
                        kv1 = item as CKeyValue;
                        if (kv1 != null)
                        {
                            if (isLike)
                            {
                                if (kv1.Key.IndexOf(key) != -1 || key.IndexOf(kv1.Key) != -1)
                                {
                                    kv = kv1;
                                    break;
                                }
                            }
                            else
                            {
                                if (kv1.Key.Equals(key))
                                {
                                    kv = kv1;
                                    break;
                                }
                            }

                        }
                    }
                    return kv;
                }
            }

            public void Add(CKeyValue cKeyValue)
            {
                int m = GetIndexByKeyVal(cKeyValue.Key);
                if (-1 != m) return;

                IList kvlist = this;

                int t1 = (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond;
                int n = GetIndexByNewKeyVal(cKeyValue);
                int t2 = (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond;
                int t3 = t2 - t1;
                if (findPositionTime < t3) findPositionTime = t3;

                if (-1 == n)
                {
                    kvlist.Add(cKeyValue);
                    return;
                }

                t1 = (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond;
                kvlist.Insert(n, cKeyValue);
                t2 = (DateTime.Now.Second * 1000) + DateTime.Now.Millisecond;
                t3 = t2 - t1;
                if (insertTime < t3) insertTime = t3;
            }

            public int findPositionTime { get; set; }
            public int insertTime { get; set; }
            public int whileNum { get; set; }

            /// <summary>
            /// 返回值为-1时，新增项赋加在最末尾，应使用Add方法 
            /// </summary>
            /// <param name="ckv"></param>
            /// <returns></returns>
            int GetIndexByNewKeyVal(CKeyValue ckv)
            {
                whileNum = 0;

                int index = -2;

                IList kvlist = this;
                if (0 == kvlist.Count) return -1;

                CKeyValue kv = null;
                int cmp = 0;
                if (1 == kvlist.Count)
                {
                    kv = (CKeyValue)kvlist[0];
                    cmp = Compare(kv.index, ckv.index);
                    index = 1 == cmp ? 0 : -1;
                    return index;
                }

                int n1 = 0, n2 = kvlist.Count - 1;
                kv = (CKeyValue)kvlist[n1];
                CKeyValue kv1 = (CKeyValue)kvlist[n2];

                cmp = Compare(kv.index, ckv.index);
                int cmp1 = Compare(kv1.index, ckv.index);

                if (1 == cmp)
                {
                    index = 0;
                    return index;
                }

                if (2 == cmp1)
                {
                    index = -1;
                    return index;
                }

                int len = kvlist.Count;
                n1 = 0;
                n2 = (len - 1) / 2;
                int n3 = n2 + 1, n4 = len - 1;

                //0 相等, 1 source>target, 2 source<target
                while (-2 == index)
                {
                    whileNum++;

                    if (n1 == n2 && n3 == n4)
                    {
                        kv = (CKeyValue)kvlist[n1];
                        cmp = Compare(kv.index, ckv.index);

                        if (1 == cmp)
                        {
                            index = n2;
                        }
                        else
                        {
                            index = n3;
                        }

                        break;
                    }

                    kv1 = (CKeyValue)kvlist[n2];
                    cmp1 = Compare(kv1.index, ckv.index);
                    if (1 == cmp1)
                    {
                        n4 = n2;
                        n2 = (n2 - n1) / 2 + n1;
                        n3 = n2 + 1;
                    }
                    else
                    {
                        n1 = n3;
                        n2 = (n4 - n3) / 2 + n3;
                        n3 = n2 + 1;
                        n3 = n3 > n4 ? n4 : n3;
                    }
                }

                return index;
            }

            /// <summary>
            /// 如果返回 -1 表示key在集合中不存在
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            int GetIndexByKeyVal(string key)
            {
                whileNum = 0;

                int index = -1;
                IList kvlist = this;
                if (0 == kvlist.Count) return -1;

                CKeyValue ckv = new CKeyValue() { Key = key };
                CKeyValue kv = null;
                int cmp = 0;
                if (1 == kvlist.Count)
                {
                    kv = (CKeyValue)kvlist[0];
                    cmp = Compare(kv.index, ckv.index);
                    if (0 == cmp) index = 0;
                    return index;
                }

                int n1 = 0, n2 = kvlist.Count - 1;
                kv = (CKeyValue)kvlist[n1];
                CKeyValue kv1 = (CKeyValue)kvlist[n2];

                cmp = Compare(kv.index, ckv.index);
                int cmp1 = Compare(kv1.index, ckv.index);

                if (1 == cmp)
                {
                    return index;
                }

                if (2 == cmp1)
                {
                    return index;
                }

                int len = kvlist.Count;
                n1 = 0;
                n2 = (len - 1) / 2;
                int n3 = n2 + 1, n4 = len - 1;

                //0 相等, 1 source>target, 2 source<target
                index = -2;
                while (-2 == index)
                {
                    whileNum++;
                    if (n1 == n2 && n3 == n4)
                    {
                        kv = (CKeyValue)kvlist[n1];
                        cmp = Compare(kv.index, ckv.index);
                        kv1 = (CKeyValue)kvlist[n3];
                        cmp1 = Compare(kv1.index, ckv.index);

                        index = -1;
                        if (0 == cmp)
                        {
                            index = n2;
                        }
                        else if (0 == cmp1)
                        {
                            index = n3;
                        }

                        break;
                    }

                    kv1 = (CKeyValue)kvlist[n2];
                    cmp1 = Compare(kv1.index, ckv.index);
                    if (0 == cmp1)
                    {
                        index = n2;
                    }
                    else if (1 == cmp1)
                    {
                        n4 = n2;
                        n2 = (n2 - n1) / 2 + n1;
                        n3 = n2 + 1;
                    }
                    else
                    {
                        n1 = n3;
                        n2 = (n4 - n3) / 2 + n3;
                        n3 = n2 + 1;
                        n3 = n3 > n4 ? n4 : n3;
                    }
                }

                return index;
            }

            IList<int> GetIndexsByContainKey(string key)
            {
                whileNum = 0;

                IList<int> lists = new List<int>();

                IList kvlist = this;
                if (0 == kvlist.Count) return lists;

                CKeyValue ckv = new CKeyValue() { Key = key };
                CKeyValue kv = null;
                int cmp = 0;
                if (1 == kvlist.Count)
                {
                    kv = (CKeyValue)kvlist[0];
                    cmp = Compare(kv.index, ckv.index, true);
                    if (0 == cmp) lists.Add(0);
                    return lists;
                }

                int n1 = 0, n2 = kvlist.Count - 1;
                kv = (CKeyValue)kvlist[n1];
                CKeyValue kv1 = (CKeyValue)kvlist[n2];

                cmp = Compare(kv.index, ckv.index);
                int cmp1 = Compare(kv1.index, ckv.index);

                if (1 == cmp)
                {
                    return lists;
                }

                if (2 == cmp1)
                {
                    return lists;
                }

                int len = kvlist.Count;
                n1 = 0;
                n2 = (len - 1) / 2;
                int n3 = n2 + 1, n4 = len - 1;
                int n5 = 0, n6 = 0;

                //0 相等, 1 source>target, 2 source<target
                while (true)
                {
                    whileNum++;
                    if (n1 == n2 && n3 == n4)
                    {
                        kv = (CKeyValue)kvlist[n1];
                        cmp = Compare(kv.index, ckv.index);
                        kv1 = (CKeyValue)kvlist[n3];
                        cmp1 = Compare(kv1.index, ckv.index);

                        // index = -1;
                        if (0 == cmp)
                        {
                            //index = n2;
                        }
                        else if (0 == cmp1)
                        {
                            //index = n3;
                        }

                        break;
                    }

                    kv1 = (CKeyValue)kvlist[n2];
                    cmp1 = Compare(kv1.index, ckv.index);
                    if (0 == cmp1)
                    {
                        lists.Add(n2);
                    }
                    else if (1 == cmp1)
                    {
                        n4 = n2;
                        n2 = (n2 - n1) / 2 + n1;
                        n3 = n2 + 1;
                    }
                    else
                    {
                        n1 = n3;
                        n2 = (n4 - n3) / 2 + n3;
                        n3 = n2 + 1;
                        n3 = n3 > n4 ? n4 : n3;
                    }
                    break;
                }

                return lists;
            }

            int Compare(int[] source, int[] target)
            {
                return Compare(source, target, false);
            }

            /// <summary>
            /// 0 相等, 1 source>target, 2 source<target
            /// </summary>
            /// <param name="source"></param>
            /// <param name="target"></param>
            /// <returns></returns>
            int Compare(int[] source, int[] target, bool isContain)
            {
                int n = 0;
                int len = source.Length;
                int x1, x2;

                if (isContain)
                {
                    if (len >= target.Length)
                    {
                        n = 1;
                        int len1 = target.Length;
                        int m = 0;
                        int i = 0;
                        while (i < len)
                        {
                            x1 = source[i];
                            x2 = target[m];
                            if (x1 == x2)
                            {
                                m++;
                            }
                            else
                            {
                                i -= m;
                                m = 0;
                            }

                            if (m == len1)
                            {
                                n = 0;
                                break;
                            }
                            i++;
                        }
                    }
                }
                else
                {
                    len = target.Length < len ? target.Length : len;
                    for (int i = 0; i < len; i++)
                    {
                        x1 = source[i];
                        x2 = target[i];
                        if (x1 > x2)
                        {
                            n = 1;
                            break;
                        }
                        else if (x1 < x2)
                        {
                            n = 2;
                            break;
                        }
                    }
                }

                if (0 == n)
                {
                    if (source.Length > target.Length)
                    {
                        n = 1;
                        if (isContain)
                        {
                            n = 0;
                        }
                    }
                    else if (source.Length < target.Length)
                    {
                        n = 2;
                    }
                }
                return n;
            }
        }
    }

    public class CKeyValue : IComparable<CKeyValue>
    {
        static Regex isNum = null;
        string key = "";

        static CKeyValue()
        {
            isNum = new Regex("[0-9]");
        }

        public string Key
        {
            get
            {
                return this.key;
            }
            set
            {
                if (string.IsNullOrEmpty(this.key) || isReset)
                {
                    this.key = value;
                    createIndex(key);
                    isReset = false;
                }
            }
        }

        void createIndex(string key)
        {
            char[] arr = key.ToCharArray();
            int len = arr.Length;
            index = new int[len];
            for (int i = 0; i < len; i++)
            {
                if (isNum.IsMatch(arr[i].ToString()))
                {
                    index[i] = Convert.ToInt32(arr[i].ToString());
                }
                else
                {
                    index[i] = arr[i];
                }
            }
        }

        public int[] index { get; set; }

        public bool isReset { get; set; }
        public object Value { get; set; }

        public object other { get; set; }

        public Type ValueType { get; set; }

        public Type otherType { get; set; }

        public int orderBy { get; set; }

        int IComparable<CKeyValue>.CompareTo(CKeyValue other)
        {
            int n = key.CompareTo(other.key);

            return n;
        }

    }
}
