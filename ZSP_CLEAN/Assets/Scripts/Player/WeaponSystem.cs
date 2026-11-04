using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZSP.Player
{
    public interface IDamageable
    {
        void TakeDamage(int amount, Vector3 point, Vector3 direction);
    }

    [System.Serializable]
    public class WeaponStats
    {
        public string displayName = "AK47";
        public int damage = 28;
        public float fireRate = 9f;        // rounds per second
        public int magazineSize = 30;
        public float reloadTime = 2.1f;
        public float range = 120f;
        public float spreadDegrees = 0.6f;
        public bool automatic = true;
        public float recoil = 0.8f;
    }

    /// <summary>
    /// Hitscan weapon system with magazine, reload, spread, recoil and switching.
    /// Assign the player camera to AimCamera and create WeaponStats entries.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        [Header("References")]
        public Camera aimCamera;
        public Transform weaponMuzzle;
        public ProgressionSystem progression;
        public LayerMask hitMask = ~0;

        [Header("Weapons")]
        public List<WeaponStats> weapons = new List<WeaponStats>();
        public int startingWeapon = 0;

        [Header("Recoil")]
        public float recoilRecovery = 8f;
        public float maxRecoil = 6f;

        public System.Action<int, int> OnAmmoChanged; // current, magazine size
        public System.Action<int> OnWeaponChanged;
        public System.Action<Vector3, Vector3> OnHit;  // point, normal

        public int CurrentAmmo => ammo;
        public int CurrentMagazineSize => CurrentWeapon != null ? CurrentWeapon.magazineSize : 0;
        public bool IsReloading => reloading;
        public WeaponStats CurrentWeapon =>
            weapons.Count == 0 ? null : weapons[Mathf.Clamp(index, 0, weapons.Count - 1)];

        private int index;
        private int ammo;
        private float nextFireTime;
        private bool reloading;
        private float recoilPitch;
        private Vector3 baseCameraLocalEuler;

        private void Start()
        {
            if (aimCamera == null) aimCamera = Camera.main;
            if (aimCamera != null) baseCameraLocalEuler = aimCamera.transform.localEulerAngles;
            Equip(startingWeapon);
        }

        private void Update()
        {
            if (weapons.Count == 0) return;

            HandleSwitching();
            HandleReload();
            HandleFire();
            RecoverRecoil();
        }

        private void HandleSwitching()
        {
            for (int i = 0; i < Mathf.Min(weapons.Count, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Equip(i);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) Equip((index + 1) % weapons.Count);
            else if (scroll < -0.01f) Equip((index - 1 + weapons.Count) % weapons.Count);
        }

        private void HandleReload()
        {
            if (Input.GetKeyDown(KeyCode.R) && !reloading && ammo < CurrentMagazineSize)
                StartCoroutine(ReloadRoutine());
        }

        private void HandleFire()
        {
            if (reloading || CurrentWeapon == null) return;

            bool trigger = CurrentWeapon.automatic
                ? Input.GetButton("Fire1")
                : Input.GetButtonDown("Fire1");

            if (trigger && Time.time >= nextFireTime && ammo > 0)
                Fire();
        }

        private void Fire()
        {
            WeaponStats w = CurrentWeapon;
            nextFireTime = Time.time + 1f / Mathf.Max(0.01f, w.fireRate);
            ammo--;
            OnAmmoChanged?.Invoke(ammo, w.magazineSize);

            Vector3 origin = aimCamera != null ? aimCamera.transform.position : transform.position;
            Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;

            float spread = Mathf.Tan(w.spreadDegrees * Mathf.Deg2Rad);
            direction += new Vector3(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0f);
            direction.Normalize();

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, w.range, hitMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                // Never hit the player's own colliders (the camera starts behind the player).
                if (hit.collider.transform.root == transform.root) continue;

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    float multiplier = progression != null ? progression.DamageMultiplier : 1f;
                    damageable.TakeDamage(Mathf.RoundToInt(w.damage * multiplier), hit.point, direction);
                }

                OnHit?.Invoke(hit.point, hit.normal);

                Rigidbody body = hit.collider.attachedRigidbody;
                if (body != null && !body.isKinematic)
                    body.AddForceAtPosition(direction * 4f, hit.point, ForceMode.Impulse);
                break;
            }

            recoilPitch = Mathf.Min(recoilPitch + w.recoil, maxRecoil);
        }

        private void RecoverRecoil()
        {
            if (recoilPitch <= 0f) return;
            recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, recoilRecovery * Time.deltaTime);
            if (aimCamera != null)
            {
                Vector3 e = aimCamera.transform.localEulerAngles;
                e.x = baseCameraLocalEuler.x - recoilPitch;
                aimCamera.transform.localEulerAngles = e;
            }
        }

        private IEnumerator ReloadRoutine()
        {
            reloading = true;
            float reloadTime = CurrentWeapon.reloadTime *
                               (progression != null ? progression.ReloadMultiplier : 1f);
            yield return new WaitForSeconds(reloadTime);
            ammo = CurrentMagazineSize;
            reloading = false;
            OnAmmoChanged?.Invoke(ammo, CurrentMagazineSize);
        }

        public void Equip(int weaponIndex)
        {
            if (weapons.Count == 0) return;
            index = Mathf.Clamp(weaponIndex, 0, weapons.Count - 1);
            ammo = CurrentMagazineSize;
            reloading = false;
            nextFireTime = 0f;
            OnWeaponChanged?.Invoke(index);
            OnAmmoChanged?.Invoke(ammo, CurrentMagazineSize);
        }
    }
}
