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
                    SerializedCollectibleDB = Collectibles.Serialize(),
                };
            }

            set {
                Collectibles.Clear();
                Collectibles.AddSerializedData(
                    ((MySaveData)value).SerializedCollectibleDB);
            }
        }

        CollectibleDB Collectibles = new CollectibleDB();

        public override string GetVersion() => "0.1.0";

        public SillyKnight() : base("Silly Knight") {
            SillyKnight.Instance = this;
        }

        public override void Initialize() {
            base.Initialize();

            Modding.ModHooks.Instance.OnEnableEnemyHook += HandleEnemyEnabled;

            // This is called _immediately_ after the game objects in the scene
            // are instantiated. Which means we can get their exact spawn
            // positions!
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                HandleSceneChanged;
        }

        private void HandleSceneChanged(UnityEngine.SceneManagement.Scene _, UnityEngine.SceneManagement.LoadSceneMode _1)
        {
            try {
                foreach (GameObject gameObject in
                         UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                    MaybeAttachKey(gameObject);
                }
            } catch (System.Exception e) {
                LogException("Error in HandleSceneChanged", e);
            }
        }

        private void MaybeAttachKey(GameObject gameObject) {
            if (gameObject.GetComponent<Nametag>() == null) {
                Nametag nametag = gameObject.AddComponent<Nametag>();

                // This uses the _current_ position of the gameObject, so we
                // have to be careful to call this function right when the game
                // object is created so we can get its spawn position.
                nametag.Key = CollectibleKey.FromGameObject(gameObject);
            }
        }

        private GameObject HandleObjectCreated(GameObject gameObject) {
            try {
                AttachKey(gameObject);
            } catch (System.Exception e) {
                LogException("Error in HandleObjectCreated", e);
            }

            return gameObject;
        }

        private CollectibleKey GetKey(GameObject gameObject) {
            Nametag nametag = gameObject.GetComponent<Nametag>();
            if (nametag != null) {
                return nametag.Key;
            }

            throw new ArgumentException($"{gameObject} does not have nametag");
        }

        private bool HandleEnemyEnabled(GameObject gameObject, bool isDead) {
            try {
                HealthManager healthManager =
                    gameObject.GetComponent<HealthManager>();
                if (isDead || healthManager == null) {
                    return isDead;
                }

                CollectibleKey key = GetKey(gameObject);
                CollectibleState? state = Collectibles.TryGet(key);
                if (state == null || state == CollectibleState.Uncollected) {
                    healthManager.OnDeath += () => Collectibles.TrySet(
                        key,
                        CollectibleState.Collected);
                } else if (state == CollectibleState.Collected) {
                    healthManager.SetGeoLarge(0);
                    healthManager.SetGeoMedium(0);
                    healthManager.SetGeoSmall(0);
                }
            } catch (System.Exception e) {
                LogException("Error in HandleEnemyEnabled", e);
            }

            return isDead;
        }

        private static string IndentString(string str, string indent = "... ") {
            return indent + str.Replace("\n", "\n" + indent);
        }

        public void LogException(string heading, System.Exception error) {
            LogError($"{heading}\n{IndentString(error.ToString())}");
        }
    }
}
