using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MT1.Core.Extensions
{
    /// <summary>
    /// string 型の拡張メソッドを管理するクラス
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 文字列が指定されたいずれかの文字列含むかどうか
        /// </summary>
        public static bool Contains(this string str, params string[] param)
        {
            return param.Any(s => str.Contains(s));
        }

    }
}
