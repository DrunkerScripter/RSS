using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSS
{

    [Serializable]
    public class ParserException : Exception
    {
        public ParserException() { }
        public ParserException(string message) : base($"Parser Error - {message} - Line {RSSParser.RSSParser.LineNo}") { }
        public ParserException(string message, int LineNo) : base($"Parser Error - {message} - Line {LineNo}") { }
        public ParserException(string message, Exception inner) : base(message, inner) { }
        protected ParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }
}
