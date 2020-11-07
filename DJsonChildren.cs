using System.Collections;
using System.Collections.Generic;
using System.DJ.DJson.Commons;
using System.Reflection;

namespace System.DJ.DJson
{
    public class DJsonChildren : IEnumerable<DJsonItem>
    {
        string json = "";
        DJson dJson = null;
        object enuma = null;

        public DJsonChildren(string json)
        {
            this.json = json;
            dJson = null;
            enuma = null;
        }

        public DJsonItem this[string key]
        {
            get
            {
                initDJson();
                return dJson[key];
            }
        }

        public DJsonItem this[int index]
        {
            get
            {
                initDJson();
                return dJson[index];
            }
        }

        public void ForEach(Action<DJsonItem> action)
        {
            initDJson();
            dJson.ForEach(item =>
            {
                action(item);
            });
        }

        public void ForEach(Func<DJsonItem, bool> func)
        {
            initDJson();
            dJson.ForEach(item =>
            {
                return func(item);
            });
        }

        public IEnumerator<DJsonItem> enumeratorDJsonItem { get; set; }
        public IEnumerator enumerator { get; set; }

        IEnumerator<DJsonItem> IEnumerable<DJsonItem>.GetEnumerator()
        {
            initDJson();
            initEnumerator(enumeratorDJsonItem);
            return enumeratorDJsonItem;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            initDJson();
            initEnumerator(enumerator);
            return enumerator;
        }

        void initEnumerator(object obj)
        {
            if (null == obj) return;
            if (null != enuma) return;
            enuma = obj;
            Type type = obj.GetType();
            MethodInfo method = type.GetMethod("init");
            method.Invoke(obj, new object[] { dJson });
        }

        void initDJson()
        {
            dJson = null == dJson ? DJson.From(json) : dJson;
        }
    }
}
