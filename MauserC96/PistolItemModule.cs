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
            if (action == Interactable.Action.UseStart)
            {
                StartCoroutine(Shoot());
            }
        }

        private IEnumerator HandleRagdollPart(RagdollPart ragdollPart, RaycastHit hitInfo)
        {
            var collisionIndex = ragdollPart.collisionHandler.GetFreeCollisionIndex();
            if (collisionIndex != -1)
            {
                if (_bulletItem == null)
                    yield return SpawnBulletCoroutine();

                //wait for the bullet to spawn
                while (_bulletItem == null)
                {
                    yield return new WaitForFixedUpdate();
                }

                var impactVelocity = 20 * (hitInfo.point - _muzzle.position).normalized;
                ragdollPart.collisionHandler.collisions[collisionIndex].NewHit(
                    _bulletItem.colliderGroups[0].colliders[0],
                    ragdollPart.colliderGroup.colliders[0],
                    _bulletItem.colliderGroups[0],
                    ragdollPart.colliderGroup,
                    impactVelocity,
                    hitInfo.point,
                    hitInfo.normal,
                    Mathf.InverseLerp(Catalog.gameData.collisionEnterVelocityRange.x,
                        Catalog.gameData.collisionEnterVelocityRange.y, impactVelocity.magnitude),
                    Catalog.GetData<MaterialData>("Blade"),
                    Catalog.GetData<MaterialData>("Flesh")
                );
            }

            yield return null;
        }

        private IEnumerator HandleItem(Item item, RaycastHit hitInfo)
        {
            var collisionIndex = item.mainCollisionHandler.GetFreeCollisionIndex();
            if (collisionIndex != -1)
            {
                if (_bulletItem == null)
                    yield return SpawnBulletCoroutine();

                //wait for the bullet to spawn
                while (_bulletItem == null)
                {
                    yield return new WaitForFixedUpdate();
                }

                var impactVelocity = 20 * (hitInfo.point - _muzzle.position).normalized;
                item.mainCollisionHandler.collisions[collisionIndex].NewHit(
                    _bulletItem.colliderGroups[0].colliders[0],
                    item.colliderGroups[0].colliders[0],
                    _bulletItem.colliderGroups[0],
                    item.colliderGroups[0],
                    impactVelocity,
                    hitInfo.point,
                    hitInfo.normal,
                    Mathf.InverseLerp(Catalog.gameData.collisionEnterVelocityRange.x,
                        Catalog.gameData.collisionEnterVelocityRange.y, impactVelocity.magnitude),
                    Catalog.GetData<MaterialData>("Blade"),
                    Catalog.GetData<MaterialData>("Plate")
                );
            }

            yield return null;
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
                _isShootCoroutineRunning = true;

                var hitInfo = new RaycastHit();
                Physics.Raycast(new Ray(_muzzle.transform.position,
                        _muzzle.forward
                    ), out hitInfo, 50,
                    (1 << 27 | 1 << 0 | 1 << 10 | 1 << 9 | 1 << 13)
                );

                Debug.Log("layer");
                Debug.Log(hitInfo.transform.gameObject.layer);
                if (hitInfo.rigidbody != null)
                {
                    Debug.Log("Has Rigidbody");
                    var ragdollPart = hitInfo.rigidbody.GetComponentInParent<RagdollPart>();

                    if (ragdollPart != null)
                    {
                        Debug.Log("Handle ragdoll part");
                        yield return HandleRagdollPart(ragdollPart, hitInfo);
                    }
                    else
                    {
                        var item = hitInfo.rigidbody.GetComponentInParent<Item>();

                        if (item != null)
                        {
                            Debug.Log("handle item");
                            yield return HandleItem(item, hitInfo);
                        }
                    }
                }

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
                _isSpawnBulletCoroutineRunning = true;
                //spawn bullet
                _bulletData.SpawnAsync(resultItem =>
                {
                    resultItem.IgnoreObjectCollision(_pistolItem);
                    resultItem.transform.position = _bulletSpawner.transform.position;
                    resultItem.transform.rotation = _bulletSpawner.transform.rotation;
                    resultItem.rb.AddForce(0.5f*Vector3.up, ForceMode.Impulse);
                    _bulletItem = resultItem;
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