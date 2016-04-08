
using System;
using System.Collections.Generic;

namespace RGS.RobloxJSONParser.Reader
{
    class RobloxEnum : RobloxBase
    {
        public static Dictionary<string, RobloxEnum> Enums;

        internal static void AddRobloxEnum(string EnumName, List<string> EnumTags)
        {
            if (Enums == null)
                Enums = new Dictionary<string, RobloxEnum>();

            Enums.Add(EnumName, new RobloxEnum(EnumName, EnumTags));
        }
        public RobloxEnum(string Name, List<string> Tags)
            : base(Name, Tags)
        { }

        public void AddEnumItem(string ItemName, List<string> Tags)
        {
            if (EnumItems == null)
                EnumItems = new List<RobloxEnumItem>();
         
            EnumItems.Add(new RobloxEnumItem(ItemName, Tags, this));
        }

        public List<RobloxEnumItem> EnumItems;

        internal bool HasEnumItem(string enumItemName)
        {
            if (EnumItems == null)
                return false;

            foreach (var Item in EnumItems)
                if (Item.Name == enumItemName)
                    return true;

            return false;
        }
    }
}
