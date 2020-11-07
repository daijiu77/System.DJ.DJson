using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text.RegularExpressions;

namespace System.DJ.DJson.Commons
{
    /// <summary>
    /// 完善及更改
    /// 1. 2020-05-01至2020-05-02 完善属性值为复杂类型的数据实体转换数据为json单元字符数据, json单元字符数据转复杂数据实体
    ///    属性类型可为: 基本数据类型, 数据实体类型, 基本数据类型数组, 数据实体对象数组, List<T>集合[T: 基本数据类型,及实体对象], Dictionary
    ///    数据可嵌套包含
    /// Author: 代久 - Allan
    /// QQ: 564343162
    /// Email: 564343162@qq.com
    /// CreateDate: 2020-03-05
    /// </summary>
    public abstract class BaseEntity
    {
        object current = null;

        public BaseEntity()
        {
            current = this;
        }

        private BaseEntity(object current)
        {
            this.current = current;
        }

        public string ForeachProperty(Func<PropertyInfo, string, object, string> func)
        {
            object eObj = current;
            string s1 = "";
            object vObj = null;
            Type entityType = typeof(BaseEntity);
            PropertyInfo[] piArr = eObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var item in piArr)
            {
                if (item.DeclaringType == entityType) continue;
                vObj = item.GetValue(eObj, null);
                s1 += func(item, item.Name, vObj);
            }
            return s1;
        }

        public void ForeachProperty(Action<PropertyInfo, string, object> action)
        {
            ForeachProperty((propertyInfo, fieldName, fieldValue) =>
            {
                action(propertyInfo, fieldName, fieldValue);
                return "";
            });
        }

        /// <summary>
        /// 把当前对象属性及值以 propertyName = propertyValue 的格式输出, 多个用逗号相隔
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s1 = ForeachProperty((propertyInfo, fieldName, fieldVale) =>
            {
                return ", " + fieldName + " = " + fieldVale;
            });

