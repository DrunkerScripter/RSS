using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RGS.RGSParser
{
    public class PRobloxProperty    
    {

        public string Name { get; }

        
        public string[] values;

        public string ValueType { get; }

        public PRobloxProperty(string name, string[] values, string ValueType)
        {
            Name = name;
            this.values = values;
            this.ValueType = ValueType;
        }

        internal bool IsEnumType()
        {
            return RobloxJSONParser.Reader.RobloxEnum.Enums.ContainsKey(ValueType);
        }
    }
}
