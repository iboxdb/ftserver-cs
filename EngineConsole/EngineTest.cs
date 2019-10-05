using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using iBoxDB.LocalServer;

namespace FTServer
{
    public class EngineTest
    {

        public static void test_order()
        {
            DB.Root("/tmp/");


            BoxSystem.DBDebug.DeleteDBFiles(3);
            DB db = new DB(3);
            Engine engine = new Engine();
            engine.Config(db.GetConfig().DBConfig);

            AutoBox auto = db.Open();

            int count = 100;
            String[] ts = new String[count];
            for (int i = 0; i < count; i++)
            {
                ts[i] = "test " + i;
            }
            for (int i = 0; i < ts.Length; i++)
            {
                using (IBox box = auto.Cube())
                {
                    engine.indexText(box, i, ts[i], false);
                    box.Commit().Assert();
                }
            }

            bool doagain = true;
            long startId = long.MaxValue;
            long tcount = 0;
            while (doagain && (startId >= 0))
            {
                doagain = false;
                using (IBox box = auto.Cube())
                {
                    foreach (KeyWord kw in engine.searchDistinct(box, "test", startId, 9))
                    {
                        Console.WriteLine(engine.getDesc(ts[(int)kw.getID()], kw, 20));
                        tcount++;
                        doagain = true;
                        startId = kw.getID() - 1;
                    }
                }
                Console.WriteLine();
                Console.WriteLine(startId);
            }
            Console.WriteLine(count + " == " + tcount);
            auto.GetDatabase().Dispose();

        }

        public static void test_main()
        {
            DB.Root("/tmp/");


            String[] ts = new String[] {
				//ID=0
				"Setting up Git\n"
                + "\n"
                + "Download and install the latest version of GitHub Desktop. "
                + "This will automatically install Git and keep it up-to-date for you.\n"
                + "On your computer, open the Git Shell application.\n"
                + "Tell Git your name so your commits will be properly labeled. Type everything after the $ here:\n"
                + "\n babies "
                + "git config --global user.name \"YOUR NAME\"\n"
                + "Tell Git the email address that will be associated with your Git commits. "
                + "The email you specify should be the same one found in your email settings. "
                + "To keep your email address hidden,"
                + " 关于 see \"Keeping your C# Java NoSQL email address abc@global.com private\".",
				//ID=1
				"关于版本控制\n"
                + "什么是“版本控制”？我为什么要关心它呢？ 版本控制是一种记录一个或若干文件内容变化，1234567890ABCDEFGH "
                + "以便将来查阅特定版本修订情况的系统。 在本书所展示的例子中，我们对保存着软件源代码的文件作版本控制，"
                + "但实际上，C lang IT 你可以对任何类型的文件进行版本控制。",
				//ID=2
				"バージョン管理に関して\n"
                + "\n"
                + "「バージョン管理」とは何でしょうか。また、なぜそれを気にする必要があるのでしょうか。 "
                + "バージョン管理とは、一つのファイルやファイルの集合に対して時間とともに加えられていく変更を記録するシステムで、"
                + "後で特定バージョンを呼び出すことができるようにするためのものです。"
                + " 本書の例では、バージョン管理されるファイルとしてソフトウェアのソースコードを用いていますが、"
                + "実際にはコンピューター上のあらゆる種類のファイルをバージョン管理のもとに置くことができます。",
				//ID=3
				"關於版本控制\n"
                + "什麼是版本控制？ 以及為什麼讀者會在意它？ 美食"
                + "版本控制是一個能夠記錄一個或一組檔案在某一段時間的變更，"
                + "使得讀者以後能取回特定版本的系統。has NoSQL"
                + "在本書的範例中，android 讀者會學到如何對軟體的原始碼做版本控制。"
                + " 即使實際上讀者幾乎可以針對電腦上任意型態的檔案做版本控制。",
				//ID=4
				"Git 简史\n"
                + "同生活中的许多伟大事物一样，Git 诞生于一个极富纷争大举创新的年代。nosql \n"
                + "\n"
                + "Linux 内核开源项目有着为数众广的参与者。 绝大多数的 Linux 内核维护工作都花在了提交补丁和保存归档的"
                + "繁琐事务上（1991－2002年间）。 到 2002 年，"
                + "整个项目组开始启用一个专有的分布式版本控制系统 BitKeeper 来管理和维护代码。\n"
                + "\n"
                + "到了 2005 年，开发 BitKeeper 的商业公司同 Linux 内核开源社区的合作关系结束，"
                + "他们收回了 Linux 内核社区免费使用 BitKeeper 的权力。"
                + " 这就迫使 Linux 开源社区（特别是 Linux 的缔造者 Linux Torvalds）基于使用 BitKcheper 时的"
                + "经验教训，开发出自己的版本系统。 他们对新的系统制订了若干目标：",
				//ID=5
				"버전 관리란?\n\n버전 관리는 무엇이고 우리는 왜 이것을 알아야 할까? 버전 관리 시스템은 파일 변화를 시간에 따라 " +
                "기록했다가 나중에 특정 시점의 버전을 다시 꺼내올 수 있는 시스템이다. 이 책에서는 버전 관리하는 예제로 소프트웨어 " +
                "소스 코드만 보여주지만, 실제로 거의 모든 컴퓨터 파일의 버전을 관리할 수 있다.\n\n그래픽 디자이너나" +
                "웹 디자이너도 버전 관리 시스템(VCS - Version Control System)을 사용할 수 있다. VCS로 이미지나 레이아웃의" +
                "버전(변경 이력 혹은 수정 내용)을 관리하는 것은 매우 현명하다. VCS를 사용하면 각 파일을 이전 상태로 되돌릴 수 있고," +
                "프로젝트를 통째로 이전 is 상태로 되돌릴 수 있고, 시간에 따라 수정 내용을 비교해 볼 수 있고," +
                "누가 문제를 일으켰는지도 추적할 수 있고, 누가 언제 만들어낸 이슈인지도 알 수 있다. VCS를 사용하면 파일을 잃어버리거나" +
                "잘못 고쳤을 때도 쉽게 복구할 수 있다. HAS GIT 이런 모든 장점을 큰 노력 없이 이용할 수 있다."
            };
            for (int tran = 0; tran < 2; tran++)
            {
                BoxSystem.DBDebug.DeleteDBFiles(3);
                DB db = new DB(3);
                Engine engine = new Engine();
                engine.Config(db.GetConfig().DBConfig);

                AutoBox auto = db.Open();


                for (int i = 0; i < ts.Length; i++)
                {
                    if (tran == 0)
                    {
                        using (var box = auto.Cube())
                        {
                            engine.indexText(box, i, ts[i], false);
                            box.Commit().Assert();
                        }
                    }
                    else
                    {
                        engine.indexTextNoTran(auto, 3, i, ts[i], false);
                    }
                }

                using (var box = auto.Cube())
                {

                    //engine.indexText(box, 4, ts[4], true);
                    box.Commit().Assert();
                }

                using (var box = auto.Cube())
                {
                    // searchDistinct() search()
                    foreach (KeyWord kw in engine.search(box, "nosql has 電 原始碼"))
                    {
                        Console.WriteLine(kw.ToFullString());
                        Console.WriteLine(engine.getDesc(ts[(int)kw.getID()], kw, 20));
                        Console.WriteLine();
                    }
                    foreach (String skw in engine.discover(box,
                                                  'n', 's', 2,
                                                  '\u2E80', '\u9fa5', 2))
                    {
                        Console.WriteLine(skw);
                    }
                }
                auto.GetDatabase().Dispose();
                Console.WriteLine("----------------------------------");
            }
        }

