using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using iBoxDB.LocalServer;
using System.IO;
using System.Threading;

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
			Dictionary<int, KeyWord> map = util.fromString (id, cs);
			RemoveMinMax (map);

			SortedSet<String> words = new SortedSet<String> ();
			foreach (KeyWord kw in map.Values) {
				Binder binder;
				if (kw.isWord) {
					if (words.Contains (kw.KWord)) {
						continue;
					}
					words.Add (kw.KWord);
					binder = box ["E", kw.KWord, kw.ID];
				} else {
					if (!map.ContainsKey (kw.Position - 1)  
						&& !map.ContainsKey (kw.Position + 1)) {
						continue;
					}
					binder = box ["N", kw.KWord, kw.ID, kw.Position];
				}
				if (isRemove) {
					binder.Delete ();
				} else {
					binder.Insert (kw, 1);
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
			Dictionary<int, KeyWord> map = util.fromString (-1, cs);
			RemoveMinMax (map);

			if (map.Count > KeyWord.MAX_WORD_LENGTH || map.Count == 0) {
				return new List<KeyWord> ();
			}
			return search (box, map.Values.ToArray ());
		}

		private void RemoveMinMax (Dictionary<int, KeyWord> map)
		{
			foreach (KeyValuePair<int,KeyWord> e
			     in new Dictionary<int, KeyWord>(map)) {
				if (e.Value.isWord && e.Value.KWord.Length < 1) {
					map.Remove (e.Key);
				}
				if (e.Value.isWord && e.Value.KWord.Length
					> KeyWord.MAX_WORD_LENGTH) {
					map.Remove (e.Key);
				}
			}
		}
		// Base
		private IEnumerable<KeyWord> search (IBox box, KeyWord[] kws)
		{
			if (kws.Length == 1) {
				return search (box, kws [0], (KeyWord)null);
			}
			KeyWord[] condition = new KeyWord[kws.Length - 1];
			Array.Copy (kws, 0, condition, 0, condition.Length);
			return search (box, kws [kws.Length - 1],
			               search (box, condition),
			               kws [kws.Length - 1].Position
				!= (kws [kws.Length - 2].Position + 1));
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
				r1_con.isWord = isWord;
				foreach (KeyWord k in search(box, nw, r1_con)) {
					k.previous = r1_con;
					yield return k;
				}
			} 
		}

		private  IEnumerable<KeyWord> search (IBox box, KeyWord kw, KeyWord condition)
		{

			if (kw.isWord) {
				if (condition == null) {
					return box.Select<KeyWord> ("from E where K==?", kw.KWord);
				} else {
					return box.Select<KeyWord> ("from E where K==? &  I==?",
					                            kw.KWord, condition.ID);
				}
			} else if (condition == null) {
				return box.Select<KeyWord> ("from N where K==?", kw.KWord);
			} else if (condition.isWord) {
				return box.Select<KeyWord> ("from N where K==? &  I==?",
				                            kw.KWord, condition.ID);
			} else {
				return box.Select<KeyWord> ("from N where K==? & I==? & P==?",
				                            kw.KWord, condition.ID, (condition.Position + 1));
			}
		}
	}

	class Util
	{

		readonly StringUtil sUtil = new StringUtil ();

		public Dictionary<int, KeyWord> fromString (long id, char[] str)
		{

			Dictionary<int, KeyWord> kws = new Dictionary<int, KeyWord> ();

			KeyWord k = null;
			for (int i = 0; i < str.Length; i++) {
				char c = str [i];
				if (c == ' ') {
					if (k != null) {
						kws.Add (k.Position, k);
					}
					k = null;
				} else if (sUtil.isWord (c)) {
					if (k == null && c != '-' && c != '#') {
						k = new KeyWord ();
						k.isWord = true;
						k.ID = id;
						k.KWord = "";
						k.Position = i;
					}
					if (k != null) {
						k.KWord = k.KWord + c;
					}
				} else {
					if (k != null) {
						kws.Add (k.Position, k);
					}
					k = new KeyWord ();
					k.isWord = false;
					k.ID = id;
					k.KWord = c.ToString ();
					k.Position = i;
					kws.Add (k.Position, k);
					k = null;
				}
			}

			return kws;
		}
	}

	class StringUtil
	{
 
		SortedSet<char> set;

		public StringUtil ()
		{
			String s = "!\"@$%&'()*+,./:;<=>?[\\]^_`{|}~\r\n"; //@-
			s += "， 　，《。》、？；：‘’“”【｛】｝——=+、｜·～！￥%……&*（）"; //@-#
			s += "｀～！＠￥％……—×（）——＋－＝【】｛｝：；’＇”＂，．／＜＞？’‘”“";//＃
			set = new SortedSet<char> ();
			foreach (char c in s) {
				set.Add (c);
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
			return c == '-' || c == '#';
		}

		public char[] clear (String str)
		{
			char[] cs = (str + " ").ToLower ().ToCharArray ();
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
				if ((ps [i].Position + ps [i].KWord.Length) < end) {
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

	public class KeyWord
	{

		public readonly static int MAX_WORD_LENGTH = 16;

		public static void config (DatabaseConfig c)
		{
			// English Language or Word (max=16)              
			c.EnsureTable<KeyWord> ("E", "K(" + MAX_WORD_LENGTH + ")", "I");

			// Non-English Language or Character
			c.EnsureTable<KeyWord> ("N", "K(1)", "I", "P");

		}
		//Key Word
		public String K;

		[NotColumn]
		public String KWord {
			get{ return K;}
			set{ K = value;}
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
		public bool isWord;
		[NotColumn]
		public KeyWord previous;

		public override String ToString ()
		{
			return K + ", Pos=" + P + ", ID=" + I + " " + (isWord ? "1" : "0");
		}

		public String ToFullString ()
		{
			return (previous != null ? previous.ToFullString () + " -> " : "") + ToString ();
		}
	}
}

