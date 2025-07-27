using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using Unity.Mathematics;
using RWCustom;
using Watcher;
using UnityEngine.Rendering;
using Graphics = UnityEngine.Graphics;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using BepInEx.Logging;
using System.Security.Cryptography;
using SlugBase;
using SlugBase.SaveData;
using MonoMod.RuntimeDetour;
using MoreSlugcats;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ProtectorCampaign;

[BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.ProtectorCampaign",
        MOD_NAME = "ProtectorCampaign",
        MOD_VERSION = "0.0.1";

    //TODO: stuff

    public static ConfigOptions Options;
    public static ManualLogSource PublicLogger;

    public static bool IsProtectorCampaign = false;
    public static SlugcatStats.Name ProtectorName;

    #region Setup
    public Plugin()
    {
        try
        {
            PublicLogger = Logger;
            Options = new ConfigOptions();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }
    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= RainWorldOnOnModsInit;
        if (IsInit)
        {
            On.RainWorldGame.ctor -= RainWorldGame_ctor;
            On.SaveState.SessionEnded -= SaveState_SessionEnded;
            On.Player.ctor -= Player_ctor;
            BackpackSlugpupHook?.Undo();
            On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DefaultFaceSprite_float_int -= PlayerGraphics_DefaultFaceSprite_float_int;

            //On.Watcher.WarpPoint.WarpPointData.ctor -= WarpPointData_ctor;
            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues -= WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues;
            On.Room.TrySpawnWarpPoint -= Room_TrySpawnWarpPoint;
            On.Watcher.WarpPoint.PerformWarp -= PupTracker.WarpPoint_PerformWarp;
            On.Watcher.WarpPoint.ProvideAir -= PupTracker.WarpPoint_ProvideAir;

            On.SSOracleBehavior.PebblesConversation.AddEvents -= Conversations.PebblesConversation_AddEvents;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= Conversations.MoonConversation_AddEvents;

            On.Player.NPCStats.ctor -= PupTracker.NPCStats_ctor;
            On.ShelterDoor.DoorClosed -= PupTracker.ShelterDoor_DoorClosed;
            On.Player.Update -= PupTracker.Player_Update;

            IsInit = false;
        }
    }

    private Hook BackpackSlugpupHook;
    private Hook SuckInCreaturesHook;

    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.Player.ctor += Player_ctor;
            try
            {
                BackpackSlugpupHook = new(typeof(Player).GetProperty(nameof(Player.CanPutSlugToBack)).GetGetMethod(), Player_CanPutSlugToBack);
            } catch (Exception ex) { Logger.LogError(ex); }

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DefaultFaceSprite_float_int += PlayerGraphics_DefaultFaceSprite_float_int;

            //On.Watcher.WarpPoint.WarpPointData.ctor += WarpPointData_ctor;
            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues;
            On.Room.TrySpawnWarpPoint += Room_TrySpawnWarpPoint;
            On.Watcher.WarpPoint.PerformWarp += PupTracker.WarpPoint_PerformWarp;
            On.Watcher.WarpPoint.ProvideAir += PupTracker.WarpPoint_ProvideAir;

            On.SSOracleBehavior.PebblesConversation.AddEvents += Conversations.PebblesConversation_AddEvents;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += Conversations.MoonConversation_AddEvents;

            //slugpup hooks
            On.Player.NPCStats.ctor += PupTracker.NPCStats_ctor;
            On.ShelterDoor.DoorClosed += PupTracker.ShelterDoor_DoorClosed;
            On.Player.Update += PupTracker.Player_Update;

            //test hooks
            On.Player.SwallowObject += Player_SwallowObject;

            ProtectorName = new("LZC_Protector");

            Conversations.RegisterConversations();
            MachineConnector.SetRegisteredOI(MOD_ID, Options);
            IsInit = true;

            Logger.LogDebug("Applied hooks");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    #endregion

    #region Misc_Hooks

    //temporary means of spawning warp points
    private void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        orig(self, grasp);

        //SetTimelineToWatcher = true;
        //self.abstractPhysicalObject.world.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = true;
        //self.SpawnDynamicWarpPoint();
        PupTracker.WarpChance += 1f;
        Logger.LogDebug("WarpChance: " + PupTracker.WarpChance);
    }

    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        try
        {
            IsProtectorCampaign = manager.rainWorld.progression.PlayingAsSlugcat == ProtectorName;
            Logger.LogDebug("IsProtectorCampaign: " + IsProtectorCampaign);
        } catch (Exception ex) { Logger.LogError(ex); IsProtectorCampaign = false; }

        orig(self, manager);

        try
        {
            if (IsProtectorCampaign)
                PupTracker.CycleStarted(self);
        }
        catch (Exception ex) { Logger.LogError(ex); IsProtectorCampaign = false; }
    }

    //Set food always to 0
    private void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
    {
        try
        {
            if (IsProtectorCampaign && survived && !self.lastMalnourished) //if recovering from starving, don't decrease health
            {
                var player = (game.session.Players[0].realizedCreature as Player);
                int deltaHealth = newMalnourished ? STARVE_PENALTY : player.FoodInRoom(true) - FOOD_GOAL;
                //var data = self.progression.miscProgressionData.GetSlugBaseData();
                var data = self.miscWorldSaveData.GetSlugBaseData();
                int curHealth = 0;
                if (data.TryGet(SAVE_KEY_HEALTH, out int health))
                    curHealth = health;
                data.Set(SAVE_KEY_HEALTH, Math.Max(MIN_HEALTH, Math.Min(MAX_HEALTH, curHealth + deltaHealth)));

                if (!newMalnourished) //for starving, just keep the default handling
                        //set food to exactly the amount required to hibernate
                    player.playerState.foodInStomach = game.GetStorySession.characterStats.foodToHibernate;
                    //self.food = game.GetStorySession.characterStats.foodToHibernate;
                Logger.LogDebug($"Set food to 0. deltaHealth: {deltaHealth}. new health: {curHealth + deltaHealth}");
            }
        } catch (Exception ex) { Logger.LogError(ex); }

        orig(self, game, survived, newMalnourished);
    }

    //Set player stats
    //Also spawn slugpup
    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        try
        {
            if (self.slugcatStats.name == ProtectorName)
            {
                int h = 0;
                //if (self.room.game.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet(SAVE_KEY_HEALTH, out int savedHealth))
                if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().TryGet(SAVE_KEY_HEALTH, out int savedHealth))
                    h = savedHealth;
                else
                    Logger.LogError("Could not find health in save data");
                Logger.LogDebug("Player health: " + h);

                self.slugcatStats.throwingSkill = h < 0 ? 0 : (h > 25 ? 2 : 1);
                self.slugcatStats.bodyWeightFac += h * 0.01f; //health slightly affects body weight
                self.slugcatStats.runspeedFac += h * 0.01f;
                self.slugcatStats.corridorClimbSpeedFac += h * 0.01f;
                self.slugcatStats.poleClimbSpeedFac += h * 0.01f;
                self.slugcatStats.swimForceFac += h * 0.01f;

                Logger.LogDebug($"Set player stats. throwing: {self.slugcatStats.throwingSkill}. weight: {self.slugcatStats.bodyWeightFac}. run: {self.slugcatStats.runspeedFac}. corridor: {self.slugcatStats.corridorClimbSpeedFac}. pole: {self.slugcatStats.poleClimbSpeedFac}. swim: {self.slugcatStats.swimForceFac}");

                PupTracker.TrySpawnSlugpup(self, world);
            }
        } catch (Exception ex) { Logger.LogError(ex); }
    }

    //Prevent backpacking slugpups
    private bool Player_CanPutSlugToBack(Func<Player, bool> orig, Player self)
    {
        if (self.SlugCatClass == ProtectorName) return false;
        return orig(self);
    }

    //Player weird eye
    private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        if (self.player.SlugCatClass == ProtectorName)
        {
            self.player.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
            orig(self, sLeaser, rCam);
            self.player.SlugCatClass = ProtectorName;
        }
        else
            orig(self, sLeaser, rCam);
    }

    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (self.player.SlugCatClass == ProtectorName)
        {
            self.player.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
            orig(self, sLeaser, rCam, timeStacker, camPos);
            self.player.SlugCatClass = ProtectorName;
        }
        else
            orig(self, sLeaser, rCam, timeStacker, camPos);
    }

    private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        if (self.player.SlugCatClass == ProtectorName)
        {
            self.player.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
            orig(self, sLeaser, rCam, palette);
            self.player.SlugCatClass = ProtectorName;
            if (!(ModManager.CoopAvailable && self.useJollyColor) && !PlayerGraphics.CustomColorsEnabled())
                sLeaser.sprites[12].color = new(10f / 16f, 1f / 16f, 10f / 16f);
        }
        else
            orig(self, sLeaser, rCam, palette);
    }

    //Give player normal, non-squinting eyes
    private string PlayerGraphics_DefaultFaceSprite_float_int(On.PlayerGraphics.orig_DefaultFaceSprite_float_int orig, PlayerGraphics self, float eyeScale, int imgIndex)
    {
        string ret;
        if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.player.slugcatStats.name == ProtectorName)
        {
            self.player.SlugCatClass = ProtectorName;
            ret = orig(self, eyeScale, imgIndex);
            self.player.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Artificer;
        }
        else
            ret = orig(self, eyeScale, imgIndex);
        return ret;
    }


    public static bool SlugpupWarp = false;
    //Makes it possible for warps to actually spawn
    private WarpPoint Room_TrySpawnWarpPoint(On.Room.orig_TrySpawnWarpPoint orig, Room self, PlacedObject po, bool saveInRegionState, bool skipIfInRegionState, bool deathPersistent)
    {
        try
        {
            if (IsProtectorCampaign)
            {
                var data = po.data as WarpPoint.WarpPointData;
                data.accessibility = WarpPoint.WarpPointData.WarpPointSpawnCondition.AnySlugcat;
                if (SlugpupWarp)
                {
                    data.oneWay = true;
                    data.oneWayEntrance = true;
                    data.uses = 1;
                    data.limitedUse = true;
                    Logger.LogDebug("Made warp one-way");
                }

                var wp = orig(self, po, saveInRegionState, skipIfInRegionState, deathPersistent);

                if (wp != null)
                {
                    if (SlugpupWarp) wp.Data.destTimeline = SlugcatStats.Timeline.Watcher;
                    SlugpupWarp = false; //overrides timeline
                }

                //Logger.LogDebug("Warp point: " + wp);
                Logger.LogDebug("Trying to spawn warp point in room " + self.abstractRoom.name + ". Result: " + wp);

                return wp;
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
        return orig(self, po, saveInRegionState, skipIfInRegionState, deathPersistent);
    }

    #endregion

}
