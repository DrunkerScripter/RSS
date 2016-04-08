using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RGS.RobloxJSONParser.Writer;

namespace RGS.Http
{
    static class StudioHttpServer
    {

        private static bool _IsActive = false;

        public static bool IsActive
        {
            get
            {
                return _IsActive;
            }
        }

        private const string URL_LINK = "http://localhost:8080/";
        private static readonly int randomURLNumber = new Random().Next(1, 10000);

        private static WebServer Server;

        public static void WriteColor(ConsoleColor c, string Message)
        {
            ConsoleColor cc = Console.ForegroundColor;

            Console.ForegroundColor = c;

            Console.Write(Message);

            Console.ForegroundColor = cc;
        }

        private static void LaunchServer(string URLLink)
        {
            Server = new WebServer(new string[] { URLLink }, Callback);

            Server.Run();

            _IsActive = true;
        }

        private static string errorJSON(string val)
        {
            return "{\"error\":\"" + val + "\"}";
        }

        private static void WriteResponseBinary(HttpListenerResponse LR, string S, bool error = true)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(error ? errorJSON(S) : S);

            LR.ContentLength64 = bytes.LongLength;

            LR.OutputStream.Write(bytes, 0, bytes.Length);
        }

        private static bool CheckForID(HttpListenerResponse response, HttpListenerRequest request)
        {
            //Check the query, first for if it's an iD
            string idHeader = request.QueryString.Get("id");

            if (idHeader == null)
                return false;
            else
            {
                //Search for the .rgps.temp file

                FileStream FS = TempFileManager.OpenFile(idHeader);

                if (FS == null)
                {
                    WriteResponseBinary(response, "no file found with that id.");
                    return true;
                }

                //  Console.WriteLine("Transferring Content... of " + idHeader);

                Stream s = response.OutputStream;

                byte[] buffer = new byte[2048];
                int amountRead = 1;

                //Simple transfer loop.
                while (true)
                {
                    amountRead = FS.Read(buffer, 0, buffer.Length);

                    // Console.WriteLine($"Amount Read {amountRead}  -  Total Bytes Read {totalTransferred}");

                    if (amountRead <= 0)
                        break;

                    s.Write(buffer, 0, amountRead);
                }

                FS.Close();

                TempFileManager.Delete(idHeader);

                GC.Collect();

                return true;
            }

        }

        private static bool CheckForNewFileRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            string idHeader = request.QueryString["request"];

            if (idHeader == null)
                return false;
            else
            {
                string strList = RSSJSONTranslator.ToList(TempFileManager.Guids.ToArray(), true);

                TempFileManager.Guids.Clear();

                WriteResponseBinary(response, strList, false);

                return true;
            }

            
        }

        private static void Callback(HttpListenerContext arg)
        {

            HttpListenerRequest request = arg.Request;
            HttpListenerResponse response = arg.Response;


            if (!request.QueryString.HasKeys())
            {
                WriteResponseBinary(response, "no keys provided");
                return;
            }


            if (CheckForID(response, request)) //will return true if handled everything.
                return;

            if (CheckForNewFileRequest(request, response))
                return;

            WriteResponseBinary(response, "Unkown ID");
        }
        


        internal static void Start()
        {
            if (IsActive)
                return;

            string URLLink = $"{URL_LINK}{randomURLNumber}/";

            try
            {
                LaunchServer(URLLink);
            }
            catch (Exception e)
            {
                WriteColor(ConsoleColor.Red, $"Error Opening Server {e.Message}");
                Console.WriteLine();

                Environment.Exit(0);
            }


            WriteColor(ConsoleColor.DarkYellow, "Launched Server on URL - ");
            
            WriteColor(ConsoleColor.Cyan, URLLink);

            Console.WriteLine();
        }

    }
}
