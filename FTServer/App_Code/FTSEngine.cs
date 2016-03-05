using System;
using System.Collections.Generic;
using System.Text;
using iBoxDB.LocalServer;
using System.IO;

namespace FTServer
{
	public class Engine
	{

		readonly Util util = new Util ();
		readonly StringUtil sUtil = new StringUtil ();

		public void Config (DatabaseConfig config)
		{
			KeyWord.config (config);
		}

		public bool indexText (IBox box, long id, String str, bool isRemove)
		{
			if (id == -1) {
				return false;
			}

			char[] cs = sUtil.clear (str);
			List<KeyWord> map = util.fromString (id, cs, true);
	 
			HashSet<String> words = new HashSet<String> ();
			foreach (KeyWord kw in map) {
				Binder binder;
				if (kw is KeyWordE) {
					if (words.Contains (kw.KWord.ToString ())) {
						continue;
					}
					words.Add (kw.KWord.ToString ());
					binder = box ["E", kw.KWord, kw.ID, kw.Position];
				} else { 
					binder = box ["N", kw.KWord, kw.ID, kw.Position];
				}
				if (isRemove) {
					binder.Delete ();
				} else {
					if (binder.TableName == "E") {
						binder.Insert ((KeyWordE)kw, 1);
					} else {
						binder.Insert ((KeyWordN)kw, 1);
					}

				}
			}
			return true;
		}

		public IEnumerable<KeyWord> searchDistinct (IBox box, String str)
		{
			long c_id = -1;
			foreach (KeyWord kw in search(box, str)) {
				if (kw.ID == c_id) {
					continue;
				}
				c_id = kw.ID;
				yield return kw;
			}			 
		}

		public String getDesc (String str, KeyWord kw, int length)
		{
			return sUtil.getDesc (str, kw, length);
		}

		public IEnumerable<KeyWord> search (IBox box, String str)
		{
			char[] cs = sUtil.clear (str);
			List<KeyWord> map = util.fromString (-1, cs, false);
		
			if (map.Count > KeyWord.MAX_WORD_LENGTH || map.Count == 0) {
				return new List<KeyWord> ();
			}

			List<KeyWord> kws = new List<KeyWord> ();

			for (int i = 0; i < map.Count; i++) {
				KeyWord kw = map [i];
				if (kw is KeyWordE) {
					String s = kw.KWord.ToString ();
					if ((s.Length > 2) && (!sUtil.mvends.Contains (s))) {
						kws.Add (kw);
						map [i] = null;
					}
				} else {
					KeyWordN kwn = (KeyWordN)kw;
					if (kwn.size () >= 2) {
						kws.Add (kw);
						map [i] = null;
					} else if (kws.Count > 0) {
						KeyWord p = kws [kws.Count - 1];
						if (p is KeyWordN) {
							if (kwn.Position == (p.Position + ((KeyWordN)p).size ())) {
								kws.Add (kw);
								map [i] = null;
							}
						}
					}
				}
			}
			for (int i = 0; i < map.Count; i++) {
				KeyWord kw = map [i];
				if (kw != null) {
					kws.Add (kw);
				}
			}

			return search (box, kws.ToArray ());
		}

		private IEnumerable<KeyWord> search (IBox box, KeyWord[] kws)
		{
			if (kws.Length == 1) {
				return search (box, kws [0], (KeyWord)null, false);
			}
			bool asWord = true;
			KeyWord kwa = kws [kws.Length - 2];
			KeyWord kwb = kws [kws.Length - 1];
			if ((kwa is KeyWordN) && (kwb is KeyWordN)) {
				asWord = kwb.Position != (kwa.Position + ((KeyWordN)kwa).size ());
			}

			KeyWord[] condition = new KeyWord[kws.Length - 1];
			Array.Copy (kws, 0, condition, 0, condition.Length);
			return search (box, kws [kws.Length - 1],
			               search (box, condition), asWord);
		}

		private IEnumerable<KeyWord> search (IBox box, KeyWord nw,
		                                     IEnumerable<KeyWord> condition, bool isWord)
		{
			long r1_id = -1;
			foreach (KeyWord r1_con in condition) {
				if (isWord) {
					if (r1_id == r1_con.ID) {
						continue;
					}
				}
				r1_id = r1_con.ID; 
				foreach (KeyWord k in search(box, nw, r1_con,isWord)) {
					k.previous = r1_con;
					yield return k;
				}
			} 
		}

