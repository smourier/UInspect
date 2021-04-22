using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace UInspect
{
    [Serializable]
    public class AutomationException : Exception
    {
        public const string Prefix = "AUT";

        public AutomationException()
            : base(Prefix + "0001: AutomationException exception.")
        {
        }

        public AutomationException(string message)
            : base(message)
        {
        }

        public AutomationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AutomationException(Exception innerException)
            : base(null, innerException)
        {
        }

        protected AutomationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static int GetCode(string message)
        {
            if (message == null)
                return -1;

            if (!message.StartsWith(Prefix, StringComparison.Ordinal))
                return -1;

            var pos = message.IndexOf(':', Prefix.Length);
            if (pos < 0)
                return -1;

            if (int.TryParse(message.Substring(Prefix.Length, pos - Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var i))
                return i;

            return -1;
        }

        public int Code => GetCode(Message);
    }
}
