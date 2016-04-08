﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;


namespace RGS.Misc
{
    static class RegExManager
    {
        public enum LineType
        {
            VARIABLE_DECLARATION,
            WIDGET_SCOPE_OPENING,
            PROPERTY_ASSIGNMENT,
            SCOPE_END,
            CUSTOM_WIDGET_SCOPE,
            BLANK
        }

        public static readonly Regex Tags = new Regex("\"tags\":(?<tags>.+?)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex SeparateTags = new Regex("\"(?<val>.+?)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex Arguments = new Regex("\"Arguments\":(?<args>.+?)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex VarsRegx = new Regex(@"\s*var\s*(?<var_name>.+?)\s*=\s*(?<var_val>.+?);", RegexOptions.Compiled);
        private static readonly Regex NewScopeDeclaration = new Regex("\\s*(widget)?\\s*(?<class_name>.+?)\\s*\"(?<var_name>.+?)\"\\s*{", RegexOptions.Compiled);
        private static readonly Regex PropertiesRegx = new Regex("\\s*(?<prop_name>[^\\s]+?)\\s*:\\s*(?<prop_val>.+?);", RegexOptions.Compiled);
        private static readonly Regex CustomWidgetScope = new Regex("\\s*\"(?<widget_name>.+?)\"\\s*{", RegexOptions.Compiled);

        private static Dictionary<string, Regex> RegexStorage = new Dictionary<string, Regex>();

        private static string GetRegex(string keyName)
        {
            return string.Format("\"{0}\":\"(?<{0}>.+?)\"", keyName);
        }

        internal static string GetKeyValue(string row, string regexKey)
        {
            Regex r;

            RegexStorage.TryGetValue(regexKey, out r);

            if (r == null)
            {
                r = new Regex(GetRegex(regexKey), RegexOptions.Compiled);
                RegexStorage.Add(regexKey, r);
            }

            Group g = r.Match(row).Groups[regexKey];

            if (g == null)
                return "";

            return g.Value;
        }

        internal static void ForEachMatch(string text, Regex regEx, Action<Match> Callback)
        {
            Match regMatch;

            for (regMatch = regEx.Match(text); regMatch.Success; regMatch = regMatch.NextMatch())
                Callback.Invoke(regMatch);
        }

        internal static string[] GetVariableDetails(string line)
        {
            Match m = VarsRegx.Match(line);

            return new string[] { m.Groups["var_name"].Value, m.Groups["var_val"].Value };
        }

        internal static string[] GetCustomWidgetScopeDetails(string Line)
        {
            Match m = CustomWidgetScope.Match(Line);

            return new string[] { m.Groups["widget_name"].Value };
        }

        internal static string[] GetWidgetScopeDetails(string line)
        {
            Match m = NewScopeDeclaration.Match(line);

            return new string[] { m.Groups["class_name"].Value, m.Groups["var_name"].Value }; 
        }

        internal static string[] GetPropertiesDetails(string line)
        {
            Match m = PropertiesRegx.Match(line);

            return new string[] { m.Groups["prop_name"].Value, m.Groups["prop_val"].Value };
        }

        /* Variable Line ID Details */
        private static List<VariableIdentifier> LineIds;

        private static void AddLineID(string RegexStr, LineType ID)
        {
            LineIds.Add(new VariableIdentifier(RegexStr, ID, (Row, IdentifierRegex) => {
                return IdentifierRegex.IsMatch(Row);
            }));
        }

        public class VariableIdentifier
        {
            private Regex IdentifierRegex;

            public Func<string, bool> Identifier;

            public LineType Name;

            public VariableIdentifier(string regex, LineType name, Func<string, Regex, bool> Identifier)
            {
                IdentifierRegex = new Regex(regex, RegexOptions.Compiled);
                Name = name;
                this.Identifier = str => { return Identifier.Invoke(str, IdentifierRegex); };
            }
        }

        private static List<VariableIdentifier> GetLineIds()
        {
            if (LineIds == null)
            {
                LineIds = new List<VariableIdentifier>();

                AddLineID(@"\s*var\s*[\w_][\w\d_]*\s*=\s*.+?;", LineType.VARIABLE_DECLARATION);

                AddLineID(@"\s*.+?:.+?;", LineType.PROPERTY_ASSIGNMENT);

                AddLineID("}", LineType.SCOPE_END); //(?=[^\"'])

                AddLineID("(widget)?\\s*\\w+\\s*\".+?\"\\s*{", LineType.WIDGET_SCOPE_OPENING);

                AddLineID("\\s*(?<widget_name>.+?)\\s*{", LineType.CUSTOM_WIDGET_SCOPE);
            }

            return LineIds;
        }

        public static LineType GetLineType(string Line)
        {
            foreach(var ID in GetLineIds())
                if (ID.Identifier.Invoke(Line))
                    return ID.Name;
            
            return LineType.BLANK;
        }


    }
}