		private  IEnumerable<KeyWord> search (IBox box, KeyWord kw, KeyWord con, bool asWord)
		{
			if (kw is KeyWordE) {
				if (con == null) {
					return Index2KeyWord<KeyWordE> (box.Select<object> ("from E where K==?", kw.KWord));
				} else {
					return Index2KeyWord<KeyWordE> (box.Select<object> ("from E where K==? &  I==?",
					                                                    kw.KWord, con.ID));
				}
			} else {
				KeyWordN kwn = (KeyWordN)kw;
				if (con is KeyWordE) {
					asWord = true;
				}
				if (con == null) {
					return Index2KeyWord<KeyWordN> (box.Select<object> ("from N where K==?", kwn.K));
				} else if (asWord) {
					return Index2KeyWord<KeyWordN> (box.Select<object> ("from N where K==? &  I==?",
					                                                    kwn.K, con.ID));
				} else {
					return Index2KeyWord<KeyWordN> (box.Select<object> ("from N where K==? & I==? & P==?",
					                                                    kwn.K, con.ID, (con.Position + ((KeyWordN)con).size ())));
				}
			}
		}

		private  IEnumerable<KeyWord> Index2KeyWord<T> (IEnumerable<object> src) where T:KeyWord, new()
		{ 
			foreach (var o in src) {  
				T cache = new T ();
				object[] os = (object[])o;
				cache.KWord = os [0];
				cache.I = (long)os [1];
				cache.P = (int)os [2]; 
				yield return cache;
			}
		}
	}

	class Util
	{

		readonly StringUtil sUtil = new StringUtil ();

		public List<KeyWord> fromString (long id, char[] str, bool includeOF)
		{

			List<KeyWord> kws = new List<KeyWord> ();

			KeyWordE k = null;
			for (int i = 0; i < str.Length; i++) {
				char c = str [i];
				if (c == ' ') {
					if (k != null) {
						kws.Add (k);
						if (includeOF) {
							k = k.getOriginalForm ();
							if (k != null) {
								kws.Add (k);
							}
						}
					}
					k = null;
				} else if (sUtil.isWord (c)) {
					if (k == null && c != '-' && c != '#') {
						k = new KeyWordE (); 
						k.ID = id;
						k.KWord = "";
						k.Position = i;
					}
					if (k != null) {
						k.KWord = k.KWord.ToString () + c;
					}
				} else {
					if (k != null) {
						kws.Add (k);
						if (includeOF) {
							k = k.getOriginalForm ();
							if (k != null) {
								kws.Add (k);
							}
						}
					}
					k = null;
					KeyWordN n = new KeyWordN (); 
					n.ID = id;
					n.Position = i;
					n.KWord = c.ToString ();
					kws.Add (n);
				 
					char c1 = str [i + 1]; 
					if ((c1 != ' ') && (!sUtil.isWord (c1))) {
						n = new KeyWordN (); 
						n.ID = id;
						n.Position = i;
						n.KWord = new String (new char[] { c, c1 });
						kws.Add (n);
						if (!includeOF) {
							kws.RemoveAt (kws.Count - 2);
							i++; 
						}						
					}   
				}
			}
			return kws;
		}
	}

	class StringUtil
	{ 
		HashSet<char> set;
		public HashSet<String> mvends;

		public StringUtil ()
		{
			String s = "!\"@$%&'()*+,./:;<=>?[\\]^_`{|}~\r\n"; //@-
			s += "， 　，《。》、？；：‘’“”【｛】｝——=+、｜·～！￥%……&*（）"; //@-#
			s += "｀～！＠￥％……—×（）——＋－＝【】｛｝：；’＇”＂，．／＜＞？’‘”“";//＃
			s += " � ★☆,。？,　！";
			set = new HashSet<char> ();
			foreach (char c in s) {
				set.Add (c);
			}
			set.Add ((char)0);

			String[] ms = new String[] {
				"are", "were", "have", "has", "had",
				"you", "she", "her", "him", "like", "will", "would", "should",
				"when", "than", "then", "that", "this", "there", "who", "those", "these",
				"with", "which", "where", "they", "them", "one",
				"does", "doesn", "did", "gave", "give",
				"something", "someone", "about", "come"
			};
			mvends = new HashSet<String> ();
			foreach (String c in ms) {
				mvends.Add (c);
			}
		}

		public bool isWord (char c)
		{
			//English
			if (c >= 'a' && c <= 'z') {
				return true;
			}
			if (c >= '0' && c <= '9') {
				return true;
			}
			//Russian
			if (c >= 0x0400 && c <= 0x052f) {
				return true;
			}
			//Germen
			if (c >= 0xc0 && c <= 0xff) {
				return true;
			}
			//Korean [uAC00-uD7A3]
			return c == '-' || c == '#';
		}

