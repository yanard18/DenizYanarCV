using UnityEngine;
using DenizYanar.Projectiles;
using Sirenix.OdinInspector;

namespace DenizYanar.Guns
{
    public class GunLauncher : MonoBehaviour
    {
        [SerializeField] [ValidateInput("@m_TBulletStartPositions.Length > 0", "There must be one start position at least")]
        [Tooltip("Projectiles instantiate at bullet start positions")]
        private Transform[] m_TBulletStartPositions;
        
        [SerializeField] [Required]
        [Tooltip("Projectile to fire")]
        private Projectile m_Projectile;

        [SerializeField] [ValidateInput("@$value > 0", "This value should be greater than zero")]
        private float m_ProjectileSpeed = 1.0f;

        [SerializeField] [ValidateInput("@$value >= 0", "Value can't be negative")]
        [Range(0, 90)]
        private float m_RecoilAmount = 5.0f;
        
        public void Shot(float damageValue)
        {
            foreach (var startPos in m_TBulletStartPositions)
            {
                var shotDirection = startPos.right;
                if (m_RecoilAmount > 0)
                {
                    var recoil = new Recoil(shotDirection, m_RecoilAmount);
                    //shotDirection = recoil.CalculateRecoil();
                }

                var p = Instantiate(m_Projectile, startPos.position, Quaternion.Euler(shotDirection));
                p.Init(shotDirection * m_ProjectileSpeed, 0, lifeTime: 5.0f, damageValue: damageValue, author: transform.root.gameObject);
            }
        }

    }
    

    public class Recoil
    {
        private readonly Vector2 m_ShotDirection;
        private readonly float m_RecoilAmount;

        public Recoil(Vector2 shotDirection, float recoilAmount)
        {
            m_ShotDirection = shotDirection;
            m_RecoilAmount = recoilAmount;
        }

        public Vector2 CalculateRecoil()
        {
            var currentAngle = Vector2.Angle(Vector2.right, m_ShotDirection);
            var newAngle= Random.Range(currentAngle - m_RecoilAmount, currentAngle + m_RecoilAmount);
            var x = Mathf.Cos(Mathf.Deg2Rad * newAngle);
            var y = Mathf.Sin(Mathf.Deg2Rad * newAngle);
            return new Vector2(x, y);

        }
    }
}
 