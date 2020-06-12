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
using System.Net;


namespace FTServer
{
    /*
    Turn off virtual memory for 8G+ RAM Machine
    use DatabaseConfig.CacheLength and PageText.max_text_length to Control Memory

    Linux:
     # free -h
     # sudo swapoff -a
     # free -h 

    Windows:
    System Properties(Win+Pause) - Advanced system settings - Advanced
    - Performance Settings - Advanced - Virtual Memory Change -
    uncheck Automatically manage paging file - select No paging file - 
    click Set - OK restart
    */
    public class Program
    {

        public static void Main(string[] args)
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
            var task = Task.Run<AutoBox>(() =>
            {
                #region Path 
                String dir = "ftsdata130c";
                String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir);
                Directory.CreateDirectory(path);
                //catch (UnauthorizedAccessException)

                Console.WriteLine("DBPath=" + path);
                DB.Root(path);
                //for get more os memory
                /*
                Console.WriteLine("Loading Memory...");
                foreach (var p in Directory.GetFiles(path))
                {
                    Console.WriteLine("Loading " + p);
                    var bs = new byte[1024 * 1024 * 32];
                    using (var fs = new FileStream(p, FileMode.Open))
                    {
                        while (fs.Read(bs) > 0) { }
                    }
                }
                Console.WriteLine("Loaded Memory");
                */
                #endregion

                #region Config
                //System.Diagnostics.Process.GetCurrentProcess()
                DB db = new DB(1);
                var cfg = db.GetConfig();
                cfg.CacheLength = cfg.MB(2048);
                //if update metadata, use low cache
                //cfg.CacheLength = cfg.MB(128);

                cfg.FileIncSize = (int)cfg.MB(4);
                cfg.SwapFileBuffer = (int)cfg.MB(4);

                Console.WriteLine("DB Cache = " + (cfg.CacheLength / 1024 / 1024) + " MB");
                new Engine().Config(cfg);


                cfg.EnsureTable<Page>("Page", "url(" + Page.MAX_URL_LENGTH + ")");
                cfg.EnsureIndex<Page>("Page", true, "textOrder");

                cfg.EnsureTable<PageText>("PageText", "id");
                cfg.EnsureIndex<PageText>("PageText", false, "textOrder");
                cfg.EnsureTable<PageSearchTerm>("/PageSearchTerm", "time", "keywords(" + PageSearchTerm.MAX_TERM_LENGTH + ")", "uid");



                #endregion

                return db.Open();

            });

            var host = CreateHostBuilder(args).Build();

            App.Auto = task.GetAwaiter().GetResult();
            using (App.Auto.GetDatabase())
            {
                host.Run();
                IndexPage.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.ConfigureLogging(logging =>
               {
                   logging.AddFilter((name, lev) =>
                   {
                       return false;
                   });
               });
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
    }



}