		public char[] clear (String str)
		{
			char[] cs = (str + "   ").ToLower ().ToCharArray ();
			for (int i = 0; i < cs.Length; i++) {
				if (set.Contains (cs [i])) {
					cs [i] = ' ';
				} 
			}
			return cs;
		}

		public String getDesc (String str, KeyWord kw, int length)
		{
			List<KeyWord> list = new List<KeyWord> ();
			while (kw != null) {
				list.Add (kw);
				kw = kw.previous;
			}
			list.Sort (
				(o1, o2) => {
				return o1.Position - o2.Position;
			}
			);
			KeyWord[] ps = list.ToArray ();
	 
			int start = -1;
			int end = -1;
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < ps.Length; i++) {
				if ((ps [i].Position + ps [i].KWord.ToString ().Length) < end) {
					continue;
				}
				start = ps [i].Position;
				end = ps [i].Position + length;
				if (end > str.Length) {
					end = str.Length;
				}
				sb.Append (str.Substring (start, end - start))
					.Append ("...");
			}
			return sb.ToString ();

		}
	}

	public abstract class KeyWord
	{

		public readonly static int MAX_WORD_LENGTH = 16;

		public static void config (DatabaseConfig c)
		{
			// English Language or Word (max=16)              
			c.EnsureTable<KeyWordE> ("E", "K(" + MAX_WORD_LENGTH + ")", "I", "P");

			// Non-English Language or Character
			c.EnsureTable<KeyWordN> ("N", "K", "I", "P");

		}

		[NotColumn]
		public abstract object KWord {
			get ;
			set;
		}
		//Position
		public int P;

		[NotColumn]
		public int Position {
			get{ return P;}
			set{ P = value;}
		}
		//Document ID
		public long I;

		[NotColumn]
		public long ID {
			get{ return I;}
			set{ I = value;}
		}

		[NotColumn]
		public KeyWord previous;

		public override String ToString ()
		{
			return KWord + " Pos=" + P + ", ID=" + I + " " + (this is KeyWordE ? "E" : "N");
		}

		public String ToFullString ()
		{
			return (previous != null ? previous.ToFullString () + " -> " : "") + ToString ();
		}
	}

	public sealed class KeyWordE : KeyWord
	{
		//Key Word
		public String K;

		[NotColumn]
		public override object KWord {
			get{ return K;}
			set {
				var t = (String)value;
				if (t.Length > KeyWord.MAX_WORD_LENGTH) {
					return;
				}
				K = t;
			}
		}

		public KeyWordE getOriginalForm ()
		{
			String of;
			if (antetypes.TryGetValue (K, out of)) {
				KeyWordE e = new KeyWordE ();
				e.I = this.I;
				e.P = this.P;
				e.K = of;
				return e;
			}
			return null;
		}

		private static Dictionary<String, String> antetypes = new Dictionary<String, String> () {
			{"dogs", "dog"},
			{"houses", "house"},
			{"grams", "gram"},

			{"kisses", "kiss"},
			{"watches", "watch"},
			{"boxes", "box"},
			{"bushes", "bush"},

			{"tomatoes", "tomato"},
			{"potatoes", "potato"},

			{"babies", "baby"},
			{"universities", "university"},
			{"flies", "fly"},
			{"impurities", "impurity"}			
		};
	}

	public sealed class KeyWordN : KeyWord
	{
		//Key Word
		public long K;

		[NotColumn]
		public override object KWord {
			get{ return KtoString (K);}
			set {
				if (value is long) {
					K = (long)value;
				} else {
					K = StringtoK ((string)value);
			
				}
			}
		}

		public byte size ()
		{
			if ((K & CMASK) != 0L) {
				return 3;
			}
			if ((K & (CMASK << 16)) != 0L) {
				return 2;
			}
			return 1;
		}

		const long CMASK = 0xFFFF;

		private static String KtoString (long k)
		{
			char c0 = (char)((k & (CMASK << 32)) >> 32);
			char c1 = (char)((k & (CMASK << 16)) >> 16);
			char c2 = (char)(k & CMASK);

			if (c2 != 0) {
				return new String (new char[] { c0, c1, c2 });
			}
			if (c1 != 0) {
				return new String (new char[] { c0, c1 });
			}
			return c0.ToString ();
		}

		private static long StringtoK (String str)
		{
			long k = (0L | str [0]) << 32;
			if (str.Length > 1) {
				k |= ((0L | str [1]) << 16);
			}
			if (str.Length > 2) {
				k |= (0L | str [2]);
			}
			return k;
		}
	}
}

