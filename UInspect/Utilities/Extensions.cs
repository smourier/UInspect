using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace UInspect.Utilities
{
    public static class Extensions
    {
        public static void Log(object value, [CallerMemberName] string methodName = null)
        {
            var name = Thread.CurrentThread.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = Thread.CurrentThread.ManagedThreadId.ToString();
            }
            System.Diagnostics.Debug.WriteLine("[" + name + "]" + methodName + " " + value);
        }

        public static bool EqualsIgnoreCase(this string str, string text, bool trim = false)
        {
            if (trim)
            {
                str = str.Nullify();
                text = text.Nullify();
            }

            if (str == null)
                return text == null;

            if (text == null)
                return false;

            if (str.Length != text.Length)
                return false;

            return string.Compare(str, text, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string Nullify(this string str)
        {
            if (str == null)
                return null;

            if (string.IsNullOrWhiteSpace(str))
                return null;

            var t = str.Trim();
            return t.Length == 0 ? null : t;
        }

        public static IEnumerable<string> SplitToList(this string str, params char[] separators)
        {
            if (str == null || separators == null || separators.Length == 0)
                yield break;

            foreach (var s in str.Split(separators))
            {
                if (!string.IsNullOrWhiteSpace(s))
                    yield return s;
            }
        }

        public static void BeginInvoke(this Control control, Action action) => control.BeginInvoke(action);

        public static T GetSelectedTag<T>(this TreeView tree)
        {
            var tag = tree.SelectedNode?.Tag;
            if (tag == null || !typeof(T).IsAssignableFrom(tag.GetType()))
                return default;

            return (T)tag;
        }
    }
}
