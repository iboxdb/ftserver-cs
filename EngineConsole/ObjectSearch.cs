using System;
using System.Collections.Generic;
using System.Text;
using IBoxDB.LocalServer;

namespace FTServer
{
    public class ObjectSearch
    {
        public static void test_main()
        {
            DB.Root("/tmp/");
            BoxSystem.DBDebug.DeleteDBFiles(7);
            DB db = new DB(7);

            db.GetConfig().EnsureTable<TextObject>("TextObject", "ID");

            Engine engine = Engine.Instance;
            engine.Config(db.GetConfig());

            AutoBox auto = db.Open();

            {
                TextObject to = new TextObject();
                to.ID = 1;
                to.Season = 2018;
                to.Group = 3;
                to.Time = new DateTime(2018, 3, 5);
                to.Keys = new string[] { "COOL", "FAST" };
                to.KVDesc = new Dictionary<string, object> { { "Name", "X-MAN" }, { "Speed", 3000 } };
                auto.Insert<TextObject>("TextObject", to);
                engine.indexTextNoTran(auto, int.MaxValue, to.ID, to.ToString(), false);

                TextObject to2 = new TextObject();
                to2.ID = 2;
                to2.Season = 2018;
                to2.Group = 4;
                to2.Time = new DateTime(2018, 3, 6);
                to2.Keys = new string[] { "Sharp" };
                to2.KVDesc = new Dictionary<string, object> { { "Speed", "gt4000" } };
                auto.Insert<TextObject>("TextObject", to2);
                engine.indexTextNoTran(auto, int.MaxValue, to2.ID, to2.ToString(), false);
            }
            //SQL and
            //auto.Select ("from TextObject where Season==?");
            //auto.Select ("from TextObject where Group==?");
            //auto.Select ("from TextObject where Season==? & Group==?");
            //auto.Select ("from TextObject where Time==?");
            //auto.Select ("from TextObject where Season==? & Time==?");

            //Full text search -and
            using (var box = auto.Cube())
            {
                Console.WriteLine("Search: Season=2018");
                String searchText = "SE-" + 2018;
                foreach (var kw in engine.searchDistinct(box, searchText, long.MaxValue, 200))
                {
                    Console.WriteLine(box["TextObject", kw.I].Select<TextObject>());
                }

                Console.WriteLine("\r\nSearch: KVDesc={ \"Name\", \"X-MAN\" } , Keys={COOL}");
                searchText = "KV-" + "Name" + "-" + "X-MAN" + " KE-" + "COOL";
                foreach (var kw in engine.searchDistinct(box, searchText, long.MaxValue, 200))
                {
                    Console.WriteLine(box["TextObject", kw.I].Select<TextObject>());
                }
            }

            Console.WriteLine("");
            //Full text search -or
            using (var box = auto.Cube())
            {
                HashSet<long> ids = new HashSet<long>();
                Console.WriteLine("Search: Time=2018-3-6 OR KVDesc={ \"Name\", \"X-MAN\" } , Keys={COOL}");

                String searchText = "TI-" + TextObject.DateTimeToStringShort(new DateTime(2018, 3, 6));
                foreach (var kw in engine.searchDistinct(box, searchText, long.MaxValue, 200))
                {
                    ids.add(kw.I);
                }

                searchText = "KV-" + "Name" + "-" + "X-MAN" + " KE-" + "COOL";
                foreach (var kw in engine.searchDistinct(box, searchText, long.MaxValue, 200))
                {
                    ids.add(kw.I);
                }

                long[] idslong = new long[ids.Count];
                ids.CopyTo(idslong);
                Array.Sort(idslong);

                for (var i = idslong.Length - 1; i >= 0; i--)
                {
                    Console.WriteLine(box["TextObject", idslong[i]].Select<TextObject>());
                }
            }

            auto.GetDatabase().Dispose();
        }
    }

    public class TextObject
    {
        public string Content
        {
            get;
            set;
        }

        public int Group
        {
            get;
            set;
        }

        public Dictionary<string, object> KVDesc
        {
            get;
            set;
        }

        public string[] Keys
        {
            get;
            set;
        }

        public DateTime Time
        {
            get;
            set;
        }

        public int Season
        {
            get;
            set;
        }

        public long ID
        {
            get;
            set;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.append("ID-" + ID + " ");
            sb.append("SE-" + Season + " ");
            sb.append("GR-" + Group + " ");
            sb.append("TI-" + DateTimeToStringShort(Time) + " ");
            if (Keys != null)
            {
                foreach (String str in Keys)
                {
                    sb.append("KE-" + str + " ");
                }
            }
            if (KVDesc != null)
            {
                foreach (var kv in KVDesc)
                {
                    sb.append("KV-" + kv.Key + "-" + kv.Value + " ");
                }
            }
            sb.append(Content);
            return sb.ToString();
        }

        public static String DateTimeToStringShort(DateTime dt)
        {
            return dt.Year + "-" + dt.Month + "-" + dt.Day;
        }
    }
}
