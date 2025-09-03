using BepInEx;
using OverseerHolograms;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProtectorCampaign;

public static class OverseerChanges
{
    private static Dictionary<SlugcatStats.Timeline, string[]> TimelineStoryRooms => new(new KeyValuePair<SlugcatStats.Timeline, string[]>[]
    {
        new(SlugcatStats.Timeline.Spear, new string[] {"DM_AI"}),
        new(SlugcatStats.Timeline.Artificer, new string[] {"LC_C01"}), //random room in LC
        new(SlugcatStats.Timeline.Sofanthiel, new string[] {"SS_AI"}),
        new(SlugcatStats.Timeline.Red, new string[] {"LF_A14", "SL_AI"}),
        new(SlugcatStats.Timeline.Gourmand, new string[] {"SS_AI", "OE_FINAL01"}),
        new(SlugcatStats.Timeline.White, new string[] {"SL_AI", "OE_FINAL01"}),
        new(SlugcatStats.Timeline.Yellow, new string[] {"SL_AI", "OE_FINAL01"}),
        new(SlugcatStats.Timeline.Rivulet, new string[] {"RM_AI", "RM_CORE", "SL_AI", "MS_CORE"})
        //Saint doesn't get any directions
    });

    private static string[] BadGates => new string[]
    {
        "GATE_UW_SL", "GATE_GW_SH", "GATE_OE_SU", "GATE_SL_MS", "GATE_DS_CC", "GATE_LF_SB"
    };

    public static string CurrentStoryRoom = "";


    public static List<string> DirectionFinder_StoryRegionPrioritys(On.OverseersWorldAI.DirectionFinder.orig_StoryRegionPrioritys orig, OverseersWorldAI.DirectionFinder self, SlugcatStats.Name saveStateNumber, string currentRegion, bool metMoon, bool metPebbles)
    {
        try
        {
            if (Plugin.IsProtectorCampaign)
            {
                currentRegion = currentRegion.ToUpperInvariant();

                var timeline = self.world.game.TimelinePoint;
                if (TimelineStoryRooms.TryGetValue(timeline, out string[] rooms))
                {
                    string[] gates = GetGatesList();
                    var slugName = Plugin.TimelineToSlugcat(timeline);
                    List<string> regionList = SlugcatStats.SlugcatStoryRegions(slugName);
                    regionList.AddRange(SlugcatStats.SlugcatOptionalRegions(slugName));

                    int[] scores = new int[rooms.Length];
                    for (int i = 0; i < rooms.Length; i++)
                    {
                        string r = rooms[i];

                        //specific logic cases
                        if (timeline == SlugcatStats.Timeline.Gourmand && r == "OE_FINAL01" && !metPebbles)
                        { //don't send Gourm to OE early; that's mean
                            scores[i] = Int32.MaxValue;
                            continue;
                        }
                        else if (r == "RM_AI" && metPebbles)
                        { //don't send Riv to Pebbles after meeting
                            scores[i] = Int32.MaxValue;
                            continue;
                        }
                        else if (r == "RM_CORE" && !metPebbles)
                        { //don't send Riv to Cell before Pebbles
                            scores[i] = Int32.MaxValue;
                            continue;
                        }
                        else if (r == "SL_AI" && self.world.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.shownEnergyCell)
                        { //don't send to Moon if have Cell
                            scores[i] = Int32.MaxValue;
                            continue;
                        }
                        else if (r == "MS_CORE" && !self.world.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.shownEnergyCell)
                        { //don't send to Cell if don't have Cell
                            scores[i] = Int32.MaxValue;
                            continue;
                        }
                        else if (r == "LF_A14" && self.world.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().TryGet(Constants.SAVE_KEY_HUNTER_NEURON, out bool grabbed) && grabbed)
                        { //don't send to Hunter if Hunter gone
                            scores[i] = Int32.MaxValue;
                            continue;
                        }

                        scores[i] = RegionDistance(gates, regionList, currentRegion, r.Substring(0, r.IndexOf('_')));
                    }

                    //find the best room
                    int bestIdx = 0, bestScore = scores[0];
                    for (int i = 1; i < scores.Length; i++)
                    {
                        if (scores[i] < bestScore)
                        {
                            bestIdx = i;
                            bestScore = scores[i];
                        }
                    }
                    CurrentStoryRoom = rooms[bestIdx];
                    SetHologramForStoryRoom(self);

                    return RegionPriorityList(gates, regionList, CurrentStoryRoom.Substring(0, CurrentStoryRoom.IndexOf('_')));

                }
            }
        } catch (Exception ex) { Error(ex); }

        return orig(self, saveStateNumber, currentRegion, metMoon, metPebbles);
    }


