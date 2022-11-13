using UnityEngine;

namespace DenizYanar.Projectiles
{
    /// <summary>
    /// Classic 2D projectile. Goes forward without any rotation and destroy after interact with any object.
    /// </summary>
    public class ClassicBullet : Projectile
    {
        protected override void Hit(Collider2D col)
        {
            Destroy(gameObject);
        }
    }
}
