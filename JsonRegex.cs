using System.Text.RegularExpressions;

namespace System.DJ.DJson
{
    static class JsonRegex
    {
        /// <summary>
        /// 匹配值为json单元对象的键值对 key : {key1:value1,key2:value2,key3:value3}
        /// </summary>
        public static Regex rgKV_ValOfJsonUnit = null;

        /// <summary>
        /// //匹配值为数组(数据实体为元素)的键值对 key : [{key1:value1,key2:value2},{key1:value1,key2:value2}] 或 key : [1, 2, 32]
        /// </summary>
        public static Regex rgKV_ValOfJsonArr = null;

        /// <summary>
        /// 匹配 key_valueOfArray_2_0004 或 key_valueOfJsonUnit_2_0004, 其中 valueOfArray 或 valueOfJsonUnit 是固定值,其它都是可变的
        /// </summary>
        public static Regex rgReplaceSign = null; //替换符匹配

        /// <summary>
        /// 匹配json单元数组 [{key1:value1,key2:value2,key3:value3},{key1:value1,key2:value2,key3:value3},{key1:value1,key2:value2,key3:value3}]
        /// </summary>
        public static Regex rgMixedArr = null; //复杂类型数组

        /// <summary>
        /// 匹配json基础数据类型数组 [1,2,3,4] 或 ["ads", "ee", "ert"]
        /// </summary>
        public static Regex rgBaseTypeArr = null; //基础数据类型数组

        /// <summary>
        /// 匹配json单元 {key1:value1,key2:value2,key3:value3}
        /// </summary>
        public static Regex rgJsonUnit = null;

        /// <summary>
        /// 匹配json单元, 错误检查-单元与单元之间缺少逗号- {key1:value1,key2:value2,key3:value3}{key1:value1,key2:value2,key3:value3}
        /// </summary>
        public static Regex rgJsonUnitErr1 = null;

        /// <summary>
        /// 匹配json单元, 错误检查 -缺少或多出{ }-
        /// </summary>
        public static Regex rgJsonUnitErr2 = null;

        /// <summary>
        /// 匹配json单元, 错误检查 -在key:value中,value为json单元时,{或}缺失或重复- {key1:value1,key2:{k1:v1, k2:v2}}, key3:value3, key4:{k1:v1}, key5: value5}
        /// </summary>
        public static Regex rgJsonUnitErr3 = null;

        /// <summary>
        /// 匹配json键值对 key : value
        /// </summary>
        public static Regex rgKV = null;

        /// <summary>
        /// 匹配 key:value 时, 错误检查 -不存在键值对的情况- {\"userName\", \"age\": 12}
        /// </summary>
        public static Regex rgKVErr1 = null;

        /// <summary>
        /// 匹配 key:value 时, 错误检查 -双引号不配对- {\"userName\", \",age\": 12}
        /// </summary>
        public static Regex rgKVErr2 = null;

        /// <summary>
        /// 匹配基础类型数组所含元素 [1,2,3,4] 或 ["ads", "ee", "ert"]
        /// </summary>
        public static Regex rgBaseTypeVal = null; //匹配基础类型数组value

        /// <summary>
        /// 匹配值为json格式的字符, {"data" : "{\"UserName\":\"ZS\",\"age\":23}", "UID": "asa", "Pwd": "admin"}, 匹配字符串 {\"UserName\":\"ZS\",\"age\":23}
        /// </summary>
        public static Regex rgStrJsonVal = null;

