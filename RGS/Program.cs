using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RGS.Misc;
using RGS.RobloxJSONParser.Reader;
using RGS.RGSParser;
using RGS.Http;
using System.Runtime.InteropServices;
using RGS.RobloxJSONParser.Writer;

namespace RGS
{
    class Program
    {

        const string VERSION = "0.0.1b";
        
        private static int ParseAPI()
        {
            Console.WriteLine("Parsing ROBLOX API...");
            try
            {
                string downloadLocation = APIFetcher.DownloadROBLOXApi();

                if (!string.IsNullOrWhiteSpace(downloadLocation))
                    RobloxParser.Parse(downloadLocation);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to download ROBLOX Api {e.Message}");
                return -1;
            }

            Console.WriteLine("Parsed ROBLOX API");
            Console.WriteLine();

            return 0;
        }

        static void Main(string[] args)
        {

            //When Program Is Ran
            //Parse ROBLOX JSON

            int Result = ParseAPI();

            if (Result == -1)
                return;

            //TODO : Open HTTP Server.
            TempFileManager.Init();
            StudioHttpServer.Start();

            CustomProperties.Init();
            //Begin Accepting Commands.

            string Command = "";
            do
            {
                Console.WriteLine($"RSS Version {VERSION}");
                Command = Console.ReadLine();

                if (Command == "clear" || Command == "cls")
                {
                    Console.Clear();
                    continue;
                }
                else if (Command == "quit")
                    break;

                string[] splitCommand = Command.Split(' ');

                Console.WriteLine();

                try {
                    RGSParserManager.Parse(splitCommand);
                }
                catch(ParserException e)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(e.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine();
            } while (Command != "quit");

            TempFileManager.Delete();

        }
      


    }
}
