using System;
using System.Text;
using UnityEngine;


namespace SillyKnight {
    readonly struct CollectibleKey {
        public readonly string SceneName;
        public readonly string ObjectName;
        public readonly Vector2 SpawnPosition;

        public CollectibleKey(string sceneName, string objectName, Vector2 spawnPosition) {
            SceneName = sceneName;
            ObjectName = objectName;
            SpawnPosition = spawnPosition;
        }

        public static CollectibleKey FromGameObject(GameObject gameObject) {
            return new CollectibleKey(
                gameObject.scene.name,
                gameObject.name,
                // gameObject.position.z is being discarded here. The explicit
                // cast is added for clarity but is unnecessary.
                (Vector2)gameObject.transform.position);
        }

        private (string, string, Vector2) ToTuple() {
            return (SceneName, ObjectName, SpawnPosition);
        }

        public override int GetHashCode() {
            return ToTuple().GetHashCode();
        }

        public override bool Equals(object other) {
            if (other is CollectibleKey otherKey) {
                return ToTuple().Equals(otherKey.ToTuple());
            } else {
                return false;
            }
        }

        public static bool operator ==(CollectibleKey a, CollectibleKey b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(CollectibleKey a, CollectibleKey b)
        {
            return !a.Equals(b);
        }

        public override string ToString() {
            return $"{SceneName}/{ObjectName} ({SpawnPosition.x}, {SpawnPosition.y})";
        }

        // The size of the arrays Serialize returns and Deserialize expects
        public const int NumSerializationTokens = 4;

        // Encodes into UTF-16 (which should be a no-op since that's how
        // strings are backed) and then converts to Base64. In the Remarks
        // section of https://docs.microsoft.com/en-us/dotnet/api/system.convert.tobase64string?view=net-5.0
        // it describes the alphabet used. Notably does not include `;`.
        private static string ToBase64(string str) {
            return Convert.ToBase64String(
                // Read "Unicode" as UTF-16
                Encoding.Unicode.GetBytes(str));
        }

        // Decodes a base 64 string into what should be valid UTF-16 which we
        // then convert to a string (which should be a no-op for the same
        // reason as above).
        private static string StringFromBase64(string str) {
            // Read "Unicode" as UTF-16
            return Encoding.Unicode.GetString(
                Convert.FromBase64String(str));
        }

        private static string ToBase64(float num) {
            byte[] bytes = BitConverter.GetBytes(num);
            if (BitConverter.IsLittleEndian) {
                // Ensure the bytes are in big-endian order
                Array.Reverse(bytes);
            }

            return Convert.ToBase64String(bytes);
        }

        private static float FloatFromBase64(string str) {
            byte[] bytes = Convert.FromBase64String(str);
            if (BitConverter.IsLittleEndian) {
                // The serialized bytes are always big endian, so we gotta flip
                // them back if we're on a little endian machine
                Array.Reverse(bytes);
            }

            return BitConverter.ToSingle(bytes, 0);
        }

        public string[] Serialize() {
            return new string[] {
                ToBase64(SceneName),
                ToBase64(ObjectName),
                ToBase64(SpawnPosition.x),
                ToBase64(SpawnPosition.y),
            };
        }

        public static CollectibleKey Deserialize(string[] serialized) {
            if (serialized.Length != NumSerializationTokens) {
                throw new ArgumentException(
                    $"Got {serialized.Length} tokens for " +
                    $"CollectibleKey.Deserialize. Expected " +
                    $"{NumSerializationTokens}.");
            }

            return new CollectibleKey(
                StringFromBase64(serialized[0]),
                StringFromBase64(serialized[1]),
                new Vector2(
                    FloatFromBase64(serialized[2]),
                    FloatFromBase64(serialized[3])));
        }

        public static CollectibleKey FromSerializedString(string serialized) {
            return Deserialize(serialized.Split(';'));
        }
    }
}
