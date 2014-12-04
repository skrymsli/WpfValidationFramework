using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValidationFramework
{
    internal static class KvPair
    {

        public static KeyValuePair<TKey, TValue> New<TKey, TValue>(TKey key, TValue arg)
        {
            return new KeyValuePair<TKey, TValue>(key, arg);
        }

    }
}
