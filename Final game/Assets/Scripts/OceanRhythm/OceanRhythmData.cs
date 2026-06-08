using UnityEngine;

public enum OceanFishType
{
    Fish,
    Octopus,
    Turtle,
    Jellyfish,
    Mystery
}

public enum OceanDecorationReward
{
    Seaweed,
    Shell,
    Star,
    Flag,
    Pearl,
    BellCharm,
    GlowStar,
    WaveRibbon
}

public enum OceanBucketSlotId
{
    TopSlot,
    LeftSlot,
    RightSlot,
    FrontSlot,
    CharmSlot
}

public struct OceanDecorationUnlockRequirement
{
    public OceanFishType fishType;
    public int requiredCount;
    public int currentCount;
    public bool usesMusicPearls;

    public int Remaining
    {
        get { return Mathf.Max(0, requiredCount - currentCount); }
    }

    public bool IsUnlocked
    {
        get { return Remaining <= 0; }
    }

    public OceanDecorationUnlockRequirement(OceanFishType fishType, int requiredCount, int currentCount)
    {
        this.fishType = fishType;
        this.requiredCount = requiredCount;
        this.currentCount = currentCount;
        this.usesMusicPearls = false;
    }

    public OceanDecorationUnlockRequirement(int requiredMusicPearls, int currentMusicPearls)
    {
        this.fishType = OceanFishType.Fish;
        this.requiredCount = requiredMusicPearls;
        this.currentCount = currentMusicPearls;
        this.usesMusicPearls = true;
    }
}

public class OceanBucketInventory
{
    private const string ShellsKey = "OceanBucket_Shells";
    private const string MusicPearlsKey = "OceanBucket_MusicPearls";
    private const string SelectedDecorationKey = "OceanBucket_SelectedDecoration";
    private const string CatchCountPrefix = "OceanBucket_Catch_";
    private const string DecorationPrefix = "OceanBucket_Decoration_";
    private const string SlotPrefix = "OceanBucket_Slot_";

    public int Shells
    {
        get { return PlayerPrefs.GetInt(ShellsKey, 0); }
    }

    public int MusicPearls
    {
        get { return PlayerPrefs.GetInt(MusicPearlsKey, 0); }
    }

    public OceanDecorationReward SelectedDecoration
    {
        get { return (OceanDecorationReward)PlayerPrefs.GetInt(SelectedDecorationKey, 0); }
    }

    public void AddCatch(OceanFishType fishType, int shellReward)
    {
        PlayerPrefs.SetInt(CatchCountPrefix + fishType, GetCatchCount(fishType) + 1);
        PlayerPrefs.SetInt(ShellsKey, Shells + Mathf.Max(0, shellReward));
        PlayerPrefs.Save();
    }

    public void AddMusicPearls(int amount)
    {
        PlayerPrefs.SetInt(MusicPearlsKey, MusicPearls + Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public int GetCatchCount(OceanFishType fishType)
    {
        return PlayerPrefs.GetInt(CatchCountPrefix + fishType, 0);
    }

    public int GetTotalCatchCount()
    {
        int total = 0;
        total += GetCatchCount(OceanFishType.Fish);
        total += GetCatchCount(OceanFishType.Octopus);
        total += GetCatchCount(OceanFishType.Turtle);
        total += GetCatchCount(OceanFishType.Jellyfish);
        total += GetCatchCount(OceanFishType.Mystery);
        return total;
    }

    public void UnlockDecoration(OceanDecorationReward reward)
    {
        PlayerPrefs.SetInt(DecorationPrefix + reward, 1);
        PlayerPrefs.Save();
    }

    public bool IsDecorationUnlocked(OceanDecorationReward reward)
    {
        return GetUnlockProgress(reward).IsUnlocked || PlayerPrefs.GetInt(DecorationPrefix + reward, 0) == 1;
    }

    public bool TrySelectDecoration(OceanDecorationReward reward)
    {
        if (!IsDecorationUnlocked(reward))
        {
            return false;
        }

        PlayerPrefs.SetInt(SelectedDecorationKey, (int)reward);
        PlayerPrefs.Save();
        return true;
    }

    public OceanDecorationUnlockRequirement GetUnlockProgress(OceanDecorationReward reward)
    {
        if (reward == OceanDecorationReward.Seaweed)
        {
            return new OceanDecorationUnlockRequirement(OceanFishType.Fish, 0, 0);
        }
        if (reward == OceanDecorationReward.Shell)
        {
            return new OceanDecorationUnlockRequirement(OceanFishType.Fish, 3, GetCatchCount(OceanFishType.Fish));
        }
        if (reward == OceanDecorationReward.Star)
        {
            return new OceanDecorationUnlockRequirement(OceanFishType.Octopus, 3, GetCatchCount(OceanFishType.Octopus));
        }
        if (reward == OceanDecorationReward.Flag)
        {
            return new OceanDecorationUnlockRequirement(OceanFishType.Turtle, 3, GetCatchCount(OceanFishType.Turtle));
        }
        if (reward == OceanDecorationReward.BellCharm)
        {
            return new OceanDecorationUnlockRequirement(3, MusicPearls);
        }
        if (reward == OceanDecorationReward.GlowStar)
        {
            return new OceanDecorationUnlockRequirement(5, MusicPearls);
        }
        if (reward == OceanDecorationReward.WaveRibbon)
        {
            return new OceanDecorationUnlockRequirement(8, MusicPearls);
        }

        return new OceanDecorationUnlockRequirement(OceanFishType.Mystery, 1, GetCatchCount(OceanFishType.Mystery));
    }

    public static OceanDecorationReward[] GetAllDecorations()
    {
        return new OceanDecorationReward[]
        {
            OceanDecorationReward.Seaweed,
            OceanDecorationReward.Shell,
            OceanDecorationReward.Star,
            OceanDecorationReward.Flag,
            OceanDecorationReward.Pearl,
            OceanDecorationReward.BellCharm,
            OceanDecorationReward.GlowStar,
            OceanDecorationReward.WaveRibbon
        };
    }

    public bool HasSlotDecoration(OceanBucketSlotId slotId)
    {
        return PlayerPrefs.HasKey(SlotPrefix + slotId);
    }

    public OceanDecorationReward GetSlotDecoration(OceanBucketSlotId slotId)
    {
        return (OceanDecorationReward)PlayerPrefs.GetInt(SlotPrefix + slotId, (int)OceanDecorationReward.Seaweed);
    }

    public void SetSlotDecoration(OceanBucketSlotId slotId, OceanDecorationReward reward)
    {
        if (!IsDecorationUnlocked(reward))
        {
            return;
        }

        PlayerPrefs.SetInt(SlotPrefix + slotId, (int)reward);
        PlayerPrefs.SetInt(SelectedDecorationKey, (int)reward);
        PlayerPrefs.Save();
    }
}
