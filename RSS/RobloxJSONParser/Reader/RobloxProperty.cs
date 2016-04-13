using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSS.RobloxJSONParser.Reader
{
    class RobloxProperty : RobloxBase
    {
        public RobloxProperty(string Name, List<string> Tags)
            : base(Name, Tags) { }

        public RobloxProperty(string Name, List<string> Tags, string valueType) : this(Name, Tags)
        {
            this.ValueType = valueType;
        }

        public string ValueType { get; set; }
    }
}
