using ThunderRoad;

namespace MauserC96
{
    public class PistolItemModule : ItemModule
    {
        public bool overridePistolData = false;
        public float range = 100;
        public float damage = 50;
        public float force = 300;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            item.gameObject.AddComponent<Pistol>();
        }
    }
}