using System;
using FTServer;

namespace EngineConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //FrenchTest();

            FTServer.EngineTest.test_main();
            //FTServer.EngineTest.test_order();
            //FTServer.ObjectSearch.test_main();

            //FTServer.EngineTest.test_big_n();
            //FTServer.EngineTest.test_big_e();

        }

        static void FrenchTest()
        {
            String str = "l’étranger ls’étranger S’inscrire S'Étatà d'étranger wouldn't I'm l'Europe l’Europe";
            Console.WriteLine(StringUtil.Instance.fromatFrenchInput(str));
        }
    }
}
