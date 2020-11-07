using System.Reflection;
using System.Text.RegularExpressions;

namespace System.DJ.DJson
{
    /// <summary>
    /// 添加json数据数组元素大括号 {} 检查规则只需要新增私有方法具可,程序会自动调用
    /// 所新增的方法名称可以自定义, 参数类型,名称,数量必须统一采用(string groupValue, string replaceResult)
    /// 且方法无返回值
    /// </summary>
    class MatchBigBracketInJson
    {
        int max = 9999;

        public MatchBigBracketInJson() { }

        public MatchBigBracketInJson(int max)
        {
            this.max = max;
        }

        public void check(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            string sjson = json.Trim();
            if ("[" != sjson.Substring(0, 1) || "]" != sjson.Substring(sjson.Length - 1)) return;
            if (!JsonRegex.rgJsonUnit.IsMatch(sjson)) return;

            string groupValue = null;

            object current = this;
            MethodInfo[] methods = current.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            ParameterInfo[] paras = null;
            string paraName = "";
            int len = methods.Length;
            int n = 0;
            while (JsonRegex.rgJsonUnitErr2.IsMatch(sjson) && max > n)
            {
                n++;
                groupValue = JsonRegex.rgJsonUnitErr2.Match(sjson).Groups[0].Value;
                sjson = sjson.Replace(groupValue, "");

                if (0 == len) continue;
                foreach (MethodInfo item in methods)
                {
                    if (2 != item.GetParameters().Length) continue;
                    paras = item.GetParameters();
                    paraName = paras[0].Name;
                    if (!paraName.Equals("groupValue")) continue;
                    item.Invoke(current, new object[] { groupValue, null });
                }
            }

            if (0 == len) return;
            foreach (MethodInfo item in methods)
            {
                if (2 != item.GetParameters().Length) continue;
                paras = item.GetParameters();
                paraName = paras[0].Name;
                if (!paraName.Equals("groupValue")) continue;
                item.Invoke(current, new object[] { null, sjson });
            }
        }

        /// <summary>
        /// 缺少后 }
        /// [{a:null,"dss1": false,{a:null,"dss2": false},{a:null,"dss3": false}] --缺少 }
        /// [{a:null,"dss1": false, --替换后最终结果
        /// </summary>
        /// <param name="s"></param>
        void NonaEndBracket(string groupValue, string replaceResult)
        {
            if (string.IsNullOrEmpty(replaceResult)) return;
            string s = @"^\[[\s\r\n]*(?<ErrItem>\{(.|(\r\n))+)";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            if (rg.IsMatch(replaceResult))
            {
                string ErrItem = rg.Match(replaceResult).Groups["ErrItem"].Value;
                throw new Exception(ErrItem + " 缺少后括号 '}' ");
            }
        }

        /// <summary>
        /// 缺少前 { , 情况1
        /// [{a:null,"dss1": false},a:null,"dss2": false},{a:null,"dss3": false}] --缺少 {
        /// {a:null,"dss1": false},a:null,"dss2": false --获取时
        /// [}, --替换后最终结果
        /// </summary>
        /// <param name="groupValue"></param>
        /// <param name="replaceResult"></param>
        void NonaStartBracket1(string groupValue, string replaceResult)
        {
            if (string.IsNullOrEmpty(groupValue)) return;
            if (!JsonRegex.rgJsonUnit.IsMatch(groupValue)) return;

            string s = "";
            s = JsonRegex.rgJsonUnit.Match(groupValue).Groups[0].Value;
            groupValue = groupValue.Replace(s, "");

            if (!JsonRegex.rgKV.IsMatch(groupValue)) return;

            string ErrItem = groupValue + "}";
            throw new Exception(ErrItem + " 缺少前括号 '{' ");
        }

        /// <summary>
        /// 缺少前 { , 情况2
        /// [a:null,"dss1": false},{a:null,"dss2": false},{a:null,"dss3": false}] --缺少 {
        /// [a:null,"dss1": false},  --替换后最终结果
        /// </summary>
        /// <param name="groupValue"></param>
        /// <param name="replaceResult"></param>
        void NonaStartBracket2(string groupValue, string replaceResult)
        {
            if (string.IsNullOrEmpty(replaceResult)) return;

            string s = @"^\[(?<ErrItem>(.|(\r\n))+\})[\s\r\n]*[\,\]]$";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            if (rg.IsMatch(replaceResult))
            {
                string ErrItem = rg.Match(replaceResult).Groups["ErrItem"].Value;
                throw new Exception(ErrItem + " 缺少前括号 '{' ");
            }
        }

        /// <summary>
        /// 重复的前括号 {
        /// [{{a:null,"dss1": false},{a:null,"dss2": false},{a:null,"dss3": false}] --多出 {
        /// [{  --替换后最终结果(其它位置多出结果一样)
        /// </summary>
        /// <param name="groupValue"></param>
        /// <param name="replaceResult"></param>
        void RepeatStartBracket(string groupValue, string replaceResult)
        {
            if (string.IsNullOrEmpty(replaceResult)) return;

            string s = @"^\[[\s\r\n]*\{$";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            if (rg.IsMatch(replaceResult))
            {
                throw new Exception("json 数据中存在重复的前括号");
            }
        }

        /// <summary>
        /// 重复的后括号 }
        /// [{a:null,"dss1": false}},{a:null,"dss2": false},{a:null,"dss3": false}] --多出 }
        /// {a:null,"dss1": false} --获取时(结尾少, 或 ])
        /// [},  --替换后最终结果(其它位置多出结果一样)
        /// </summary>
        /// <param name="groupValue"></param>
        /// <param name="replaceResult"></param>
        void RepeatEndBracket(string groupValue, string replaceResult)
        {
            if (string.IsNullOrEmpty(groupValue)) return;

            string s = @"^\{(.|(\r\n))+\}$";
            Regex rg = new Regex(s, RegexOptions.IgnoreCase);
            if (rg.IsMatch(groupValue))
            {
                throw new Exception("字符 " + groupValue + " 中存在重复的后括号 '}'");
            }
        }
    }
}
