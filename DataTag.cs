using System;
using System.Collections.Generic;
using UnityEngine;

namespace SillyKnight {
    // Attach arbitrary data to a game object: as seen on tv.
    // TODO: This needs to detach from geo when its returned to the pool (or
    //       otherwise clear itself before/when its added to a scene)
    class DataTag<T> : MonoBehaviour {
        private Dictionary<string, T> _data;

        public static T GetData(GameObject gameObject, string key) {
            DataTag<T> tag = gameObject?.GetComponent<DataTag<T>>();
            if (tag == null) {
                throw new ArgumentException(
                    $"gameObject ({gameObject}) does not have a "
                    $"{DataTag<T>.ToString()}");
            }

            if (tag.TryGetValue(key, out T value)) {
                return value;
            } else {
                throw new ArgumentException(
                    $"gameObject does not have data under key {key}");
            }
        }

        public static bool TryAttach(GameObject gameObject, string key, T value) {
            if (gameObject.GetComponent<DataTag<T>>() == null) {
                DataTag<T> tag = gameObject.AddComponent<DataTag<T>>();
                tag._data.Add(key, value);
                return true;
            } else {
                return false;
            }
        }

        public static void AttachOrSet(GameObject gameObject, string key, T value) {
            if (gameObject.GetComponent<DataTag<T>>() is DataTag<T> tag) {
                tag._data[key] = value;
            } else {
                DataTag<T> tag = gameObject.AddComponent<DataTag<T>>();
                tag._data.Add(key, value);
            }
        }
    }
}
