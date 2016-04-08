using RGS.RGSParser;
using RGS.RobloxJSONParser.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RGS.Misc.RegExManager;

namespace RGS.RGSParser
{


    public class NullableList<E> 
    {
        private List<E> _List;

        public List<E> List
        {
            get
            {
                return _List;
            }
        }

        public void Add(E Item)
        {
            if (_List == null)
                _List = new List<E>();

            _List.Add(Item);
        }

        public E Get(string Item)
        {
            if (List == null)
                return default(E);

            foreach(var i in List)
            {
                PRobloxInstance Inst = i as PRobloxInstance;

                if (Inst != null)
                    if (Inst.Name == Item)
                        return i;
            }

            return default(E);
            
        }
        
    }




    public class RGSParser 
    {
        //StreamReader from the file.
        private StreamReader RS;

        //whether we are inside a string
       // private bool inQuotes;

        //StringBuilder to be able to concatenate stuff for string's
        private StringBuilder Line;

        //Line Number of the RGSS
        private int LineNo = 1;

        //Current Hierachy for the ROBLOX instances with a scope open.
        private Stack<PRobloxInstance> Instances;

        //List of all the widgets that are are marked with the 'widget' keyword.
        private NullableList<PRobloxInstance> CustomInstances; 

        //List of all the widgets that are fully parsed.
        private NullableList<PRobloxInstance> FinishedParsedInstances;

        //List of all the variables that have been cleared and checked for values.
        private Dictionary<string, string> Variables;

        //The variable parser(macro thing) that handles inserting variable values into property statments.
        private VariableParser VarParser;

        public List<PRobloxInstance> CustomWidgets
        {
            get
            {
                return CustomInstances.List;
            }
        }

        public List<PRobloxInstance> ParsedInstances
        {
            get
            {
                return FinishedParsedInstances.List;
            }
        }

        public RGSParser(StreamReader rS)
        {
            RS = rS;

            CustomInstances = new NullableList<PRobloxInstance>();
            FinishedParsedInstances = new NullableList<PRobloxInstance>();
            Instances = new Stack<PRobloxInstance>();
            Variables = new Dictionary<string, string>();
            VarParser = new VariableParser(Variables, LineNo);
        }

        internal void Close()
        {
            RS.Close();
        }

        /* Processing Functions and stuff */

        private void CheckForBlankVars(ref string[] details, string key1Name, string key2Name)
        {
            if (string.IsNullOrWhiteSpace(details[0]))
                throw new ParserException($"{key1Name} cannot be left blank.", LineNo);

            if (string.IsNullOrWhiteSpace(details[1]))
                throw new ParserException($"{key2Name} cannot be left blank.", LineNo);
        }

        private bool inWidgetScope()
        {
            return Instances.Count > 0;
        }

        private void ProcessPropertyAssignment(string Line)
        {
            if (!inWidgetScope())
                throw new ParserException("Not in any widget scope, missing a semicolon or something?", LineNo);

            string[] details = GetPropertiesDetails(Line);

            CheckForBlankVars(ref details, "Property Name", "Property Value");

            PRobloxInstance Instance = Instances.Peek();

            RobloxProperty Property;

            bool HasProperty = Instance.GetProperty(details[0], out Property);

            if (!HasProperty)
                throw new ParserException($"The Instance {Instance.Name} does not have a property {details[0]}", LineNo);

            if (Property.HasTag("readonly"))
                throw new ParserException($"The Property {details[0]} is readonly", LineNo);

            string[] PropertyValue = VarParser.FetchPropertyValue(details[0], details[1], Property.ValueType);
            
            Instance.AddProperty(details[0], PropertyValue, Property.ValueType);
        }

        private void ProcessScopeEnd(string Line)
        {
            if (!inWidgetScope())
                throw new ParserException("You're not in a widget scope, possibly missing a semicolon?", LineNo);

            PRobloxInstance Instance = Instances.Pop();

            if (Instance.IsCustom)
                CustomInstances.Add(Instance);
            else if (Instances.Count == 0)
                FinishedParsedInstances.Add(Instance);

        }

