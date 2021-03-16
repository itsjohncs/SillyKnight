using System;
using System.Collections.Generic;
using UnityEngine;

namespace SillyKnight
{
    // A play-on-words on ThreadLocalStorage but this isn't actually
    // thread-safe and is really just a dressed-up global.
    class StackStorage<T> : IDisposable {
        private static Dictionary<string, T> _values;

        public static T GetValue(string key) {
            if (_values.TryGetValue(key, out T value)) {
                return value;
            } else {
                throw new ArgumentException($"No value stored with key {key}");
            }
        }

        public string Key { get; private set; };

        public StackStorage(string key, T value) {
            if (_values.ContainsKey(key)) {
                throw new ArgumentException(
                    $"Value already stored with key {key}");
            } else {
                _values.Add(key, value);
                Key = key;
            }
        }

        public void Dispose() {
            _values.Remove(Key);
        }
    }
}
