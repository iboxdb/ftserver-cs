using System;
using IBoxDB.LocalServer;

namespace FTServer
{

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