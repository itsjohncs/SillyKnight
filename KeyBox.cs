using System;
using UnityEngine;

namespace SillyKnight
{
    class KeyBox : IDisposable {
        private static CollectibleKey _key;
        private static bool _hasValue = false;

        public static CollectibleKey GetKey() {
            if (_hasValue) {
                return _key;
            } else {
                throw new InvalidOperationException("Nothing in box");
            }
        }

        public KeyBox(CollectibleKey key) {
            if (_hasValue) {
                SillyKnight.Instance.LogError(
                    $"Already have key in box (current key is {_key}, " +
                    $"trying to store key {key}).");
            } else {
                _key = key;
                _hasValue = true;
            }
        }

        public void Dispose() {
            _key = default(CollectibleKey);
            _hasValue = false;
        }
    }
}
