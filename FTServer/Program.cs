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
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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
                String dir = "DATA_FTS_CS_161";
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

                for (long l = IndexServer.IndexDBStart; l < start; l++)
                {
                    App.Indices.add(l, true);
                }
                App.Indices.add(start, false);
                Log("Current Index DB (" + start + ")");

                Log("ReadOnly CacheLength = " + (Config.Readonly_CacheLength / 1024L / 1024L) + " MB (" + Config.Readonly_CacheLength + ")");
                Log("ReadOnly Max DB Count = " + Config.Readonly_MaxDBCount);

                Log("MinCache = " + (Config.minCache() / 1024L / 1024L) + " MB");

                App.Index = App.Indices.get(App.Indices.length() - 1);

                return Task.FromResult<IDisposable>(null);
            });

            var host = CreateHostBuilder(args).Build();

            task.GetAwaiter().GetResult();

            Task.Delay(2000).ContinueWith((t) =>
            {
                try
                {
                    foreach (var nw in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    {
                        if (nw.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Console.WriteLine("http://" + nw.ToString() + ":" + App.HttpPort);
                        }
                    }
                }
                catch
                {

                }
                try
                {

                    var resultsPath = "http://127.0.0.1:" + App.HttpPort;
                    Console.WriteLine("use Browser to Open " + resultsPath);
                    var psi = new ProcessStartInfo(resultsPath);
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                }
                catch
                {

                }
                return Task.FromResult<object>(null);
            });

            host.Run();

            IndexPage.Shutdown();
            IndexPage.addSearchTerm(IndexPage.SystemShutdown);
            App.Item.GetDatabase().Dispose();
            App.Indices.Dispose();
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
                                if (lev == LogLevel.Information)
                                {
                                    switch (name)
                                    {
                                        case "Microsoft.AspNetCore.Routing.EndpointMiddleware":
                                        case "Microsoft.AspNetCore.Mvc.ViewFeatures.ViewResultExecutor":
                                        case "Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker":
                                        case "Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware":
                                        case "Microsoft.AspNetCore.Hosting.Diagnostics":
                                            return false;
                                        default:
                                            return true;
                                    }
                                }
                                return false;
                            });
                        });
                   webBuilder.UseStartup<Startup>()
                     .UseUrls("http://*:" + App.HttpPort);
               });
    }

    public class App
    {
        internal readonly static bool IsAndroid = false;
        public static int HttpPort = 5066;

        //for Application
        public static AutoBox Item;

        //for New Index
        public static AutoBox Index;

        //for Readonly PageIndex
        public static readonly ReadonlyList Indices = new ReadonlyList();

        public static void Log(String msg)
        {
            Console.WriteLine(msg);
        }
    }


}
