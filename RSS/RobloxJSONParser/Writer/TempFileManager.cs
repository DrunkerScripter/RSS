using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSS.RobloxJSONParser.Writer
{
    static class TempFileManager
    {
            
        private static readonly string _TempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_rgss_files");
        internal static string TempDirectory
        {
            get
            {
                if (!Directory.Exists(_TempDir))
                    CreateTempDir();
               
                return _TempDir;
            }
        }

        private static FileSystemWatcher FSW;

        private static void CreateTempDir()
        {
            Directory.CreateDirectory(_TempDir);

            FSW = new FileSystemWatcher(_TempDir);

            FSW.Deleted += (sender, e) =>
            {
                if (Guids.Contains(e.Name))
                    Guids.Remove(e.Name);
            };

            FSW.EnableRaisingEvents = true;
        }

        internal static FileStream OpenFile(string GUID)
        {
            string Combined = Path.Combine(TempDirectory, GUID);

            if (!File.Exists(Combined))
                return null;

            try
            {
                return File.Open(Combined, FileMode.Open);
            }
            catch
            {
                return null;
            }
        }

        internal static List<string> Guids;

        internal static void Log(string guidName)
        {
            if (Guids == null)
                Guids = new List<string>();

            Guids.Add(guidName);
        }

        internal static void Init()
        {
            if (Directory.Exists(_TempDir))
                Directory.Delete(_TempDir, true);

            CreateTempDir();
        }

        //Delets all files.
        internal static void Delete()
        {
            try
            {
                Directory.Delete(_TempDir, true);
            }
            catch { }
        }

        //Deletes certain temp file.
        internal static void Delete(string fileName)
        {
            string combinedPath = Path.Combine(_TempDir, fileName);
            if (File.Exists(combinedPath))
                File.Delete(combinedPath);
        }
        
            
        
    }
}
