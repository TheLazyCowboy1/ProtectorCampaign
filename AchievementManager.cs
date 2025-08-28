using RWCustom;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtectorCampaign;

public static class AchievementManager
{
    private const string GivePearlID = "LZC_Protector_GivePearl";
    private const string GrabNeuronID = "LZC_Protector_GrabNeuron";
    private const string AbandonSlugpupID = "LZC_Protector_AbandonSlugpup";

    private static string[] ALL_ACHIEVEMENTS => new string[] { GivePearlID, GrabNeuronID, AbandonSlugpupID };

    public static void ClearAllAchievements()
    {
        try
        {
            var data = Custom.rainWorld.progression.miscProgressionData.GetSlugBaseData();
            foreach (string id in ALL_ACHIEVEMENTS)
            {
                if (data.TryGet(id, out bool gotten) && gotten)
                {
                    data.Set(id, false);
                    Debug("Cleared achievement " + id);
                }
            }
        }
        catch (Exception ex) { Error(ex); }
    }

    private static void TryGrantAchievement(string ID)
    {
        try
        {
            var data = Custom.rainWorld.progression.miscProgressionData.GetSlugBaseData();
            if (!data.TryGet(ID, out bool met) || !met)
            {
                FakeAchievementCompat.ShowAchievement(ID);
                data.Set(ID, true);
                Debug($"Showed {ID} achievement!");
            }
            else
                Debug($"Already showed {ID} achievement.");
        }
        catch (Exception ex) { Error(ex); }
    }

    public static void GavePearlToMoon() => TryGrantAchievement(GivePearlID);
    public static void GrabbedHunterNeuron() => TryGrantAchievement(GrabNeuronID);
    public static void AbandonSlugpup() => TryGrantAchievement(AbandonSlugpupID);


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
