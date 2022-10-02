﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipliz;

namespace Compass
{
    static class ExtendDictionary
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            return defaultValue;
        }

        public static Vector3Int Vector3Int(this Vector3Int vector, string asd)
        {

            return new Vector3Int();
        }
    }
}
