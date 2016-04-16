using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSS.RobloxJSONParser.Reader
{
    class RobloxBase
    {

        private List<string> Tags;

        internal bool HasTag(string Tag)
        {
            if (Tags == null)
                return false;

            return Tags.Contains(Tag);
        }

        public string Name { get; }

        public RobloxBase(string Name, List<string> Tags)
        {
            this.Name = Name;
            this.Tags = Tags;
        }

    }
}
