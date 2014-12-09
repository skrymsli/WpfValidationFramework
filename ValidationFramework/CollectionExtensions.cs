using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ValidationFramework
{
    internal static class CollectionExtensions
    {

        public static IEnumerable<KeyValuePair<TValue, IEnumerable<TKey>>> Inverse<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> This)
        {
            var inverse = new Dictionary<TValue, List<TKey>>();
            // I'm stupid, so I am writing this the stupid way
            foreach (var kv in This.SelectMany(kv => ToKeyValueList(kv.Key, kv.Value)))
            {
                if (!inverse.ContainsKey(kv.Value))
                {
                    inverse[kv.Value] = new List<TKey>();
                }
                inverse[kv.Value].Add(kv.Key);
            }

            return inverse.Select(kv => new KeyValuePair<TValue, IEnumerable<TKey>>(kv.Key, kv.Value));
        }

        private static List<KeyValuePair<TKey, TValue>> ToKeyValueList<TKey, TValue>(TKey key, IEnumerable<TValue> value)
        {
            return value.Select(v => new KeyValuePair<TKey, TValue>(key, v)).ToList();
        }

        public static void AddAll<T>(this IList This, IEnumerable<T> values)
        {
            if (values == null) return;

            foreach (var item in values)
            {
                if (!This.Contains(item)) This.Add(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence)
            {
                action(item);
            }
        }

        public static void PForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            sequence.AsParallel().ForAll(action);
        }

        
    }
}

