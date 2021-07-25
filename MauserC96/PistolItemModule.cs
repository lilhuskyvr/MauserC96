using System;
using System.Collections;
using System.Collections.Generic;
using RainyReignGames.RevealMask;
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
        public EffectData data = Catalog.GetData<EffectData>("HitBladeDecalFlesh");
        public EffectData bloodHitData = Catalog.GetData<EffectData>("HitRagdollOnFlesh");
        public int moduleIndex = 3;

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
                Shoot();
            }
        }

        public void Shoot()
        {
            StartCoroutine(ShootCoroutine());
        }

        public IEnumerator ShootCoroutine()
        {
            if (_isShootCoroutineRunning)
            {
                Debug.Log("Shoot is running");
                yield break;
            }

            _isShootCoroutineRunning = true;

            var effect = _shootEffectData.Spawn(_muzzle.position, _muzzle.rotation);
            effect.Play();

            _animation.Play(_pistolItemModule.shootAnimation);
            if (_ragdollHand != null)
                PlayerControl.GetHand(_ragdollHand.side).HapticPlayClip(Catalog.gameData.haptics.hit);

            RaycastHit raycastHit = new RaycastHit();
            Ray ray = new Ray(_bulletSpawner.transform.position, _bulletSpawner.transform.forward);
            Physics.Raycast(ray, 
                out raycastHit,
                300, (1 << 27 | 1 << 0 | 1 << 13));

            if (raycastHit.rigidbody == null)
            {
                _isShootCoroutineRunning = false;
                yield break;
            }

            var ragdollPart = raycastHit.rigidbody.GetComponentInParent<RagdollPart>();

            if (ragdollPart == null)
            {
                _isShootCoroutineRunning = false;
                yield break;
            }

            if (ragdollPart.ragdoll.creature.state != Creature.State.Dead)
            {
                ragdollPart.ragdoll.creature.Kill();
            }

            ragdollPart.rb.AddForce(300 * ray.direction, ForceMode.Impulse);

            var effectModuleReveal = data.modules[moduleIndex] as EffectModuleReveal;

            var revealMaterialControllers = new List<RevealMaterialController>();

            foreach (Creature.RendererData renderer in ragdollPart.renderers)
            {
                if ((bool) renderer.revealDecal && (
                    renderer.revealDecal.type == RevealDecal.Type.Default &&
                    effectModuleReveal.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Default)
                    || renderer.revealDecal.type == RevealDecal.Type.Body &&
                    effectModuleReveal.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Body)
                    || renderer.revealDecal.type == RevealDecal.Type.Outfit &&
                    effectModuleReveal.typeFilter.HasFlag(EffectModuleReveal.TypeFilter.Outfit)))
                {
                    revealMaterialControllers.Add(renderer.revealDecal.revealMaterialController);
                    if ((bool) renderer.splitRenderer)
                        revealMaterialControllers.Add(
                            renderer.splitRenderer.GetComponent<RevealMaterialController>());
                }
            }

            var reveal = new GameObject();
            reveal.transform.position = raycastHit.point;
            reveal.transform.rotation = Quaternion.LookRotation(raycastHit.normal);

            var bulletHitEffect = bloodHitData.Spawn(reveal.transform.position, reveal.transform.rotation);
            bulletHitEffect.SetIntensity(10);
            bulletHitEffect.Play();

            Vector3 direction = -reveal.transform.forward;
            GameManager.local.StartCoroutine(RevealMaskProjection.ProjectAsync(
                reveal.transform.position + -direction * effectModuleReveal.offsetDistance,
                direction,
                reveal.transform.up, effectModuleReveal.depth, effectModuleReveal.maxSize,
                effectModuleReveal.maskTexture,
                effectModuleReveal.maxChannelMultiplier,
                revealMaterialControllers,
                effectModuleReveal.revealData, null));

            EffectInstance effectInstance = new EffectInstance();
            var collisionInstance = new CollisionInstance();
            collisionInstance.damageStruct.hitRagdollPart = ragdollPart;
            collisionInstance.pressureRelativeVelocity = Vector3.one;
            collisionInstance.damageStruct.damageType = DamageType.Pierce;
            collisionInstance.targetCollider = raycastHit.collider;
            collisionInstance.damageStruct.damage = 50f;
            effectInstance.AddEffect(data, reveal.transform.position, reveal.transform.rotation, reveal.transform,
                collisionInstance);

            _isShootCoroutineRunning = false;
            yield return null;
        }
    }
}