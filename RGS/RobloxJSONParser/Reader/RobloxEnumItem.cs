using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RGS.RobloxJSONParser.Reader
{
    class RobloxEnumItem : RobloxBase
    {
        public RobloxEnum Parent { get; set; }

        public RobloxEnumItem(string Name, List<string> Tags, RobloxEnum Parent)
            :base(Name, Tags)
        {
            this.Parent = Parent;
        }
    }
}
