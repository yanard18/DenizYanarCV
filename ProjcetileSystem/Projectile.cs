using UnityEngine;

namespace DenizYanar.Projectiles
{
    /// <summary>
    /// Base class for all kind of projectiles in the game.
    /// Accurate at very fast projectiles.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Projectile : MonoBehaviour
    {
        public GameObject Author { get; private set; }
        protected float m_DamageValue { get; private set; }

        [SerializeField]
        [Tooltip("Define the collision layer that projectile can interact")]
        private LayerMask m_HitBoxLayer;

        [SerializeField]
        private bool m_bIsDebugEnabled;

        protected Rigidbody2D m_Rb;
        private bool m_DidHit;

        /// <summary>
        /// Initialization command for the projectile. Projectile can not work without the init command.
        /// </summary>
        /// <param name="trajectory">Initial trajectory of the projectile.</param>
        /// <param name="angularVelocity">Add rotational force to projectile and make it spin.</param>
        /// <param name="lifeTime">The time span of the projectile before it destroyed.</param>
        /// <param name="damageValue">Damage value of the projectile.</param>
        /// <param name="author">"Author represent who fired the projectile.</param>
        public void Init(Vector2 trajectory, float angularVelocity = 0, float lifeTime = 5.0f, float damageValue = 0, GameObject author = null)
        {
            Author = author != null ? author : null;
            m_Rb.velocity = trajectory;
            m_Rb.angularVelocity = angularVelocity;
            m_DamageValue = damageValue;
            DetectHit();
            if (lifeTime > 0f)
                Destroy(gameObject, lifeTime);
        }

        protected abstract void Hit(Collider2D col);
        protected virtual void Awake() => m_Rb = GetComponent<Rigidbody2D>();

        protected virtual void FixedUpdate() => DetectHit();

        protected void StopProjectile()
        {
            m_Rb.velocity = Vector2.zero;
            m_Rb.angularVelocity = 0f;
        }

        #region Private Methods

        /// <summary>
        /// Calculate if the projectile hits before it happen.
        /// This is important for very fast projectiles.
        /// </summary>
        private void DetectHit()
        {
            if (m_DidHit) return;

            var velocity = m_Rb.velocity;
            var currentPosition = m_Rb.position;
            var desiredVelocityVector = velocity * Time.fixedDeltaTime;

            // ReSharper disable once Unity.PreferNonAllocApi
            RaycastHit2D[] hit = Physics2D.CircleCastAll(
                currentPosition,
                0.1f,
                desiredVelocityVector.normalized,
                desiredVelocityVector.magnitude,
                m_HitBoxLayer);


#if UNITY_EDITOR
            if (m_bIsDebugEnabled)
                Debug.DrawRay(currentPosition, desiredVelocityVector, Color.magenta, 5.0f);
#endif

            if (hit.Length <= 0) return;

            foreach (var t in hit)
            {
#if UNITY_EDITOR
                if (m_bIsDebugEnabled)
                    Debug.Log(t.transform.name);
#endif

                if (t.transform.gameObject == Author) continue;

                m_DidHit = true;
                transform.position = t.point;
                Hit(t.collider);

#if UNITY_EDITOR
                if (m_bIsDebugEnabled)
                    Debug.Log("Hit to: " + t.transform.name);
#endif

                return;
            }
        }

        #endregion


        #region Public Methods

        #endregion
    }
}