            if (!string.IsNullOrEmpty(s1))
            {
                s1 = s1.Substring(2);
            }
            return s1;
        }

        /// <summary>
        /// 把当前对象属性值解析为一个json单元字符串数据
        /// </summary>
        /// <returns></returns>
        public string ToJsonUnit()
        {
            string sign = ", ";
            string s1 = ForeachProperty((propertyInfo, fieldName, fieldVale) =>
            {
                string fv = getValueByType(propertyInfo.PropertyType, fieldVale);
                return sign + "\"" + fieldName + "\": " + fv;
            });

            if (!string.IsNullOrEmpty(s1))
            {
                s1 = s1.Substring(sign.Length);
                s1 = "{" + s1 + "}";
            }
            return s1;
        }

        /// <summary>
        /// 把json单元字符数据解析为当前对象属性值
        /// </summary>
        /// <param name="jsonUnit"></param>
        public void fromJsonUnit(string jsonUnit)
        {
            EList<CKeyValue> kvs = DJson.JsonUnitToEList(jsonUnit);
            CKeyValue kv = null;
            string fn = "";
            object vObj = null;
            object _obj = current;
            ForeachProperty((propertyInfo, fieldName, fieldVale) =>
            {
                fn = propertyInfo.Name;
                kv = kvs[fn.ToLower()];
                if (null == kv) return;
                if (DJTools.IsBaseType(propertyInfo.PropertyType))
                {
                    vObj = kv.Value;
                    if (null != vObj)
                    {
                        vObj = DJTools.ConvertTo(vObj, propertyInfo.PropertyType);
                    }
                }
                else
                {
                    //复杂类型属性
                    vObj = getPropertyValueByType(propertyInfo.PropertyType, kv.Value);
                }

                if (null == vObj) return;

                try
                {
                    propertyInfo.SetValue(_obj, vObj, null);
                }
                catch (Exception ex)
                {

                    throw new Exception("属性 [" + propertyInfo.Name + "] 对应的值 " + vObj.ToString() + " 无法转换为类型 " + propertyInfo.PropertyType.FullName);
                }
            });
        }

        class Temp : BaseEntity
        {
            public Temp(object current) : base(current) { }
        }

        #region -- these are private methods.

        #region Create a object by type of property, and set a value for property of object.

        #region PropertyType = Dictionary
        object createDictionaryByType(Type type)
        {
            object dic = null;
            if (null == type.GetInterface("IDictionary")) return dic;

            Type[] types = type.GetGenericArguments();
            Type dicType = typeof(Dictionary<string, int>);
            string asseName = dicType.Assembly.GetName().Name;
            string dicTypeName = dicType.FullName;
            string s = @"\[[^\[\]]+\]";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            if (rg.IsMatch(dicTypeName))
            {
                int n = 0;
                int len = types.Length;
                string txt = "";
                Type ele = null;
                MatchCollection mc = rg.Matches(dicTypeName);
                foreach (Match item in mc)
                {
                    if (n == len) break;
                    ele = types[n];
                    s = ele.FullName;
                    s += ", " + ele.Assembly.GetName().Name;
                    s += ", Version=" + ele.Assembly.GetName().Version.ToString();
                    s += ", Culture=neutral";
                    s += ", PublicKeyToken=null";
                    s = "[" + s + "]";
                    txt = item.Groups[0].Value;
                    dicTypeName = dicTypeName.Replace(txt, s);
                    n++;
                }
            }

            object v = Activator.CreateInstance(asseName, dicTypeName) as ObjectHandle;
            if (null == v) return dic;
            dic = ((ObjectHandle)v).Unwrap();

            return dic;
        }

        void dictionaryAdd(object dic, EList<CKeyValue> ckvs)
        {
            if (null == dic) return;
            if (null == ckvs) return;
            if (0 == ckvs.Count) return;

            Type dicType = dic.GetType();
            if (null == dicType.GetInterface("IDictionary")) return;

            MethodInfo methodInfo = dicType.GetMethod("Add");
            if (null == methodInfo) return;

            string key = "";
            object val = null;

            Type[] types = dicType.GetGenericArguments();
            Type valueType = types[1];
            bool isSuccess = false;
            ckvs.ForEach(kv =>
            {
                key = kv.Key;
                if (DJTools.IsBaseType(valueType))
                {
                    val = DJTools.ConvertTo(kv.Value, valueType, ref isSuccess);
                    if (!isSuccess)
                    {
                        throw new Exception("无法把数据 " + val.ToString() + " 转换为类型 " + valueType.FullName);
                    }
                }
                else
                {
                    val = getPropertyValueByType(valueType, kv.Value);
                }

                if (null == val) return;

                try
                {
                    methodInfo.Invoke(dic, new object[] { key, val });
                }
                catch { }
            });
        }
        #endregion

        #region PropertyType = List<T>
        object createListByType(Type type)
        {
            object list = null;
            if (null == type.GetInterface("IList")) return list;

            Type[] types = type.GetGenericArguments();
            Type listType = typeof(List<string>);
            string asseName = listType.Assembly.GetName().Name;
            string dicTypeName = listType.FullName;
            string s = @"\[[^\[\]]+\]";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            if (rg.IsMatch(dicTypeName))
            {
                int n = 0;
                int len = types.Length;
                string txt = "";
                Type ele = null;
                MatchCollection mc = rg.Matches(dicTypeName);
                foreach (Match item in mc)
                {
                    if (n == len) break;
                    ele = types[n];
                    s = ele.FullName;
                    s += ", " + ele.Assembly.GetName().Name;
                    s += ", Version=" + ele.Assembly.GetName().Version.ToString();
                    s += ", Culture=neutral";
                    s += ", PublicKeyToken=null";
                    s = "[" + s + "]";
                    txt = item.Groups[0].Value;
                    dicTypeName = dicTypeName.Replace(txt, s);
                    n++;
                }
            }

            object v = Activator.CreateInstance(asseName, dicTypeName) as ObjectHandle;
            if (null == v) return list;
            list = ((ObjectHandle)v).Unwrap();
            return list;
        }

        void listAdd(object list, List<EList<CKeyValue>> ckvs)
        {
            if (null == list) return;
            if (null == ckvs) return;
            if (0 == ckvs.Count) return;

            Type listType = list.GetType();
            if (null == listType.GetInterface("IList")) return;

            MethodInfo methodInfo = listType.GetMethod("Add");
            if (null == methodInfo) return;

            object ele = null;
            Type[] types = listType.GetGenericArguments();
            CKeyValue kv = null;
            foreach (EList<CKeyValue> item in ckvs)
            {
                if (0 == item.Count) continue;

                if (DJTools.IsBaseType(types[0]))
                {
                    kv = item[0];
                    ele = DJTools.ConvertTo(kv.Value, types[0]);
                }
                else
                {
                    try
                    {
                        ele = Activator.CreateInstance(types[0]);
                    }
                    catch
                    {
                        break;
                    }
                    entityAdd(ref ele, item);
                }

                try
                {
                    methodInfo.Invoke(list, new object[] { ele });
                }
                catch { }
            }
        }
        #endregion

        #region PropertyType = Array
        object createArrayByType(Type type, int length)
        {
            object arr = null;
            if (false == type.IsArray) return arr;

            try
            {
                arr = type.InvokeMember("Set", BindingFlags.CreateInstance, null, arr, new object[] { length });
            }
            catch { }

            return arr;
        }

        void arrayAdd(object arrObj, List<EList<CKeyValue>> ckvs)
        {
            if (null == arrObj) return;
            if (null == ckvs) return;
            if (0 == ckvs.Count) return;

            Type type = arrObj.GetType();
            if (false == type.IsArray) return;

            Array array = (Array)arrObj;
            Type eleType = type.GetElementType();

            bool isBaseType = DJTools.IsBaseType(eleType);
            object ele = null;
            int n = 0;
            int len = array.Length;
            bool isSuccess = false;
            foreach (EList<CKeyValue> item in ckvs)
            {
                if (n == len) break;

                if (isBaseType)
                {
                    ele = "";
                }
                else
                {
                    try
                    {
                        ele = Activator.CreateInstance(eleType);
                    }
                    catch
                    {
                        break;
                    }
                }

                entityAdd(ref ele, item);
                if (isBaseType)
                {
                    ele = DJTools.ConvertTo(ele, eleType, ref isSuccess);
                    if (!isSuccess)
                    {
                        throw new Exception("无法把数据 " + ele.ToString() + " 转换为类型 " + eleType.FullName);
                    }
                }
                array.SetValue(ele, n);
                n++;
            }
        }
        #endregion

        void entityAdd(ref object entity, EList<CKeyValue> kvs)
        {
            if (null == entity) return;
            if (0 == kvs.Count) return;

            if (DJTools.IsBaseType(entity.GetType()))
            {
                entity = kvs[0].Value;
                return;
            }

            string fn = "";
            object fv = null;
            bool isSuccess = false;
            CKeyValue kv = null;
            PropertyInfo[] piArr = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo item in piArr)
            {
                fn = item.Name;
                kv = kvs[fn.ToLower()];
                if (null == kv) continue;

                fv = getPropertyValueByType(item.PropertyType, kv.Value);
                if (null == fv) continue;

                if (DJTools.IsBaseType(item.PropertyType))
                {
                    fv = DJTools.ConvertTo(fv, item.PropertyType, ref isSuccess);
                    if (!isSuccess)
                    {
                        throw new Exception("属性 [" + fn + "] 对应的值 " + fv.ToString() + " 无法转换为类型 " + item.PropertyType.FullName);
                    }
                }

                try
                {
                    item.SetValue(entity, fv, null);
                }
                catch { }
            }
        }

        object getPropertyValueByType(Type type, object valueObj)
        {
            object val = null;
            if (null == type) return valueObj;
            if (null == valueObj) return valueObj;

            if (null != type.GetInterface("ICollection"))
            {
                //Dictionary<string, int> dic;
                //集合类型的属性
                if (type.IsArray && valueObj.GetType() == typeof(List<EList<CKeyValue>>))
                {
                    //数组类型
                    //Type type1 = type.GetElementType();
                    List<EList<CKeyValue>> vlist = (List<EList<CKeyValue>>)valueObj;
                    val = createArrayByType(type, vlist.Count);
                    arrayAdd(val, vlist);
                }
                else if (null != type.GetInterface("IList") && valueObj.GetType() == typeof(List<EList<CKeyValue>>))
                {
                    //List 集合类型
                    //Type[] types = type.GetGenericArguments();
                    List<EList<CKeyValue>> vlist = (List<EList<CKeyValue>>)valueObj;
                    val = createListByType(type);
                    listAdd(val, vlist);
                }
                else if (null != type.GetInterface("IDictionary") && valueObj.GetType() == typeof(EList<CKeyValue>))
                {
                    //Dictionary 键值对
                    //Type[] types = type.GetGenericArguments();
                    EList<CKeyValue> elist = (EList<CKeyValue>)valueObj;
                    val = createDictionaryByType(type);
                    dictionaryAdd(val, elist);
                }
            }
            else if (!DJTools.IsBaseType(type) && valueObj.GetType() == typeof(EList<CKeyValue>))
            {
                //数据实体类型属性
                EList<CKeyValue> elist = (EList<CKeyValue>)valueObj;
                try
                {
                    val = Activator.CreateInstance(type);
                }
                catch { }

                entityAdd(ref val, elist);
            }
            else
            {
                val = valueObj;
                if (valueObj.GetType() == typeof(CKeyValue))
                {
                    val = ((CKeyValue)valueObj).Value;
                }
            }
            return val;
        }

        #endregion

        string getValueByDictionary(IDictionary dic)
        {
            if (null == dic) return "null";
            if (0 == dic.Count) return "null";

            string sign = ", ";
            string fv = "";
            string key = "";
            object val = null;
            Type type = null;
            Type[] types = dic.GetType().GetGenericArguments();
            Type type1 = types[1];
            foreach (var item in dic)
            {
                type = item.GetType();
                key = type.GetProperty("Key").GetValue(item, null).ToString();
                val = type.GetProperty("Value").GetValue(item, null);
                fv += sign + "\"" + key + "\": " + getValueByType(type1, val);
            }

            fv = fv.Substring(sign.Length);
            fv = "{" + fv + "}";
            return fv;
        }

        string getValueByList(IList list)
        {
            if (null == list) return "null";
            if (0 == list.Count) return "null";
            string sign = ", ";
            string fv = "";
            Type[] types = list.GetType().GetGenericArguments();
            Type type = types[0];
            foreach (var item in list)
            {
                fv += sign + getValueByType(type, item);
            }

            fv = fv.Substring(sign.Length);
            fv = "[" + fv + "]";
            return fv;
        }

        string getValueByArray(Array arr)
        {
            if (null == arr) return "null";
            if (0 == arr.Length) return "null";
            string sign = ", ";
            string fv = "";
            Type type = arr.GetType().GetElementType();
            foreach (var item in arr)
            {
                fv += sign + getValueByType(type, item);
            }
            fv = fv.Substring(sign.Length);
            fv = "[" + fv + "]";
            return fv;
        }

        string getValueByBaseEntity(BaseEntity baseEntity)
        {
            if (null == baseEntity) return "null";
            string fv = baseEntity.ToJsonUnit();
            return fv;
        }

        string getValueByOtherEntity(object entity)
        {
            if (null == entity) return "null";
            Temp temp = new Temp(entity);
            string fv = temp.ToJsonUnit();
            return fv;
        }

        string getValueByType(Type type, object fieldVale)
        {
            string fv = "";
            if (null == fieldVale) return "null";

            if (type == typeof(string)
            || type == typeof(DateTime)
            || type == typeof(Guid))
            {
                fv = "\"" + fieldVale.ToString() + "\"";
            }
            else if (type == typeof(bool))
            {
                fv = fieldVale.ToString().ToLower();
            }
            else if (type.IsArray)
            {
                Array arr = null == fieldVale ? null : (Array)fieldVale;
                fv = getValueByArray(arr);
            }
            else if (null != type.GetInterface("IDictionary"))
            {
                IDictionary dic = null == fieldVale ? null : (IDictionary)fieldVale;
                fv = getValueByDictionary(dic);
            }
            else if (null != type.GetInterface("IList"))
            {
                IList list = null == fieldVale ? null : (IList)fieldVale;
                fv = getValueByList(list);
            }
            else if (type.IsSubclassOf(typeof(BaseEntity)))
            {
                BaseEntity baseEntity = (BaseEntity)fieldVale;
                fv = getValueByBaseEntity(baseEntity);
            }
            else if (!DJTools.IsBaseType(type))
            {
                fv = getValueByOtherEntity(fieldVale);
            }
            else
            {
                fv = fieldVale.ToString();
            }
            return fv;
        }
        #endregion
    }
}
