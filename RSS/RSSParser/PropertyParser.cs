using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobloxStyleLanguage.RSSParser;
using RobloxStyleLanguage.RobloxJSONParser.Reader;
using System.Drawing;
using RobloxStyleLanguage.RobloxJSONParser.Writer;

namespace RobloxStyleLanguage.RSSParser
{
    static class PropertyParser
    {

        delegate string[] PropertyProcessCallback(string Input, Scope S);

        internal static bool IsNumeric(string s)
        {
            int i;

            bool isInt = int.TryParse(s, out i);

            if (isInt)
                return true;

            double d;

            bool isDouble = double.TryParse(s, out d);

            return isDouble;
        }

        private static Dictionary<string, PropertyProcessCallback> PropertyCallbacks;

        private static string FetchVarVal(Scope sc, string varVal)
        {
            string newVarVal = sc.FetchStringVariableValue(varVal);

            if (newVarVal != null)
                return newVarVal;

            return varVal;
        }

        internal static void Parse(Scope Sc, string varVal, string propName, string[] classes = null)
        {
            if (PropertyCallbacks == null)
                AddCallbacks();

            //Properties can be defined within a WidgetScope.
            
            if (Sc.ID == "Widget")
            {
                var WScope = Sc as WidgetScope;

                if (WScope != null)
                {
                    RSSInstance Instance = WScope.Instance;

                    RobloxProperty Prop;

                    if (!Instance.HasProperty(propName, out Prop))
                        throw new ParserException($"The Instance {Instance.Name} does not have the property {propName}");

                    //Check to make sure the value passed isn't a variable.

                    varVal = FetchVarVal(Sc, varVal);

                    //Check for a callback.

                    PropertyProcessCallback Callback;

                    bool isValidFunc = PropertyCallbacks.TryGetValue(Prop.ValueType, out Callback);

                    if (!isValidFunc)
                    {
                        /////ENUM CALLBACK
                        RobloxEnum Enum;
                        if (!RobloxEnum.Enums.TryGetValue(Prop.ValueType, out Enum))
                            throw new ParserException($"Failed to find the correct datatype for {Prop.Name} for Instance {Instance.Name}");

                        if (!Enum.HasEnumItem(varVal))
                            throw new ParserException($"The Enunm {Enum.Name} does not have the item {varVal}");

                        Instance.AddProperty(new RSSProperty(Prop.Name, Prop.ValueType, new string[] { RobloxStyleLanguage.RobloxJSONParser.Writer.JSONWriter.Quotify(varVal) }));   
                    }
                    else
                        Instance.AddProperty(new RSSProperty(Prop.Name, Prop.ValueType, Callback(varVal, WScope))); 

                }
            }
            else if (Sc.ID == "StyleId")
            {
                RobloxProperty Prop = null;
                foreach(var Name in classes)
                {
                    RobloxInstance Inst = RobloxParser.RobloxHierachy[Name];

                    if (!Inst.GetProperty(propName, out Prop))
                        throw new ParserException($"The ClassName {Name} in the style {((StyleScope)Sc.Parent).StyleName} does not have the property you are trying to set: {propName}");
                }
                
                PropertyProcessCallback Callback;
                
                bool isValidFunc = PropertyCallbacks.TryGetValue(Prop.ValueType, out Callback);

                if (isValidFunc)
                    ((StyleIdScope)Sc).AddProperty(new RSSProperty(Prop.Name, Prop.ValueType, Callback(varVal, Sc)));
            }

        }

        private static void ValidateStringArray(ref string[] vals, Scope sc, int expectedNoOfArgs)
        {
            //Make sure correct number of elements.
            if (vals.Length != expectedNoOfArgs)
                throw new ParserException($"Incorrect number of arguments given, expected {expectedNoOfArgs}, got {vals.Length}");

            for (int i = 0; i < vals.Length; i++)
            {
                //Check to see if it's a variable.
                vals[i] = sc.FetchStringVariableValue(vals[i]);

                //Make sure it's numeric
                if (!IsNumeric(vals[i]))
                    throw new ParserException($"Expected a numerical value for value #{i+1}, but couldn't evaulate it as numerical");
            }
        }


        private static void AddCallbacks()
        {
            PropertyCallbacks = new Dictionary<string, PropertyProcessCallback>();

            PropertyProcessCallback NumericalCallback = (str, scope) => {
                if (!IsNumeric(str))
                    throw new ParserException("Property value must be numerical");

                return new string[] { str };
            };

            PropertyCallbacks.Add("int", NumericalCallback);
            PropertyCallbacks.Add("double", NumericalCallback);
            PropertyCallbacks.Add("float", NumericalCallback);
            
            PropertyCallbacks.Add("string", (str, scope) => {
                bool isString = (str.StartsWith("\"") && str.EndsWith("\""));

                if (!isString)
                    throw new ParserException("Property value must be a string");

                return new string[] { str };
            });

            PropertyCallbacks.Add("bool", (str, scope) =>
            {
                bool isBool = (str == "true" || str == "false");

                if (!isBool)
                    throw new ParserException("Property value must be a bool");

                return new string[] { str };
            });

            PropertyCallbacks.Add("UDim2", (str, scope) => {
                string[] vals = str.Split(',');

                ValidateStringArray(ref vals, scope, 4);

                return vals;
            });

            PropertyCallbacks.Add("Color3", (str, scope) => {
                if (str.StartsWith("#"))
                {
                    try
                    {
                        Color col = ColorTranslator.FromHtml(str);

                        return new string[] { col.R.ToString(), col.G.ToString(), col.B.ToString() };
                    }
                    catch (Exception)
                    {
                        throw new ParserException("Failed to convert to Color3");
                    }
                }
                else
                {
                    var vals = str.Split(',');

                    ValidateStringArray(ref vals, scope, 3);

                    return vals;
                }
            });

            PropertyCallbacks.Add("Proportion", (str, scope) => {
                if (!(str.EndsWith("%") || str.EndsWith("px")))
                    throw new ParserException("Failed to convert to proportion, must end in either a '%' sign or 'px'");

                return new string[] { JSONWriter.Quotify(str) };
                    
                
            });

            PropertyCallbacks.Add("Style", (str, scope) => {
                RSSParser Current = RSSParser.CurrentParser;

                if (Current.Styles == null)
                    throw new ParserException($"No Style called {str}");

                foreach (var style in Current.Styles)
                    if (style.styleName == str)
                        return new string[] { JSONWriter.Quotify(str) };

                throw new ParserException($"No Style called {str}");
            });
            
        }

        
    }
}
