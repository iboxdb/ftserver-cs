using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

using IBoxDB.LocalServer;


namespace FTServer
{

    //Ported from
    internal static class CSharpBridge
    {
        public static String toString<T>(this T o)
        {
            return o.ToString();
        }
        public static bool equals<T>(this T o, T o2)
        {
            return o.Equals(o2);
        }
        public static StringBuilder append(this StringBuilder self, char str)
        {
            return self.Append(str);
        }
        public static StringBuilder append(this StringBuilder self, string str)
        {
            return self.Append(str);
        }

        public static int length(this StringBuilder self)
        {
            return self.Length;
        }

        public static char charAt(this StringBuilder self, int pos)
        {
            return self[pos];
        }

        public static void insert(this StringBuilder self, int pos, char c)
        {
            self.Insert(pos, c);
        }
        public static bool isEmpty(this String self)
        {
            return self.Length == 0;
        }
        public static bool contains(this String self, string s)
        {
            return self.Contains(s);
        }
        public static int length(this String self)
        {
            return self.Length;
        }

        public static string[] split(this String self, String s)
        {
            return self.Split(s);
        }

        public static char[] toCharArray(this String self)
        {
            return self.ToCharArray();
        }

        public static string toLowerCase(this String self)
        {
            return self.ToLower();
        }

        public static string substring(this String self, int start, int end)
        {
            return self.Substring(start, end - start);
        }
        public static string substring(this String self, int start)
        {
            return self.Substring(start);
        }
        public static string trim(this string self)
        {
            return self.Trim();
        }
        public static char charAt(this string self, int index)
        {
            return self[index];
        }
        public static int lastIndexOf(this string self, char c, int index)
        {
            return self.LastIndexOf(c, index);
        }
        public static int nextInt(this Random self, int value)
        {
            return self.Next(value);
        }

        public static bool add<T>(this HashSet<T> self, T v)
        {
            return self.Add(v);
        }
        public static bool remove<T>(this HashSet<T> self, T v)
        {
            return self.Remove(v);
        }
        public static bool contains<T>(this HashSet<T> self, T v)
        {
            return self.Contains(v);
        }

        public static void remove<T>(this ArrayList<T> self, int pos)
        {
            self.RemoveAt(pos);
        }

        public static int size<T>(this ArrayList<T> self)
        {
            return self.Count;
        }

        public static T[] toArray<T>(this ArrayList<T> self)
        {
            return self.ToArray();
        }

        public static T get<T>(this ArrayList<T> self, int pos)
        {
            return self[pos];
        }

        public static void add<T>(this ConcurrentQueue<T> self, T obj)
        {
            self.Enqueue(obj);
        }
        public static void remove<T>(this ConcurrentQueue<T> self)
        {
            T o;
            self.TryDequeue(out o);
        }
        public static int size<T>(this ConcurrentQueue<T> self)
        {
            return self.Count;
        }

    }

    internal class ArrayList<T> : List<T>
    {
        public bool isEmpty()
        {
            return this.Count == 0;
        }

        public void add(T t)
        {
            this.Add(t);
        }
        public void addAll(IEnumerable<T> t)
        {
            base.AddRange(t);
        }
        public void add(int index, T p)
        {
            this.Insert(index, p);
        }
        public bool contains(T t)
        {
            return this.Contains(t);
        }
    }

    public class LinkedHashSet<T> : SortedSet<T>
    {
        public int size()
        {
            return base.Count;
        }

        public void add(T t)
        {
            base.Add(t);
        }
    }

    internal class EngineIterator<T> : Iterator<T>
    {
    }

    internal class Iterator<T> : IEnumerator<T>
    {
        public delegate bool MoveNextDelegate();

        public delegate T CurrentDelegate();

        public MoveNextDelegate hasNext;
        public CurrentDelegate next;

        public bool MoveNext()
        {
            return hasNext();
        }

        public T Current
        {
            get
            {
                return next();
            }
        }

        void System.Collections.IEnumerator.Reset()
        {

        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return this.Current;
            }
        }

        void IDisposable.Dispose()
        {

        }
    }

    internal class Iterable<T> : IEnumerable<T>
    {


        public IEnumerator<T> GetEnumerator()
        {
            return iterator;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return iterator;
        }

        public EngineIterator<T> iterator;
    }

    internal class Arrays
    {
        public static T[] copyOf<T>(T[] kws, int len)
        {
            T[] condition = new T[len];
            Array.Copy(kws, 0, condition, 0, Math.Min(kws.Length, condition.Length));
            return condition;
        }
    }

}