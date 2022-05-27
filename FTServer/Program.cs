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

                //String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir);

                //path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), dir);
                //path = Path.Combine("/mnt/hgfs/DB", dir);
                String path = Path.Combine("../", dir);

                Directory.CreateDirectory(path);
                Log("DBPath=" + Path.GetFullPath(path));
                DB.Root(path);

                #endregion

                Log("ReadOnly CacheLength = " + (Config.Readonly_CacheLength / 1024L / 1024L) + " MB (" + Config.Readonly_CacheLength + ")");
                Log("ReadOnly Max DB Count = " + Config.Readonly_MaxDBCount);

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

                App.Index = App.Indices.get(App.Indices.length() - 1);

                Log("ReadOnly Index DB (" + start + "), start from " + IndexServer.IndexDBStart);
                Log("MinCache = " + (Config.minCache() / 1024L / 1024L) + " MB");
                //bigger will more accurate, smaller faster will jump some pages
                Engine.KeyWordMaxScan = 10;
                Log("KeyWordMaxScan = " + Engine.KeyWordMaxScan);
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
                   webBuilder.UseSockets((soc) =>
                   {
                       soc.Backlog = 2;
                       Log("Backlog: " + soc.Backlog + ", IOQueueCount: " + soc.IOQueueCount + ", NoDelay: " + soc.NoDelay);
                   });
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



}
