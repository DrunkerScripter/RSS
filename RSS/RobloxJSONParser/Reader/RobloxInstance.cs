using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxStyleLanguage.RobloxJSONParser.Reader
{
    class RobloxInstance : RobloxBase
    {

        /*
            The original parser was able to handle Callback's, function's, events, blah blah blah
            Then i thought, wait wat why am i supporting and wasting data on unessary things, so i thought
            stuff that, let's just make it support properties and dat's it.... 
        */

        private string _SuperclassName { get; set; }

        public RobloxInstance Superclass
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_SuperclassName))
                    return null; //No superclass (only class for this is Instance and in the olden times "<<<ROOT>>>"

                return RobloxParser.RobloxHierachy[_SuperclassName];
            }
        }

        private List<RobloxProperty> Properties;

        internal bool GetProperty(string PropertyName, out RobloxProperty PropertyInst)
        {
            if (this.Properties != null)
                foreach (var Property in Properties)
                    if (Property.Name == PropertyName)
                    {
                        PropertyInst = Property;
                        return true;
                    }

            if (Superclass != null)
                return Superclass.GetProperty(PropertyName, out PropertyInst);

            PropertyInst = null;
            return false;
        }

        internal void AddProperty(RobloxProperty Property)
        {
            if (Properties == null)
                Properties = new List<RobloxProperty>();

            Properties.Add(Property);
        }

        public RobloxInstance(string Name, List<string> Tags) : base(Name, Tags) { }

        public RobloxInstance(string Name, List<string> Tags, string superclass) : this(Name, Tags)
        {
            _SuperclassName = superclass;
        }


    }
}
