/* iBoxDB FTServer Bruce Yang CL-N */
using System;
using System.Collections.Generic;
using System.Threading;

using IBoxDB.LocalServer;

namespace FTServer
{
    public class Engine
    {
        public readonly static Engine Instance = new Engine();
        public static long KeyWordMaxScan = long.MaxValue;

        private Engine()
        {

        }
        public void Config(DatabaseConfig config)
        {
            KeyWord.config(config);
        }


        public long indexText(IBox box, long id, String str, bool isRemove, ThreadStart delay = null)
        {
            if (id == -1)
            {
                return -1;
            }
            long itCount = 0;
            char[] cs = StringUtil.Instance.clear(str);
            List<KeyWord> map = StringUtil.Instance.fromString(id, cs, true);


            foreach (KeyWord kw in map)
            {
                delay?.Invoke();
                insertToBox(box, kw, isRemove);
                itCount++;
            }
            return itCount;
        }

        public long indexTextNoTran(AutoBox auto, int commitCount, long id, String str, bool isRemove)
        {
            if (id == -1)
            {
                return -1;
            }
            long itCount = 0;
            char[] cs = StringUtil.Instance.clear(str);
            List<KeyWord> map = StringUtil.Instance.fromString(id, cs, true);


            IBox box = null;
            int ccount = 0;
            foreach (KeyWord kw in map)
            {
                if (box == null)
                {
                    box = auto.Cube();
                    ccount = commitCount;
                }
                insertToBox(box, kw, isRemove);
                itCount++;
                if (--ccount < 1)
                {
                    box.Commit().Assert();
                    box = null;
                }
            }
            if (box != null)
            {
                box.Commit().Assert();
            }
            return itCount;
        }

        static void insertToBox(IBox box, KeyWord kw, bool isRemove)
        {
            Binder binder;
            if (kw is KeyWordE)
            {
                binder = box["/E", ((KeyWordE)kw).K, kw.I, kw.P];
            }
            else
            {
                binder = box["/N", ((KeyWordN)kw).K, kw.I, kw.P];
            }
            if (isRemove)
            {
                binder.Delete();
            }
            else
            {
                if (binder.TableName == "/E")
                {
                    binder.Insert((KeyWordE)kw);
                }
                else
                {
                    binder.Insert((KeyWordN)kw);
                }
            }
        }

        private Random ran = new Random();

        public LinkedHashSet<String> discoverEN(IBox box,
                char efrom, char eto, int elength)
        {
            LinkedHashSet<String> list = new LinkedHashSet<String>();
            if (elength > 0)
            {
                int len = ran.nextInt(KeyWord.MAX_WORD_LENGTH) + 1;
                char[] cs = new char[len];
                for (int i = 0; i < cs.Length; i++)
                {
                    cs[i] = (char)(ran.nextInt(eto - efrom) + efrom);
                }
                KeyWordE kw = new KeyWordE();
                kw.keyWord(new String(cs));
                foreach (KeyWordE tkw in lessMatch(box, kw))
                {
                    String str = tkw.K;
                    if (str.charAt(0) < efrom)
                    {
                        break;
                    }
                    int c = list.size();
                    list.add(str);
                    if (list.size() > c)
                    {
                        elength--;
                        if (elength <= 0)
                        {
                            break;
                        }
                    }
                }
            }
            return list;
        }

        public LinkedHashSet<String> discoverCN(IBox box,
                char nfrom, char nto, int nlength)
        {

            LinkedHashSet<String> list = new LinkedHashSet<String>();
            if (nlength > 0)
            {
                char[] cs = new char[2];
                for (int i = 0; i < cs.Length; i++)
                {
                    cs[i] = (char)(ran.nextInt(nto - nfrom) + nfrom);
                }
                KeyWordN kw = new KeyWordN();
                kw.longKeyWord(cs[0], cs[1], (char)0);
                foreach (KeyWord tkw in lessMatch(box, kw))
                {
                    String str = ((KeyWordN)tkw).toKString();
                    if (str.charAt(0) < nfrom)
                    {
                        break;
                    }
                    int c = list.size();
                    list.add(str);
                    if (list.size() > c)
                    {
                        nlength--;
                        if (nlength <= 0)
                        {
                            break;
                        }
                    }
                }
            }
            return list;
        }


        public IEnumerable<KeyWord> searchDistinct(IBox box, String str)
        {
            return searchDistinct(box, str, long.MaxValue, long.MaxValue);
        }
        // startId -> descending order
        public IEnumerable<KeyWord> searchDistinct(IBox box, String str, long startId, long len)
        {
            long c_id = -1;
            foreach (KeyWord kw in search(box, str, startId))
            {
                if (len < 1)
                {
                    break;
                }
                if (kw.I == c_id)
                {
                    continue;
                }
                c_id = kw.I;
                len--;
                yield return kw;
            }
        }

        public String getDesc(String str, KeyWord kw, int length)
        {
            return StringUtil.Instance.getDesc(str, kw, length);
        }

        public IEnumerable<KeyWord> search(IBox box, String str)
        {
            return search(box, str, long.MaxValue);
        }

