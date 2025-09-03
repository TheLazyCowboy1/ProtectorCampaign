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
    public const string GivePearlID = "LZC_Protector_GivePearl";
    public const string GrabNeuronID = "LZC_Protector_GrabNeuron";
    public const string AbandonSlugpupID = "LZC_Protector_AbandonSlugpup";
    public const string PupAtDMMoon = "LZC_Protector_PupAtDMMoon";
    public const string GiveNeuronDMMoon = "LZC_Protector_GiveNeuronDMMoon";

    private static string[] ALL_ACHIEVEMENTS => new string[] { GivePearlID, GrabNeuronID, AbandonSlugpupID, PupAtDMMoon, GiveNeuronDMMoon };

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

    /// <summary>
    /// Intended to grant an achievement in the middle or at the end of a conversation.
    /// </summary>
    public class AchievementEvent : Conversation.DialogueEvent
    {
        string achievementID;

        public AchievementEvent(Conversation owner, int initialWait, string ID) : base(owner, initialWait)
        {
            achievementID = ID;
        }

        public override void Activate()
        {
            base.Activate();

            TryGrantAchievement(achievementID);
        }

        public override bool IsOver => owner == null || age > initialWait; //if this isn't here, initialWait doesn't work!
    }


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
