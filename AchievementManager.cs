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

    private static string[] ALL_ACHIEVEMENTS => new string[] { GivePearlID };

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

    public static void GavePearlToMoon()
    {
        try
        {
            var data = Custom.rainWorld.progression.miscProgressionData.GetSlugBaseData();
            if (!data.TryGet(GivePearlID, out bool met) || !met)
            {
                FakeAchievementCompat.ShowAchievement(GivePearlID);
                data.Set(GivePearlID, true);
                Debug("Showed GivePearl achievement!");
            }
            else
                Debug("Already showed GivePearl achievement.");
        } catch (Exception ex) { Error(ex); }
    }


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