        //150 seconds test
        public static void test_big_n()
        {
            String book = "/hero.txt";
            long dbid = 1;
            char split = '。';

            //set this true
            bool rebuild = true;
            int notranCount = -1;//10;
            String strkw = "黄蓉 郭靖 洪七公";
            //strkw = "洪七公 黄蓉 郭靖";
            //strkw = "黄蓉 郭靖 公";
            //strkw = "郭靖 黄蓉";
            //strkw = "黄蓉";
            //strkw = "时察";
            //strkw = "的";
            //strkw = "七十二路";
            //strkw = "十八掌";
            //strkw = "日日夜夜无穷无尽的";
            //strkw = "牛家村边绕 日日夜夜无穷无尽的";
            //strkw = "这几天";
            //strkw = "有 这几天";
            //strkw = "这几天 有";
            test_big(book, dbid, rebuild, split, strkw, notranCount);
        }

        //30 seconds test
        public static void test_big_e()
        {
            String book = "/phoenix.txt";
            long dbid = 2;
            char split = '.';

            //set true
            bool rebuild = true;
            int notranCount = 10; //-1;

            String strkw = "Harry";
            strkw = "Harry Philosopher";
            //strkw = "Philosopher";
            //strkw = "\"Harry Philosopher\"";
            //strkw = "\"He looks\"";
            //strkw = "He looks";
            //strkw = "\"he drove toward town he thought\"";
            //strkw = "\"he drove toward\"";
            //strkw = "\"he thought\"";
            //strkw = "\"he thought\" toward";
            //strkw = "toward \"he thought\"";
            //strkw = "he thought";
            //strkw = "he thought toward";
            //strkw = "He";
            test_big(book, dbid, rebuild, split, strkw, notranCount);
        }

