using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DenizYanar.Guns
{
    public class GunMagazine : MonoBehaviour
    {
        public enum EMagazineStatus
        {
            FULL,
            LOADED,
            EMPTY
        };

        private enum EReloadStatus
        {
            NORMAL,
            RELOADING
        };
        
        private EReloadStatus _reloadStatus;

        [SerializeField] [ValidateInput("@$value > 0", "Magazine capacity has to be greater than zero")]
        private int m_MagazineCapacity = 30;
        [SerializeField] [ValidateInput("@$value >= 0", "Current ammo can't be negative")]
        private int m_Ammo = 30;
        [SerializeField] [ValidateInput("@$value >= 0", "Total ammo can't be negative")]
        private int m_AmmoThatReloadable = 90;
        [SerializeField] 
        private bool m_bHasUnlimitedAmmo;
        
        [SerializeField] 
        private float m_ReloadDuration = 1.0f;

        public EMagazineStatus MagazineStatus;


        public void SpendAmmo()
        {
            if(m_bHasUnlimitedAmmo || m_Ammo <= 0) return;

            m_Ammo--;
            
            MagazineStatus = m_Ammo > 0 ? EMagazineStatus.LOADED : EMagazineStatus.EMPTY;
            if(m_Ammo == 0) StartReload();
        }
        
        public void StartReload()
        {
            if (MagazineStatus == EMagazineStatus.FULL || _reloadStatus == EReloadStatus.RELOADING || m_AmmoThatReloadable <= 0) return;
            StartCoroutine(ReloadCooldownCoroutine(m_ReloadDuration));
        }

        private void Reload()
        {
            var reloadAmount = 0;
            if (HasEnoughAmmoToReload(reloadAmount))
                reloadAmount = m_AmmoThatReloadable;
            else
                reloadAmount = m_MagazineCapacity - m_Ammo;

            m_AmmoThatReloadable -= reloadAmount;
            
            FillTheMagazine(reloadAmount);

            SetMagazineStatus();
        }

        private bool HasEnoughAmmoToReload(int reloadAmount) => m_AmmoThatReloadable < reloadAmount;

        private void SetMagazineStatus()
        {
            if (m_Ammo == m_MagazineCapacity)
                MagazineStatus = EMagazineStatus.FULL;
            else if (m_Ammo < m_MagazineCapacity && m_Ammo > 0)
                MagazineStatus = EMagazineStatus.LOADED;
            else
                MagazineStatus = EMagazineStatus.EMPTY;
        }

        private void FillTheMagazine(int reloadAmount)
        {
            m_Ammo += reloadAmount;
            m_Ammo = Mathf.Clamp(m_Ammo, 0, m_MagazineCapacity);
        }


        private IEnumerator ReloadCooldownCoroutine(float duration)
        {
            _reloadStatus = EReloadStatus.RELOADING;
            yield return new WaitForSeconds(duration);
            _reloadStatus = EReloadStatus.NORMAL;
            Reload();
        }

    }
}
