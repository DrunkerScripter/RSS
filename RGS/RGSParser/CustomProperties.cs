using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RGS.RobloxJSONParser.Reader;

namespace RGS.RGSParser
{
    static class CustomProperties
    {

        internal static void AddEnumProperty(RobloxInstance GuiObject, string EnumName, params string[] Values)
        {
            GuiObject.AddProperty(new RobloxProperty(EnumName, null, EnumName));

            RobloxEnum Enum = new RobloxEnum(EnumName, null);

            foreach (var Item in Values)
                Enum.AddEnumItem(Item, null);

            RobloxEnum.Enums.Add(EnumName, Enum);
        }

        internal static bool Init()
        {

            //Adds all the custom properties and stuff to the API 

            RobloxInstance GuiObject;

            bool Success = RobloxParser.RobloxHierachy.TryGetValue("GuiObject", out GuiObject);

            if (!Success)
                return false;

            //Add all the properties.

            AddEnumProperty(GuiObject, "Alignment", "Centre", "Top-Left", "Top-Right", "Top-Centre", "Bottom-Left", "Bottom-Right", "Bottom-Centre", "Centre-Left", "Centre-Right");

            GuiObject.AddProperty(new RobloxProperty("padding-left", null, "Proportion"));
            GuiObject.AddProperty(new RobloxProperty("padding-right", null, "Proportion"));
            GuiObject.AddProperty(new RobloxProperty("padding-top", null, "Proportion"));
            GuiObject.AddProperty(new RobloxProperty("padding-bottom", null, "Proportion"));

            return true;
        }
    }
}
