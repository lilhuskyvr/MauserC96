using System;
using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace MauserC96
{
    public class PistolItemModule : ItemModule
    {
        public string bulletItemId = "AOTBullet", muzzleEffectId;
        public float bulletSpeed = 100;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            item.gameObject.AddComponent<Pistol>();
        }
    }

    public class Pistol : MonoBehaviour
    {
        private Item _pistolItem;
        private Transform _bulletSpawner;
        private Transform _shellSpawner;
        private Transform _muzzle;
        private bool _isShootCoroutineRunning;
        private bool _isSpawnBulletCoroutineRunning;
        private Item _bulletItem;
        private PistolItemModule _pistolItemModule;
        private ItemData _bulletData;
        private EffectData _muzzleEffectData;

        private void Start()
        {
            _pistolItem = GetComponent<Item>();
            _pistolItemModule = _pistolItem.data.GetModule<PistolItemModule>();

            LoadCustomReferences();
            RegisterEvents();
            LoadCatalogData();
        }

        private void LoadCustomReferences()
        {
            _bulletSpawner = _pistolItem.GetCustomReference("BulletSpawner");
            _shellSpawner = _pistolItem.GetCustomReference("ShellSpawner");
            _muzzle = _pistolItem.GetCustomReference("Muzzle");
        }

        private void RegisterEvents()
        {
            _pistolItem.OnHeldActionEvent += PistolItemOnOnHeldActionEvent;
        }

        private void LoadCatalogData()
        {
            _bulletData = Catalog.GetData<ItemData>(_pistolItemModule.bulletItemId);
            _muzzleEffectData = Catalog.GetData<EffectData>(_pistolItemModule.muzzleEffectId);
        }

        private void PistolItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
        {
            StartCoroutine(SpawnBulletCoroutine());
            if (action == Interactable.Action.UseStart)
            {
                StartCoroutine(Shoot());
            }
        }

        public IEnumerator Shoot()
        {
            if (_isShootCoroutineRunning)
            {
                Debug.Log("Shoot is running");
                yield return null;
            }
            else
            {
                if (_bulletItem == null)
                    yield return SpawnBulletCoroutine();

                //wait for the bullet to spawn
                while (_bulletItem == null)
                {
                    yield return new WaitForFixedUpdate();
                }
                
                _isShootCoroutineRunning = true;

                _bulletItem.transform.SetParent(null, true);
                _bulletItem.transform.localScale = _pistolItem.transform.localScale;
                _bulletItem.rb.isKinematic = false;
                _bulletItem.SetColliderAndMeshLayer(GameManager.GetLayer(LayerName.MovingObject));
                _bulletItem.rb.detectCollisions = true;
                _bulletItem.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                // //add smoke
                // var smokeEffect = _smokeData.Spawn(_bulletItem.transform.Find("TailIndicator"));
                // smokeEffect.SetIntensity(10);
                // smokeEffect.Play();
                //missile start flying
                _bulletItem.rb.AddForce(_pistolItemModule.bulletSpeed * _bulletItem.flyDirRef.forward,
                    ForceMode.Impulse);
                _bulletItem.isThrowed = true;
                _bulletItem.isFlying = true;
                _bulletItem.disallowDespawn = false;
                //forget it
                _bulletItem = null;
                _isShootCoroutineRunning = false;
                yield return null;
            }
        }

        public IEnumerator SpawnBulletCoroutine()
        {
            if (_isSpawnBulletCoroutineRunning || _bulletItem != null)
            {
                yield return null;
            }
            else
            {
                Debug.Log("Start Spawning Bullet");
                _isSpawnBulletCoroutineRunning = true;
                //spawn bullet
                _bulletData.SpawnAsync(resultItem =>
                {
                    resultItem.disallowDespawn = true;
                    resultItem.IgnoreObjectCollision(_pistolItem);
                    resultItem.rb.isKinematic = true;
                    resultItem.transform.position = _bulletSpawner.transform.position;
                    resultItem.transform.rotation = _bulletSpawner.transform.rotation;
                    resultItem.transform.SetParent(_bulletSpawner, true);
                    _bulletItem = resultItem;
                    Debug.Log("Spawned bullet");
                });

                while (_bulletItem == null)
                {
                    yield return new WaitForFixedUpdate();
                }

                _isSpawnBulletCoroutineRunning = false;

                yield return null;
            }
        }
    }
}