using System;
using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace MauserC96
{
    public class PistolItemModule : ItemModule
    {
        public string bulletItemId = "AOTBullet",
            bulletShellItemId = "AOTBulletShell",
            shootEffectId = "AOTGunShoot",
            shootAnimation = "Shoot";

        public float bulletSpeed = 50;
        public bool hasBulletShell = false;

        public string bulletSpawnerTransform = "BulletSpawner",
            bulletShellSpawnerTransform = "BulletShellSpawner",
            muzzleTransform = "Muzzle";

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
        private Transform _bulletShellSpawner;
        private Transform _muzzle;
        private bool _isShootCoroutineRunning;
        private bool _isSpawnBulletCoroutineRunning;
        private Item _bulletItem;
        private Item _bulletShellItem;
        private PistolItemModule _pistolItemModule;
        private ItemData _bulletData;
        private ItemData _bulletShellData;
        private EffectData _shootEffectData;
        private Animation _animation;
        private RagdollHand _ragdollHand;

        private void Start()
        {
            _pistolItem = GetComponent<Item>();
            _pistolItemModule = _pistolItem.data.GetModule<PistolItemModule>();

            _animation = _pistolItem.GetComponentInChildren<Animation>();

            LoadCustomTransforms();
            RegisterEvents();
            LoadCatalogData();
        }

        private void LoadCustomTransforms()
        {
            _bulletSpawner = _pistolItem.gameObject.transform.Find(_pistolItemModule.bulletSpawnerTransform);
            if (_pistolItemModule.hasBulletShell)
            {
                _bulletShellSpawner =
                    _pistolItem.gameObject.transform.Find(_pistolItemModule.bulletShellSpawnerTransform);
            }

            _muzzle = _pistolItem.gameObject.transform.Find(_pistolItemModule.muzzleTransform);
        }

        private void RegisterEvents()
        {
            _pistolItem.OnHeldActionEvent += PistolItemOnOnHeldActionEvent;
            _pistolItem.OnGrabEvent += PistolItemOnOnGrabEvent;
        }

        private void PistolItemOnOnGrabEvent(Handle handle, RagdollHand ragdollhand)
        {
            _ragdollHand = ragdollhand;
        }

        private void LoadCatalogData()
        {
            _bulletData = Catalog.GetData<ItemData>(_pistolItemModule.bulletItemId);
            _bulletShellData = Catalog.GetData<ItemData>(_pistolItemModule.bulletShellItemId);
            _shootEffectData = Catalog.GetData<EffectData>(_pistolItemModule.shootEffectId);
        }

        private void PistolItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
        {
            _ragdollHand = ragdollhand;
            if (action == Interactable.Action.UseStart)
            {
                StartCoroutine(Shoot());
            }
        }

        private IEnumerator HandleItem(Item item, RaycastHit hitInfo)
        {
            if (_bulletItem == null)
                yield return SpawnBulletCoroutine();

            //wait for the bullet to spawn
            while (_bulletItem == null)
            {
                yield return new WaitForFixedUpdate();
            }

            var impactVelocity = 20 * (hitInfo.point - _muzzle.position).normalized;
            item.rb.AddForce(impactVelocity, ForceMode.Impulse);

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

                if (_bulletItem == null || _pistolItemModule.hasBulletShell && _bulletShellItem == null)
                    yield return SpawnBulletCoroutine();

                var effect = _shootEffectData.Spawn(_muzzle.position, _muzzle.rotation);
                effect.Play();

                _animation.Play(_pistolItemModule.shootAnimation);
                if (_ragdollHand != null)
                    PlayerControl.GetHand(_ragdollHand.side).HapticPlayClip(Catalog.gameData.haptics.hit);

                //shoot bullet
                _bulletItem.transform.SetParent(null, true);
                _bulletItem.rb.isKinematic = false;
                _bulletItem.SetColliderAndMeshLayer(GameManager.GetLayer(LayerName.MovingObject));
                _bulletItem.rb.detectCollisions = true;
                _bulletItem.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _bulletItem.disallowDespawn = false;
                try
                {
                    _bulletItem.imbues[0].Transfer(_pistolItem.imbues[0].spellCastBase, _pistolItem.imbues[0].energy);
                }
                catch (Exception exception)
                {
                    //ignored
                }
                _bulletItem.rb.AddForce(_pistolItemModule.bulletSpeed * _bulletItem.flyDirRef.forward,
                    ForceMode.Impulse);
                _bulletItem.isThrowed = true;
                _bulletItem.isFlying = true;

                //eject shell
                _bulletShellItem.transform.SetParent(null, true);
                _bulletShellItem.rb.isKinematic = false;
                _bulletShellItem.rb.AddForce(5f * _bulletShellSpawner.up, ForceMode.Impulse);
                _bulletShellItem.disallowDespawn = false;

                _bulletItem = null;
                _bulletShellItem = null;
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
                    resultItem.transform.SetParent(_pistolItem.transform, true);
                    resultItem.rb.isKinematic = true;
                    resultItem.disallowDespawn = true;
                    resultItem.rb.detectCollisions = false;

                    //invisible
                    resultItem.Hide(true);

                    _bulletItem = resultItem;
                });

                if (_pistolItemModule.hasBulletShell)
                {
                    _bulletShellData.SpawnAsync(resultItem =>
                    {
                        resultItem.IgnoreObjectCollision(_pistolItem);
                        resultItem.transform.position = _bulletShellSpawner.transform.position;
                        resultItem.transform.rotation = _bulletShellSpawner.transform.rotation;
                        resultItem.transform.SetParent(_pistolItem.transform, true);
                        resultItem.rb.isKinematic = true;
                        resultItem.disallowDespawn = true;
                        resultItem.rb.detectCollisions = false;

                        _bulletShellItem = resultItem;
                    });
                }

                while (_bulletItem == null || _pistolItemModule.hasBulletShell && _bulletShellItem == null)
                {
                    yield return new WaitForFixedUpdate();
                }

                _isSpawnBulletCoroutineRunning = false;

                yield return null;
            }
        }
    }
}