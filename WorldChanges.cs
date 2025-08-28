using MoreSlugcats;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProtectorCampaign.Constants;

namespace ProtectorCampaign;

public static class WorldChanges
{
    private const string BrokenGravRooms = "WORA_THRONE02;WORA_THRONE03;WORA_THRONE04;WORA_THRONE05;WORA_THRONE06;WORA_THRONE07;WORA_THRONE08;WORA_THRONE09;WORA_THRONE10;WORA_THRONE11;WORA_THRONE12;WORA_AI";

    //Load room settings for LZC_Protector if they exist
    //This makes LZC_Protector its own timeline but ONLY for room settings
    public static void RoomSettings_ctor(On.RoomSettings.orig_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame orig, RoomSettings self, Room room, string name, Region region, bool template, bool firstTemplate, SlugcatStats.Timeline timelinePoint, RainWorldGame game)
    {
        try
        {
            if (Plugin.IsProtectorCampaign && !template && WorldLoader.FindRoomFile(name, false, "_settings-LZC_Protector.txt", false) != null)
            {
                Debug("Found settings file " + name + "_settings-LZC_Protector.txt");
                orig(self, room, name, region, template, firstTemplate, new("LZC_Protector"), game);
                return;
            }
        } catch (Exception ex) { Error(ex); }

        orig(self, room, name, region, template, firstTemplate, timelinePoint, game);

        try
        {
            //add BrokenZeroG for Throne leadup
            if (BrokenGravRooms.Contains(name.ToUpperInvariant()) && self.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) == null)
                self.effects.Add(new(RoomSettings.RoomEffect.Type.BrokenZeroG, 0.5f, false));
        } catch (Exception ex) { Error(ex); }
    }


    //Add Hunter to LF_A14
    public static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign && self.abstractRoom.firstTimeRealized
                && self.abstractRoom.name.ToUpperInvariant() == "LF_A14" && self.game.TimelinePoint == SlugcatStats.Timeline.Red
                && (!self.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().TryGet(SAVE_KEY_HUNTER_NEURON, out bool hadNeuron) || !hadNeuron))
            {
                var pos = new WorldCoordinate(self.abstractRoom.index, 28, 15, 0);
                var hunter = new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, pos, new EntityID(-1, -1));
                //var hunter = new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, pos, self.game.GetNewID());
                hunter.rippleLayer = 1;
                hunter.state = new PlayerState(hunter, 0, SlugcatStats.Name.Red, true); //mark the player as a ghost using player0's controls
                hunter.state.Die(); //might also want to try using player3's controls; would help for live slugcats
                //(hunter.state as PlayerNPCState).forceFullGrown = true;
                //(hunter.state as PlayerNPCState).isPup = false;
                //(hunter.state as PlayerNPCState).slugcatCharacter = SlugcatStats.Name.Red;
                var neuron = new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.NSHSwarmer, null, pos, self.game.GetNewID());

                self.abstractRoom.AddEntity(hunter);
                self.abstractRoom.AddEntity(neuron);

                Debug("Spawned Hunter and his neuron (hopefully)");
            }
        }
        catch (Exception ex) { Error(ex); }

        orig(self);
    }

    //Achievement for picking up Hunter's neuron
    public static void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
    {
        orig(self, obj, graspUsed);

        try
        {
            if (Plugin.IsProtectorCampaign && obj.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.NSHSwarmer)
            {
                var data = self.room.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData();
                if (!data.TryGet(SAVE_KEY_HUNTER_NEURON, out bool hadNeuron) || !hadNeuron)
                {
                    AchievementManager.GrabbedHunterNeuron();
                    data.Set(SAVE_KEY_HUNTER_NEURON, true);
                }
            }
        }
        catch (Exception ex) { Error(ex); }
    }


    //Sets logic for when OE gate opens
    public static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
    {
        if (!Plugin.IsProtectorCampaign)
            return orig(self);
        try
        {
            if (self.room.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().TryGet(SAVE_KEY_OE_OPEN, out bool open) && open)
                return true; //save key says it's open; good enough for me!
        } catch (Exception ex) { Error(ex); }
        return false;
    }


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
