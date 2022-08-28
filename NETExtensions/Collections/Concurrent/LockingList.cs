using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETExtensions.Collections.Concurrent
{
    /// <summary>
    /// Thread-safe list locking on operations. Might be locking on more than it needs to.
    /// </summary>
    public class LockingList<T> : IList<T>
    {
        public List<T> List = new List<T>();
        public Mutex Mutex = new Mutex();

        public int IndexOf(T item)
        {
            int output;
            lock (Mutex)
                output = List.IndexOf(item);
            return output;
        }

        public void Insert(int index, T item)
        {
            lock (Mutex)
                List.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            lock (Mutex)
                List.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                T output;
                lock (Mutex)
                    output = List[index];
                return output;
            }
            set
            {
                lock (Mutex)
                    List[index] = value;
            }
        }

        public void Add(T item)
        {
            lock (Mutex)
                List.Add(item);
        }

        public void Clear()
        {
            lock (Mutex)
                List.Clear();
        }

        public bool Contains(T item)
        {
            bool output;
            lock (Mutex)
                output = List.Contains(item);
            return output;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (Mutex)
                List.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            bool output;
            lock (Mutex)
                output = List.Remove(item);
            return output;
        }

        public int Count
        {
            get
            {
                int output;
                lock (Mutex)
                    output = List.Count;
                return output;
            }
        }

        /// <summary>
        /// Idk but we'll put it at false. Might be class-specific.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        private List<T> Clone()
        {
            List<T> newList = new List<T>();

            lock (Mutex)
            {
                List.ForEach(x => newList.Add(x));
            }

            return newList;
        }
    }
}