    public static string DirectionFinder_StoryRoomInRegion(On.OverseersWorldAI.DirectionFinder.orig_StoryRoomInRegion orig, OverseersWorldAI.DirectionFinder self, string currentRegion, bool metMoon)
    {
        //if we have a story room for this region, use it!
        if (Plugin.IsProtectorCampaign
            && CurrentStoryRoom != "" && CurrentStoryRoom.Substring(0, CurrentStoryRoom.IndexOf('_')) == currentRegion.ToUpperInvariant())
            return CurrentStoryRoom;

        //default
        return orig(self, currentRegion, metMoon);
    }


    public static bool WorldLoader_OverseerSpawnConditions(On.WorldLoader.orig_OverseerSpawnConditions orig, WorldLoader self, SlugcatStats.Name character)
    {
        if (character == Plugin.ProtectorName && self.timelinePosition == SlugcatStats.Timeline.White)
            return true; //always true in Survivor timeline
        if (Plugin.IsProtectorCampaign)
            return orig(self, character) || UnityEngine.Random.value < 0.3f; //guarenteed 30% chance of spawning, just in case

        return orig(self, character); //otherwise, default to the normal behavior
    }


    private static void SetHologramForStoryRoom(OverseersWorldAI.DirectionFinder self)
    {
        int symbol = 0; //slugcat

        switch(CurrentStoryRoom)
        {
            case "SL_AI":
            case "DM_AI":
                symbol = 1; //Moon
                break;
            case "SS_AI":
            case "RM_AI":
                symbol = 3; //Pebbles
                break;
            case "RM_CORE":
            case "MS_CORE":
                symbol = 4; //Energy Cell
                break;
        }

        self.world.game.GetStorySession.saveState.miscWorldSaveData.playerGuideState.guideSymbol = symbol;
        Debug($"Set overseer guidance symbol to {OverseerHologram.OverseerGuidanceSymbol(symbol)} for story room " + CurrentStoryRoom);
    }


    private static string[] GetGatesList()
    {
        return File.ReadAllLines(AssetManager.ResolveFilePath(Path.Combine("world", "gates", "locks.txt")))
            .Where(l => !l.IsNullOrWhiteSpace())
            .Select(l => l.Substring(0, l.IndexOf(' ')))
            .Except(BadGates)
            .ToArray();
    }

    private static int RegionDistance(string[] gates, List<string> regionList, string regionA, string regionB)
    {
        if (regionA == regionB) return 0;

        List<string> accessible = new(gates.Length * 2);
        List<string> newAccessible = new(gates.Length * 2);
        bool contained(string s) => accessible.Contains(s) || newAccessible.Contains(s);

        accessible.Add(regionA);
        string vanillaA = Region.GetVanillaEquivalentRegionAcronym(regionA);
        if (!accessible.Contains(vanillaA))
            accessible.Add(vanillaA);

        //loop through gates, making regions accessible each time
        for (int i = 1; i < gates.Length; i++)
        {
            foreach (string g in gates)
            {
                string[] d = g.Split('_');

                string r = "";

                if (accessible.Contains(d[1]) && !contained(d[2]) && regionList.Contains(d[2]))
                    r = d[2];
                else if (accessible.Contains(d[2]) && !contained(d[1]) && regionList.Contains(d[1]))
                    r = d[1];
                else
                    continue;

                newAccessible.Add(r);
                string v = Region.GetVanillaEquivalentRegionAcronym(r);
                if (!contained(v))
                    newAccessible.Add(v);

                if (r == regionB || v == regionB) return i;
            }

            if (newAccessible.Count <= 0)
                break;
            accessible.AddRange(newAccessible);
            newAccessible.Clear();
        }

        Debug("Failed to find region " + regionB);
        return gates.Length; //can't find it!!!
    }

    private static List<string> RegionPriorityList(string[] gates, List<string> regionList, string regionA)
    {
        List<string> accessible = new(gates.Length * 2);
        List<string> newAccessible = new(gates.Length * 2);
        bool contained(string s) => accessible.Contains(s) || newAccessible.Contains(s);

        accessible.Add(regionA);
        string vanillaA = Region.GetVanillaEquivalentRegionAcronym(regionA);
        if (!accessible.Contains(vanillaA))
            accessible.Add(vanillaA);

        //loop through gates, making regions accessible each time
        for (int i = 1; i < gates.Length; i++)
        {
            foreach (string g in gates)
            {
                string[] d = g.Split('_');

                string r = "";

                if (accessible.Contains(d[1]) && !contained(d[2]) && regionList.Contains(d[2]))
                    r = d[2];
                else if (accessible.Contains(d[2]) && !contained(d[1]) && regionList.Contains(d[1]))
                    r = d[1];
                else
                    continue;

                newAccessible.Add(r);
                string v = Region.GetVanillaEquivalentRegionAcronym(r);
                if (!contained(v))
                    newAccessible.Add(v);
            }

            if (newAccessible.Count <= 0)
                break;
            accessible.AddRange(newAccessible);
            newAccessible.Clear();
        }

        //list out the regions, for debugging purposes
        accessible.Reverse();
        Debug("Overseer region priority list: " + accessible.Aggregate((a, b) => a + ", " + b));
        return accessible;
    }


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
