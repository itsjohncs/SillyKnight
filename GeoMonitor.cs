namespace SillyKnight {
    // Deals with tracking geo collection
    class GeoMonitor {
        public class OnGeoCollectedArgs : EventArgs
        {
            public OnGeoCollectedArgs(CollectibleKey whoSpawnedThisGeo) {
                WhoSpawnedThisGeo = whoSpawnedThisGeo;
            }

            public CollectibleKey WhoSpawnedThisGeo { get; set; }
        }

        // Only triggered if we know who spawned the geo that's collected
        public event EventHandler<OnGeoCollectedArgs> OnGeoCollected;

        public void Initialize() {
            // These two handlers work together to mark geo with which enemy
            // spawned it.
            On.HealthManager.Die += _handleDeathOfOther;
            On.GeoControl.OnEnable += _handleGeoEnable;

            // These two handlers work together to detect when the hero
            // collects geo
            On.GeoControl.OnTriggerEnter2D += _handleGeoCollisionEnter;
            On.HeroController.AddGeo += _handleIncreaseGeoCount;
        }

        private const string WHICH_GEO = "which geo";

        private void _handleGeoCollisionEnter(On.GeoControl.orig_OnTriggerEnter2D orig, GeoControl self, UnityEngine.Collider2D collision)
        {
            try {
                // _handleIncreaseGeoCount doesn't get to know which specific
                // Geo game object is getting collected so we have to use some
                // stack storage to pass it down.
                var stackStorage = new StackStorage<GameObject>(
                    WHICH_GEO,
                    self.gameObject);
                try {
                    orig(self, self, collision);
                } finally {
                    stackStorage.Dispose();
                }
            } catch (System.Exception e) {
                SillyKnight.Instance.LogException(
                    "Error in _handleGeoCollisionEnter", e);
            }
        }

        private void _handleIncreaseGeoCount(On.HeroController.orig_AddGeo orig, HeroController self, int amount)
        {
            try {
                GameObject whichGeo =
                    StackStorage<GameObject>.GetValue(WHICH_GEO);
                CollectibleKey? whoSpawnedThisGeo =
                    DataTag<CollectibleKey>.TryGet(
                        whichGeo,
                        WHO_SPAWNED_THIS_GEO);
                if (whoSpawnedThisGeo is CollectibleKey key) {
                    OnGeoCollected?.Invoke(this, new OnGeoCollectedArgs(key));
                }
            } catch (System.Exception e) {
                SillyKnight.Instance.LogException(
                    "Error in _handleIncreaseGeoCount", e);
            }
        }

        private const string WHO_IS_DYING_KEY = "who is dying";
        private const string WHO_SPAWNED_THIS_GEO = "who spawned this geo";

        // Death of something other than the knight
        private void _handleDeathOfOther(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            try {
                CollectibleKey whoIsDying =
                    DataTag<CollectibleKey>.GetData(
                        self.gameObject, SillyKnight.NAMETAG_KEY);
                var stackStorage = new StackStorage<CollectibleKey>(
                    WHO_IS_DYING_KEY, whoIsDying);
                try {
                    // If this death causes geo to spawn, _handleGeoEnable will
                    // get triggered at some point during orig's execution.
                    orig(self, attackDirection, attackType, ignoreEvasion);
                } finally {
                    stackStorage.Dispose();
                }
            } catch (System.Exception e) {
                SillyKnight.Instance.LogException(
                    "Error in _handleDeathOfOther", e);
            }
        }

        private void _handleGeoEnable(On.GeoControl.orig_OnEnable orig, GeoControl self)
        {
            try {
                CollectibleKey whoIsDying =
                    StackStorage<CollectibleKey>.GetValue(WHO_IS_DYING_KEY);
                DataTag<CollectibleKey>.AttachOrSet(
                    self.gameObject,
                    WHO_SPAWNED_THIS_GEO,
                    whoIsDying)
            } catch (System.Exception e) {
                SillyKnight.Instance.LogException(
                    "Error in _handleGeoEnable", e);
            }
        }
    }
}
