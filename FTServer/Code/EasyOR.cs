using System;
using System.Text;


namespace FTServer
{
    // the easy way convert to OR search,by removing one word
    public class EasyOR
    {

        static String[] removedWords;

        static EasyOR()
        {
            removedWords = new String[] { "\"", "and", "with", "of", "的" };
        }

        internal static ArrayList<String> toOrCondition(String str)
        {
            if (str == null || str.length() == 0)
            {
                return new ArrayList<String>();
            }
            foreach (String s in removedWords)
            {
                str = str.Replace(s, " ");
            }
            char[] encs = StringUtil.Instance.clear(str);
            char[] cncs = Arrays.copyOf(encs, encs.Length);

            for (int i = 0; i < encs.Length; i++)
            {
                if (StringUtil.Instance.isWord(encs[i]))
                {

                }
                else
                {
                    encs[i] = ' ';
                }
            }

            for (int i = 0; i < cncs.Length; i++)
            {
                if (StringUtil.Instance.isWord(cncs[i]))
                {
                    cncs[i] = ' ';
                }
            }

            String en = compress(encs);
            String cn = compress(cncs);

            ArrayList<String> result = new ArrayList<String>();
            if (en.length() > 0 && cn.length() > 0)
            {
                result.add(en);
                result.add(cn);
            }
            else if (en.length() > 0)
            {
                result.addAll(removeOneEN(en));
            }
            else if (cn.contains(" "))
            {
                result.addAll(removeOneEN(cn));
            }
            else
            {
                result.addAll(removeOneCN(cn));
            }

            return result;
        }

        public static String compress(char[] cs)
        {
            StringBuilder r = new StringBuilder();
            foreach (char c in cs)
            {
                if (r.length() > 0 && r.charAt(r.length() - 1) == ' ' && c == ' ')
                {
                    continue;
                }
                r.append(c);
            }
            return r.toString().trim();
        }

        private static ArrayList<String> removeOneCN(String str)
        {
            ArrayList<String> r = new ArrayList<String>();
            if (str.length() <= 2)
            {
                return r;
            }
            else if (str.length() <= 5)
            {
                String a1 = str.substring(0, 2).trim();
                if (a1.length() > 1)
                {
                    r.add(a1);
                }
                String a2 = str.substring(str.length() - 2).trim();
                if (a2.length() > 1)
                {
                    r.add(a2);
                }
            }
            else
            {
                ArrayList<String> a1s = removeOneCN(str.substring(0, 5));
                String a1 = link(a1s);
                if (a1.length() > 1)
                {
                    r.add(a1);
                }

                ArrayList<String> a2s = removeOneCN(str.substring(str.length() - 5));
                String a2 = link(a2s);
                if (a2.length() > 1)
                {
                    r.add(a2);
                }
            }
            return r;
        }

        private static ArrayList<String> removeOneEN(String str)
        {
            ArrayList<String> r = new ArrayList<String>();
            String[] sps = str.split(" ");
            if (sps.Length <= 1)
            {
                return r;
            }
            else if (sps.Length == 2)
            {
                r.add(sps[0]);
                r.add(sps[1]);
            }
            else
            {
                for (int i = 0; i < sps.Length; i++)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int j = 0; j < sps.Length; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }
                        sb.append(" " + sps[j]);
                    }
                    r.add(sb.toString().trim());
                }
            }
            return r;
        }

        private static String link(ArrayList<String> aas)
        {
            StringBuilder sb = new StringBuilder();
            foreach (String s in aas)
            {
                sb.append(s + " ");
            }
            return sb.toString().trim();
        }

    }

}