        static JsonRegex()
        {
            string s = "";

            //匹配值为json单元对象的键值对 key : {key1:value1,key2:value2,key3:value3}
            s = @"(((?<FieldName1>"")(?<FieldName>[a-z0-9_]+)"")|(?<FieldName>[a-z0-9_]+))(?<SplitStr>\s*\:\s*)(?<FieldValue>((\{(""?[a-z0-9_]*[""a-z0-9][\s\r\n]*\:[\s\r\n]*[""a-z0-9][a-z0-9_]*""?)+\})|(\[((\,\s*)?\{(""?[a-z0-9_]*[""a-z0-9][\s\r\n]*\:[\s\r\n]*[""a-z0-9][a-z0-9_]*""?)+\}(\s*\,)?)+\])))";
            rgKV_ValOfJsonUnit = new Regex(s, RegexOptions.IgnoreCase);

            //匹配值为数组(数据实体为元素)的键值对 key : [{key1:value1,key2:value2,key3:value3},{key1:value1,key2:value2,key3:value3},{key1:value1,key2:value2,key3:value3}]
            s = @"(((?<FieldName1>"")(?<FieldName>[a-z0-9_]+)"")|(?<FieldName>[a-z0-9_]+))(?<SplitStr>\s*\:\s*)((?<FieldValue>\[[\s\r\n]*(\{(((?!\[[\s\r\n]*\{)(?!\}[\s\r\n]*\]))(.|(\r\n)))+\})*[\s\r\n]*\])|";
            //匹配值为数组(基础数据类型)的键值对 key : [1, 2, 32]
            s += @"(?<FieldValue>\[[\s\r\n]*(((?!\[[\s\r\n]*\{)(?!\}[\s\r\n]*\])(?!\][\s\r\n]*\,[\s\r\n]*[0-9a-z_""])(?!\}[\s\r\n]*\,[\s\r\n]*\{)(?!\:[\s\r\n]*\{))(.|(\r\n)))+[\s\r\n]*\]))";
            rgKV_ValOfJsonArr = new Regex(s, RegexOptions.IgnoreCase);

            //匹配 key_valueOfArray_2_0004 或 key_valueOfJsonUnit_2_0004
            //其中 valueOfArray 或 valueOfJsonUnit 是固定值,其它都是可变的
            s = @"([0-9a-z_]+_valueOfArray_[0-9]+_[0-9]+)|([0-9a-z_]+_valueOfJsonUnit_[0-9]+_[0-9]+)";
            rgReplaceSign = new Regex(s, RegexOptions.IgnoreCase);

            //匹配json数组 [{key1:value1,key2:value2,key3:value3},{key1:value1,key2:value2,key3:value3},{key1:value1,key2:value2,key3:value3}]
            s = @"^\[[\s\r\n]*\{(((?!\[\{)(?!\}\])(?!\:\s*\{))(.|(\r\n)))+\}[\s\r\n]*\]$";
            rgMixedArr = new Regex(s, RegexOptions.IgnoreCase);

            //匹配json基础数据类型数组 [1,2,3,4] 或 ["ads", "ee", "ert"]
            s = @"(^(\[)((\,\s*)?[0-9]+(\s*\,)?)+(\])$)|(^(\[)((\,\s*)?""[^""]+""(\s*\,)?)+(\])$)|(^(\[)((\,\s*)?((true)|(false)|(null))(\s*\,)?)+(\])$)";
            rgBaseTypeArr = new Regex(s, RegexOptions.IgnoreCase);

            //匹配json单元 {key1:value1,key2:value2,key3:value3}
            s = @"((?<JsonUnit>\{((?!\}\s*\,\s*\{)(.|(\r\n)))+\})[\s\r\n]*\,[\s\r\n]*\{)|((?<JsonUnit>\{(.|(\r\n))+\})[\s\r\n]*\]$)|(?<JsonUnit>^\{(\,?[\s\r\n]*((""[a-z0-9_]+""[\s\r\n]*\:[\s\r\n]*""[^""]*"")|(""[a-z0-9_]+""[\s\r\n]*\:[\s\r\n]*((true)|(false)|(null)|([0-9\.\-]+))))[\s\r\n]*\,?)+\}$)";
            rgJsonUnit = new Regex(s, RegexOptions.IgnoreCase);

            //匹配json单元, 错误检查 -单元与单元之间缺少逗号,}{之间缺少逗号- {key1:value1,key2:value2,key3:value3}{key1:value1,key2:value2,key3:value3}
            s = @"((""[a-z0-9_]+"")|([a-z0-9_]+))[\s\r\n]*\:[\s\r\n]*((""(((?!""[\s\r\n]*\})(?!""[\s\r\n]*\,))(.|(\r\n)))*"")|([0-9a-z_]+))[\s\r\n]*\}[\s\r\n]*\{";
            s += @"((""[a-z0-9_]+"")|([a-z0-9_]+))[\s\r\n]*\:[\s\r\n]*((""(((?!""[\s\r\n]*\})(?!""[\s\r\n]*\,))(.|(\r\n)))*"")|([0-9a-z_]+))[\s\r\n]*[\,\}]";
            rgJsonUnitErr1 = new Regex(s, RegexOptions.IgnoreCase);

            //匹配json单元, 错误检查 -缺少或多出{}- 
            //1. [{a:null,"dss1": false},{a:null,"dss2": false,{a:null,"dss3": false}] --缺少 }
            //      [{a:null,"dss2": false, -- Group[0].Value 替换后最终结果,末尾可以是 , 或 ]
            // ***************************
            //2. [{a:null,"dss1": false},a:null,"dss2": false},{a:null,"dss3": false}] --缺少 {
            //      {a:null,"dss1": false},a:null,"dss2": false -- Group[0].Value 捕获取时 ###################
            //      [}, --Group[0].Value -- Group[0].Value 替换后最终结果
            // ***************************
            //3. [a:null,"dss1": false},{a:null,"dss2": false},{a:null,"dss3": false}] --缺少 {
            //      [a:null,"dss1": false},  -- Group[0].Value 替换后最终结果
            // ***************************
            //4. [{{a:null,"dss1": false},{a:null,"dss2": false},{a:null,"dss3": false}] --多出 {
            //      [{  -- Group[0].Value 替换后最终结果(其它位置多出结果一样)
            // ***************************
            //5. [{a:null,"dss1": false}},{a:null,"dss2": false},{a:null,"dss3": false}] --多出 }
            //      {a:null,"dss1": false} -- Group[0].Value 获取时(结尾少, 或 ]) ###################
            //      [},  -- Group[0].Value 替换后最终结果(其它位置多出结果一样)
            s = @"((?<Open>\{)[^\{\}]*)+((?<Close-Open>\})[^\{\}]*)+(?(Open)(?!))";
            rgJsonUnitErr2 = new Regex(s, RegexOptions.IgnoreCase);

            //匹配json单元, 错误检查 -在key:value中,value为json单元时,{或}缺失或重复- {key1:value1,key2:{k1:v1, k2:v2}}, key3:value3, key4:{k1:v1}, key5: value5}
            //s = @"((?<Open>\{)[^\{\}]*)+((?<Close-Open>\})[^\{\}]*)+(?(Open)(?!))";
            //rgJsonUnitErr3 = new Regex(s, RegexOptions.IgnoreCase);

            //匹配json键值对 key : value
            s = @"(((?<Key1>"")(?<Key>[a-z0-9_]+)"")|(?<Key>[a-z0-9_]+))(?<SplitStr>\s*\:\s*)(((?<Value1>"")(?<Value>((?!""[\s\r\n]*\,)(.|(\r\n)))*)""[\s\r\n]*\,)|";
            s += @"((?<Value1>"")(?<Value>((?!""[\s\r\n]*\})(.|(\r\n)))*)""[\s\r\n]*\})|(?<Value>[0-9a-z_]+))";
            rgKV = new Regex(s, RegexOptions.IgnoreCase);

            //匹配 key:value, 错误检查 -不存在键值对的情况- {\"userName\", \"age\": 12}
            s = @"[\{\,][\s\r\n]*((""(((?![\{\,][\s\r\n]*"")(?!""[\s\r\n]*[\,\}])(?!""[\s\r\n]*\:[\s\r\n]*""))(.|(\r\n)))*"")|([0-9a-z_]+))[\s\r\n]*[\,\}]";
            rgKVErr1 = new Regex(s, RegexOptions.IgnoreCase);

            //匹配 key:value 时, 错误检查 -双引号不配对- {\"userName\": \"dss\", \",age\": 12\", \"userName\": \"dss\"}
            //匹配情况：<, \",age> | <: 12\", \"userName> | <: \"12, \"userName> 
            s = @"([\{\[\,][\s\r\n]*""+[\s\r\n]*[\,\}\]])"; //匹配 , " , 或 { ", 或 , " } 等
            s += @"|([\,\{][^\:\,\{]+\:[^""\,\}\]]+""+[\s\r\n]*[\,\}\]])"; //匹配  ,|{ key: value " ,|}|]  ---其中 | 表示或
            s += @"|([\,\{][^\:\,\{]+\:[\s\r\n]*""+((?!""[\s\r\n]*\,)(.|(\r\n)))+\,[\s\r\n]*[^\:\,]+\:)"; //匹配  ,|{ key1: "value1 , key2:
            s += @"|([\,\{][^\:\,\{]+\:[\s\r\n]*""+(((?!""[\s\r\n]*\})(?!\,[^\,\:]+\:.+))(.|(\r\n)))+\})"; //匹配  ,|{ key1: "value1}
            s += @"|([\{\,][\s\r\n]*""+[\s\r\n]*[0-9a-z_]+[\s\r\n]*\:[\s\r\n]*(((?!""[\s\r\n]*\,)(?!\,[^\,\:]+\:.+))(.|(\r\n)))+[\,\}])"; //匹配 {|, "key: value ,|}
            rgKVErr2 = new Regex(s, RegexOptions.IgnoreCase);

            //匹配基础类型数组value [1,2,3,4] 或 ["ads", "ee", "ert"]
            s = @"((?<Value1>"")(?<Value>((?!""[\s\r\n]*\,)(.|(\r\n)))*)""[\s\r\n]*[\,\]])|(?<Value>[0-9a-z_]+)";
            rgBaseTypeVal = new Regex(s, RegexOptions.IgnoreCase);

            //匹配值为json格式的字符, {"data" : "{\"UserName\":\"ZS\",\"age\":23}", "UID": "asa", "Pwd": "admin"}, 匹配字符串 {\"UserName\":\"ZS\",\"age\":23} 
            s = @"""(?<Value>((^\{(\,?[\s\r\n]*((""[a-z0-9_]+""[\s\r\n]*\:[\s\r\n]*""[^""]*"")|(""[a-z0-9_]+""[\s\r\n]*\:[\s\r\n]*((true)|(false)|(null)|([0-9\.\-]+))))[\s\r\n]*\,?)+\}$)|";
            //匹配值为json格式的字符, {"data" : "[{\"UserName\":\"ZS\",\"age\":23},{\"UserName\":\"LS\",\"age\":16}]", "UID": "asa", "Pwd": "admin"}, 匹配字符串 [{\"UserName\":\"ZS\",\"age\":23},{\"UserName\":\"LS\",\"age\":16}]
            s += @"(\[(\,?[\s\r\n]*\{(\,?[\s\r\n]*((""[a-z0-9_]+""[\s\r\n]*\:[\s\r\n]*""[^""]*"")|(""[a-z0-9_]+""[\s\r\n]*\:[\s\r\n]*((true)|(false)|(null)|([0-9\.\-]+))))[\s\r\n]*\,?)+\}[\s\r\n]*\,?)+\])))""";
            rgStrJsonVal = new Regex(s, RegexOptions.IgnoreCase);
        }
    }
}
