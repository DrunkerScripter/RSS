using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using RobloxStyleLanguage.RSSParser;
using RobloxStyleLanguage;
using RobloxStyleLanguage.Statments;
using RobloxStyleLanguage.RobloxJSONParser.Reader;

#region Statments
namespace RobloxStyleLanguage.Statments
{
    interface Statment
    {
        StatmentType ID { get; }

        bool IsMatch(string Line);

        string[] GetDetails(string Line);

        Scope Generate(string Line, Scope Parent);
    }

    class VariableDeclaration : Statment
    {
        private static readonly Regex MatchRegex = new Regex("\\s*var\\s.+?\\s*=\\s*.+", RegexOptions.Compiled);
        private static readonly Regex DetailsRegex = new Regex("\\s*var\\s(?<varname>.+?)\\s*=\\s*(?<varval>.+)", RegexOptions.Compiled);

        public StatmentType ID
        {
            get
            {
                return StatmentType.Variable;
            }
        }

        public string[] GetDetails(string Line)
        {
            Match m = DetailsRegex.Match(Line);

            string varName = m.Groups["varname"].Value;
            string rawVarVal = m.Groups["varval"].Value;

            if (string.IsNullOrWhiteSpace(varName))
                throw new ParserException("Cannot have blank variable name");

            if (string.IsNullOrWhiteSpace(rawVarVal))
                throw new ParserException("Cannot have blank variable value");

            return new string[] { varName, rawVarVal };
        }

        public bool IsMatch(string Line)
        {
            return MatchRegex.IsMatch(Line);
        }

        public Scope Generate(string Line, Scope Parent)
        {
            return null;
        }

    }
}

class WidgetDeclaration : Statment
{
    private static readonly Regex MatchRegex = new Regex("\\s*(widget)?\\s*.+?\\s*\".+?\"\\s*{", RegexOptions.Compiled);
    private static readonly Regex DetailsRegex = new Regex("\\s*(widget)?\\s*(?<classname>.+?)\\s*\"(?<name>.+?)\"\\s*{", RegexOptions.Compiled);

    public StatmentType ID
    {
        get
        {
            return StatmentType.WidgetScopeOpening;
        }
    }

    public string[] GetDetails(string Line)
    {
        Match m = DetailsRegex.Match(Line);

        string widgetName = m.Groups["classname"].Value;
        string widgetClassName = m.Groups["name"].Value;

        if (string.IsNullOrWhiteSpace(widgetName))
            throw new ParserException("You cannot leave the classname blank");

        return new string[] { widgetName, widgetClassName };
    }

    public bool IsMatch(string Line)
    {
        return MatchRegex.IsMatch(Line) && !Line.StartsWith("style ");
    }

    public Scope Generate(string Line, Scope Parent)
    {
        return new WidgetScope(RSSParser.CurrentParser, new StatmentType[] { StatmentType.WidgetScopeOpening , StatmentType.Variable , StatmentType.PropertyAssignment, StatmentType.CustomWidgetScopeOpening }, Parent);
    }
    
}

class WidgetScopeClosing : Statment
{
    public StatmentType ID
    {
        get
        {
            return StatmentType.ScopeEnding;
        }
    }

    public Scope Generate(string Line, Scope Parent)
    {
        return null;
    }

    public string[] GetDetails(string Line)
    {
        return null;
    }

    public bool IsMatch(string Line)
    {
        return Line == "}";
    }
}

class PropertyAssignment : Statment
{

    private static readonly Regex MatchRegex = new Regex("\\s*.+?\\s*:.+?", RegexOptions.Compiled);
    private static readonly Regex DetailsRegex = new Regex("\\s*(?<propname>.+?):\\s*(?<propertyvalue>.+)", RegexOptions.Compiled);

    public StatmentType ID
    {
        get
        {
            return StatmentType.PropertyAssignment;
        }
    }

    public Scope Generate(string Line, Scope Parent)
    {
        return null;
    }

    public string[] GetDetails(string Line)
    {
        Match m = DetailsRegex.Match(Line);

        string propName = m.Groups["propname"].Value;
        string propVal = m.Groups["propertyvalue"].Value;

        if (string.IsNullOrWhiteSpace(propName))
            throw new ParserException("Cannot leave the property value blank.");

        return new string[] { propName, propVal };
    }

    public bool IsMatch(string Line)
    {
        return MatchRegex.IsMatch(Line);
    }
}

class CustomWidgetDeclaration : Statment
{

    private static readonly Regex MatchRegex = new Regex("\\s*\".+?\"\\s*{", RegexOptions.Compiled);
    private static readonly Regex DetailsRegex = new Regex("\\s*\"(?<custom_widget_name>.+?)\"", RegexOptions.Compiled);

