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
            var task = Task.Run<IndexServer>(() =>
            {
                #region Path 
                String dir = "DATA_FTS_CS_150";
                String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dir);
                //String path = Path.Combine("../", dir);
                Directory.CreateDirectory(path);
                Log("DBPath=" + path);
                DB.Root(path);

                #endregion

                var db = new IndexServer();
                App.Auto = db.GetInstance(1).Get();
                App.Item = db.GetInstance(2).Get();
                return db;
            });

            var host = CreateHostBuilder(args).Build();

            using (task.GetAwaiter().GetResult())
            {
                host.Run();
                IndexPage.Shutdown();
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

        //for PageIndex
        public static AutoBox Auto;
        public static IBox Cube()
        {
            return Auto.Cube();
        }
        public static void Log(String msg)
        {
            Console.WriteLine(msg);
        }
    }



}