        public IEnumerable<KeyWord> search(IBox box, String str, long startId)
        {
            if (startId < 0)
            {
                return new ArrayList<KeyWord>();
            }
            char[] cs = StringUtil.Instance.clear(str);
            ArrayList<KeyWord> map = StringUtil.Instance.fromString(-1, cs, false);

            if (map.size() > KeyWord.MAX_WORD_LENGTH || map.isEmpty())
            {
                return new ArrayList<KeyWord>();
            }

            MaxID maxId = new MaxID();
            maxId.id = startId;
            maxId.jumpTime = 0;

            IEnumerator<KeyWord> cd = search(box, map.ToArray(), maxId).GetEnumerator();
            return new Iterable<KeyWord>()
            {

                iterator = new EngineIterator<KeyWord>()
                {
                    hasNext = () =>
                    {
                        if (cd.MoveNext())
                        {
                            maxId.jumpTime = 0;
                            return true;
                        }
                        return false;
                    },
                    next = () =>
                    {
                        return cd.Current;
                    }
                }
            };
        }

        private IEnumerable<KeyWord> search(IBox box, KeyWord[] kws, MaxID maxId)
        {

            if (kws.Length == 1)
            {
                return search(box, kws[0], (KeyWord)null, maxId);
            }

            return search(box, kws[kws.Length - 1],
                           search(box, Arrays.copyOf(kws, kws.Length - 1), maxId),
                           maxId);
        }

        private IEnumerable<KeyWord> search(IBox box, KeyWord nw,
                                             IEnumerable<KeyWord> condition, MaxID maxId)
        {
            IEnumerator<KeyWord> cd = condition.GetEnumerator();

            IEnumerator<KeyWord> r1 = null;

            KeyWord r1_con = null;
            long r1_id = -1;


            return new Iterable<KeyWord>()
            {

                iterator = new EngineIterator<KeyWord>()
                {


                    hasNext = () =>
                    {
                        if (r1 != null && r1.MoveNext())
                        {
                            return true;
                        }
                        while (cd.MoveNext())
                        {
                            r1_con = cd.Current;

                            if (r1_id == r1_con.I)
                            {
                                continue;
                            }
                            if (!nw.isLinked)
                            {
                                r1_id = r1_con.I;
                            }

                            r1 = search(box, nw, r1_con, maxId).GetEnumerator();
                            if (r1.MoveNext())
                            {
                                return true;
                            }

                        }
                        return false;
                    },

                    next = () =>
                    {
                        KeyWord k = r1.Current;
                        k.previous = r1_con;
                        return k;
                    }
                }

            };

        }

        private static IEnumerable<KeyWord> search(IBox box,
                                                    KeyWord kw, KeyWord con, MaxID maxId)
        {


            if (kw is KeyWordE && con is KeyWordE)
            {
                if (((KeyWordE)kw).K.equals(((KeyWordE)con).K))
                {
                    maxId.id = -1;
                    return new List<KeyWord>();
                }
            }
            if (kw is KeyWordN && con is KeyWordN)
            {
                if (((KeyWordN)kw).K == ((KeyWordN)con).K)
                {
                    maxId.id = -1;
                    return new List<KeyWord>();
                }
            }

            String ql = kw is KeyWordE
                ? "from /E where K==? & I<=?"
                    : "from /N where K==? & I<=?";


            int linkPos = kw.isLinked ? (con.P + con.size()
                + (kw is KeyWordE ? 1 : 0)) : -1;

            long currentMaxId = long.MaxValue;
            KeyWord cache = null;
            IEnumerator<KeyWord> iter = null;
            bool isLinkEndMet = false;

            return new Iterable<KeyWord>()
            {
                iterator = new EngineIterator<KeyWord>()
                {

                    hasNext = () =>
                    {
                        if (maxId.id == -1)
                        {
                            return false;
                        }

                        if (iter == null || currentMaxId > (maxId.id))
                        {
                            currentMaxId = maxId.id;
                            iter = kw is KeyWordE ?
                                (IEnumerator<KeyWord>)box.Scale<KeyWordE>(ql, ((KeyWordE)kw).K, maxId.id).GetEnumerator() :
                                    box.Scale<KeyWordN>(ql, ((KeyWordN)kw).K, maxId.id).GetEnumerator();
                        }

                        while (iter.MoveNext())
                        {

                            cache = iter.Current;

                            maxId.id = cache.I;
                            maxId.jumpTime++;
                            if (maxId.jumpTime > Engine.KeyWordMaxScan)
                            {
                                break;
                            }

                            currentMaxId = maxId.id;
                            if (con != null && con.I != maxId.id)
                            {
                                return false;
                            }

                            if (isLinkEndMet)
                            {
                                continue;
                            }

                            if (linkPos == -1)
                            {
                                return true;
                            }

                            int cpos = cache.P;
                            if (cpos > linkPos)
                            {
                                continue;
                            }
                            if (cpos == linkPos)
                            {
                                if (kw.isLinkedEnd)
                                {
                                    isLinkEndMet = true;
                                }
                                return true;
                            }
                            return false;
                        }

                        maxId.id = -1;
                        return false;

                    },

                    next = () =>
                    {
                        return cache;
                    }

                }
            };


        }

        private static IEnumerable<KeyWord> lessMatch(IBox box, KeyWord kw)
        {
            if (kw is KeyWordE)
            {
                return box.Scale<KeyWordE>("from /E where K<=? limit 0, 50", ((KeyWordE)kw).K);

            }
            else
            {
                return box.Scale<KeyWordN>("from /N where K<=? limit 0, 50", ((KeyWordN)kw).K);
            }
        }

        private sealed class MaxID
        {
            public long id = long.MaxValue;
            public long jumpTime = 0;

        }
    }

}

