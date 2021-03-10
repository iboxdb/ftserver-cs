using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using IBoxDB.LocalServer;
using static FTServer.App;
using System.Collections.Concurrent;

namespace FTServer
{

    public class Program
    {

        public static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
            var task = Task.Run<IDisposable>(() =>
            {
                #region Path 
                String dir = "DATA_FTS_CS_150";
                String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir);

                //path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dir);
                //path = Path.Combine("/mnt/hgfs/DB", dir);
                //path = Path.Combine("../", dir);
                Directory.CreateDirectory(path);
                Log("DBPath=" + path);
                DB.Root(path);

                #endregion

                App.Item = new IndexServer().GetInstance(IndexServer.ItemDB).Get();

                long start = IndexServer.IndexDBStart;
                foreach (var f in Directory.GetFiles(path))
                {
                    var fn = Path.GetFileNameWithoutExtension(f).Replace("db", "");
                    long.TryParse(fn, out long r);
                    if (r > start) { start = r; }
                }

                App.Indices = new List<AutoBox>();
                for (long l = IndexServer.IndexDBStart; l < start; l++)
                {
                    App.Indices.Add(new ReadonlyIndexServer().GetInstance(l).Get());
                }
                App.Indices.Add(new IndexServer().GetInstance(start).Get());
                Log("Current Index DB (" + start + ")");
                App.Index = App.Indices[App.Indices.Count - 1];

                return Task.FromResult<IDisposable>(null);
            });

            var host = CreateHostBuilder(args).Build();

            task.GetAwaiter().GetResult();
            host.Run();
            IndexPage.Shutdown();
            IndexPage.addSearchTerm("SystemShutdown", true);
            App.Item.GetDatabase().Dispose();
            foreach (var d in App.Indices)
            {
                d.GetDatabase().Dispose();
            }
            Log("DB Closed");
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
        //for Application
        public static AutoBox Item;

        //for New Index
        public static AutoBox Index;

        //for Readonly PageIndex
        public static List<AutoBox> Indices;

        public static void Log(String msg)
        {
            Console.WriteLine(msg);
        }
    }


}
