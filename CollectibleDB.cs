using System;
using System.Collections.Generic;
using UnityEngine;


namespace SillyKnight {
    class CollectibleDB {
        // Maps from scene name to a dictionary mapping from key to state. The
        // seperation of collectibles by scene is done only for query speed,
        // since CollectibleKey has the scene name in it already.
        private Dictionary<string, Dictionary<CollectibleKey, CollectibleState>> _states =
            new Dictionary<string, Dictionary<CollectibleKey, CollectibleState>>();

        public void Clear() {
            _states = new Dictionary<string, Dictionary<CollectibleKey, CollectibleState>>();
        }

        private const string _serializationVersion = "1";

        // Serializes the DB into a single string.
        //
        // HollowKnight doesn't ship with
        // System.Runtime.Serialization.Formatters.dll so I don't think it's
        // safe to use a stdlib serializer... Thus we make our own.
        //
        // Format is simple. It's a series of strings seperated by semicolons.
        // First string is the version of the serialization formatter (in case
        // we need to change the format in a back-incompat way). That's
        // followed by CollectibleKey.NumSerializationTokens number of strings
        // that make up a single key, followed by a single string that holds
        // the state of that collectible, then it repeats for however many
        // states are stored.
        public string Serialize() {
            var parts = new List<string>();

            parts.Add(_serializationVersion);

            foreach (Dictionary<CollectibleKey, CollectibleState> states in _states.Values) {
                foreach (KeyValuePair<CollectibleKey, CollectibleState> kv in states) {
                    parts.AddRange(kv.Key.Serialize());
                    parts.Add(((int)kv.Value).ToString());
                }
            }

            return String.Join(";", parts.ToArray());
        }

        // Adds all the data in serialized. Will not call Clear() first so you
        // may want to... NOTE: will invoke OnStatsChanged a bunch 🤷‍♀️
        public void AddSerializedData(string serialized) {
            if (serialized == null || serialized == "") {
                return;
            }

            string[] parts = serialized.Split(';');

            if (parts[0] != _serializationVersion) {
                throw new ArgumentException(
                    $"Unknown serialization version {parts[0]}. You may " +
                    $"a new version of the mod to load this save file.");
            } else if ((parts.Length - 1) % (CollectibleKey.NumSerializationTokens + 1) != 0) {
                throw new ArgumentException("CollectibleDB in save data is corrupt");
            }

            string[] keyParts = new string[CollectibleKey.NumSerializationTokens];
            for (int i = 1; i < parts.Length; i += CollectibleKey.NumSerializationTokens + 1) {
                // Copy just the parts for a single key into keyParts
                Array.Copy(
                    parts, i,
                    keyParts, 0,
                    CollectibleKey.NumSerializationTokens);
                CollectibleKey k = CollectibleKey.Deserialize(keyParts);

                // Convert the one CollectibleState part into a CollectibleState
                CollectibleState state = (CollectibleState)int.Parse(
                    parts[i + CollectibleKey.NumSerializationTokens]);

                TrySet(k, state);
            }
        }

        public bool TrySet(CollectibleKey k, CollectibleState newState) {
            if (!_states.ContainsKey(k.SceneName)) {
                _states.Add(
                    k.SceneName,
                    new Dictionary<CollectibleKey, CollectibleState>());
            }

            CollectibleState? oldState = null;
            if (_states[k.SceneName].TryGetValue(
                    k, out CollectibleState state)) {
                oldState = state;
            }

            if (oldState == null || (int)oldState < (int)newState) {
                _states[k.SceneName][k] = newState;

                SillyKnight.Instance.LogDebug(
                    $"Updated state of '{k}' to {newState} (was {oldState})");
                SillyKnight.Instance.LogFine(
                    $"... Serialized key: {String.Join(";", k.Serialize())}");

                return true;
            } else {
                return false;
            }
        }

        public bool Contains(CollectibleKey k) {
            if (_states.TryGetValue(
                    k.SceneName,
                    out Dictionary<CollectibleKey, CollectibleState> sceneStates)) {
                return sceneStates.ContainsKey(k);
            } else {
                return false;
            }
        }

        public CollectibleState? TryGet(CollectibleKey k) {
            if (_states.TryGetValue(
                    k.SceneName,
                    out Dictionary<CollectibleKey, CollectibleState> sceneStates)) {
                if (sceneStates.TryGetValue(k, out CollectibleState state)) {
                    return state;
                }
            }

            return null;
        }
    }
}
