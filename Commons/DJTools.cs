using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace System.DJ.DJson.Commons
{
    /// <summary>
    /// Author: 代久 - Allan
    /// QQ: 564343162
    /// Email: 564343162@qq.com
    /// CreateDate: 2020-03-05
    /// </summary>
    public static class DJTools
    {
        public static object ConvertTo(object value, Type type, ref bool isSuccess)
        {
            isSuccess = true;
            if (null == value) return value;
            if (null == type) return value;
            if (!IsBaseType(value.GetType())) return value;
            if (!IsBaseType(type)) return value;

            object obj = null;
            object v = value;
            if (type == typeof(Guid?))
            {
                v = v == null ? Guid.Empty.ToString() : v;
                Guid guid = new Guid(v.ToString());
                obj = guid;
            }
            else if (type == typeof(int?)
                || type == typeof(short?)
                || type == typeof(long?)
                || type == typeof(float?)
                || type == typeof(double?)
                || type == typeof(decimal?))
            {
                v = v == null ? 0 : v;
                value = v;
            }
            else if (type == typeof(bool?))
            {
                v = v == null ? false : v;
                value = v;
            }
            else if (type == typeof(DateTime?))
            {
                v = v == null ? DateTime.MinValue : v;
                value = v;
            }

            if (type == typeof(Guid))
            {
                string sv = null == value ? "" : value.ToString();
                sv = string.IsNullOrEmpty(sv) ? Guid.Empty.ToString() : sv;
                obj = new Guid(sv);
            }
            else if (null == obj)
            {
                string s = type.ToString();
                string typeName = s.Substring(s.LastIndexOf(".") + 1);
                typeName = typeName.Replace("]", "");
                typeName = typeName.Replace("&", "");
                string methodName = "To" + typeName;
                try
                {
                    Type t = Type.GetType("System.Convert");
                    //执行Convert的静态方法
                    obj = t.InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, new object[] { value });
                }
                catch (Exception)
                {
                    obj = value;
                    isSuccess = false;
                    //throw;
                }
            }

            return obj;
        }

        public static object ConvertTo(object value, Type type)
        {
            bool isSuccess = false;
            return ConvertTo(value, type, ref isSuccess);
        }

        public static bool IsBaseType(Type type)
        {
            byte[] arr = type.Assembly.GetName().GetPublicKeyToken();
            if (0 == arr.Length) return false;
            bool mbool = ((typeof(ValueType) == type.BaseType) || (typeof(string) == type));
            return mbool;

            //string s = type.ToString();
            //string typeName = s.Substring(s.LastIndexOf(".") + 1);
            //typeName = typeName.Replace("]", "");
            //typeName = typeName.Replace("&", "");
            //string methodName = "To" + typeName;

            //Type t = Type.GetType("System.Convert");
            //MethodInfo[] miArr = t.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public);
            //foreach (MethodInfo m in miArr)
            //{
            //    if (methodName.Equals(m.Name))
            //    {
            //        mbool = true;
            //        break;
            //    }
            //}
            //return mbool;
        }

        /// <summary>
        /// 根据类型获取该数据对应的类型默认值
        /// </summary>
        /// <param name="type">数据类型</param>
        /// <returns></returns>
        public static string getDefaultByType(Type type)
        {
            string s = "";
            string ganaricName = "IEnumerable";
            if (type == typeof(Guid))
            {
                s = Guid.Empty.ToString();
            }
            else if (type == typeof(DateTime))
            {
                s = DateTime.MinValue.ToString("yyyy/MM/dd hh:mm:ss");
            }
            else if (type == typeof(bool))
            {
                s = "false";
            }
            else if (type == typeof(string))
            {
                s = "\"\"";
            }
            else if (null != type.GetInterface(ganaricName))
            {
                s = "null";
            }
            else
            {
                s = "default(" + type.Name + ")";
            }
            return s;
        }

        /// <summary>
        /// 根据参数标识(@ : ?)获取参数类名称
        /// </summary>
        /// <param name="dbType">参数标识(@ : ?)</param>
        /// <param name="AssemblyName"></param>
        /// <returns></returns>
        public static string GetParamertClassNameByDbTag(string dbType, ref string AssemblyName)
        {
            string pcn = "";
            switch (dbType.Trim())
            {
                case "@": //sql server @ParameterName
                    pcn = "System.Data.SqlClient.SqlParameter";
                    AssemblyName = "System.Data.dll";
                    break;
                case ":": //oracle :ParameterName
                    pcn = "Data.OracleClient.OracleParameter";
                    AssemblyName = "Data.OracleClient.dll";
                    break;
                case "?": //mysql ?ParameterName
                    pcn = "MySql.Data.MySqlClient.MySqlParameter";
                    AssemblyName = "MySql.Data.dll";
                    break;
            }
            return pcn;
        }

        /// <summary>
        /// 根据参数标识(@ : ?)获取参数类名称
        /// </summary>
        /// <param name="dbType">参数标识(@ : ?)</param>
        /// <returns></returns>
        public static string GetParamertClassNameByDbTag(string dbType)
        {
            string AssemblyName = "";
            return GetParamertClassNameByDbTag(dbType, ref AssemblyName);
        }

        public static string ToJson<T>(this List<T> baseEntities) where T : BaseEntity
        {
            string json = "";
            if (null == baseEntities) return json;
            if (0 == baseEntities.Count) return json;

            string splitTag = ", ";
            foreach (BaseEntity be in baseEntities)
            {
                json += splitTag + be.ToJsonUnit();
            }

            if (!string.IsNullOrEmpty(json))
            {
                json = json.Substring(splitTag.Length);
                json = "[" + json + "]";
            }
            return json;
        }

        public static string ExtFormat(this string formatStr, params string[] arr)
        {
            string s1 = formatStr;
            if (null == arr) return s1;
            if (0 == arr.Length) return s1;

            int n = 0;
            foreach (string item in arr)
            {
                s1 = s1.Replace("{" + n + "}", item);
                n++;
            }
            return s1;
        }

        public static string GetDllRootPath(string rootPath)
        {
            string root_path = rootPath;
            string[] dlls = Directory.GetFiles(root_path, "*.dll");
            string[] exes = Directory.GetFiles(root_path, "*.exe");
            string[] dirs = null;
            string dirName = "bin";
            int n = 0;
            while ((0 == dlls.Length && 0 == exes.Length) && 10 > n)
            {
                root_path = Path.Combine(root_path, dirName);
                if (!Directory.Exists(root_path)) break;
                dlls = Directory.GetFiles(root_path, "*.dll");
                exes = Directory.GetFiles(root_path, "*.exe");
                if (0 < dlls.Length || 0 < exes.Length) break;
                dirs = Directory.GetDirectories(root_path);
                if (0 == dirs.Length) break;
                dirName = new DirectoryInfo(dirs[0]).Name;
                n++;
            }

            dlls = Directory.GetFiles(root_path, "*.dll");
            exes = Directory.GetFiles(root_path, "*.exe");
            if (0 == dlls.Length && 0 == exes.Length)
            {
                string err = "无效的根路径<{0}>";
                err = err.ExtFormat(root_path);
                throw new Exception(err);
            }

            return root_path;
        }

        static List<string> GetDllPathCollection(string rootPath)
        {
            List<string> anList = new List<string>();

            string[] dlls = Directory.GetFiles(rootPath, "*.dll");
            string[] exes = Directory.GetFiles(rootPath, "*.exe");

            Assembly assembly = null;
            byte[] bt = null;
            AssemblyName assemblyName = null;
            foreach (var item in dlls)
            {
                assembly = Assembly.LoadFrom(item);
                if (null == assembly) continue;
                assemblyName = assembly.GetName();
                bt = assemblyName.GetPublicKeyToken();
                if (0 != bt.Length) continue;
                anList.Add(item);
            }

            if (0 < exes.Length) anList.Add(exes[0]);

            return anList;
        }

        public static List<Assembly> GetAssemblyCollection(string rootPath)
        {
            List<Assembly> assemblies = new List<Assembly>();
            string root_path = GetDllRootPath(rootPath);
            List<string> dllPathCollection = GetDllPathCollection(root_path);

            Assembly asse = null;
            foreach (string item in dllPathCollection)
            {
                try
                {
                    asse = Assembly.LoadFrom(item);
                    assemblies.Add(asse);
                }
                catch { }
            }

            return assemblies;
        }

        /// <summary>
        /// 用数据对象 newObj 属性的值初始化原对象
        /// </summary>
        /// <param name="srcObj">原对象</param>
        /// <param name="newObj">数据对象</param>
        /// <param name="excludeFields">排除赋值的属性</param>
        public static void InitPropertyBy(this object srcObj, object newObj, string[] excludeFields)
        {
            object _obj = srcObj;
            object nObj = newObj;
            object v = null;
            bool mbool = false;
            string fn = "";
            PropertyInfo[] pis = _obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var pi in pis)
            {
                fn = pi.Name.ToLower();
                mbool = true;

                if (null != excludeFields)
                {
                    foreach (var item in excludeFields)
                    {
                        if (item.ToLower().Equals(fn))
                        {
                            mbool = false;
                            break;
                        }
                    }
                }

                if (mbool)
                {
                    if (null == nObj.GetType().GetProperty(pi.Name)) continue;

                    v = nObj.GetType().GetProperty(pi.Name).GetValue(nObj, null);
                    v = DJTools.ConvertTo(v, pi.PropertyType);
                    try
                    {
                        pi.SetValue(_obj, v, null);
                    }
                    catch (Exception)
                    {

                        //throw;
                    }

                }
            }
        }

        public static void ForeachAssembly(Func<Type, bool> func, List<Assembly> assemblies)
        {
            if (null == assemblies) return;
            if (0 == assemblies.Count) return;
            Type[] types = null;
            bool mbool = false;
            foreach (Assembly asse in assemblies)
            {
                types = asse.GetTypes();
                foreach (Type item in types)
                {
                    if (!item.IsVisible) continue;
                    if (item.IsGenericType) continue;

                    mbool = func(item);
                    if (!mbool) break;
                }

                if (!mbool) break;
            }
        }

        public static List<object> GetImplByAssemblies(List<Assembly> assemblies, EList<CKeyValue> interfaceCollection)
        {
            List<object> impls = new List<object>();
            object impl = null;

            Func<Type, bool> IsInheritInterface = type =>
            {
                bool mbool = false;
                if (0 == interfaceCollection.Count) return mbool;
                foreach (CKeyValue item in interfaceCollection)
                {
                    if (null == type.GetInterface(item.Key)) continue;
                    mbool = true;
                    break;
                }
                return mbool;
            };

            ForeachAssembly(item =>
            {
                if (item.IsAbstract) return true;
                if (item.IsInterface) return true;
                if (!IsInheritInterface(item)) return true;

                try
                {
                    impl = Activator.CreateInstance(item);
                    impls.Add(impl);
                }
                catch { }
                return true;
            }, assemblies);
            return impls;
        }

        public static EList<CKeyValue> GetInterfaces(List<Assembly> assemblies)
        {
            EList<CKeyValue> interfaceList = new EList<CKeyValue>();
            ForeachAssembly(item =>
            {
                if (!item.IsInterface) return true;
                interfaceList.Add(new CKeyValue() { Key = item.FullName, Value = item });
                return true;
            }, assemblies);
            return interfaceList;
        }

        [DllImport("Kernel32.dll")]
        extern static int FormatMessage(int flag, ref IntPtr source, int msgid, int langid, ref string buf, int size, ref IntPtr args);

        public static string GetSysErrMsg()
        {
            int errCode = Marshal.GetLastWin32Error();
            IntPtr tempptr = IntPtr.Zero;
            string msg = null;
            FormatMessage(0x1300, ref tempptr, errCode, 0, ref msg, 255, ref tempptr);
            return msg;
        }

        public static string GetClassName(Type type)
        {
            string name = GetClassName(type, false);            
            return name;
        }

        public static string GetClassName(Type type, bool isFullName)
        {
            string name = type.Name;

            if(null != type.FullName)
            {
                if (-1 != type.FullName.IndexOf("+"))
                {
                    string ns = type.Namespace + ".";
                    string s1 = type.FullName.Substring(0, type.FullName.IndexOf("+"));
                    s1 = s1.Substring(ns.Length);
                    name = s1 + "." + type.Name;
                }
            }

            Regex rg = new Regex(@"(?<typeName>.+)`1$", RegexOptions.IgnoreCase);
            if (rg.IsMatch(name))
            {
                name = rg.Match(name).Groups["typeName"].Value;
            }

            if (isFullName)
            {
                name = type.Namespace + "." + name;
            }

            Type[] genericTypes = type.GetGenericArguments();
            if (0 < genericTypes.Length)
            {
                string gts = "";
                foreach (Type item in genericTypes)
                {
                    gts += "," + GetClassName(item, isFullName);
                }
                gts = gts.Substring(1);
                name += "<" + gts + ">";
            }

            return name;
        }

        public static bool IsImplementInterface(this Type instanceType, Type interfaceType)
        {
            bool mbool = false;

            string interfaceName = GetClassName(interfaceType, true);
            Type[] genericTypes = instanceType.GetInterfaces();
            string itName = "";
            foreach (Type item in genericTypes)
            {
                itName = GetClassName(item, true);
                if (itName.Equals(interfaceName))
                {
                    mbool = true;
                    break;
                }
            }

            return mbool;
        }

        public static string RootPath
        {
            get
            {
                string rootPath = "";
                Assembly asse = Assembly.GetExecutingAssembly();
                string path = asse.Location;
                FileInfo fileInfo = new FileInfo(path);
                DirectoryInfo dri = fileInfo.Directory;
                rootPath = dri.FullName;
                return rootPath;
            }
        }

    }
}
