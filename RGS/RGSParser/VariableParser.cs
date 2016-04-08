using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RGS.RobloxJSONParser.Reader;
using RGS.RobloxJSONParser.Writer;

namespace RGS.RGSParser
{
    class VariableParser
    {
        private Dictionary<string, string> variables;
        public int LineNo { set; get; }

        public VariableParser(Dictionary<string, string> variables, int LineNo)
        {
            this.variables = variables;
            this.LineNo = LineNo;
        }

        private const string CONTENT_ID = "http://www.roblox.com/asset/?ID=";

        //util

        internal static bool IsNumeric(string val)
        {
            int i;

            bool isAnInt = int.TryParse(val, out i);

            if (isAnInt)
                return true;

            double d;

            bool isADouble = double.TryParse(val, out d);

            return isADouble;
        }

        private string GetVariableValue(string varName)
        {
            if (variables == null)
                return string.Empty;

            string varVal;

            varName = varName.Trim();

            bool doesVarExsist = variables.TryGetValue(varName, out varVal);

            if (!doesVarExsist)
                return string.Empty;

            return varVal;
        }

        private void CheckLength(ref string[] items, int expectedNumber, string Name)
        {
            if (items.Length != expectedNumber)
                throw new ParserException($"Incorrect number of elements in given value for {Name}, expected {expectedNumber}, got {items.Length}", LineNo);
        }

        private void ParseIntStringArray(ref string[] stringArray)
        {
            for (int i = 0; i < stringArray.Length; i++)
            {
                if (IsNumeric(stringArray[i]))
                    continue;

                string varVal = GetVariableValue(stringArray[i]);

                if (varVal == string.Empty)
                    throw new ParserException($"Cannot convert arg #{i + 1} to a number", LineNo);

                stringArray[i] = varVal;
            }
        }

        //All da parse functions
        private string[] ParseUDim2(string PropertyValue)
        {
            string[] induvidualParts = PropertyValue.Split(',');

            CheckLength(ref induvidualParts, 4, "UDim2");

            ParseIntStringArray(ref induvidualParts);

            return induvidualParts;
        }

        private string[] ParseColor3(string PropertyValue)
        {
            if (PropertyValue.StartsWith("#"))
            {
                //hex
                try {
                    System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(PropertyValue);
                    
                    return new string[] { c.R.ToString(), c.G.ToString(), c.B.ToString() };
                }
                catch
                {
                    throw new ParserException("Cannot convert hex to Color3", LineNo);
                }
            }
            else
            {
                string[] induvidualParts = PropertyValue.Split(',');

                CheckLength(ref induvidualParts, 3, "Color3");

                ParseIntStringArray(ref induvidualParts);

                return induvidualParts;
            }
        }

        private string[] ParseNumerical(string PropertyValue)
        {
            if (IsNumeric(PropertyValue))
                return new string[] { PropertyValue };

            throw new ParserException($"Unable to convert {PropertyValue} to a numerical value", LineNo);
        }

        private string[] ParseProportion(string PropertyValue)
        {

            if (!(PropertyValue.EndsWith("%") || PropertyValue.EndsWith("px")))
                throw new ParserException("A Proportion type must end with either a % or px");

            string strNumerical = PropertyValue.Substring(0, PropertyValue.Length - (PropertyValue.EndsWith("%") ? 1 : 2));

            if (!IsNumeric(strNumerical))
                throw new ParserException("Property value must be a valid numerical value, and end with either a % OR px");

            return new string[] { RSSJSONTranslator.Quotify(PropertyValue) };
        }

        private string[] ParseEnum(string enumName, string origPropString)
        {
            RobloxEnum Enum;

            bool validEnum = RobloxEnum.Enums.TryGetValue(enumName, out Enum);

            if (!validEnum)
                throw new ParserException($"Unkown Property {enumName}", LineNo);

            if (!Enum.HasEnumItem(origPropString))
                throw new ParserException($"The Enum {enumName} does not have a member called {origPropString}", LineNo);

            return new string[] { origPropString };
        }
        

        //Public methods

        internal string[] FetchPropertyValue(string propname, string origPropString, string valueType)
        {

            //Do a quick check to make sure we haven't been directly given a variable.

            origPropString = origPropString.Trim();

            if (variables != null)
            {
                string varVal = GetVariableValue(origPropString);

                if (varVal != string.Empty)
                    origPropString = varVal;
            }

            switch(valueType)
            {
                case "UDim2": //Size and positioning and stuff
                    return ParseUDim2(origPropString);

                case "Color3": //COLAH
                    return ParseColor3(origPropString);

                case "int": //whole number
                case "float": //low precision decimal
                case "double": //high precision decimal
                    return ParseNumerical(origPropString);

                case "Proportion": //a quatity specified in either pixels or percentage.
                    return ParseProportion(origPropString);
                case "Content": //Asset ID's
                    if (origPropString.StartsWith(CONTENT_ID))
                        return new string[] { origPropString };
                    else if (IsNumeric(origPropString))
                        return new string[] { CONTENT_ID + " " + origPropString };

                    throw new ParserException($"Unable to recognise {origPropString} as a ROBLOX asset.", LineNo);
                case "bool": //true/+false

                    if (!(origPropString == "false" || origPropString == "true"))
                        throw new ParserException($"Unable to recognise {origPropString} as a boolean", LineNo);

                    return new string[] { (origPropString == "true").ToString().ToLower() };

                case "string": //Text
                    return new string[] { origPropString };
                case "Object": //NOPE.
                    break;

                default: // Enum
                    return ParseEnum(propname, origPropString);
          }


            return null; //wut
        }
    }
}
