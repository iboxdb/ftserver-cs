using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using iBoxDB.LocalServer;

namespace FTServer
{
	public class MainClass
	{

		public static void test_main ()
		{ 
			//System.Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/ftsdata/"
			DB.Root ("/tmp/");
			iBoxDB.DBDebug.DDebug.DeleteDBFiles(1);
			DB db = new DB (1);

			String[] ts = new String[] {
				//ID=0
				"Setting up Git\n"
				+ "\n"
				+ "Download and install the latest version of GitHub Desktop. "
				+ "This will automatically install Git and keep it up-to-date for you.\n"
				+ "On your computer, open the Git Shell application.\n"
				+ "Tell Git your name so your commits will be properly labeled. Type everything after the $ here:\n"
				+ "\n"
				+ "git config --global user.name \"YOUR NAME\"\n"
				+ "Tell Git the email address that will be associated with your Git commits. "
				+ "The email you specify should be the same one found in your email settings. "
				+ "To keep your email address hidden,"
				+ " 关于 see \"Keeping your C# Java NoSQL email address abc@global.com private\".",
				//ID=1
				"关于版本控制\n"
				+ "什么是“版本控制”？我为什么要关心它呢？ 版本控制是一种记录一个或若干文件内容变化，"
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
				+ "什麼是版本控制？ 以及為什麼讀者會在意它？ "
				+ "版本控制是一個能夠記錄一個或一組檔案在某一段時間的變更，"
				+ "使得讀者以後能取回特定版本的系統。 NoSQL"
				+ "在本書的範例中，讀者會學到如何對軟體的原始碼做版本控制。"
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
				+ "经验教训，开发出自己的版本系统。 他们对新的系统制订了若干目标："
			};

			Engine engine = new Engine ();
			engine.Config (db.GetConfig ().DBConfig);

			AutoBox auto = db.Open ();

			for (int i = 0; i < ts.Length; i++) {
				using (var box = auto.Cube()) {
					engine.indexText (box, i, ts [i], false);
					box.Commit ().Assert ();
				}
			}
			using (var box = auto.Cube()) {
				//engine.indexText(box, 4, ts[4], true);
				box.Commit ().Assert ();
			}

			using (var box = auto.Cube()) {				 
				foreach (KeyWord kw in engine.searchDistinct(box, "版本 C it ")) {
					Console.WriteLine (kw.ToFullString ());
					Console.WriteLine (engine.getDesc (ts [(int)kw.ID], kw, 100));
				}
			}

		}
	}

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

