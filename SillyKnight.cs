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

            // This is called _immediately_ after the game objects in the scene
            // are instantiated. Perfect time to grab their spawn positions for
            // their nametags.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded +=
                HandleSceneChanged;

            Modding.ModHooks.Instance.OnEnableEnemyHook += HandleEnemyEnabled;

            // Die is in the callstack of OnEnable when geo is flung from
            // corpses. So we can store the dying enemy in Die, and then grab
            // it in OnEnable, to associate Geo with a particular enemy.
            On.HealthManager.Die += HandleEnemyDeath;
            On.GeoControl.OnEnable += HandleGeoSpawned;

            On.GeoControl.Getter += GeoControl_Getter;
            On.GeoControl.Disable += GeoControl_Disable;
            On.GeoControl.PlayCollectSound += GeoControl_PlayCollectSound;
            On.HeroController.AddGeo += HeroController_AddGeo;
            On.HeroController.AddGeoQuietly += HeroController_AddGeoQuietly;
        }

        private void HeroController_AddGeoQuietly(On.HeroController.orig_AddGeoQuietly orig, HeroController self, int amount)
        {
            Log("Stack trace in HeroController_AddGeoQuietly:");
            Log(IndentString(System.Environment.StackTrace));

            orig(self, amount);
        }

        private void HeroController_AddGeo(On.HeroController.orig_AddGeo orig, HeroController self, int amount)
        {
            Log("Stack trace in HeroController_AddGeo:");
            Log(IndentString(System.Environment.StackTrace));

            orig(self, amount);
        }

        private float GeoControl_PlayCollectSound(On.GeoControl.orig_PlayCollectSound orig, GeoControl self)
        {
            Log("Stack trace in GeoControl_PlayCollectSound:");
            Log(IndentString(System.Environment.StackTrace));

            return orig(self);
        }

        private void GeoControl_Disable(On.GeoControl.orig_Disable orig, GeoControl self, float waitTime)
        {
            Log("Stack trace in GeoControl_Disable:");
            Log(IndentString(System.Environment.StackTrace));

            orig(self, waitTime);
        }

        private IEnumerator GeoControl_Getter(On.GeoControl.orig_Getter orig, GeoControl self)
        {
            Log("Stack trace in GeoControl_Getter:");
            Log(IndentString(System.Environment.StackTrace));

            return orig(self);
        }

        private void HandleGeoSpawned(On.GeoControl.orig_OnEnable orig, GeoControl self)
        {
            try {
                CollectibleKey spawnedBy = KeyBox.GetKey();
                if (self.gameObject.GetComponent<SpawnedByHolder>() == null) {
                    SpawnedByHolder link =
                        self.gameObject.AddComponent<SpawnedByHolder>();
                    link.SpawnedBy = spawnedBy;
                }
            } catch (System.Exception e) {
                LogException("Error in HandleGeoSpawned", e);
            }
        }

        private void HandleEnemyDeath(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            try {
                KeyBox box = new KeyBox(GetKeyFromNametag(self.gameObject));
                try {
                    orig(self, attackDirection, attackType, ignoreEvasion);
                } finally {
                    box.Dispose();
                }
            } catch (System.Exception e) {
                LogException("Error in HandleEnemyDeath", e);
            }
        }

        private void HandleSceneChanged(UnityEngine.SceneManagement.Scene _, UnityEngine.SceneManagement.LoadSceneMode _1)
        {
            try {
                foreach (GameObject gameObject in
                         UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                    // Nametags will be attached to lots of non-enemy objects,
                    // but they won't do any harm.
                    MaybeAttachNametag(gameObject);
                }
            } catch (System.Exception e) {
                LogException("Error in HandleSceneChanged", e);
            }
        }

        private void MaybeAttachNametag(GameObject gameObject) {
            if (gameObject.GetComponent<Nametag>() == null) {
                Nametag nametag = gameObject.AddComponent<Nametag>();

                // This uses the _current_ position of the gameObject, so we
                // have to be careful to call this function right when the game
                // object is created so we can get its spawn position.
                nametag.Key = CollectibleKey.FromGameObject(gameObject);
            }
        }

        private CollectibleKey GetKeyFromNametag(GameObject gameObject) {
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
                if (healthManager == null) {
                    return isDead;
                }

                CollectibleKey key = GetKeyFromNametag(gameObject);
                if (Collectibles.TryGet(key) == CollectibleState.Collected) {
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
