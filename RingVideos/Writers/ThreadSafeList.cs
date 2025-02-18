using System;
using System.Collections.Generic;
using System.Collections;

namespace RingVideos.Writers
{
   public class ThreadSafeList<T> : IList<T>
   {
      private readonly List<T> _list = new List<T>();
      private readonly object _lock = new object();

      //public ThreadSafeList(object lockObject)
      //{
      //   _lock = lockObject;
      //}

      public void Add(T item)
      {
         lock (_lock)
         {
            _list.Add(item);
         }
      }

      public bool Remove(T item)
      {
         lock (_lock)
         {
            return _list.Remove(item);
         }
      }

      public int IndexOf(T item)
      {
         lock (_lock)
         {
            return ((IList<T>)_list).IndexOf(item);
         }
      }

      public void Insert(int index, T item)
      {
         lock (_lock)
         {
            ((IList<T>)_list).Insert(index, item);
         }
      }

      public void RemoveAt(int index)
      {
         lock (_lock)
         {
            ((IList<T>)_list).RemoveAt(index);
         }
      }

      public void Clear()
      {
         lock (_lock)
         {
            ((ICollection<T>)_list).Clear();
         }
      }

      public bool Contains(T item)
      {
         lock (_lock)
         {
            return ((ICollection<T>)_list).Contains(item);
         }
      }

      public void CopyTo(T[] array, int arrayIndex)
      {
         lock (_lock)
         {
            ((ICollection<T>)_list).CopyTo(array, arrayIndex);
         }
      }

      public IEnumerator<T> GetEnumerator()
      {
         lock (_lock)
         {
            return ((IEnumerable<T>)_list).GetEnumerator();
         }
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         lock (_lock)
         {
            return ((IEnumerable)_list).GetEnumerator();
         }
      }

      public void ForEach(Action<T> action)
      {
         if (action == null)
         {
            throw new ArgumentNullException("action");
         }
         lock(_lock)
         {
            foreach (T item in _list)
            {
               action(item);
            }
         }
      }
      public T this[int index]
      {
         get
         {
            lock (_lock)
            {
               return _list[index];
            }
         }
         set
         {
            lock (_lock)
            {
               _list[index] = value;
            }
         }
      }

      public int Count
      {
         get
         {
            lock (_lock)
            {
               return _list.Count;
            }
         }
      }

      public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;
   }
}
