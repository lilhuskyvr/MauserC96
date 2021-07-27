using ThunderRoad;
using UnityEngine;

namespace MauserC96
{
    public class PistolItemData : ItemData
    {
        public float damage = 50;
        public float range = 100;
        public float force = 300;

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            Debug.Log("Pistol Item Data" + damage + range + force);
        }
    }
}