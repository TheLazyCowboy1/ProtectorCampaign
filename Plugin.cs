using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using Watcher;
using BepInEx.Logging;
using SlugBase.SaveData;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Menu;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ProtectorCampaign;

[BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("ddemile.fake_achievements", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.ProtectorCampaign",
        MOD_NAME = "Protector Campaign",
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
            On.Menu.KarmaLadder.AddEndgameMeters += KarmaLadder_AddEndgameMeters;
            On.Player.ctor -= Player_ctor;
            BackpackSlugpupHook?.Undo();

            On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DefaultFaceSprite_float_int -= PlayerGraphics_DefaultFaceSprite_float_int;

            //On.Watcher.WarpPoint.WarpPointData.ctor -= WarpPointData_ctor;
            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues -= WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues;
            On.Room.TrySpawnWarpPoint -= Room_TrySpawnWarpPoint;
            WarpFatigueHook?.Undo();
            On.Watcher.WarpPoint.PerformWarp -= PupTracker.WarpPoint_PerformWarp;
            On.Watcher.WarpPoint.ProvideAir -= PupTracker.WarpPoint_ProvideAir;

            On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame -= RoomSettings_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame;

            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues -= Conversations.WorldLoader_ctor;
            On.OverWorld.InitiateSpecialWarp_WarpPoint -= Conversations.OverWorld_InitiateSpecialWarp_WarpPoint;
            On.SSOracleBehavior.PebblesConversation.AddEvents -= Conversations.PebblesConversation_AddEvents;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= Conversations.MoonConversation_AddEvents;
            On.SSOracleBehavior.SeePlayer -= Conversations.SSOracleBehavior_SeePlayer;
            On.SLOracleBehaviorHasMark.InitateConversation -= Conversations.SLOracleBehaviorHasMark_InitateConversation;

            On.Player.NPCStats.ctor -= PupTracker.NPCStats_ctor;
            On.ShelterDoor.DoorClosed -= PupTracker.ShelterDoor_DoorClosed;
            On.Player.Update -= PupTracker.Player_Update;

            IsInit = false;
        }
    }

    private Hook BackpackSlugpupHook;
    private Hook WarpFatigueHook;

    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            //Setup
            On.RainWorldGame.ctor += RainWorldGame_ctor;

            //Player health/stats/abilities
            On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.Menu.KarmaLadder.AddEndgameMeters += KarmaLadder_AddEndgameMeters;
            On.Player.ctor += Player_ctor;
            try {
                BackpackSlugpupHook = new(typeof(Player).GetProperty(nameof(Player.CanPutSlugToBack)).GetGetMethod(),
                    Player_CanPutSlugToBack);
            } catch (Exception ex) { Logger.LogError(ex); }

            //Appearance
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DefaultFaceSprite_float_int += PlayerGraphics_DefaultFaceSprite_float_int;

            //Warps
            On.Room.TrySpawnWarpPoint += Room_TrySpawnWarpPoint;
            try {
                WarpFatigueHook = new(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.warpTraversalsLeftUntilFullWarpFatigue)).GetGetMethod(),
                    StoryGameSession_WarpTraversalsLeftUntilFullWarpFatigue);
            } catch (Exception ex) { Logger.LogError(ex); }
            On.Watcher.WarpPoint.PerformWarp += PupTracker.WarpPoint_PerformWarp;
            On.Watcher.WarpPoint.ProvideAir += PupTracker.WarpPoint_ProvideAir;

            //World changes
            On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame += RoomSettings_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame;

            //conversations
            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += Conversations.WorldLoader_ctor;
            On.OverWorld.InitiateSpecialWarp_WarpPoint += Conversations.OverWorld_InitiateSpecialWarp_WarpPoint;
            On.SSOracleBehavior.PebblesConversation.AddEvents += Conversations.PebblesConversation_AddEvents;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += Conversations.MoonConversation_AddEvents;
            On.SSOracleBehavior.SeePlayer += Conversations.SSOracleBehavior_SeePlayer;
            On.SLOracleBehaviorHasMark.InitateConversation += Conversations.SLOracleBehaviorHasMark_InitateConversation;

            //slugpup hooks
            On.Player.NPCStats.ctor += PupTracker.NPCStats_ctor;
            On.ShelterDoor.DoorClosed += PupTracker.ShelterDoor_DoorClosed;
            On.Player.Update += PupTracker.Player_Update;

            //test hooks
            On.Player.SwallowObject += Player_SwallowObject;

            ProtectorName = new("LZC_Protector");

            //register fake passage icons for displaying health
            try
            {
                //FSprite fSprite = new("Kill_Slugcat", true);
                var element = Futile.atlasManager.GetElementWithName("Kill_Slugcat");
                Futile.atlasManager._allElementsByName.Add(Protector_Health_String + "A", element);
                Futile.atlasManager._allElementsByName.Add(Protector_Health_String + "B", element);
            } catch (Exception ex) { Logger.LogError(ex); }

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
        if (IsProtectorCampaign)
        {
            PupTracker.WarpChance += 1f;
            Logger.LogDebug("WarpChance: " + PupTracker.WarpChance);
        }
    }

    #endregion

    #region Startup

    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        try
        {
            //detect whether it's a protector campaign
            IsProtectorCampaign = manager.rainWorld.progression.PlayingAsSlugcat == ProtectorName;
            Logger.LogDebug("IsProtectorCampaign: " + IsProtectorCampaign);
        } catch (Exception ex) { Logger.LogError(ex); IsProtectorCampaign = false; }

        orig(self, manager);

        try
        {
            //setup pup tracker
            if (IsProtectorCampaign)
            {
                PupTracker.CycleStarted(self);
            }
        }
        catch (Exception ex) { Logger.LogError(ex); IsProtectorCampaign = false; }
    }


    //Set player stats
    //Spawn slugpup
    //Give stomach pearl
    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        try
        {
            if (self.slugcatStats.name == ProtectorName)
            {
                SetPlayerStats(self);
            }
            if (IsProtectorCampaign && self.playerState.playerNumber == 0 && world.game.Players[0] == abstractCreature && world.game.IsStorySession
                && self.room.abstractRoom.name == "SS_AI" && world.game.GetStorySession.saveState.cycleNumber == 0)
            {
                //spawn slugpup
                StartCoroutine(PupTracker.TrySpawnSlugpup(self, world));
                //PupTracker.TrySpawnSlugpup(self, world);

                //stomach pearl
                self.objectInStomach = new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                    new(self.room.abstractRoom.index, -1, -1, 0), world.game.GetNewID(), -1,
                    -1, null, DataPearl.AbstractDataPearl.DataPearlType.Red_stomach);
                Logger.LogDebug("Pearl in stomach: " + self.objectInStomach);
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    #endregion

    #region PlayerStats

    //vars used for UI display in sleep screen
    private int lastHealth = 0, lastDeltaHealth = 0;
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
                deltaHealth = Mathf.Clamp(deltaHealth, MIN_HEALTH - curHealth, MAX_HEALTH - curHealth);
                data.Set(SAVE_KEY_HEALTH, Mathf.Clamp(curHealth + deltaHealth, MIN_HEALTH, MAX_HEALTH)); //this clamp should be redundant

                if (!newMalnourished) //for starving, just keep the default handling
                        //set food to exactly the amount required to hibernate
                    player.playerState.foodInStomach = game.GetStorySession.characterStats.foodToHibernate;
                    //self.food = game.GetStorySession.characterStats.foodToHibernate;
                Logger.LogDebug($"Set food to 0. deltaHealth: {deltaHealth}. new health: {curHealth + deltaHealth}");

                lastHealth = curHealth; lastDeltaHealth = deltaHealth; //used for UI display
            }
        } catch (Exception ex) { Logger.LogError(ex); }

        orig(self, game, survived, newMalnourished);
    }

    //visualize the health change for the player's convenience
    private void KarmaLadder_AddEndgameMeters(On.Menu.KarmaLadder.orig_AddEndgameMeters orig, Menu.KarmaLadder self)
    {
        try
        {
            if (IsProtectorCampaign)
            {
                var menu = self.menu as KarmaLadderScreen;
                var tracker = new WinState.IntegerTracker(new(Protector_Health_String, false), 0, MIN_HEALTH - 1, MIN_HEALTH, MAX_HEALTH + 1);
                tracker.lastShownProgress = self.playerDeath ? lastHealth + lastDeltaHealth : lastHealth; //don't show progress if already dead
                tracker.progress = lastHealth + lastDeltaHealth;
                menu.winState.endgameTrackers.Add(tracker);
            }
        } catch (Exception ex) { Logger.LogError(ex); }

        orig(self);
    }

    private void SetPlayerStats(Player self)
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
    }

    //Prevent backpacking slugpups
    private bool Player_CanPutSlugToBack(Func<Player, bool> orig, Player self)
    {
        if (self.SlugCatClass == ProtectorName) return false;
        return orig(self);
    }

    #endregion

    #region PlayerAppearance

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

    #endregion

    #region WarpLogic

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

    //disable warp fatigue; it's annoying while testing
    private int StoryGameSession_WarpTraversalsLeftUntilFullWarpFatigue(Func<StoryGameSession, int> orig, StoryGameSession self)
    {
        return IsProtectorCampaign ? 9999 : orig(self);
    }

    #endregion

}