        private static void test_big(String book, long dbid, bool rebuild,
                                      char split, String strkw, int notranCount)
        {
            DB.Root("/tmp/");

            if (rebuild)
            {
                BoxSystem.DBDebug.DeleteDBFiles(dbid);
            }
            DB db = new DB(dbid);

            String[] tstmp = File.OpenText(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                book).ReadToEnd().Split(split);

            //three times data
            List<String> list = new List<String>();
            for (int i = 0; i < 3; i++)
            {
                foreach (String str in tstmp)
                {
                    list.Add(str);
                }
            }
            String[] ts = list.ToArray();


            Engine engine = new Engine();
            engine.Config(db.GetConfig().DBConfig);
            //engine.maxSearchTime = 1000;

            AutoBox auto = db.Open();

            var b = DateTime.Now;
            if (rebuild)
            {
                long rbcount = 0;

                Parallel.For(0, ts.Length, (i) =>
                {
                    if (notranCount < 1)
                    {
                        using (var box = auto.Cube())
                        {
                            Interlocked.Add(ref rbcount, engine.indexText(box, i, ts[i], false));
                            box.Commit().Assert();
                        }
                    }
                    else
                    {
                        Interlocked.Add(ref rbcount, engine.indexTextNoTran(auto, notranCount, i, ts[i], false));
                    }
                });

                Console.WriteLine("Index " + (DateTime.Now - b).TotalSeconds + " -" + rbcount);
            }

            b = DateTime.Now;
            int c = 0;
            for (int i = 0; i < 20; i++)
            {
                b = DateTime.Now;
                c = 0;
                using (var box = auto.Cube())
                {
                    foreach (KeyWord kw in engine.searchDistinct(box, strkw))
                    {
                        c++;
                    }
                }
                Console.WriteLine("DB: " + c + " , " + (DateTime.Now - b).TotalSeconds + "s");
            }


            StringUtil sutil = new StringUtil();
            for (int i = 0; i < ts.Length; i++)
            {
                ts[i] = ts[i].ToLower() + " ";
                ts[i] = " " + new String(sutil.clear(ts[i])) + " ";
            }

            strkw = strkw.ToLower();
            String[] kws = strkw.Split(new char[] { ' ' });
            String tmp_kws = null;
            for (int i = 0; i < kws.Length; i++)
            {
                if (kws[i].length() < 1)
                {
                    kws[i] = null;
                    continue;
                }
                if (tmp_kws == null)
                {
                    if (kws[i].StartsWith("\""))
                    {
                        tmp_kws = kws[i];
                        kws[i] = null;
                    }
                }
                else if (tmp_kws != null)
                {
                    tmp_kws += (" " + kws[i]);

                    if (kws[i].EndsWith("\""))
                    {
                        kws[i] = tmp_kws.substring(1, tmp_kws.length() - 1);
                        tmp_kws = null;
                    }
                    else
                    {
                        kws[i] = null;
                    }
                }
            }


            b = DateTime.Now;
            c = 0;
            int starti = 0;
        Test:
            while (starti < ts.Length)
            {
                int i = starti++;
                for (int j = 0; j < kws.Length; j++)
                {
                    if (kws[j] == null)
                    {
                        continue;
                    }
                    int p = 0;
                Test_P:
                    while (p >= 0)
                    {
                        p = ts[i].IndexOf(kws[j], p + 1);
                        if (p < 0)
                        {
                            goto Test;
                        }
                        if (onlyPart(ts[i], kws[j], p))
                        {
                            goto Test_P;
                        }
                        break;
                    }
                }
                c++;

            }
            Console.WriteLine("MEM: " + c + " , " + (DateTime.Now - b).TotalSeconds + "s -" + ts.Length);

            auto.GetDatabase().Dispose();
        }

        private static bool onlyPart(String str, String wd, int p)
        {
            char last = wd[wd.Length - 1];
            if (last > 256)
            {
                return false;
            }

            char pc = str[p + wd.length()];
            if (pc >= 'a' && pc <= 'z')
            {
                return true;
            }
            if (pc == '-')
            {
                return true;
            }

            int bef = p;
        Test:
            while (bef > 0)
            {
                pc = str[bef - 1];
                if (pc >= 'a' && pc <= 'z')
                {
                    return true;
                }
                if (pc == '-')
                {
                    bef--;
                    goto Test;
                }
                break;
            }

            return false;
        }

        public class TA
        {
            public int a;
            public int b;

            public override string ToString()
            {
                return a + "-" + b;
            }
        }

        public static void test_db()
        {
            DB db = new DB(new byte[0]);
            db.GetConfig().EnsureTable<TA>("TA", "a", "b");
            var auto = db.Open();
            using (var box = auto.Cube())
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        box["TA"].Insert(new TA { a = i, b = j });
                    }
                }
                box.Commit().Assert();
            }

            foreach (var ta in auto.Select<TA>("from TA where a>= ? & a< ? & b >= ? & b<?", -100, 100, 3, 5))
            {
                Console.WriteLine(ta);
            }
        }
    }
}

