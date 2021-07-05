using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IBoxDB.LocalServer;

namespace FTServer
{
    public class ConcurrentLinkedDeque<T> where T : class
    {

        LinkedList<T> list = new LinkedList<T>();

        public int size()
        {
            lock (this)
            {
                return list.Count;
            }
        }
        public T pollFirst()
        {
            lock (this)
            {
                if (list.Count > 0)
                {
                    T f = list.First.Value;
                    list.RemoveFirst();
                    return f;
                }
                return default(T);
            }
        }

        public void addFirst(T t)
        {
            lock (this)
            {
                list.AddFirst(t);
            }
        }

        public void addLast(T t)
        {
            lock (this)
            {
                list.AddLast(t);
            }
        }
    }

}