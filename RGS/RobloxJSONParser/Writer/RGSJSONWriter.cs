using System;
using System.Linq;
using System.Text;
using System.IO;
using RGS.RGSParser;
using System.Collections.Generic;

namespace RGS.RobloxJSONParser.Writer
{
    class CleverStreamWriter : StreamWriter //Couldn't think of a better name.
    {
        private char lastLetter { get; set; }

        public override void Write(char c)
        {
            lastLetter = c;

            base.Write(c);
        }

        public override void Write(string value)
        {
            lastLetter = value.Last();

            base.Write(value);
        }

        public void WriteCommaIfNeeded()
        {
            if (lastLetter != ',')
                Write(',');
        }

        public CleverStreamWriter(string DIR)
            : base(File.Open(DIR, FileMode.OpenOrCreate))
        { }
    }

    class RSSJSONTranslator //Hacker together weird json writer which is rlly ugly.
    { 

        private RSSJSONTranslator(ref RGSParser.RGSParser SP, string DIR)
        {
            Parser = SP;

            SP.Close();
            
            if (File.Exists(DIR))
                File.Delete(DIR);

            this.DIR = DIR;

            FileWriter = new CleverStreamWriter(DIR);
        }

        private CleverStreamWriter FileWriter;
        private RGSParser.RGSParser Parser;
        private string DIR;

        internal static void WriteRGSToJSONFile(RGSParser.RGSParser SP, string outputName)
        {
            RSSJSONTranslator Trans = new RSSJSONTranslator(ref SP, outputName);

            Trans.Write();

            Trans.Close();
        }

        
        private void Close()
        {
            //Before everything closes make a record of the file.
            FileWriter.Close();

            //Copy it to the temp dir.

            Guid id = Guid.NewGuid();

            File.Copy(DIR, Path.Combine(TempFileManager.TempDirectory, id.ToString() + ".rgsp"));

            //Log that this guid needs to be updated in studio.

            TempFileManager.Log(id.ToString());
        }

        //Just-In-Case
        ~RSSJSONTranslator()
        {
            FileWriter.Close();
        }

        private void WriteBetween(char letter, Action TextBetween)
        {
            FileWriter.Write(letter);

            TextBetween.Invoke();

            FileWriter.Write((letter == '{' ? '}' : ']'));
        }

        private void WriteProperty(string PropertyName, string PropertyValue)
        {
            FileWriter.Write($"\"{PropertyName}\":{PropertyValue}");
        }

        internal static string Quotify(string s)
        {
            return $"\"{s}\"";
        }

        private void WriteInstance(PRobloxInstance Instance)
        {
            WriteBetween('{', new Action(() =>
            {

                //Write ClassName and Name

                WriteProperty("Name", Quotify(Instance.Name));

                FileWriter.WriteCommaIfNeeded();

                WriteProperty("ClassName", Quotify(Instance.ClassName));

                if (Instance.Properties != null)
                {
                    FileWriter.WriteCommaIfNeeded();
                    int count = 0;
                    foreach (var Property in Instance.Properties)
                    {
                        count += 1;
                        
                        WriteProperty(Property.Name, MakeSingleString(Property.values, Property.IsEnumType()));

                        if (count != Instance.Properties.Count)
                            FileWriter.WriteCommaIfNeeded();
                    }
                }

                if (Instance.isCloneOfCustomChild)
                {
                    //Make note of it.
                    FileWriter.WriteCommaIfNeeded();

                    WriteProperty("CustomWidgetName", Quotify(Instance.customChildName));
                }

                if (Instance.Children != null && Instance.Children.Count > 0)
                {
                    //Time for the children!, don't arrest me please, i know that sounded weird
                    FileWriter.WriteCommaIfNeeded();
                    WriteLargeList("Children", Instance.Children);

                }

            }));

        }

        private string MakeSingleString(string[] values, bool isEnum)
        {
            if (values.Length == 1)
                return (isEnum ? Quotify(values[0]) : values[0]);
            else
                return ToList(values);
        }

        internal static string ToList(string[] values, bool surroundInQuotes = false)
        {
            StringBuilder Builder = new StringBuilder();

            Builder.Append("[");

            for (int i = 0; i < values.Length; i++)
            {
                Builder.Append((surroundInQuotes ? $"\"{values[i]}\"" : values[i]));
                if (i != values.Length - 1)
                    Builder.Append(",");
            }
            
            Builder.Append("]");

            return Builder.ToString();
        }


        private void WriteLargeList(string Key, List<PRobloxInstance> Instances)
        {
            FileWriter.Write($"\"{Key}\":" + "[");
            //a.Invoke();

            int i = 0;

            foreach (var Instance in Instances)
            {
                WriteInstance(Instance);
                i += 1;
                if (i != Instances.Count)
                    FileWriter.WriteCommaIfNeeded();
            }

            FileWriter.Write(']');
        }
        
        

        private void WriteCustomWidgets()
        {
            if (Parser.CustomWidgets != null)
            {
                WriteLargeList("CustomWidgets", Parser.CustomWidgets);

                FileWriter.WriteCommaIfNeeded();
            }

            //Write finished widgets.
            if (Parser.ParsedInstances  != null)
            {
                WriteLargeList("ParsedInstances", Parser.ParsedInstances);
            }
        }

        private void Write()
        {
            WriteBetween('{', new Action(WriteCustomWidgets));
            
        }


    }
}