        private void ProcessVariableDeclaration(string Line)
        {
            string[] details = GetVariableDetails(Line);

            CheckForBlankVars(ref details, "Variable Name", "Variable Value");

            if (Variables.ContainsKey(details[0]))
                throw new ParserException($"You already have a variable called {details[0]}", LineNo);

            Variables.Add(details[0], details[1].Trim());
        }

        private void ProcessWidgetScope(string Line)
        {
            string[] details = GetWidgetScopeDetails(Line);

            bool isCustom = Line.StartsWith("widget");

            CheckForBlankVars(ref details, "ClassName", "WidgetName");
         
            PRobloxInstance Instance = new PRobloxInstance(details[0], details[1], isCustom);

            if (Instances.Count >= 1)
                Instances.Peek().AddChild(Instance); 
            
            Instances.Push(Instance);
        }

        private void ProcessCustomWidgetScope(string Line)
        {
            string[] details = GetCustomWidgetScopeDetails(Line);

            if (string.IsNullOrWhiteSpace(details[0]))
                throw new ParserException($"The custom widget scope name cannot be blank", LineNo);

            //Check that the custom widget actually exsists.
            PRobloxInstance Inst = CustomInstances.Get(details[0]);

            if (Inst == null)
                throw new ParserException($"Unkown custom widget {details[0]}", LineNo);

            PRobloxInstance CustomWidget = new PRobloxInstance(Inst.ClassName, details[0], false, true, Inst.Name);

            if (Instances.Count >= 1)
                Instances.Peek().AddChild(CustomWidget);

            Instances.Push(CustomWidget);
        }


        private void ProcessStatment(string statment)
        {
            LineType L = GetLineType(statment);

            switch(L)
            {
                case LineType.PROPERTY_ASSIGNMENT:
                    ProcessPropertyAssignment(statment);
                    break;
                case LineType.SCOPE_END:
                    ProcessScopeEnd(statment);
                    break;
                case LineType.VARIABLE_DECLARATION:
                    ProcessVariableDeclaration(statment);
                    break;
                case LineType.WIDGET_SCOPE_OPENING:
                    ProcessWidgetScope(statment);
                    break;
                case LineType.CUSTOM_WIDGET_SCOPE:
                    ProcessCustomWidgetScope(statment);
                    break;
                case LineType.BLANK:
                    throw new ParserException($"Unkown Line {Line}", LineNo);
            }

            Line.Clear();
        }

        private void WriteToJson(string fileDir)
        {
            RGS.RobloxJSONParser.Writer.RSSJSONTranslator.WriteRGSToJSONFile(this, fileDir);
        }

        public void Parse(string fileDir)
        {
            char lastLetter;

            bool inQuotes = false;

            Line = new StringBuilder();

            while (!RS.EndOfStream)
            {
                lastLetter = (char)RS.Read();

                if ((lastLetter.ToString() == "\n" || lastLetter.ToString() == "\t")) //you may cri because of "\n" but i tried Environment.NewLine and it no work.... ;(
                {
                    if (lastLetter.ToString() == "\n")
                        LineNo += 1;

                    if (!inQuotes)
                        continue;
                }

                Line.Append(lastLetter);

                VarParser.LineNo = LineNo;

                if (!inQuotes)
                    switch (lastLetter)
                    {
                        case '}':
                        case '{': //Scope Opening.
                        case ';': //End of statment
                            ProcessStatment(Line.ToString().Trim());
                            
                            continue;
                    }

            }

            //Attempt to process last line if not whitespace

            string lastLine = Line.ToString();
            if (!string.IsNullOrWhiteSpace(lastLine))
                ProcessStatment(lastLine + (!lastLine.EndsWith(";") ? ";" : ""));

            //Move on to JSON Writing

            WriteToJson(fileDir);


        }
    }
}
