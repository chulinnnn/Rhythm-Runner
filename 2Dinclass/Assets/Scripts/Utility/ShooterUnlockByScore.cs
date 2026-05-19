using UnityEngine;

/// <summary>
/// Unlocks shooter objects based on score, with optional fire-rate override on unlock.
/// </summary>
public class ShooterUnlockByScore : MonoBehaviour
{
    [System.Serializable]
    public class ShooterUnlockEntry
    {
        [Tooltip("Shooter root object to enable.")]
        public GameObject shooterObject;

        [Tooltip("Unlock when score is greater than this value.")]
        public int unlockScore = 10;

        [Tooltip("If enabled, set fireRate for all ShootingController under this shooter.")]
        public bool overrideFireRateOnUnlock = false;

        [Tooltip("Lower value means faster shooting. Example: 0.25")]
        public float fireRateAfterUnlock = 0.25f;
    }

    [Tooltip("Configure score thresholds for each shooter.")]
    public ShooterUnlockEntry[] shooterUnlocks;

    [Header("Global Speed Up")]
    [Tooltip("When score is greater than this value, speed up all shooters below.")]
    public int globalSpeedUpScore = 20;

    [Tooltip("Shooter roots to speed up together (e.g. all 3 shooters).")]
    public GameObject[] shootersToSpeedUp;

    [Tooltip("Unified faster fireRate applied to all listed shooters.")]
    public float globalFireRateAfterSpeedUp = 0.15f;

    private bool[] hasUnlocked;
    private bool hasAppliedGlobalSpeedUp;

    private void Awake()
    {
        if (shooterUnlocks == null)
            return;

        hasUnlocked = new bool[shooterUnlocks.Length];
    }

    private void Update()
    {
        if (shooterUnlocks == null || shooterUnlocks.Length == 0 || GameManager.instance == null)
            return;

        int currentScore = GameManager.score;

        for (int i = 0; i < shooterUnlocks.Length; i++)
        {
            if (hasUnlocked[i])
                continue;

            ShooterUnlockEntry entry = shooterUnlocks[i];
            if (entry == null || entry.shooterObject == null)
                continue;

            if (currentScore > entry.unlockScore)
            {
                entry.shooterObject.SetActive(true);
                ApplyFireRateOverride(entry);
                hasUnlocked[i] = true;
            }
        }

        if (!hasAppliedGlobalSpeedUp && currentScore > globalSpeedUpScore)
        {
            ApplyGlobalSpeedUp();
            hasAppliedGlobalSpeedUp = true;
        }
    }

    private void ApplyFireRateOverride(ShooterUnlockEntry entry)
    {
        if (!entry.overrideFireRateOnUnlock)
            return;

        ShootingController[] guns = entry.shooterObject.GetComponentsInChildren<ShootingController>(true);
        foreach (ShootingController gun in guns)
        {
            gun.fireRate = Mathf.Max(0.01f, entry.fireRateAfterUnlock);
        }
    }

    private void ApplyGlobalSpeedUp()
    {
        if (shootersToSpeedUp == null || shootersToSpeedUp.Length == 0)
            return;

        float targetFireRate = Mathf.Max(0.01f, globalFireRateAfterSpeedUp);
        foreach (GameObject shooterRoot in shootersToSpeedUp)
        {
            if (shooterRoot == null)
                continue;

            ShootingController[] guns = shooterRoot.GetComponentsInChildren<ShootingController>(true);
            foreach (ShootingController gun in guns)
            {
                gun.fireRate = targetFireRate;
            }
        }
    }
}
