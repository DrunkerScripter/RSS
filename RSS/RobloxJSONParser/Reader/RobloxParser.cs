using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSS.Misc;
using System.Text.RegularExpressions;

namespace RSS.RobloxJSONParser.Reader
{
    internal static class RobloxParser
    {
        //private RobloxParser() { }

        public static Dictionary<string, RobloxInstance> RobloxHierachy;

        private static void ParseClass(string Row)
        {
            string Name = RegExManager.GetKeyValue(Row, "Name");
            string Superclass = RegExManager.GetKeyValue(Row, "Superclass");

            List<string> Tags = DecodeTags(Row);

            if (RobloxHierachy == null)
                RobloxHierachy = new Dictionary<string, RobloxInstance>();

            RobloxHierachy.Add(Name, new RobloxInstance(Name, Tags, Superclass));
        }

        private static void ParseProperty(string Row)
        {
            string ClassName = RegExManager.GetKeyValue(Row, "Class");

            RobloxInstance Inst;

            if (!RobloxHierachy.TryGetValue(ClassName, out Inst))
                throw new ArgumentException("Invalid ClassName " + ClassName);

            string Name = RegExManager.GetKeyValue(Row, "Name");
            string ValueType = RegExManager.GetKeyValue(Row, "ValueType");

            List<string> Tags = DecodeTags(Row);
            
            Inst.AddProperty(new RobloxProperty(Name, Tags, ValueType));
        }
          
        private static void ParseEnum(string Row)
        {
            string EnumName = RegExManager.GetKeyValue(Row, "Name");

            List<string> Tags = DecodeTags(Row);

            RobloxEnum.AddRobloxEnum(EnumName, Tags);
        } 

        private static void ParseEnumItem(string Row)
        {
            string Name = RegExManager.GetKeyValue(Row, "Name");
            List<string> Tags = DecodeTags(Row);

            string Enum = RegExManager.GetKeyValue(Row, "Enum");
            
            RobloxEnum e = RobloxEnum.Enums[Enum];

            e.AddEnumItem(Name, Tags);
        }

        private static void ProcessRow(string Row)
        {
            if (Row == "[") //Don't ask.
                return;
          
            string TypeOfRow = RegExManager.GetKeyValue(Row, "type");

            switch(TypeOfRow)
            {
                case "Class":
                    ParseClass(Row);
                    break;
                case "Property":
                    ParseProperty(Row);
                    break;
                case "Enum":
                    ParseEnum(Row);
                    break;
                case "EnumItem":
                    ParseEnumItem(Row);
                    break;
            }

        }
        private static List<string> DecodeTags(string Row)
        {
            List<string> Tags = new List<string>();

            RegExManager.ForEachMatch(Row, RegExManager.Tags, (regExMatch) => {
                Group g = regExMatch.Groups["tags"];
                if (!string.IsNullOrWhiteSpace(g.Value))
                    RegExManager.ForEachMatch(g.Value, RegExManager.SeparateTags, (Argument) => {
                        if (!string.IsNullOrWhiteSpace(Argument.Value))
                            if (Argument.Value.StartsWith("\"") && Argument.Value.EndsWith("\""))
                                Tags.Add(Argument.Value.Substring(1, Argument.Value.Length - 2));
                            else
                                Tags.Add(Argument.Value);
                    });
            });
            
            return Tags;
        }

        internal static void Parse(string downloadPath)
        {
            StreamReader Reader = null;

            try
            {
                Reader = new StreamReader(File.Open(downloadPath, FileMode.Open));

                ForEachCurleyBrace(Reader, ProcessRow);
            }
            catch (Exception e) { throw e; } //I want the exception to be caught higher up in the program.
            finally //but i want this finally statmet
            {
                if (Reader != null)
                    Reader.Close();
            }
        }

        internal static void ForEachCurleyBrace(StreamReader reader, Action<string> callback)
        {
            //Keeps reading from a string until it gets a complete {} accouting for sub {}'s inside 

            StringBuilder builder = new StringBuilder();
            char currentLetter;
            int depth = 0;

            while (!reader.EndOfStream)
            {
                currentLetter = (char)reader.Read();

                switch(currentLetter)
                {
                    case '{':
                        depth += 1;
                        break;
                    case '}':
                        depth -= 1;
                        break;
                    case ',':
                        if (depth == 0)
                            continue;

                        break;
                }

                builder.Append(currentLetter);

                if (depth == 0)
                {
                    string str = builder.ToString();

                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        callback.Invoke(str);
                        builder.Clear();
                    }
                }

            }


        }
    }
}
