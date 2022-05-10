using System;
using IBoxDB.LocalServer;

namespace FTServer{

    public abstract class KeyWord
    {

        public readonly static int MAX_WORD_LENGTH = 16;

        public static void config(DatabaseConfig c)
        {
            // English Language or Word (max=16)              
            c.EnsureTable<KeyWordE>("/E", "K(" + MAX_WORD_LENGTH + ")", "I", "P");

            // Non-English Language or Character
            c.EnsureTable<KeyWordN>("/N", "K", "I", "P");

        }

        public abstract Object getKeyWord();

        public abstract void setKeyWord(Object k);

        public abstract int size();
        //Position
        public int P;

        //Document ID
        public long I;

        [NotColumn]
        public KeyWord previous;
        [NotColumn]
        public bool isLinked;
        [NotColumn]
        public bool isLinkedEnd;

        public String ToFullString()
        {
            return (previous != null ? previous.ToFullString() + " -> " : "") + ToString();
        }


    }

    public sealed class KeyWordE : KeyWord
    {
        //Key Word
        public String K;

        public override Object getKeyWord()
        {
            return K;
        }

        public override void setKeyWord(Object k)
        {
            String t = (String)k;
            if (t.length() > KeyWord.MAX_WORD_LENGTH)
            {
                return;
            }
            K = t;
        }

        public override int size()
        {
            return K.length();
        }

        public override String ToString()
        {
            return K + " Pos=" + P + ", ID=" + I + " E";
        }
    }

    public sealed class KeyWordN : KeyWord
    {
        //Key Word 
        public long K;

        public override Object getKeyWord()
        {
            return K;
        }

        public override void setKeyWord(Object k)
        {
            K = (long)k;
        }

        public override int size()
        {
            if ((K & CMASK) != 0L)
            {
                return 3;
            }
            if ((K & (CMASK << 16)) != 0L)
            {
                return 2;
            }
            return 1;
        }

        const long CMASK = 0xFFFF;

        private static String KtoString(long k)
        {
            char c0 = (char)((k & (CMASK << 32)) >> 32);
            char c1 = (char)((k & (CMASK << 16)) >> 16);
            char c2 = (char)(k & CMASK);

            if (c2 != 0)
            {
                return new String(new char[] { c0, c1, c2 });
            }
            if (c1 != 0)
            {
                return new String(new char[] { c0, c1 });
            }
            return c0.ToString();
        }

        public void longKeyWord(char c0, char c1, char c2)
        {
            long k = (0L | c0) << 32;
            if (c1 != 0)
            {
                k |= ((0L | c1) << 16);
                if (c2 != 0)
                {
                    k |= (0L | c2);
                }
            }
            K = k;
        }

        public String toKString()
        {
            return KtoString(K);
        }

        public override String ToString()
        {
            return toKString() + " Pos=" + P + ", ID=" + I + " N";
        }
    }
}