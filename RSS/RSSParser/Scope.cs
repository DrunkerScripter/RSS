using RSS.RSSParser;
using RSS.Statments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSS.RSSParser
{


    //A block of statments between two {}
    //Every scope may have variables defined within it.
    class Scope
    {
        protected RSSParser Parser;

        //Types of values accepted by this scope.
        public StatmentType[] AcceptedValues { get; set; }

        //String ID of the type of scope it is.
        public string ID { get; set; }

        //List of the all the finished parsed scopes beneath it.
        protected List<Scope> Scopes { get; set; }

        //Dictionary of all the variables.
        public Dictionary<string, string> Variables;

        public Scope(RSSParser Parser, StatmentType[] Accepted, Scope Parent, string Scope = "Generic")
        {
            this.Parser = Parser;
            AcceptedValues = Accepted;
            Variables = new Dictionary<string, string>();
            ID = Scope;
            this.Parent = Parent;
        }

        public Scope Parent { get; private set; }

        internal string FetchStringVariableValue(string varVal)
        {
            //Reverse stack search for variable value (aka start at local scope and then work to global)
            
            string fetchedVarVal;

            bool isVar = Variables.TryGetValue(varVal, out fetchedVarVal);

            if (isVar)
                return fetchedVarVal;
            else if(Parent != null)
            {
                return Parent.FetchStringVariableValue(varVal);
            }
            

            return varVal;
        }

        internal bool AcceptsStatmentType(StatmentType St)
        {
            foreach (StatmentType StatTyp in AcceptedValues)
                if (St == StatTyp)
                    return true;

            return false;
        }

    }

    //Mutliple scopes are going to be defined within in each other and so a record will need to be kept of the structure.
    //Multiple scopes can be defined within this scope.
    class StackScope : Scope
    {
        public new Stack<Scope> Scopes;

        public StackScope(RSSParser Parser, StatmentType[] ID, Scope Parent, string ScopeType) : base(Parser, ID, Parent, ScopeType)
        {
            Scopes = new Stack<Scope>();
        }
        
        private void ProcessVariable(string[] details)
        {
            Variables.Add(details[0], FetchStringVariableValue(details[1]) ?? details[1]);
        }

        private void CloseScope()
        {
            if (Scopes.Count == 0)
                throw new ParserException("Attempted to end the global scope.");

            Scope scope = Scopes.Pop();

            RSSParser.Finalise(scope);
        }

        private void ProcessPropertyAssignment(string[] details, Scope sc)
        {
            switch (sc.ID)
            {
                case "Widget":
                    var WS = sc as WidgetScope;

                    if (WS != null)
                    {
                        PropertyParser.Parse(sc, details[1], details[0]);
                    }

                    break;
                case "StyleId":
                    var SS = sc as StyleIdScope;

                    if (SS != null)
                    {
                        PropertyParser.Parse(sc, details[1], details[0], SS.ClassNames);
                    }
                    break;
            }
            
        }

        private void ProcessStyleScope(string Line, string[] details, Statment ID)
        {
            StyleScope sc = (StyleScope)ID.Generate(Line, (Scopes.Count == 0 ? this : Scopes.Peek()));

            Scopes.Push(sc);
        }

        internal void AddStatment(Statment Stat, string line)
        {
            if (Stat == null)
            {
               // Console.WriteLine($"Unkown statment '{line}'");
                return;
            }

            //Check for the mismatch of a type.
            if (Scopes.Count > 0)
                if (Stat.ID != StatmentType.ScopeEnding && Stat.ID != StatmentType.Any)
                    if (!Scopes.Peek().AcceptsStatmentType(Stat.ID))
                        throw new ParserException("Attempted to open an unaccepted scope");



            string[] details = Stat.GetDetails(line);

            switch (Stat.ID)
            {
                case StatmentType.Variable:
                    ProcessVariable(Stat.GetDetails(line));
                    break;
                case StatmentType.ScopeEnding:
                    CloseScope();
                    break;
                case StatmentType.PropertyAssignment:
                    ProcessPropertyAssignment(details, Scopes.Peek());
                    break;
                case StatmentType.CustomWidgetScopeOpening:
                    ProcessCustomWidgetScope(line, Stat, details);
                    //Check the custom class exsists.
                    break;
                case StatmentType.StyleScopeOpening:
                    ProcessStyleScope(line, details, Stat);
                    break;
                case StatmentType.StyleIdOpening:
                    ProcessStyleId(details, Stat);
                    break;
                default:
                    bool isCustom = line.StartsWith("widget");
                    RSSInstance Instance = new RSSInstance(details[0], details[1], isCustom);

                    WidgetScope sc = (WidgetScope)Stat.Generate(line, (Scopes.Count == 0 ? this : Scopes.Peek()));

                    sc.Instance = Instance;

                    Scopes.Push(sc);
                    break;
            }
            

        }

        private void ProcessStyleId(string[] details, Statment stat)
        {
            if (Scopes.Count == 0)
                throw new ParserException("Can't open a style id this scope.");

            StyleIdScope StScope = (StyleIdScope)stat.Generate(null, (Scopes.Count == 0 ? this : Scopes.Peek()));

            StScope.ClassNames = details;

            Scopes.Push(StScope);
        }

        private void ProcessCustomWidgetScope(string Line, Statment Stat, string[] details)
        {
            RSSInstance CustomInstance;

            if (!Parser.TryGetWidget(details[0], out CustomInstance))
                throw new ParserException($"No custom widget named {details[0]}");

            RSSInstance Instance = new RSSInstance(CustomInstance.ClassName, details[0], CustomInstance.Name);

            WidgetScope sc = (WidgetScope)Stat.Generate(Line, (Scopes.Count == 0 ? this : Scopes.Peek()));

            sc.Instance = Instance;

            Scopes.Push(sc);
        }
    }


    class WidgetScope : StackScope
    {
        public WidgetScope(RSSParser Parser, StatmentType[] ID, Scope Parent) : base(Parser, ID, Parent, "Widget")
        {
             
        }

        //Instance this scope is bound to
        public RSSInstance Instance;
        
    }

    class StyleScope : ListScope<RSSStyleItem>
    {
        public string StyleName { get; set; }

        public StyleScope(RSSParser Parser, StatmentType[] ID, Scope Parent) : base(Parser, ID, Parent, "Style") { }

        public StyleScope(RSSParser Parser, StatmentType[] ID, Scope Parent, string Name) : base(Parser, ID, Parent, "Style")
        {
            StyleName = Name;
        }

        internal void AddStatment(StyleIdScope IdScope)
        {
            if (this.Ids == null)
                this.Ids = new List<RSSStyleItem>();

            Ids.Add(new RSSStyleItem(IdScope.ClassNames, IdScope.Ids));
        }
    }

    class StyleIdScope : ListScope<RSSProperty>
    {
        public string[] ClassNames;

        public StyleIdScope(RSSParser Parser, StatmentType[] ID, Scope Parent, string[] ClassNames) : base(Parser, ID, Parent, "StyleId") { }

        internal void AddProperty(RSSProperty rSSProperty)
        {
            if (Ids == null)
                Ids = new List<RSSProperty>();

            Ids.Add(rSSProperty);

        }
    }

    //Only 1 level of scopes are allowed to be defined within this scope.
    //The type of the scope defined within this scope is enforced with the type parameter.
    class ListScope<E> : Scope
    {
        public List<E> Ids;

        public ListScope(RSSParser Parser, StatmentType[] ID, Scope Parent, string ScopeType = "Generic") : base(Parser, ID, Parent, ScopeType) { }

       
    }







}
