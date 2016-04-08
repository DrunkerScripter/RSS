using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RGS.RGSParser
{
    static class RGSParserManager
    {

        private static bool ParseRSSFile(StreamReader RS, string fileDir)
        {
            

            RGSParser P = new RGSParser(RS);

            try
            {
                P.Parse(fileDir);
            }
            catch (ParserException e)
            {
                Console.Clear();
                //COLORZ
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                
                Console.WriteLine(e.Message);

                Console.ForegroundColor = ConsoleColor.White;

                return false;
            }
            finally
            {
                P.Close();
            }

            return true;
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
            try {
                string fileDir = GetFileDir(Options);

                Console.WriteLine("Parsing file to " + fileDir);

                bool Success;

                using (StreamReader S = new StreamReader(File.Open(Options[0], FileMode.Open)))
                    Success = ParseRSSFile(S, fileDir);

                if (Success)
                    Console.WriteLine("Parsing Operation Finished Successfully.");
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






    }
}
