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
using static ProtectorCampaign.Constants;
using RWCustom;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

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
        MOD_VERSION = "0.0.3";

    //TODO: stuff

    public static Plugin Instance;

    public static ConfigOptions Options;
    public static ManualLogSource PublicLogger;

    public static bool IsProtectorCampaign = false;
    public static SlugcatStats.Name ProtectorName;

    #region Setup
    public Plugin()
    {
        try
        {
            Instance = this;
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
            On.Player.Die -= Player_Die;
            On.Player.Grabbed -= Player_Grabbed;
            On.Player.Update -= Player_Update;

            On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DefaultFaceSprite_float_int -= PlayerGraphics_DefaultFaceSprite_float_int;

            //On.Watcher.WarpPoint.WarpPointData.ctor -= WarpPointData_ctor;
            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues -= WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues;
            On.Room.TrySpawnWarpPoint_PlacedObject_bool -= Room_TrySpawnWarpPoint;
            WarpFatigueHook?.Undo();
            //OverworldTimelineHook?.Undo();
            //On.Watcher.WarpPoint.PerformWarp -= PupTracker.WarpPoint_PerformWarp;
            On.Watcher.WarpPoint.PerformWarp -= WarpPoint_PerformWarp;
            On.OverWorld.InitiateSpecialWarp_WarpPoint -= OverWorld_InitiateSpecialWarp_WarpPoint;
            On.OverWorld.Update -= OverWorld_Update;

            On.Watcher.WarpPoint.ProvideAir -= PupTracker.WarpPoint_ProvideAir;

            On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame -= WorldChanges.RoomSettings_ctor;
            On.Room.Loaded -= WorldChanges.Room_Loaded;
            On.Player.SlugcatGrab -= WorldChanges.Player_SlugcatGrab;
            On.RegionGate.customOEGateRequirements -= WorldChanges.RegionGate_customOEGateRequirements;
            On.HUD.Map.Update -= WorldChanges.Map_Update;
            PlayingAsSlugcatHook?.Undo();

            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues -= Conversations.WorldLoader_ctor;
            On.SSOracleBehavior.PebblesConversation.AddEvents -= Conversations.PebblesConversation_AddEvents;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= Conversations.MoonConversation_AddEvents;
            On.SSOracleBehavior.SeePlayer -= Conversations.SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.SSSleepoverBehavior.ctor -= Conversations.SSSleepoverBehavior_ctor;
            On.SLOracleBehaviorHasMark.InitateConversation -= Conversations.SLOracleBehaviorHasMark_InitateConversation;
            On.SLOracleBehaviorHasMark.GrabObject -= Conversations.SLOracleBehaviorHasMark_GrabObject;

            On.RainWorldGame.SpawnCritters -= PupTracker.RainWorldGame_SpawnCritters;
            On.Player.NPCStats.ctor -= PupTracker.NPCStats_ctor;
            On.ShelterDoor.DoorClosed -= PupTracker.ShelterDoor_DoorClosed;
            On.Player.Update -= PupTracker.Player_Update;
            On.Player.DeathByBiteMultiplier -= PupTracker.Player_DeathByBiteMultiplier;

            On.OverseersWorldAI.DirectionFinder.StoryRegionPrioritys -= OverseerChanges.DirectionFinder_StoryRegionPrioritys;
            On.OverseersWorldAI.DirectionFinder.StoryRoomInRegion -= OverseerChanges.DirectionFinder_StoryRoomInRegion;
            On.WorldLoader.OverseerSpawnConditions -= OverseerChanges.WorldLoader_OverseerSpawnConditions;

            IsInit = false;
        }
    }

    private Hook BackpackSlugpupHook;
    private Hook WarpFatigueHook;
    //private Hook OverworldTimelineHook;
    private Hook PlayingAsSlugcatHook;

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
            On.Player.Die += Player_Die;
            On.Player.Grabbed += Player_Grabbed;
            On.Player.Update += Player_Update;

            //Appearance
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DefaultFaceSprite_float_int += PlayerGraphics_DefaultFaceSprite_float_int;

            //Warps
            //On.Room.TrySpawnWarpPoint += Room_TrySpawnWarpPoint;
            On.Room.TrySpawnWarpPoint_PlacedObject_bool += Room_TrySpawnWarpPoint;
            try {
                WarpFatigueHook = new(typeof(StoryGameSession).GetProperty(nameof(StoryGameSession.warpTraversalsLeftUntilFullWarpFatigue)).GetGetMethod(),
                    StoryGameSession_WarpTraversalsLeftUntilFullWarpFatigue);
                //OverworldTimelineHook = new(typeof(OverWorld).GetProperty(nameof(OverWorld.PlayerTimelinePosition)).GetGetMethod(),
                    //Overworld_PlayerTimelinePosition);
            } catch (Exception ex) { Logger.LogError(ex); }
            //On.Watcher.WarpPoint.PerformWarp += PupTracker.WarpPoint_PerformWarp;
            On.Watcher.WarpPoint.PerformWarp += WarpPoint_PerformWarp;
            On.OverWorld.InitiateSpecialWarp_WarpPoint += OverWorld_InitiateSpecialWarp_WarpPoint;
            On.OverWorld.Update += OverWorld_Update;

            On.Watcher.WarpPoint.ProvideAir += PupTracker.WarpPoint_ProvideAir;

            //World changes
            On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame += WorldChanges.RoomSettings_ctor;
            On.Room.Loaded += WorldChanges.Room_Loaded;
            On.Player.SlugcatGrab += WorldChanges.Player_SlugcatGrab;
            On.RegionGate.customOEGateRequirements += WorldChanges.RegionGate_customOEGateRequirements;
            On.HUD.Map.Update += WorldChanges.Map_Update;
            try {
                PlayingAsSlugcatHook = new(typeof(PlayerProgression).GetProperty(nameof(PlayerProgression.PlayingAsSlugcat)).GetGetMethod(), WorldChanges.PlayerProgression_PlayingAsSlugcat);
            } catch (Exception ex) { Logger.LogError(ex); }

            //conversations
            //On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += Conversations.WorldLoader_ctor;
            //On.OverWorld.InitiateSpecialWarp_WarpPoint += Conversations.OverWorld_InitiateSpecialWarp_WarpPoint;
            On.SSOracleBehavior.PebblesConversation.AddEvents += Conversations.PebblesConversation_AddEvents;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += Conversations.MoonConversation_AddEvents;
            On.SSOracleBehavior.SeePlayer += Conversations.SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.SSSleepoverBehavior.ctor += Conversations.SSSleepoverBehavior_ctor;
            On.SLOracleBehaviorHasMark.InitateConversation += Conversations.SLOracleBehaviorHasMark_InitateConversation;
            On.SLOracleBehaviorHasMark.GrabObject += Conversations.SLOracleBehaviorHasMark_GrabObject;

            //slugpup hooks
            On.RainWorldGame.SpawnCritters += PupTracker.RainWorldGame_SpawnCritters;
            On.Player.NPCStats.ctor += PupTracker.NPCStats_ctor;
            On.ShelterDoor.DoorClosed += PupTracker.ShelterDoor_DoorClosed;
            On.Player.Update += PupTracker.Player_Update;
            On.Player.DeathByBiteMultiplier += PupTracker.Player_DeathByBiteMultiplier;

            //overseer hooks
            On.OverseersWorldAI.DirectionFinder.StoryRegionPrioritys += OverseerChanges.DirectionFinder_StoryRegionPrioritys;
            On.OverseersWorldAI.DirectionFinder.StoryRoomInRegion += OverseerChanges.DirectionFinder_StoryRoomInRegion;
            On.WorldLoader.OverseerSpawnConditions += OverseerChanges.WorldLoader_OverseerSpawnConditions;

            //test hooks
            On.Player.SwallowObject += Player_SwallowObject;
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;

            ProtectorName = new("LZC_Protector");

            //register fake passage icons for displaying health
            TryRegisterHealthSprites();

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

            PupTracker.MaybeSpawnSlugpup(self, self.room.world);
        }
    }

    //Make menu show timeline
    private void SlugcatPageContinue_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        orig(self, menu, owner, pageIndex, slugcatNumber);

        try
        {
            if (slugcatNumber == ProtectorName)
            {
                //VERY expensive operation, but get the save state
                var saveState = Custom.rainWorld.progression.GetOrInitiateSaveState(slugcatNumber, null, menu.manager.menuSetup, false);
                self.regionLabel.text = SlugcatStats.getSlugcatName(TimelineToSlugcat(saveState.currentTimelinePosition)) + " - " + self.regionLabel.text;
            }
        } catch (Exception ex) { Logger.LogError(ex); }
    }

    #endregion

    #region Startup

    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        //Clear misc tables and vars
        try
        {
            PlayerInfos.Clear();
        } catch (Exception ex) { Logger.LogError(ex); }

        try
        {
            //detect whether it's a protector campaign
            IsProtectorCampaign = manager.rainWorld.progression.PlayingAsSlugcat == ProtectorName;
            Logger.LogDebug("IsProtectorCampaign: " + IsProtectorCampaign);
            PupTracker.ShelterDoorClosed = false;
        } catch (Exception ex) { Logger.LogError(ex); IsProtectorCampaign = false; }

        orig(self, manager);

        try
        {
            //double-check it is a story session
            if (!self.IsStorySession)
                IsProtectorCampaign = false;

            //setup pup tracker
            if (IsProtectorCampaign)
            {
                PupTracker.CycleStarted(self);

                //ModManager.CoopAvailable = true; //fixes permaDeaths not being triggered properly
            }
        }
        catch (Exception ex) { Logger.LogError(ex); IsProtectorCampaign = false; }
    }


    //Set player stats
    //Spawn slugpup
    //Give stomach pearl
    //enable back-spear
    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        try
        {
            if (self.slugcatStats.name == ProtectorName)
            {
                SetPlayerStats(self);

                //enable back-spear
                self.spearOnBack ??= new(self);
            }
            if (IsProtectorCampaign && self.playerState.playerNumber == 0 && world.game.Players[0] == abstractCreature && world.game.IsStorySession)
            {
                if (self.room.abstractRoom.name == "SS_AI" && world.game.GetStorySession.saveState.cycleNumber == 0) //start of the game
                {
                    //spawn slugpup
                    PupTracker.TrySpawnSlugpup(this, self, world);
                    //PupTracker.TrySpawnSlugpup(self, world);

                    //stomach pearl
                    self.objectInStomach = new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                        new(self.room.abstractRoom.index, -1, -1, 0), world.game.GetNewID(), -1,
                        -1, null, DataPearl.AbstractDataPearl.DataPearlType.Red_stomach);
                    Logger.LogDebug("Pearl in stomach: " + self.objectInStomach);
                }
                //else //later in the game
                    //PupTracker.MaybeSpawnSlugpup(this, self, world); //happens much too late!
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    #endregion

    #region PlayerStats

    private bool healthSpritesRegistered = false;
    private void TryRegisterHealthSprites()
    {
        if (healthSpritesRegistered) return;
        try
        {
            //FSprite fSprite = new("Kill_Slugcat", true);
            var element = Futile.atlasManager.GetElementWithName("Kill_Slugcat");
            if (element != null)
            {
                Futile.atlasManager._allElementsByName.Add(Protector_Health_String + "A", element);
                Futile.atlasManager._allElementsByName.Add(Protector_Health_String + "B", element);
                healthSpritesRegistered = true;
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }
    
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
                TryRegisterHealthSprites();

                var menu = self.menu as KarmaLadderScreen;
                var tracker = new WinState.IntegerTracker(new(Protector_Health_String, false), 0, MIN_HEALTH - 1, MIN_HEALTH, MAX_HEALTH + 1);
                tracker.lastShownProgress = self.playerDeath ? lastHealth + lastDeltaHealth : lastHealth; //don't show progress if already dead
                tracker.progress = lastHealth + lastDeltaHealth;
                menu.winState.endgameTrackers.Add(tracker);
            }
        } catch (Exception ex) { Logger.LogError(ex); }

        orig(self);
    }

    private struct PlayerInfo
    {
        public float shockChance;
        public float shockQueued;
    }
    //private ConditionalWeakTable<Player, PlayerInfo> PlayerInfos = new();
    private Dictionary<int, PlayerInfo> PlayerInfos = new(4);

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

        if (!PlayerInfos.ContainsKey(self.playerState.playerNumber))
        {
            int w = 0;
            if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().TryGet(SAVE_KEY_WARPS_SPAWNED, out int savedWarps))
                w = savedWarps;

            PlayerInfos.Add(self.playerState.playerNumber, new() {
                shockChance = BASE_SHOCK_CHANCE + h * SHOCK_CHANCE_PER_HEALTH + w * SHOCK_CHANCE_PER_WARP,
                shockQueued = 0
            });
        }

        Logger.LogDebug($"Set player stats. throwing: {self.slugcatStats.throwingSkill}. weight: {self.slugcatStats.bodyWeightFac}. run: {self.slugcatStats.runspeedFac}. corridor: {self.slugcatStats.corridorClimbSpeedFac}. pole: {self.slugcatStats.poleClimbSpeedFac}. swim: {self.slugcatStats.swimForceFac}");
    }

    //Prevent backpacking slugpups
    private bool Player_CanPutSlugToBack(Func<Player, bool> orig, Player self)
    {
        if (self.SlugCatClass == ProtectorName) return false;
        return orig(self);
    }


    private void Player_Die(On.Player.orig_Die orig, Player self)
    {
        try
        {
            //exclude permaDeaths; we shouldn't block against those (getting pulled in den or falling down pit)
            if (self.SlugCatClass == ProtectorName && !self.playerState.permaDead
                && PlayerInfos.TryGetValue(self.playerState.playerNumber, out var info)
                && UnityEngine.Random.value < info.shockChance)
            {
                Logger.LogDebug("Preventing player death. shock chance: " + info.shockChance);

                info.shockChance -= DEATH_BLOCK_WEIGHT;
                info.shockQueued = DEATH_BLOCK_WEIGHT;
                PlayerInfos[self.playerState.playerNumber] = info;

                //PlayerReleaseShock(self, DEATH_BLOCK_WEIGHT);

                return; //don't die
            }
        } catch (Exception ex) { Logger.LogError(ex); }

        orig(self);
    }

    private void Player_Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
    {
        orig(self, grasp);

        try
        {
            //don't shock other players
            if (self.SlugCatClass == ProtectorName && grasp.grabber != null && grasp.grabber is not Player
                && PlayerInfos.TryGetValue(self.playerState.playerNumber, out var info)
                && UnityEngine.Random.value < info.shockChance)
            {
                Logger.LogDebug("Preventing player grabbed. shock chance: " + info.shockChance);

                float strength = grasp.grabber.TotalMass / self.TotalMass * CREATURE_SHOCK_WEIGHT;
                info.shockChance -= strength;
                info.shockQueued = strength;
                PlayerInfos[self.playerState.playerNumber] = info;

                //grasp.grabber.Stun(120);
                //self.room.AddObject(new CreatureSpasmer(grasp.grabber, false, grasp.grabber.stun));
                //grasp.grabber.LoseAllGrasps(); //drop him!
                //self.dangerGrasp = null; //make sure this thing doesn't stick around too long

                //PlayerReleaseShock(self, strength);
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    //Release the queued shock
    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        try
        {
            if (PlayerInfos.TryGetValue(self.playerState.playerNumber, out PlayerInfo info) && info.shockQueued > 0)
            {
                Creature.Grasp[] tempList = self.grabbedBy.ToArray();
                foreach (var g in tempList)
                {
                    g.grabber.Stun(120);
                    self.room.AddObject(new CreatureSpasmer(g.grabber, false, g.grabber.stun));
                    g.grabber.LoseAllGrasps();
                }
                PlayerReleaseShock(self, info.shockQueued);

                info.shockQueued = 0;
                PlayerInfos[self.playerState.playerNumber] = info;
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }

        orig(self, eu);
    }

    //mostly cosmetic shock results
    private void PlayerReleaseShock(Player self, float strength)
    {
        Logger.LogDebug("Player releasing shock. Strength = " + strength);

        self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk, false, 0.25f * strength, 1f);

        self.AddMud(Mathf.CeilToInt(200 * strength), 400, new(0.5f, 0.5f, 1)); //add light-blue mud to player for up to 10 seconds

        //add sparks
        for (int i = 0; i < 10; i++)
            self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * 15, new(0.5f, 0.5f, 1), null, 16, 28));

        //add centipede underwater shock effect, which does deal some damage
        self.room.AddObject(new UnderwaterShock(self.room, self, self.mainBodyChunk.pos,
            Mathf.CeilToInt(20 * strength), 100 * strength, Mathf.Max(0.1f * strength, 0.7f),
            self, new(0.7f, 0.7f, 1)));
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
    private WarpPoint Room_TrySpawnWarpPoint(On.Room.orig_TrySpawnWarpPoint_PlacedObject_bool orig, Room self, PlacedObject po, bool saveInRegionState)
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

                var wp = orig(self, po, saveInRegionState);

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
        return orig(self, po, saveInRegionState);
    }

    //disable warp fatigue; it's annoying while testing
    private int StoryGameSession_WarpTraversalsLeftUntilFullWarpFatigue(Func<StoryGameSession, int> orig, StoryGameSession self)
    {
        return IsProtectorCampaign ? 9999 : orig(self);
    }

    private SlugcatStats.Timeline Overworld_PlayerTimelinePosition(Func<OverWorld, SlugcatStats.Timeline> orig, OverWorld self)
    {
        return self.warpData != null ? self.warpData.destTimeline : orig(self);
    }

    //correctly set the timeline
    private void WarpPoint_PerformWarp(On.Watcher.WarpPoint.orig_PerformWarp orig, WarpPoint self)
    {
        Conversations.UpdateTimeline(self);
        PupTracker.WarpPerformed(self);

        orig(self);

        try
        {
            self.room.game.GetStorySession.saveState.currentTimelinePosition = self.Data.destTimeline;
            Logger.LogDebug("Warping to timeline " + self.Data.destTimeline);
        } catch (Exception ex) { Logger.LogError(ex); }
    }

    private void OverWorld_InitiateSpecialWarp_WarpPoint(On.OverWorld.orig_InitiateSpecialWarp_WarpPoint orig, OverWorld self, ISpecialWarp callback, WarpPoint.WarpPointData warpData, bool useNormalWarpLoader)
    {
        SlugcatStats.Timeline source = self.game.TimelinePoint, dest = warpData.destTimeline;
        orig(self, callback, warpData, useNormalWarpLoader);

        try
        {
            warpData.destTimeline = dest;
            self.game.GetStorySession.saveState.currentTimelinePosition = source;
            Logger.LogDebug($"Aborted switching timelines. Current timelines: source={source}, dest={dest}");
        }
        catch (Exception ex) { Logger.LogError(ex); }
    }

    private void OverWorld_Update(On.OverWorld.orig_Update orig, OverWorld self)
    {
        try
        {
            if (self.warpingPreload && Region.RegionReadyToWarp)
            {
                var save = self.game.GetStorySession.saveState;
                var source = save.currentTimelinePosition;
                save.currentTimelinePosition = self.warpData.destTimeline;
                orig(self);
                save.currentTimelinePosition = source;

                Logger.LogDebug($"Loading warp world loader using timeline {self.warpData.destTimeline} instead of {source}");
                return;
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }

        orig(self);
    }

    #endregion

    #region Tools
    /// <summary>
    /// Returns the first Slugcat that uses this timeline, or just directly converts the timeline as a last resort.
    /// </summary>
    /// <param name="timeline">The timeline to convert to a SlugcatStats.Name</param>
    /// <returns></returns>
    public static SlugcatStats.Name TimelineToSlugcat(SlugcatStats.Timeline timeline)
    {
        foreach (string name in SlugcatStats.Name.values.entries)
        {
            if (SlugcatStats.SlugcatToTimeline(new(name)) == timeline)
                return new(name);
        }
        return new(timeline.value);
    }
    #endregion

}
