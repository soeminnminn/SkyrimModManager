using System;
using System.Runtime.Serialization;

namespace BsaBrowser.Wildcard
{
    internal class WildcardPatternException : Exception
    {
        public WildcardPatternException()
        {
        }

        public WildcardPatternException(string message)
            : base(message)
        {
        }

        public WildcardPatternException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected WildcardPatternException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
