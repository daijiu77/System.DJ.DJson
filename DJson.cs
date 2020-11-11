using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.DJ.DJson.Commons;

namespace System.DJ.DJson
{
    public class DJson : IEnumerable<DJsonItem>
    {
        Regex rgKV_ValOfJsonUnit = null;
        Regex rgKV_ValOfJsonArr = null;
        Regex rgKV_ValOfJsonStr = null;
        Regex rgReplaceSign = null; //替换符匹配

        Regex rgBaseTypeArr = null; //基础数据类型数组
        Regex rgBaseTypeVal = null; //匹配基础类型数组value
        Regex rgJsonUnit = null;
        Regex rgKV = null;

        Regex rgStrJsonVal = null;

        DJsonItem dJsonItem;

        List<object> results = new List<object>();
        Dictionary<string, object> dicResults = new Dictionary<string, object>();
        Dictionary<string, CKeyValue> valueOfUnitStrDic = new Dictionary<string, CKeyValue>();
        Dictionary<string, string> strJsonValDic = new Dictionary<string, string>();

        MatchBigBracketInJson matchBigBracket = null;

        string strJsonValTag = "{0}_repaceStrJsonVal_{0}";

        string json = "";
        int num = 0;

        ReplaceChar[] rcArr = new ReplaceChar[]
        {
            new ReplaceChar(){oldStr=@"\""", newStr = "#$#"}
        };

        class ReplaceChar
        {
            public string oldStr { get; set; }
            public string newStr { get; set; }
        }

        static int max = 9999;

        private DJson()
        {

            rgKV_ValOfJsonUnit = JsonRegex.rgKV_ValOfJsonUnit;

            rgKV_ValOfJsonArr = JsonRegex.rgKV_ValOfJsonArr;

            string s = @"(((?<FieldName1>"")(?<FieldName>[a-z0-9_]+)"")|(?<FieldName>[a-z0-9_]+))(?<SplitStr>\s*\:\s*)""(?<FieldValue>((\{(""?[a-z0-9_]*[""a-z0-9][\s\r\n]*\:[\s\r\n]*[""a-z0-9][a-z0-9_]*""?)+\})|(\[((\,\s*)?\{(""?[a-z0-9_]*[""a-z0-9][\s\r\n]*\:[\s\r\n]*[""a-z0-9][a-z0-9_]*""?)+\}(\s*\,)?)+\])))""[\s\r\n]*((\,[\s\r\n]*[a-z0-9_""])|(\}))";
            rgKV_ValOfJsonStr = new Regex(s, RegexOptions.IgnoreCase);

            rgReplaceSign = JsonRegex.rgReplaceSign;

            rgBaseTypeArr = JsonRegex.rgBaseTypeArr;

            rgJsonUnit = JsonRegex.rgJsonUnit;

            rgKV = JsonRegex.rgKV;

            rgBaseTypeVal = JsonRegex.rgBaseTypeVal;

            rgStrJsonVal = JsonRegex.rgStrJsonVal;
        }

        public static DJson From(string json, int maxNumber)
        {
            DJson djson = new DJson();
            djson.json = json;
            max = maxNumber;
            djson.matchBigBracket = new MatchBigBracketInJson(maxNumber);
            djson.Analyze();
            return djson;
        }

        public static DJson From(string json)
        {
            return From(json, max);
        }

        public static List<T> From<T>(string json, int maxNumber) where T : BaseEntity
        {
            Type type = typeof(T);
            T ele = null;
            List<T> list = new List<T>();

            Action<string> err = (msg) =>
            {
                msg = string.IsNullOrEmpty(msg) ? "json数据格式错误" : msg;
                throw new Exception(msg);
            };

            foreach (DJsonItem item in From(json, maxNumber))
            {
                if (null == item.value) err(null);
                if (typeof(string) != item.value.GetType()) err(null);
                if (0 == item.value.ToString().Length) err(null);
                if (!string.IsNullOrEmpty(item.key)) err("json数据格式错误, 缺少中括号[]");

                try
                {
                    ele = (T)Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                ((BaseEntity)ele).fromJsonUnit(item.value.ToString());
                list.Add(ele);
            }

            return list;
        }

        public static List<T> From<T>(string json) where T : BaseEntity
        {
            return From<T>(json, max);
        }

        public static void ForEach(Func<DJsonItem, bool> func, string json)
        {
            bool mbool = false;
            foreach (DJsonItem item in From(json))
            {
                mbool = func(item);
                if (!mbool) break;
            }
        }

        public static void ForEach(Action<DJsonItem> action, string json)
        {
            ForEach(item =>
            {
                action(item);
                return true;
            }, json);
        }

        public static EList<CKeyValue> JsonUnitToEList(string jsonUnit)
        {
            EList<CKeyValue> cKeyValues = new EList<CKeyValue>();
            CKeyValue ckv = null;
            Type vType = null;
            foreach (var item in DJson.From(jsonUnit))
            {
                ckv = new CKeyValue();
                ckv.Key = item.key.ToLower();
                ckv.other = item.key;
                if (item.isJsonOfValue)
                {
                    ckv.Value = GetChildItem(item.value.ToString(), ref vType);
                    ckv.ValueType = vType;
                }
                else
                {
                    if (null != item.value)
                    {
                        ckv.ValueType = item.value.GetType();
                    }
                    ckv.Value = item.value;
                }
                cKeyValues.Add(ckv);
            }
            return cKeyValues;
        }

        static object GetChildItem(string json, ref Type valType)
        {
            object vObj = null;
            object ele = null;
            int n = 0;
            bool isArr = false;
            Type vType = null;
            foreach (DJsonItem item in DJson.From(json))
            {
                if (0 == n)
                {
                    if (item.isArrayItemOfValue)
                    {
                        vObj = new List<EList<CKeyValue>>();
                        valType = typeof(List<EList<CKeyValue>>);
                        isArr = true;
                    }
                    else
                    {
                        vObj = new EList<CKeyValue>();
                        valType = typeof(EList<CKeyValue>);
                        isArr = false;
                    }
                }

                if (item.isJsonOfValue || (item.isArrayItemOfValue && null == item.key))
                {
                    ele = GetChildItem(item.value.ToString(), ref vType);
                }
                else if (item.isArrayItemOfValue)
                {
                    ele = new EList<CKeyValue>();
                    ((EList<CKeyValue>)ele).Add(new CKeyValue()
                    {
                        Key = null == item.key ? null : item.key.ToLower(),
                        Value = item.value,
                        ValueType = item.value.GetType(),
                        other = item.key
                    });
                }
                else
                {
                    ele = item.value;
                    vType = getTypeByValue(ele);
                }

                if (isArr)
                {
                    ((List<EList<CKeyValue>>)vObj).Add((EList<CKeyValue>)ele);
                }
                else
                {
                    ((EList<CKeyValue>)vObj).Add(new CKeyValue()
                    {
                        Key = item.key.ToLower(),
                        Value = ele,
                        ValueType = vType,
                        other = item.key
                    });
                }
                n++;
            }
            return vObj;
        }

        public DJsonItem this[int index]
        {
            get
            {
                IEnumerator<DJsonItem> ienum = new Enumerator(this);
                ((Enumerator)ienum).index = index;
                ienum.MoveNext();
                DJsonItem item = ienum.Current;
                return item;
            }
        }

        public DJsonItem this[string key]
        {
            get
            {
                dJsonItem.key = null;
                dJsonItem.value = null;
                dJsonItem.isJsonOfValue = false;
                dJsonItem.count = 0;
                dJsonItem.index = -1;
                if (0 < dicResults.Count)
                {
                    object vObj = null;
                    dicResults.TryGetValue(key, out vObj);
                    if (null != vObj)
                    {
                        ValueIndexObj vi = new ValueIndexObj(vObj);

                        object val = vi.value;
                        int index = vi.index;
                        int count = dicResults.Count;
                        DJsonItemInit(ref dJsonItem, val, index, count);
                    }
                }
                return dJsonItem;
            }
        }

        object convertValue(DJsonItem item)
        {
            object v = item.value;
            if (null == v) return v;
            if (0 == valueOfUnitStrDic.Count) return v;

            string vs = v.ToString();
            string s = @"[0-9a-z_][0-9a-z_]*_valueOfJsonStr_[0-9]+_[0-9]*[0-9]";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            int n = 0;
            while (rg.IsMatch(vs) && n < max)
            {
                string sign = rg.Match(vs).Groups[0].Value;
                CKeyValue kv = null;
                valueOfUnitStrDic.TryGetValue(sign, out kv);
                if (null == kv) return v;
                v = kv.Value;
                vs = vs.Replace(sign, v.ToString());
                n++;
            }

            return vs;
        }

        public void ForEach(Func<DJsonItem, bool> func)
        {
            bool mbool = false;
            DJson djson = this;
            foreach (DJsonItem item in djson)
            {
                mbool = func(item);
                if (!mbool) break;
            }
        }

        public void ForEach(Action<DJsonItem> action)
        {
            ForEach(item =>
            {
                action(item);
                return true;
            });
        }

        void Analyze()
        {
            results.Clear();
            dicResults.Clear();
            num = 0;
            if (null == json) return;
            json = json.Trim();
            if (string.IsNullOrEmpty(json)) return;

            repaceStrJsonVal(ref json);

            if ("[" == json.Substring(0, 1) && "]" == json.Substring(json.Length - 1))
            {
                JsonUnit();
                return;
            }

            if ("{" == json.Substring(0, 1) && "}" == json.Substring(json.Length - 1))
            {
                KeyVal();
                return;
            }

            throw new Exception("error: 传入的字符串非json数据");
        }

        /// <summary>
        /// 替换值为 json 格式的字符串为指定的字符,例：{"data": "{\"uid\":\"admin\"}"}, 把值 {\"uid\":\"admin\"} 替换为 0_repaceStrJsonVal_0
        /// </summary>
        /// <param name="json"></param>
        void repaceStrJsonVal(ref string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            int n = 0;
            string s = "";
            string sign = "";
            string key = strJsonValTag;
            while (rgStrJsonVal.IsMatch(json) && n < max)
            {
                s = rgStrJsonVal.Match(json).Groups["Value"].Value;
                sign = DJTools.ExtFormat(key, n.ToString());
                json = json.Replace(s, sign);
                strJsonValDic.Add(sign, s);
                n++;
            }
        }

        /// <summary>
        /// 把指定的字符重置为 json 格式的字符串,例：{"data": "0_repaceStrJsonVal_0"}, 把值 0_repaceStrJsonVal_0 替换为 {\"uid\":\"admin\"}
        /// </summary>
        /// <param name="json"></param>
        void resetStrJsonVal(ref string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            string s = @"(?<sign>";
            s += DJTools.ExtFormat(strJsonValTag, "[0-9]+");
            s += ")";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            int n = 0;
            string sign = "";
            while (rg.IsMatch(json) && max > n)
            {
                sign = rg.Match(json).Groups["sign"].Value;
                s = "";
                strJsonValDic.TryGetValue(sign, out s);
                json = json.Replace(sign, s);
                n++;
            }
        }

        class KeyValObj
        {
            public KeyValObj(object kvObj)
            {
                Type type = kvObj.GetType();
                key = type.GetProperty("key").GetValue(kvObj, null).ToString();
                value = type.GetProperty("value").GetValue(kvObj, null);
                string isArr = type.GetProperty("is_arr").GetValue(kvObj, null).ToString();
                is_arr = Convert.ToBoolean(isArr);

                string isJson= type.GetProperty("is_json").GetValue(kvObj, null).ToString();
                is_json = Convert.ToBoolean(isJson);
            }

            public string key { get; set; }
            public object value { get; set; }

            public bool is_arr { get; set; }

            public bool is_json { get; set; }
        }

        class ValueIndexObj
        {
            public ValueIndexObj() { }

            public ValueIndexObj(object valueIndexObj)
            {
                Type type = valueIndexObj.GetType();
                value = type.GetProperty("val").GetValue(valueIndexObj, null);
                string s = type.GetProperty("index").GetValue(valueIndexObj, null).ToString();
                index = Convert.ToInt32(s);
            }

            public object set(object val, int index)
            {
                return new { val, index };
            }

            public object value { get; set; }
            public int index { get; set; }
        }

        void dictionaryAddItem(object val)
        {
            if (typeof(string) == val.GetType()) return;

            KeyValObj kv = new KeyValObj(val);
            string key = kv.key;
            object obj = null;
            dicResults.TryGetValue(key, out obj);
            if (null != obj) return;
            int index = dicResults.Count;
            object ele = new ValueIndexObj().set(val, index);
            dicResults.Add(key, ele);
        }

        void DJsonItemInit(ref DJsonItem current, object val, int index, int count)
        {
            current.isJsonOfValue = false;
            current.key = null;

            KeyValObj kv = new KeyValObj(val);
            current.key = string.IsNullOrEmpty(kv.key) ? null : kv.key;
            current.value = kv.value;
            current.isArrayItemOfValue = kv.is_arr;
            if (null != current.value)
            {
                if (typeof(string) == current.value.GetType())
                {
                    string v = current.value.ToString();
                    if (v.Equals("|null|"))
                    {
                        current.value = null;
                    }
                    else
                    {
                        current.isJsonOfValue = JsonRegex.rgJsonUnit.IsMatch(v);
                        current.isJsonOfValue = false == current.isJsonOfValue ? JsonRegex.rgBaseTypeArr.IsMatch(v) : current.isJsonOfValue;
                        current.isJsonOfValue = false == current.isJsonOfValue ? JsonRegex.rgMixedArr.IsMatch(v) : current.isJsonOfValue;
                        current.isJsonOfValue = false == current.isJsonOfValue ? kv.is_json : current.isJsonOfValue;
                    }
                    object v1 = convertValue(current);
                    if (null != v1)
                    {
                        v = v1.ToString();
                        resetStrJsonVal(ref v);
                        v1 = v;
                    }

                    current.value = v1;
                }
            }

            current.index = index;
            current.count = count;
            current.children = null;

            if (false == current.isJsonOfValue) return;
            current.children = new DJsonChildren(current.value.ToString());
            Enumerator enuma = new Enumerator();
            current.children.enumerator = enuma;
            current.children.enumeratorDJsonItem = enuma;
        }

        /// <summary>
        /// 数组情况, 元素为json单元或基本数据类型
        /// </summary>
        void JsonUnit()
        {
            if (JsonRegex.rgJsonUnitErr1.IsMatch(json))
            {
                string s = JsonRegex.rgJsonUnitErr1.Match(json).Groups[0].Value;
                throw new Exception("在 " + s + " 中出语法错误, }{之间缺少逗号");
            }

            if (rgBaseTypeArr.IsMatch(json))
            {
                BaseTypeArr();
                return;
            }

            MixedTypeArr();
        }

        /// <summary>
        /// 数组情况, 元素为基本数据类型: string, int, float 等
        /// </summary>
        void BaseTypeArr()
        {
            string json1 = json;
            int n = 0;

            string Value = "";
            string Value1 = "";
            string Value2 = "";
            Match m = null;
            object vObj = null;
            while (rgBaseTypeVal.IsMatch(json1) && max > n)
            {
                m = rgBaseTypeVal.Match(json1);
                Value1 = m.Groups["Value1"].Value;
                Value = m.Groups["Value"].Value;
                Value2 = Value1 + Value + Value1;
                json1 = json1.Replace(Value2, "");
                resetStrJsonVal(ref Value);
                vObj = convertTo(Value1, Value);

                vObj = new { key = Value + "_" + n, value = vObj, is_arr = true, is_json = false };
                results.Add(vObj);

                n++;
            }
        }

        /// <summary>
        /// 数组情况, 元素为复杂类型(json单元)
        /// </summary>
        void MixedTypeArr()
        {
            Dictionary<string, CKeyValue> dictionary = new Dictionary<string, CKeyValue>();
            //json = json.Replace(rcArr[0].oldStr, rcArr[0].newStr);
            string sjson = replaceAll(json, dictionary);

            matchBigBracket.check(sjson);

            string JsonUnit = "";
            int n = 0;
            while (rgJsonUnit.IsMatch(sjson) && max > n)
            {
                n++;
                JsonUnit = rgJsonUnit.Match(sjson).Groups["JsonUnit"].Value;
                sjson = sjson.Replace(JsonUnit, "");
                restReplace(dictionary, ref JsonUnit);
                resetStrJsonVal(ref JsonUnit);
                results.Add(new { key = "", value = JsonUnit, is_arr = true, is_json = true });
            }

            if (rgJsonUnit.IsMatch(sjson) && max == n)
            {
                throw new Exception("json字符数据量过大, 未完全解析, 建议分量解析或加大最大转换量");
            }
        }

        /// <summary>
        /// json单元情况, 遍历键值对
        /// </summary>
        void KeyVal()
        {
            Dictionary<string, CKeyValue> dictionary = new Dictionary<string, CKeyValue>();
            //json = json.Replace(rcArr[0].oldStr, rcArr[0].newStr);
            string sjson = replaceAll(json, dictionary);
            string s = "";

            //无效的键值对
            Action<string> disableKV = jsonItem =>
            {
                if (JsonRegex.rgKVErr1.IsMatch(jsonItem))
                {
                    s = JsonRegex.rgKVErr1.Match(jsonItem).Groups[0].Value;
                    //throw new Exception("字符 <" + s + "> 是一个无效的键值对");
                }
            };

            //无效的双引号
            Action<string> disableShuangYingHao = jsonItem =>
            {
                if (JsonRegex.rgKVErr2.IsMatch(jsonItem))
                {
                    s = JsonRegex.rgKVErr2.Match(jsonItem).Groups[0].Value;
                    //throw new Exception("字符 <" + s + "> 有语法错误, 无效的双引号");
                }
            };

            disableKV(sjson);
            disableShuangYingHao(sjson);

            string Value = "";
            foreach (var item in dictionary)
            {
                Value = item.Value.Value.ToString();
                disableShuangYingHao(Value);

                if (-1 == item.Key.IndexOf("valueOfJsonUnit")) continue;
                disableKV(Value);
            }

            string Key = "";
            string Key1 = "";
            string Key2 = "";
            string SplitStr = "";

            string Value1 = "";
            string Value2 = "";
            string sign = "";
            int n = 0;

            object vObj = null;

            Match m = null;
            while (rgKV.IsMatch(sjson) && max > n)
            {
                n++;
                m = rgKV.Match(sjson);
                Key1 = m.Groups["Key1"].Value;
                Key = m.Groups["Key"].Value;
                Key2 = Key1 + Key + Key1;

                SplitStr = m.Groups["SplitStr"].Value;

                Value1 = m.Groups["Value1"].Value;
                Value = m.Groups["Value"].Value;
                Value2 = Value1 + Value + Value1;

                sign = Key2 + SplitStr + Value2;
                sjson = sjson.Replace(sign, "");

                restReplace(dictionary, ref Value);

                resetStrJsonVal(ref Value);
                vObj = convertTo(Value1, Value);

                vObj = new { key = Key, value = vObj, is_arr = false, is_json = false };
                results.Add(vObj);
                dictionaryAddItem(vObj);
            }

            if (rgKV.IsMatch(sjson) && max == n)
            {
                throw new Exception("json字符数据量过大, 未完全解析, 建议分量解析或加大最大转换量");
            }
        }

        /// <summary>
        /// 在键值对中,值是否被双引号所包含,值被双引号包含视为字符串,如果不存在双引号则进行数据类型判断并将值转换为对应的数据类型
        /// </summary>
        /// <param name="shuangYinHao">双引号</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        object convertTo(string shuangYinHao, string value)
        {
            if (null == value) return value;
            object vObj = value;
            string v = value;
            if (string.IsNullOrEmpty(shuangYinHao))
            {
                if (DataTypeRegex.isBool(v))
                {
                    vObj = Convert.ToBoolean(v);
                }
                else if (DataTypeRegex.isInt(v))
                {
                    vObj = Convert.ToInt32(v);
                }
                else if (DataTypeRegex.isFloat(v))
                {
                    vObj = Convert.ToDouble(v);
                }
                else if (DataTypeRegex.isDataTime(v))
                {
                    vObj = Convert.ToDateTime(v);
                }
                else if (DataTypeRegex.isGuid(v))
                {
                    string s = Guid.NewGuid().ToString();
                    if (s.Length == v.Length) vObj = new Guid(v);
                }
                else if (v.ToLower().Equals("null"))
                {
                    vObj = "|null|";
                }
            }
            return vObj;
        }

        static Type getTypeByValue(object value)
        {
            Type type = null;
            if (null == value) return type;
            string v = value.ToString();
            if (DataTypeRegex.isBool(v))
            {
                type = typeof(Boolean);
            }
            else if (DataTypeRegex.isInt(v))
            {
                type = typeof(int);
            }
            else if (DataTypeRegex.isFloat(v))
            {
                type = typeof(float);
            }
            else if (DataTypeRegex.isDataTime(v))
            {
                type = typeof(DateTime);
            }
            else if (DataTypeRegex.isGuid(v))
            {
                string s = Guid.NewGuid().ToString();
                if (s.Length == v.Length) type = typeof(Guid);
            }
            else
            {
                type = typeof(string);
            }
            return type;
        }

        void restReplace(Dictionary<string, CKeyValue> dictionary, ref string json)
        {
            int n = 0;
            string sign = "";
            CKeyValue kv = null;
            while (rgReplaceSign.IsMatch(json) && max > n)
            {
                n++;
                sign = rgReplaceSign.Match(json).Groups[0].Value;
                kv = dictionary[sign];
                if (null == kv) break;
                json = json.Replace(sign, kv.Value.ToString());
            }
        }

        string replaceAll(string json, Dictionary<string, CKeyValue> dictionary)
        {
            string json1 = json;

            if (rgKV_ValOfJsonStr.IsMatch(json1))
            {
                valueOf(valueOfUnitStrDic, num, rgKV_ValOfJsonStr, "_valueOfJsonStr_", "\"", ref json1);
            }

            //valueOfJsonUnit
            if (rgKV_ValOfJsonUnit.IsMatch(json1))
            {
                valueOf(dictionary, num, rgKV_ValOfJsonUnit, "_valueOfJsonUnit_", "", ref json1);
            }

            //valueOfArray
            if (rgKV_ValOfJsonArr.IsMatch(json1))
            {
                valueOf(dictionary, num, rgKV_ValOfJsonArr, "_valueOfArray_", "", ref json1);
            }

            num++;
            if (rgKV_ValOfJsonStr.IsMatch(json1) || rgKV_ValOfJsonUnit.IsMatch(json1) || rgKV_ValOfJsonArr.IsMatch(json1))
            {
                json1 = replaceAll(json1, dictionary);
            }

            return json1;
        }

        void valueOf(Dictionary<string, CKeyValue> dic, int num, Regex regex, string repaceTag, string yh, ref string json)
        {
            int len = max.ToString().Length;
            string FieldName1 = "";
            string FieldName2 = "";
            string FieldName = "";
            string FieldValue = "";
            string SplitStr = "";
            string sign = "";
            string s = "";
            Match mc = null;
            int n = 0;
            if (null == yh) yh = "";

            while (regex.IsMatch(json) && max > n)
            {
                mc = regex.Match(json);
                FieldName1 = mc.Groups["FieldName1"].Value;
                FieldName = mc.Groups["FieldName"].Value;
                FieldName2 = FieldName;
                if (!string.IsNullOrEmpty(FieldName1)) FieldName2 = "\"" + FieldName + "\"";

                SplitStr = mc.Groups["SplitStr"].Value;
                FieldValue = mc.Groups["FieldValue"].Value;
                sign = FieldName + repaceTag + num + "_" + n.ToString("D" + len);

                s = FieldName2 + SplitStr + yh + FieldValue + yh;
                json = json.Replace(s, FieldName2 + SplitStr + sign);
                //FieldValue = FieldValue.Replace(rcArr[0].newStr, rcArr[0].oldStr);
                dic.Add(sign, new CKeyValue() { Key = FieldName, Value = FieldValue });
                n++;
            }

            if (max == n && rgKV_ValOfJsonUnit.IsMatch(json))
            {
                throw new Exception("待转换的json字符数据过大,请尝试分批转换");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<DJsonItem> IEnumerable<DJsonItem>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        class Enumerator : IEnumerator, IEnumerator<DJsonItem>
        {
            DJsonItem current;

            DJson djson = null;
            List<object> results = null;

            public Enumerator() { }

            public Enumerator(DJson djson)
            {
                init(djson);
            }

            public void init(DJson djson)
            {
                this.djson = djson;
                results = djson.results;

                index = 0;
                count = results.Count;
            }

            public int index { get; set; }
            public int count { get; set; }

            object IEnumerator.Current => current;

            DJsonItem IEnumerator<DJsonItem>.Current => current;

            void IDisposable.Dispose()
            {
                index = 0;
                //throw new NotImplementedException();
            }

            bool IEnumerator.MoveNext()
            {
                if (index == count) return false;

                object val = results[index];
                djson.DJsonItemInit(ref current, val, index, count);

                index++;
                return true;
            }

            void IEnumerator.Reset()
            {
                index = 0;
            }
        }
    }
}
