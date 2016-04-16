using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSS.RSSParser;
using System.IO;
using RSS.RobloxJSONParser.Writer;
using RSS.Statments;

namespace RSS.RSSParser
{
    class RSSParser
    {
        internal static int LineNo { get; private set; }


        private RSSParser(System.IO.StreamReader Reader) {
            this.Reader = Reader;
        }

        //Stream to read from input file.
        private System.IO.StreamReader Reader;

        //Whether the Stream is reading from within a block of quotes?
        private bool inQuotes = false;

        //Mutable buffer for current line.
        private StringBuilder Line;

        //Entire scope of the program
        private StackScope Program;

        //List of the finished instance which are to be parsed into JSON.
        public List<RSSInstance> FinishedWidgets;

        //List that are custom widgets.
        public List<RSSInstance> CustomWidgets;

        //List of all the widget styles.
        public List<RSSStyle> Styles;

        private void ProcessLine()
        {
            string str = Line.ToString().Trim();

            Statment ID = StatmentManager.GetLineType(str);

            Program.AddStatment(ID, str);

            Line.Clear();
        }

      
        public bool Parse()
        {
            Line = new StringBuilder();
            Program = new StackScope(this, new StatmentType[] { StatmentType.Any }, null, "Global");

            char currentLetter;

            char lastLetter = '0';

            int LineNo = 1;

            while (!Reader.EndOfStream)
            {
                currentLetter = (char)Reader.Read();

                //Check to see if we've entered/exited a string.
                if (currentLetter == '"' && lastLetter != '\\')
                    inQuotes = !inQuotes;
                else if (currentLetter == '\n')
                {
                    LineNo += 1;
                    RSSParser.LineNo = LineNo;
                }

                //We never concatenate a string.
                if (currentLetter != ';')
                    Line.Append(currentLetter);
                

                if (!inQuotes) //Special characters will only work if not in a string.
                {
                    switch(currentLetter)
                    {
                        case '}':
                        case '{':
                        case ';':
                            //Process the statment.
                            ProcessLine();
                            break;
                    }
                }
                
                lastLetter = currentLetter; //Needed for dealing with strings.
            }
            

            return true;
        }

        private void AddToNeccessaryList(RSSInstance Instance)
        {
            if (Instance.IsCustomChild)
            {
                if (CustomWidgets == null)
                    CustomWidgets = new List<RSSInstance>();

                CustomWidgets.Add(Instance);
            }
            else
            {
                if (FinishedWidgets == null)
                    FinishedWidgets = new List<RSSInstance>();

                //Is defined within the top scpoe.
                FinishedWidgets.Add(Instance);
            }
        }

        internal void FinalizeScope(Scope scope)
        {
            if (scope.ID == "Widget")
            {
                //All stuff in there is a widget, so we can cast it so a WidgetScope.
                var WScope = scope as WidgetScope;

                if (WScope != null)
                {
                    if (WScope.Instance.IsCustomChild && WScope.Parent != Program)
                        throw new ParserException("Can only define a custom widget inside the global scope");

                    if (WScope.Parent == Program)
                        AddToNeccessaryList(WScope.Instance);                    
                    else
                        ((WidgetScope)(scope.Parent)).Instance.AddChild(WScope.Instance);
                    
                }
                else
                    throw new ParserException("Widget scope operation failed, I don't know why...");
            }
            else if (scope.ID == "Style")
            {
                var SScope = scope as StyleScope;

                if (SScope != null)
                {
                    if (Styles == null)
                        Styles = new List<RSSStyle>();

                    Styles.Add(new RSSStyle(SScope.StyleName, SScope.Ids));
                }
            }
            else if (scope.ID == "StyleId")
            {
                var SScope = scope as StyleIdScope;

                if (SScope != null)
                    ((StyleScope)SScope.Parent).AddStatment(SScope);
                
            }
        }

        internal bool TryGetWidget(string customWidgetName, out RSSInstance customInstance)
        {
            if (CustomWidgets == null)
            {
                customInstance = null;
                return false;
            }

            foreach(var Inst in CustomWidgets)
            {
                if (Inst.Name == customWidgetName)
                {
                    customInstance = Inst;
                    return true;
                }
            }

            customInstance = null;
            return false;

        }

        #region Static Methods

        internal static RSSParser CurrentParser;

        private static void WarnError(ParserException E)
        {
            Console.Clear();
            //COLORZ
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            Console.WriteLine(E.Message);

            Console.ForegroundColor = ConsoleColor.White;
        }
        private static string GetOptionValue(ref string[] options, string tagLookingFor)
        {
            //starts at 1 because the first arg is the path to the file.
            for (int i = 1; i < options.Length; i++)
            {
                string optTrim = options[i].Trim();

                if (optTrim.StartsWith("-") && optTrim.EndsWith(tagLookingFor))
                    if (i + 1 <= options.Length - 1)
                    {
                        string opt = options[i + 1];

                        if (string.IsNullOrWhiteSpace(opt) || opt.StartsWith("-"))
                            throw new ParserException($"Invalid Option given for tag {optTrim}");

                        return opt;
                    }
            }

            return null;
        }

        private static string GetFileDir(string[] Options)
        {
            string fileDir = GetOptionValue(ref Options, "o") ?? Path.GetFileName(Options[0]);

            if (!fileDir.EndsWith(".rgsp"))
            {
                if (Directory.Exists(fileDir))
                    fileDir = Path.Combine(fileDir, Path.GetFileNameWithoutExtension(Options[0]) + ".rgsp");
                else
                {
                    string lenOfExtention = Path.GetExtension(fileDir);
                    fileDir = fileDir.Substring(0, fileDir.Length - lenOfExtention.Length) + ".rgsp";
                }
            }

            return fileDir;
        }

        public static void Parse(string[] Options)
        {
            try
            {
                string fileDir = GetFileDir(Options);

                Console.WriteLine("Parsing file to " + fileDir);

                bool Success;

                using (StreamReader S = new StreamReader(File.Open(Options[0], FileMode.Open)))
                {
                    CurrentParser = new RSSParser(S);
                    Success = CurrentParser.Parse();
                }

                if (Success)
                {
                    CurrentParser.WriteToJSON(fileDir);
                    Console.WriteLine("Parsing Operation Finished Successfully.");
                }
            }
            catch (FileNotFoundException)
            {
                throw new ParserException($"Failed to find the file {Options[0]}");
            }
            catch (ArgumentException e)
            {
                throw new ParserException(e.Message);
            }

        }

        private void Close()
        {
            Line.Clear();
            Reader.Close();

        }

        private void WriteToJSON(string fileDir)
        {

            CurrentParser.Close();
            JSONWriter.Write(this, fileDir);
        }

        internal static void Finalise(Scope scope)
        {
            if (CurrentParser == null)
                return; //WTF

            CurrentParser.FinalizeScope(scope);
        }


        #endregion
    }
}
