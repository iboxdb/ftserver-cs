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
using Microsoft.Extensions.Hosting;

namespace FTServer
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var task = Task.Run<AutoBox>(() =>
            {
                #region Path
                App.IsVM = false;
                String dir = "ftsdata92";
                String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir);
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (UnauthorizedAccessException)
                {
                    App.IsVM = true;
                    path = Path.Combine(System.Environment.CurrentDirectory, "Data", dir);
                    Directory.CreateDirectory(path);
                }
                Console.WriteLine("DBPath=" + path);
                DB.Root(path);
                #endregion

                #region Config
                //System.Diagnostics.Process.GetCurrentProcess()
                DB db = new DB(1);
                var cfg = db.GetConfig();
                cfg.CacheLength = cfg.MB(App.IsVM ? 16 : 512);
                cfg.FileIncSize = (int)cfg.MB(4);

                new Engine().Config(cfg);
                cfg.EnsureTable<Page>("Page", "id");
                cfg.EnsureIndex<Page>("Page", true, "url(" + Page.MAX_URL_LENGTH + ")");
                cfg.EnsureTable<PageLock>("PageLock", "url(" + Page.MAX_URL_LENGTH + ")");
                #endregion

                return db.Open();

            });

            var host = CreateHostBuilder(args).Build();

            App.Auto = task.GetAwaiter().GetResult();
            using (App.Auto.GetDatabase())
            {
                host.Run();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseStartup<Startup>();
               });
    }

    public class App
    {
        public static AutoBox Auto;
        public static IBox Cube()
        {
            return Auto.Cube();
        }
        public static bool IsDevelopment;
        public static bool IsVM;
    }



}
