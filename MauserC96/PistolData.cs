using UnityEngine;

namespace MauserC96
{
    public class PistolData : MonoBehaviour
    {
        [SerializeField] public AudioClip firingSound;

        [SerializeField] [Tooltip("Green = up. Blue = forward. Red = right")]
        public Transform raycastPoint;

        //how far the bullet can travel
        [SerializeField] [Tooltip("How far the bullet can travel")]
        public float range = 100;

        //how far the bullet can travel
        [SerializeField] [Tooltip("Bullet damage. Creature normal health = 50")]
        public float damage = 50;

        //how far the bullet can travel
        [SerializeField] [Tooltip("The force of the bullet to push the ragdollPart")]
        public float force = 300;

        
        [SerializeField] [Tooltip("Animation will be added here")]
        public Transform pistol3DObject;
        
        [SerializeField] public AnimationClip shootAnimationClip;

        private void Awake()
        {
            if (shootAnimationClip != null)
                shootAnimationClip.legacy = true;
        }
    }
}