    public StatmentType ID
    {
        get
        {
            return StatmentType.CustomWidgetScopeOpening;
        }
    }

    public string[] GetDetails(string Line)
    {
        Match m = DetailsRegex.Match(Line);

        string customWidgetName = m.Groups["custom_widget_name"].Value;

        if (string.IsNullOrWhiteSpace(customWidgetName))
            throw new ParserException("Must specify the custom widget name");

        return new string[] { customWidgetName };   
    }

    public Scope Generate(string Line, Scope Parent)
    {
        return new WidgetScope(RSSParser.CurrentParser, new StatmentType[] { StatmentType.WidgetScopeOpening, StatmentType.Variable, StatmentType.PropertyAssignment, StatmentType.CustomWidgetScopeOpening }, Parent);
    }

    public bool IsMatch(string Line)
    {
        return MatchRegex.IsMatch(Line) && Line.StartsWith("\"");
    }
}

class StyleScopeStatment : Statment
{
    private static readonly Regex MatchRegex = new Regex("\\s*style\\s*\".+?\"\\s*{", RegexOptions.Compiled);
    private static readonly Regex DetailsRegex = new Regex("\\s*style\\s*\"(?<style_name>.+?)\"\\s*{", RegexOptions.Compiled);

    public StatmentType ID
    {
        get
        {
            return StatmentType.StyleScopeOpening;
        }
    }

    public string[] GetDetails(string Line)
    {
        Match m = DetailsRegex.Match(Line);

        string styleName = m.Groups["style_name"].Value;

        if (string.IsNullOrWhiteSpace(styleName))
            throw new ParserException("The style name cannot be blank.");

        return new string[] { styleName };
    }

    public Scope Generate(string Line, Scope Parent)
    {
        string[] details = GetDetails(Line);

        return new StyleScope(RSSParser.CurrentParser, new StatmentType[] { StatmentType.StyleIdOpening, StatmentType.Variable }, Parent, details[0]);
    }

    public bool IsMatch(string Line)
    {
        return MatchRegex.IsMatch(Line) && Line.StartsWith("style");
    }

}

class StyleIdStatment : Statment
{
    private static readonly Regex MatchRegex = new Regex("\\s*\\.(.+)(\\s*,\\s*.[.+?])?\\s*{", RegexOptions.Compiled);
    private static readonly Regex DetailsRegex = new Regex("\\s*(?<ids>\\..+?){", RegexOptions.Compiled);

    public StatmentType ID
    {
        get
        {
            return StatmentType.StyleIdOpening;
        }
    }

    public string[] GetDetails(string Line)
    {
        Match m = DetailsRegex.Match(Line);

        string ids = m.Groups["ids"].Value;

        string[] splitIds = ids.Split(',');

        string[] newSplit = new string[splitIds.Length];

        for (int i = 0; i < splitIds.Length; i++)
        {
            string trimmed = splitIds[i].Trim();

            if (!trimmed.StartsWith("."))
                throw new ParserException("Invalid style id, they must begin with a .");

            string withoutDot = trimmed.Remove(0, 1);

            if (!RobloxParser.RobloxHierachy.ContainsKey(withoutDot))
                throw new ParserException($"Invalid key for style, no Instance with a ClassName of {withoutDot}");

            newSplit[i] = withoutDot;
        }

        return newSplit;
    }

    public Scope Generate(string Line, Scope Parent)
    {
        return new StyleIdScope(RSSParser.CurrentParser, new StatmentType[] { StatmentType.PropertyAssignment }, Parent, null);
    }

    public bool IsMatch(string Line)
    {
        return MatchRegex.IsMatch(Line);
    }
}


#endregion

namespace RobloxStyleLanguage.RSSParser
{

    enum StatmentType
    {
        Variable,
        WidgetScopeOpening,
        ScopeEnding,
        StyleScopeOpening,
        StyleIdOpening,
        PropertyAssignment,
        CustomWidgetScopeOpening,
        Unknown,
        Any
    }


    static class StatmentManager
    {

        private static List<Statment> Statments;

        private static void CreateStatments()
        {
            Statments = new List<Statment>();

            Statments.Add(new VariableDeclaration());
            Statments.Add(new WidgetDeclaration());
            Statments.Add(new WidgetScopeClosing());
            Statments.Add(new PropertyAssignment());
            Statments.Add(new StyleScopeStatment());
            Statments.Add(new StyleIdStatment());
            Statments.Add(new CustomWidgetDeclaration());
            
        }

        internal static Statment GetLineType(string Line)
        {
            if (Statments == null)
                CreateStatments();

            foreach (var S in Statments)
                if (S.IsMatch(Line))
                    return S;

            return null;
        }
    }
    
}
