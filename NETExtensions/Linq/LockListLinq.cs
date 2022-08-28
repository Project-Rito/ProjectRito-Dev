using System;
using System.Collections.Generic;
using System.Text;
using NETExtensions.Collections.Concurrent;

namespace NETExtensions.Linq
{
    public static class LockListLinq
    {
        public static List<T> FindAll<T>(this LockingList<T> _this, Predicate<T> match)
        {
            List<T> output;
            lock (_this.Mutex)
                output = _this.List.FindAll(match);
            return output;
        }
    }
}
