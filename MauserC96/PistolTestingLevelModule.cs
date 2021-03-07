using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using ThunderRoad;
using UnityEngine;

// ReSharper disable UnusedMember.Global

namespace MauserC96
{
    public class PistolTestingLevelModule : LevelModule
    {
        private Item _pistolItem;
        private Item _pistolBullet;
        private Creature _testCreature;

        public override IEnumerator OnLoadCoroutine(Level level)
        {
            DebugLogConsole.AddCommandInstance("m96s",
                "Spawn A Pistol", "SpawnPistol",
                this);
            
            DebugLogConsole.AddCommandInstance("m96b",
                "Spawn A Pistol", "SpawnBullet",
                this);

            DebugLogConsole.AddCommandInstance("m96n",
                "Navigate", "NavigatePistol",
                this);

            DebugLogConsole.AddCommandInstance("m96f",
                "Fire", "Fire",
                this);

            DebugLogConsole.AddCommandInstance("m96nf",
                "Navigate & Fire", "NavigateFire",
                this);

            DebugLogConsole.AddCommandInstance("m96c",
                "Test Creature", "SpawnTestCreature",
                this);

            DebugLogConsole.AddCommandInstance("m96d",
                "TestDecal", "TestDecal",
                this);

            return base.OnLoadCoroutine(level);
        }

        public void SpawnPistol()
        {
            Catalog.GetData<ItemData>("AOTPistol").SpawnAsync(item =>
            {
                item.transform.position = Player.local.transform.position + 3f * Player.local.transform.forward +
                                          2 * Player.characterData.height * Vector3.up;

                item.transform.rotation = Player.local.creature.transform.rotation;

                item.rb.isKinematic = true;

                _pistolItem = item;
            });
        }

        public void NavigatePistol()
        {
            foreach (var creature in Creature.list)
            {
                if (!creature.isPlayer && creature.state == Creature.State.Alive)
                {
                    _pistolItem.transform.LookAt(creature.ragdoll.headPart.transform);
                }
            }
        }

        public void Fire()
        {
            GameManager.local.StartCoroutine(_pistolItem.gameObject.GetComponent<Pistol>().Shoot());
        }

        public void NavigateFire()
        {
            NavigatePistol();
            Fire();
        }

        public override void Update(Level level)
        {
            // if (_testCreature)
            // {
            //     foreach (Creature.RendererData renderer in _testCreature.renderers)
            //     {
            //         if ((bool) renderer.revealDecal && renderer.revealDecal.type == RevealDecal.Type.Body)
            //             renderer.revealDecal.Reset();
            //     }
            // }
        }

        public void SpawnTestCreature()
        {
            GameManager.local.StartCoroutine(Catalog.GetData<CreatureData>("HumanFemale").SpawnCoroutine(
                Player.local.transform.position + Player.local.transform.forward, Player.local.transform.rotation, null,
                creature =>
                {
                    creature.SetFaction(2);
                    (creature.brain.instance as BrainHuman).canLeave = false;
                    _testCreature = creature;
                }));
        }

        public void TestDecal()
        {
            // GameManager.local.StartCoroutine(Catalog.GetData<CreatureData>("HumanFemale").SpawnCoroutine(
            //     Player.local.transform.position + Player.local.transform.forward, Player.local.transform.rotation, null,
            //     creature =>
            //     {
            //         creature.SetFaction(5);
            //         creature.brain.Load("BaseWarrior");
            //         creature.brain.instance.Start();
            //         (creature.brain.instance as BrainHuman).canLeave = false;
            //     }));


            var hitInfo = new RaycastHit();
            Physics.Raycast(new Ray(_testCreature.transform.position + _testCreature.transform.forward,
                _testCreature.ragdoll.headPart.transform.position -
                (_testCreature.transform.position + _testCreature.transform.forward)
            ), out hitInfo, 10, (1 << 27 | 1 << 0 | 1 << 13));

            Debug.Log("Hit infor");
            Debug.Log(hitInfo.point);
            
            var freeindex = _testCreature.ragdoll.headPart.collisionHandler.GetFreeCollisionIndex();
            _testCreature.ragdoll.headPart.collisionHandler.collisions[freeindex].NewHit(
                _pistolItem.colliderGroups[0].colliders[0],
                hitInfo.collider,
                _pistolItem.colliderGroups[0],
                _testCreature.ragdoll.headPart.colliderGroup,
                new Vector3(100, 100, 100),
                hitInfo.point,
                hitInfo.normal,
                100,
                Catalog.GetData<MaterialData>("Blade"),
                Catalog.GetData<MaterialData>("Flesh")
            );

            var effect = Catalog.GetData<EffectData>("HitProjectileOnFlesh")
                .Spawn(hitInfo.point, Quaternion.identity);
            effect.SetTarget(_testCreature.ragdoll.headPart.transform);
            effect.SetIntensity(10);
            effect.Play();
        }
    }
}