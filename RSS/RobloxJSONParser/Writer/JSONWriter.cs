using RobloxStyleLanguage.RSSParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxStyleLanguage.RobloxJSONParser.Writer
{

    class RetentiveStream : StreamWriter
    {
        internal char lastLetter { get; set; }

        public new void Write(char c)
        {
            lastLetter = c;

            base.Write(c);
        }

        public new void Write(string s)
        {
            lastLetter = s.Last();

            base.Write(s);
        }

        public void WriteComma()
        {
            if (lastLetter != ',' && (lastLetter == ']' || lastLetter == '}' || lastLetter == '"'))
                Write(',');
        }


        public RetentiveStream(FileStream FS) : base(FS) { }
        public RetentiveStream(string fileDir) : base(fileDir) { }
    }


    class JSONWriter : IDisposable
    {
        internal static string Quotify(string s)
        {
            bool onEnd = s.EndsWith("\"");
            bool atStart = s.StartsWith("\"");

            if (onEnd && atStart)
                return s;
            else
                return $"\"{s}\"";
        }

        private RSSParser.RSSParser Parser;
        private RetentiveStream Stream;
        private string fileDir;

        internal static string SerialiseToJSONList(string[] values, bool quotify = false)
        {
            StringBuilder list = new StringBuilder();

            list.Append("[");

            for (int i = 0; i < values.Length; i++)
            {
                list.Append((quotify ? Quotify(values[i]) : values[i]));
                
                if (i != values.Length - 1)
                    list.Append(',');
            }

            list.Append("]");

            return list.ToString();
        }

        private string Serialise(string[] values)
        {
            if (values.Length == 1)
                return values[0];
            else
            {
                return SerialiseToJSONList(values);
            }
        }

        private void WriteArrayWithProperties<E>(E[] Arr, Action<E> Callback)
        {
            int stopComma = Arr.Length - 1;

            for (int i = 0; i < Arr.Length; i++)
            {
                Callback.Invoke(Arr[i]);
                
                if (i != stopComma)
                    Stream.WriteComma();
                
            }
        }

        public void WriteProperty(RSSProperty Prop)
        {
            Stream.Write($"{Quotify(Prop.Name)}:{Serialise(Prop.Values)}");
        }

        public void WriteInstance(RSSInstance Inst)
        {
            WriteBetween('{', '}', () => {
                Inst.AddProperty(new RSSProperty("ClassName", "string", new string[] { Quotify(Inst.ClassName) }));
                Inst.AddProperty(new RSSProperty("Name", "string", new string[] { Quotify(Inst.Name) }));

                if (!string.IsNullOrEmpty(Inst.CustomChildName))
                    Inst.AddProperty(new RSSProperty("CustomWidgetName", "string", new string[] { Quotify(Inst.CustomChildName) }));

                WriteArrayWithProperties(Inst.Properties.ToArray(), (Prop) => {
                    WriteProperty(Prop);
                });

                if (Inst.Children != null && Inst.Children.Count > 0)
                {
                    Stream.WriteComma();
                    WriteInstanceList("Children", Inst.Children);
                }

            });
        }

        public void WriteInstanceList(string Name, List<RSSInstance> ListOfInstances)
        {
            Stream.Write($"{Quotify(Name)}:");
            if (ListOfInstances != null)
                WriteBetween('[', ']', () => {

                    var Instances = ListOfInstances.ToArray();

                    for (int i = 0; i < Instances.Length; i++)
                    {
                        WriteInstance(Instances[i]);

                        if (i != Instances.Length - 1)
                            Stream.WriteComma();
                    }

                });
        }
        
        private void WriteStyleItem(RSSStyleItem Item)
        {
            WriteBetween('{', '}', () =>
            {
                Stream.Write("\"Classes\":");
                Stream.Write(SerialiseToJSONList(Item.Ids, true));

                if (Item.Properties != null && Item.Properties.Count > 0)
                {
                    Stream.WriteComma();
                    Stream.Write("\"Properties\":");
                    WriteBetween('{', '}', () =>
                    {
                        WriteArrayWithProperties(Item.Properties.ToArray(), WriteProperty);
                    });
                }
            });
        }

        private void WriteStyleIdList(List<RSSStyleItem> Ids)
        {
            if (Ids != null && Ids.Count > 0)
            {
                Stream.WriteComma();
                Stream.Write("\"StyleIds\":");
                WriteBetween('[', ']', () =>
                    {

                        RSSStyleItem[] IdsArr = Ids.ToArray();
                        
                        for (int i = 0; i < IdsArr.Length; i++)
                        {
                            WriteStyleItem(IdsArr[i]);

                            if (i != IdsArr.Length - 1)
                                Stream.WriteComma();
                        }
                    });
            }
        }

        private void WriteStyle(RSSStyle Style)
        {
            WriteBetween('{', '}', () =>
            {
                Stream.Write($"\"Name\":{Quotify(Style.styleName)}");

                WriteStyleIdList(Style.styleIds);
            });
        }

        private void WriteStyleList(string Name, List<RSSStyle> Styles)
        {
            Stream.Write($"{Quotify(Name)}:");
            WriteBetween('[', ']', () => {

                var St = Styles.ToArray();

                for (int i = 0; i < St.Length; i++)
                {
                    WriteStyle(St[i]);

                    if (i != St.Length - 1)
                        Stream.WriteComma();
                }

            });
        }


        private void WriteBetween(char first, char end, Action Centre)
        {
            Stream.Write(first);

            Centre();

            Stream.Write(end);
        }

        public void SerialiseParser()
        {
            WriteBetween('{', '}', () =>
            {
                if (Parser.CustomWidgets != null && Parser.CustomWidgets.Count > 0)
                    WriteInstanceList("CustomWidgets", Parser.CustomWidgets);


                if (Parser.FinishedWidgets != null && Parser.FinishedWidgets.Count > 0)
                {
                    Stream.WriteComma();
                    WriteInstanceList("ParsedInstances", Parser.FinishedWidgets);
                }       

                if (Parser.Styles != null && Parser.Styles.Count > 0)
                {
                    Stream.WriteComma();
                    WriteStyleList("Styles", Parser.Styles);
                }

            });
        }
        
        private JSONWriter(RSSParser.RSSParser Parser, string fileDir) {
            this.Parser = Parser;
            Stream = new RetentiveStream(fileDir);
            this.fileDir = fileDir;
        }

        private void Close()
        {
            Stream.Close();

            //Copy it to the temp dir.

            Guid id = Guid.NewGuid();

            File.Copy(fileDir, Path.Combine(TempFileManager.TempDirectory, id.ToString() + ".rgsp"));

            //Log that this guid needs to be updated in studio.

            TempFileManager.Log(id.ToString());
        }

        internal static void Write(RSSParser.RSSParser Parse, string fileDir)
        {
            JSONWriter W = new JSONWriter(Parse, fileDir);

            W.SerialiseParser();

            W.Close();
        }

        public void Dispose()
        {
            ((IDisposable)Stream).Dispose();
        }
    }
}
