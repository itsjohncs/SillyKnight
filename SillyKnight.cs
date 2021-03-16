using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SillyKnight {
    public class SillyKnight : Modding.Mod {
        public static SillyKnight Instance = null;

        private class MySaveData : Modding.ModSettings {
            public string SerializedCollectibleDB;
        }

        public override Modding.ModSettings SaveSettings {
            get {
                return new MySaveData {
                    SerializedCollectibleDB = _collectibleDB.Serialize(),
                };
            }

            set {
                _collectibleDB.Clear();
                _collectibleDB.AddSerializedData(
                    ((MySaveData)value).SerializedCollectibleDB);
            }
        }

        private CollectibleDB _collectibleDB = new CollectibleDB();
        private GeoMonitor _geoMonitor = new GeoMonitor();

        // We try to attach a CollectibleKey to every object immediately when
        // it spawns using DataTag. This is the key we use to do that.
        public const string NAMETAG_KEY = "nametag";

        public override string GetVersion() => "0.1.0";

        public SillyKnight() : base("Silly Knight") {
            SillyKnight.Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            _geoMonitor.Initialize();
            //_geoMonitor.OnGeoCollected += _handleGeoCollected;

            // This is called immediately after the game objects in the scene
            // are instantiated. Perfect time to grab their spawn positions for
            // their nametags.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                _handleSceneChanged;

            // Prevents the enemy from dropping geo if its been collected
            // already.
            Modding.ModHooks.Instance.OnEnableEnemyHook += _handleEnemyEnabled;
        }

        private void _handleSceneChanged(UnityEngine.SceneManagement.Scene _, UnityEngine.SceneManagement.LoadSceneMode _1)
        {
            try {
                foreach (GameObject gameObject in
                         UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                    DataTag<CollectibleKey>.TryAttach(
                        gameObject,
                        NAMETAG_KEY,
                        CollectibleKey.FromGameObject(gameObject));
                }
            } catch (System.Exception e) {
                LogException("Error in _handleSceneChanged", e);
            }
        }

        private bool _handleEnemyEnabled(GameObject gameObject, bool isDead) {
            try {
                CollectibleKey key = DataTag<CollectibleKey>.GetData(
                    gameObject, NAMETAG_KEY)
                if (_collectibleDB.TryGet(key) == CollectibleState.Collected) {
                    HealthManager healthManager =
                        gameObject.GetComponent<HealthManager>();
                    healthManager.SetGeoLarge(0);
                    healthManager.SetGeoMedium(0);
                    healthManager.SetGeoSmall(0);
                }
            } catch (System.Exception e) {
                LogException("Error in _handleEnemyEnabled", e);
            }

            return isDead;
        }

        private static string _indentString(string str, string indent = "... ") {
            return indent + str.Replace("\n", "\n" + indent);
        }

        public void LogException(string heading, System.Exception error) {
            LogError($"{heading}\n{_indentString(error.ToString())}");
        }
    }
}
