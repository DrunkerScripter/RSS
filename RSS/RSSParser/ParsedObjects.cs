using RSS.RobloxJSONParser.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSS.RSSParser
{
    class RSSProperty
    {
        //What type of the property is it (Color3, UDim2, etc)
        public string ValueType { get; set; }

        //Name of the property
        public string Name { get; set; }

        //Arrary of the values
        public string[] Values { get; set; }


        public RSSProperty(string PropName, string ValueType, string[] Values)
        {
            this.Name = PropName;
            this.ValueType = ValueType;
            this.Values = Values;
        }

    }

    class RSSInstance
    {
        //Classname of the instance
        public string ClassName { get; set; }

        //Actual name
        public string Name { get; set; }

        //Whether it is a custom child and defined with "widget"
        public bool IsCustomChild { get; set; }

        //If it's a clone of a custom child then this is the name of the "widget" it has been cloned from.
        public string CustomChildName { get; set; }

        //All the properties of the instance
        public List<RSSProperty> Properties;

        //Any descendants
        public List<RSSInstance> Children;


        public RSSInstance(string ClassName, string Name)
        {
            this.ClassName = ClassName;
            this.Name = Name;
        }

        public RSSInstance(string ClassName, string Name, bool isCustom) : this(ClassName, Name)
        {
            IsCustomChild = isCustom;
        }

        public RSSInstance(string ClassName, string Name, string CustomChildName) : this(ClassName, Name)
        {
            this.CustomChildName = CustomChildName;
        }

        internal bool HasProperty(string propName, out RobloxProperty Prop)
        {
            return RobloxParser.RobloxHierachy[ClassName].GetProperty(propName, out Prop);
        }

        internal void AddProperty(RSSProperty prop)
        {
            if (Properties == null)
                Properties = new List<RSSProperty>();

            Console.WriteLine($"Added Property {prop.Name} with (first) value {prop.Values[0]} to {Name}");

            Properties.Add(prop);
        }

        internal void AddChild(RSSInstance instance)
        {
            if (Children == null)
                Children = new List<RSSInstance>();

            Children.Add(instance);
        }
    }


    class RSSStyle
    {
        public List<RSSStyleItem> styleIds;
        public string styleName;

        public RSSStyle(string styleName, List<RSSStyleItem> styleIds)
        {
            this.styleName = styleName;
            this.styleIds = styleIds;
        }
    }

    class RSSStyleItem
    {

        public string[] Ids;

        public List<RSSProperty> Properties;

        internal void AddProperty(RSSProperty Property)
        {
            if (Properties == null)
                Properties = new List<RSSProperty>();

            Properties.Add(Property);
        }

        public RSSStyleItem(string[] ids, List<RSSProperty> props)
        {
            this.Ids = ids;
            this.Properties = props;
        }

    }

    
}
