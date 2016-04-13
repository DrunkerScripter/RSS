using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSS.RobloxJSONParser.Reader;

namespace RSS.RSSParser
{
    static class CustomProperties
    {

        private static void AddRobloxEnum(string EnumName, params string[] EnumItems)
        {
            RobloxEnum Enum = new RobloxEnum(EnumName, null);

            foreach (var Item in EnumItems)
                Enum.AddEnumItem(Item, null);

            RobloxEnum.Enums.Add(EnumName, Enum);
        }

        internal static void Init()
        {

            RobloxInstance GuiObject = RobloxParser.RobloxHierachy["GuiObject"]; //This better be hear.


            //Add custom properties
            //GuiObject is superclass to all flat plane objects.
            GuiObject.AddProperty(new RobloxProperty("padding-left", null, "Proportion"));
            GuiObject.AddProperty(new RobloxProperty("padding-right", null, "Proportion"));
            GuiObject.AddProperty(new RobloxProperty("padding-top", null, "Proportion"));
            GuiObject.AddProperty(new RobloxProperty("padding-bottom", null, "Proportion"));


            GuiObject.AddProperty(new RobloxProperty("Alignment", null, "Alignment"));

            AddRobloxEnum("Alignment", "Top-Left", "Top-Centre", "Top-Right", "Centre-Left", "Centre", "Centre-Right", "Bottom-Left", "Bottom-Right");

        }

    }
}   
