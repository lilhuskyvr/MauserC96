using System;
using System.Collections;
using System.Collections.Generic;
using RainyReignGames.RevealMask;
using ThunderRoad;
using UnityEngine;

namespace MauserC96
{
    public class Pistol : MonoBehaviour
    {
        private Item _pistolItem;
        private bool _isShootCoroutineRunning;
        private Animation _animation;
        private AudioSource _audioSource;
        private RagdollHand _ragdollHand;
        public EffectData data = Catalog.GetData<EffectData>("HitBladeDecalFlesh");
        public EffectData bloodHitData = Catalog.GetData<EffectData>("HitRagdollOnFlesh");
        public int moduleIndex = 3;
        public PistolData pistolData;
        public PistolItemModule pistolItemModule;

        private void Start()
        {
            _pistolItem = GetComponent<Item>();
            pistolItemModule = _pistolItem.data.GetModule<PistolItemModule>();
            RegisterEvents();
            pistolData = GetComponent<PistolData>();
            if (pistolItemModule.overridePistolData)
            {
                pistolData.damage = pistolItemModule.damage;
                pistolData.range = pistolItemModule.range;
                pistolData.force = pistolItemModule.force;
            }
            _animation = pistolData.pistol3DObject.gameObject.AddComponent<Animation>();
            _animation.AddClip(pistolData.shootAnimationClip, "Shoot");
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = pistolData.firingSound;
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

        private void PistolItemOnOnHeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
        {
            _ragdollHand = ragdollhand;
            if (action == Interactable.Action.UseStart)
            {
                Shoot();
            }
        }

        public IEnumerator PlayGunSound()
        {
            _audioSource.Play();
            yield return null;
        }

        public IEnumerator PlayMuzzleEffect()
        {
            pistolData.muzzleFlash.Play();
            yield return null;
        }

        public IEnumerator PlayShootAnimation()
        {
            _animation.Play("Shoot");
            yield return null;
        }

        public IEnumerator SimulateProjectile(Vector3 target)
        {
            if (pistolData.bullet == null)
                yield break;
            Debug.Log("simulate projectile");
            pistolData.bullet.gameObject.SetActive(true);
            pistolData.bullet.position = pistolData.muzzle.position;
            pistolData.bullet.rotation = pistolData.muzzle.rotation;
            Debug.Log("simulate projectile 2");
            Debug.Log(Vector3.Distance(pistolData.bullet.position, target));
            while (Vector3.Distance(pistolData.bullet.position, target) >= 0.1f)
            {
                pistolData.bullet.position = Vector3.MoveTowards(pistolData.bullet.position, target, 100f*Time.deltaTime);
                Debug.Log(pistolData.bullet.position);
                yield return new WaitForFixedUpdate();
            }

            pistolData.bullet.gameObject.SetActive(false);
            yield return null;
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

            StartCoroutine(PlayMuzzleEffect());

            StartCoroutine(PlayGunSound());

            StartCoroutine(PlayShootAnimation());

            if (_ragdollHand != null)
                PlayerControl.GetHand(_ragdollHand.side).HapticPlayClip(Catalog.gameData.haptics.hit);

            RaycastHit raycastHit = new RaycastHit();
            Ray ray = new Ray(
                pistolData.raycastPoint.transform.position,
                pistolData.raycastPoint.transform.forward
            );
            Physics.Raycast(
                ray,
                out raycastHit,
                pistolData.range, (1 << 27 | 1 << 0 | 1 << 13)
            );

            if (raycastHit.rigidbody == null)
            {
                _isShootCoroutineRunning = false;
                yield break;
            }

            StartCoroutine(SimulateProjectile(raycastHit.point));

            var ragdollPart = raycastHit.rigidbody.GetComponentInParent<RagdollPart>();

            if (ragdollPart == null)
            {
                _isShootCoroutineRunning = false;
                yield break;
            }

            SpawnBulletHole(raycastHit, ragdollPart);

            if (ragdollPart.ragdoll.creature.state != Creature.State.Dead)
            {
                ragdollPart.ragdoll.creature.Damage(
                    new CollisionInstance(new DamageStruct(DamageType.Energy, pistolData.damage))
                );
            }

            ragdollPart.rb.AddForce(pistolData.force * ray.direction, ForceMode.Impulse);

            _isShootCoroutineRunning = false;
            yield return null;
        }

        private void SpawnBulletHole(RaycastHit raycastHit, RagdollPart ragdollPart)
        {
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

            TriggerRealisticBleed(ragdollPart, raycastHit.collider, reveal.transform);
        }

        public void TriggerRealisticBleed(RagdollPart ragdollPart, Collider collider, Transform location)
        {
            EffectInstance effectInstance = new EffectInstance();
            var collisionInstance = new CollisionInstance();
            collisionInstance.damageStruct.hitRagdollPart = ragdollPart;
            collisionInstance.pressureRelativeVelocity = Vector3.one;
            collisionInstance.damageStruct.damageType = DamageType.Pierce;
            collisionInstance.targetCollider = collider;
            effectInstance.AddEffect(data, location.position, location.rotation, location,
                collisionInstance);
        }
    }
}