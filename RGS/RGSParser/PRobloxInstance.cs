using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RGS.RobloxJSONParser.Reader;

namespace RGS.RGSParser
{
    public class PRobloxInstance
    {
        public string Name { get; set; }

        public string ClassName { get; set; }

        public List<PRobloxProperty> Properties = null;

        public List<PRobloxInstance> Children = null;

        public bool isCloneOfCustomChild { get; set; }
        public string customChildName { get; set; }


        public bool IsCustom { get; set; }

        internal void AddProperty(string Name, string[] values, string valType)
        {
            if (Properties == null)
                Properties = new List<PRobloxProperty>();

            Properties.Add(new PRobloxProperty(Name, values, valType));
        }

        public PRobloxInstance(string ClassName, string Name, bool IsCustom)
        {
            this.ClassName = ClassName;
            this.Name = Name;
            this.IsCustom = IsCustom;
        }

        public PRobloxInstance(string ClassName, string Name, bool IsCustom, bool isCustomChild, string customChildName) : this(ClassName, Name, IsCustom)
        {
            this.isCloneOfCustomChild = isCustomChild;
            this.customChildName = customChildName;
        }

        internal void AddChild(PRobloxInstance instance)
        {
            if (Children == null)
                Children = new List<PRobloxInstance>();

            Children.Add(instance);
        }


        internal bool GetProperty(string propName, out RobloxProperty property)
        {
            RobloxInstance Instance = RobloxParser.RobloxHierachy[ClassName];

            return Instance.GetProperty(propName, out property);
        }
    }
}
