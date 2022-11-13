using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DenizYanar.Guns
{
    public class Gun : MonoBehaviour
    {
        /// <summary>
        /// Core part of the gun. Gun script takes all component of the gun and act as the brain of the gun algorithm.
        /// Also gun needs GunMagazine & GunLauncher
        /// </summary>

        #region Fields

        [SerializeField]
        [ValidateInput("@$value > 0", "It has to be greater than zero")]
        [Tooltip("The time which gun needs to fire another projectile.")]
        private float m_FireCooldown;

        [SerializeField, Min(0)]
        private float m_DamageValue;

        private GunLauncher m_Launcher;
        private GunMagazine m_Magazine;

        public bool m_HasCooldown { get; private set; }
        private bool m_IsFireEnable;

        #endregion /Fields

        public void Reload() => m_Magazine.StartReload();

        /// <summary>
        /// Starts to fire until StopFire() method called.
        /// </summary>
        public void StartFire() => m_IsFireEnable = true;

        /// <summary>
        /// Stop auto-firing.
        /// </summary>
        public void StopFire() => m_IsFireEnable = false;

        /// <summary>
        /// Fire only once.
        /// </summary>
        public void FireOneShot() => Fire();


        private void Awake()
        {
            m_Magazine = GetComponentInChildren<GunMagazine>();
            m_Launcher = GetComponentInChildren<GunLauncher>();
        }

        private void Update()
        {
            if (m_IsFireEnable) Fire();
        }


        private void Fire()
        {
            if (m_HasCooldown) return;
            if (m_Magazine.MagazineStatus == GunMagazine.EMagazineStatus.EMPTY) return;
            m_Magazine.SpendAmmo();
            m_Launcher.Shot(m_DamageValue);
            StartCoroutine(StartFireCooldown(m_FireCooldown));
        }

        private IEnumerator StartFireCooldown(float cooldownDuration)
        {
            m_HasCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            m_HasCooldown = false;
        }
    }
}