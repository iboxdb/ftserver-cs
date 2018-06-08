using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using iBoxDB.LocalServer;
using AngleSharp.Parser.Html;
using AngleSharp.Extensions;

namespace FTServer
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var task = Task.Run<AutoBox>(() =>
            {
                #region Path
                bool isVM = false;
                String dir = "ftsdata91";
                String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir);
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (UnauthorizedAccessException)
                {
                    isVM = true;
                    path = Path.Combine(System.Environment.CurrentDirectory, "Data", dir);
                    Directory.CreateDirectory(path);
                }
                Console.WriteLine("DBPath=" + path);
                DB.Root(path);
                #endregion

                #region Config
                DB db = new DB(1);
                var cfg = db.GetConfig().DBConfig;
                cfg.CacheLength = cfg.MB(isVM ? 16 : 512);
                cfg.FileIncSize = (int)cfg.MB(16);

                new Engine().Config(cfg);
                cfg.EnsureTable<Page>("Page", "id");
                cfg.EnsureIndex<Page>("Page", true, "url(" + Page.MAX_URL_LENGTH + ")");
                #endregion

                return db.Open();

            });

            var host = CreateWebHostBuilder(args).Build();

            App.Auto = task.GetAwaiter().GetResult();
            using (App.Auto.GetDatabase())
            {
                host.Run();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }

    public class App
    {
        public static AutoBox Auto;
    }



}
