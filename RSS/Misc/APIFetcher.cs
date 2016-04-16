using System.IO;
using System.Net;

namespace RobloxStyleLanguage.Misc
{
    static class APIFetcher
    {

        private const string API_DUMP_LINK = "http://anaminus.github.io/rbx/json/api/latest.json";

        internal static string DownloadROBLOXApi()
        {
            string download_path = Path.Combine(Directory.GetCurrentDirectory(), "APIDUMP.json");

            using (WebClient Client = new WebClient())
                Client.DownloadFile(API_DUMP_LINK, download_path);

            return download_path;
        }

    }
}
