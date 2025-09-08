using MoreSlugcats;
using SlugBase.SaveData;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;
using static ProtectorCampaign.Constants;

namespace ProtectorCampaign;

public static class PupTracker
{
    public static AbstractCreature Slugpup;
    public static float WarpChance = 0;

    //changed to a coroutine so we can safely wait for the aimap to generate
    private static IEnumerator SpawnSlugpupCoroutine(Player player, World world, EntityID? setID = null)
    {
        //try to fix AIMap
        bool hadToWait = player?.room?.aimap == null;
        while (player?.room != null && player.room.aimap == null) yield return null;
        try
        {
            Debug("Trying to spawn slugpup. Had to wait for AiMap: " + hadToWait);

            EntityID ID;
            if (setID == null)
            {
                ID = world.game.GetNewID();
                //save slugpup ID
                var data = world.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData();
                data.Set(SAVE_KEY_SLUGPUP_ID, ID);
                data.Set(SAVE_KEY_PUP_REQUIRED, true);
            }
            else
                ID = setID.Value;

            //create slugpup
            Slugpup = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, player.abstractPhysicalObject.pos, ID);
            var state = Slugpup.state as PlayerNPCState;
            state.Glowing = true;

            //realize slugpup
            player.room.abstractRoom.AddEntity(Slugpup);
            Slugpup.RealizeInRoom();
            if (setID == null)
                Slugpup.realizedCreature.mainBodyChunk.pos = new(300, 300); //tile 15,15
            else
                Slugpup.realizedCreature.mainBodyChunk.pos = player.mainBodyChunk.pos; //spawn on top of player if it's a second spawn

            Debug("Spawned slugpup! ID: " + ID);
        } catch (Exception ex) { Error(ex); }
        yield break;
    }

    public static void TrySpawnSlugpup(MonoBehaviour plugin, Player player, World world) => plugin.StartCoroutine(SpawnSlugpupCoroutine(player, world));

    //Spawn the slugpup if it should exist but doesn't
    public static void MaybeSpawnSlugpup(Player player, World world)
    {
        var data = world.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData();
        if (data.TryGet(SAVE_KEY_SLUGPUP_ID, out EntityID ID)) //there SHOULD be a slugpup
        {
            //find the slugpup
            bool done = false;
            AbstractCreature crit = null;
            foreach (var room in world.abstractRooms)
            {
                foreach (var ac in room.creatures)
                {
                    if (ac.ID == ID)
                    {
                        crit = ac;
                        done = true;
                        break;
                    }
                }
                if (done) break;
            }

            if (crit == null)
            {
                Debug("MISSING SLUGPUP!!! Trying to spawn a new one...");
                Plugin.Instance.StartCoroutine(SpawnSlugpupCoroutine(player, world, ID)); //use old ID
            }
            else if (crit.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)
            {
                var newID = world.game.GetNewID();
                Debug("Another entity has stolen the slugpup's ID. NEW SLUGPUP ID: " + newID);
                Plugin.Instance.StartCoroutine(SpawnSlugpupCoroutine(player, world, newID)); //get a new ID
            }
        }
    }

    //After all creatures have been loaded into shelters, ensure that we have the slugpup!
    public static void RainWorldGame_SpawnCritters(On.RainWorldGame.orig_SpawnCritters orig, RainWorldGame self, System.Collections.Generic.List<string> objs, AbstractCreature player)
    {
        orig(self, objs, player);

        try
        {
            if (Slugpup == null) //only search for pup if it hasn't been realized yet
                MaybeSpawnSlugpup(player.realizedObject as Player, self.world);
        } catch (Exception ex) { Error(ex); }
    }


    public static void CycleStarted(RainWorldGame game)
    {
        Slugpup = null; //must re-find slugpup

        var saveState = game.GetStorySession.saveState;
        var miscData = saveState.miscWorldSaveData.GetSlugBaseData();
        var persistentData = saveState.deathPersistentSaveData.GetSlugBaseData();

        WarpChance = STARTING_WARP_CHANCE;
        if (miscData.TryGet(SAVE_KEY_WARP_CHANCE, out float savedChance))
            WarpChance = savedChance;
        Debug("Actual warp chance: " + WarpChance);

        //save this chance higher for next cycle
        miscData.Set(SAVE_KEY_WARP_CHANCE, WarpChance + WARP_CHANCE_INCREASE);

        //if a warp was skipped, override effective chance with 100%
        //skipping a warp really ought to be impossible
        if (persistentData.TryGet(SAVE_KEY_SKIPPED_WARP, out bool skipped) && skipped)
        {
            WarpChance = Mathf.Max(WarpChance, SKIPPED_WARP_OVERRIDE_CHANCE);
            Debug("Skipped warp; using chance: " + WarpChance);
        }

        //decide whether or not to allow warps this cycle
        WarpChance = UnityEngine.Random.value < WarpChance ? WarpChance : 0;
        Debug("Final chance this cycle: " + WarpChance);
    }

    public static void MetMoon(OracleBehavior behavior)
    {
        try
        {
            var data = behavior.oracle.room.world.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData();
            if (data.TryGet(SAVE_KEY_WARP_CHANCE, out float chance))
            {
                data.Set(SAVE_KEY_WARP_CHANCE, chance + MEET_MOON_WARP_CHANCE_INCREASE);
                Debug("Gave pearl to Moon. Set warp chance next cycle to " + (chance + MEET_MOON_WARP_CHANCE_INCREASE));
            }
            else
                Error("Could not find SAVE_KEY_WARP_CHANCE");
        }
        catch (Exception ex) { Error(ex); }
    }

    //Set slugpup color and stats
    public static void NPCStats_ctor(On.Player.NPCStats.orig_ctor orig, Player.NPCStats self, Player player)
    {
        orig(self, player);

        try
        {
            if (Plugin.IsProtectorCampaign && player.abstractPhysicalObject.world.game.IsStorySession
                && player.abstractPhysicalObject.world.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().TryGet(SAVE_KEY_SLUGPUP_ID, out EntityID ID)
                && ID == player.abstractPhysicalObject.ID)
            {
                self.EyeColor = 1;
                self.Dark = true;
                self.H = 0.6f; //greenish-cyan?
                self.S = 0.9f;
                self.L = 0.95f; //how dark = 95%
                self.Wideness = 0.25f; //bit thinner than average (0.35f)
                self.Size = 0.5f; //bit bigger than average

                player.npcCharacterStats.lungsFac *= 0.7f; //make them breathe longer, for convenience's sake

                Debug("Set slugpup color and stats");
                Slugpup = player.abstractCreature; //slugpup successfully found!
            }
        } catch (Exception ex) { Error(ex); }
    }

    //Trying to save without the slugpup == death
    public static bool ShelterDoorClosed = false;
    public static void ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
    {
        try
        {
            if (ShelterDoorClosed) //prevents this function from running EVERY TICK
                return;
            ShelterDoorClosed = true;

            if (Plugin.IsProtectorCampaign && self.room.game.IsStorySession)
            {
                var saveState = self.room.game.GetStorySession.saveState;
                var data = saveState.miscWorldSaveData.GetSlugBaseData();
                if (data.TryGet(SAVE_KEY_SLUGPUP_ID, out EntityID ID)) //there is a slugpup
                {
                    //but he's not in the shelter
                    if (!self.room.physicalObjects.Any(l => l.Any(p => p.abstractPhysicalObject.ID == ID && p is Player player && player.playerState.alive)))
                    {
                        Debug("COULD NOT FIND ALIVE SLUGPUP IN SHELTER!!!");

                        if (data.TryGet(SAVE_KEY_PUP_REQUIRED, out bool req) && req) //and the pup is required
                        {
                            var data2 = saveState.deathPersistentSaveData.GetSlugBaseData();
                            data2.TryGet(SAVE_KEY_ABANDONS, out int abandons);
                            data2.Set(SAVE_KEY_ABANDONS, abandons + 1); //set abandons higher for next time

                            if (abandons > 0) //just go to death screen; no big deal
                            {
                                self.room.game.GoToDeathScreen(); //This would be a perfect place to go to a "slugpup dead" dream sequence before going to death screen
                            }
                            else //shame the player
                            {
                                AchievementManager.AbandonSlugpup();
                                saveState.deathPersistentSaveData.karma = 0; //set karma to minimum
                                saveState.deathPersistentSaveData.reinforcedKarma = false;
                                self.room.AddObject(new ShamePlayerCutscene(self.room));
                            }
                            return; //DON'T go to win screen!!!
                        }
                        else //the pup is NOT required
                        {
                            //so forget about him. No more slugpups to worry about!!
                            data.Remove(SAVE_KEY_SLUGPUP_ID);
                            Debug("Removed slugpup ID from save data. BYE-BYE SLUGPUP!!");
                        }
                    }
                }

                //travelled through a warp and the pup survived = all good
                if (self.room.game.GetStorySession.warpsTraversedThisCycle > 0)
                    saveState.deathPersistentSaveData.GetSlugBaseData().Set(SAVE_KEY_SKIPPED_WARP, false);
            }
        } catch (Exception ex) { Error(ex); }

        orig(self);
    }




    private const string THRONE_WARP_ROOMS = "WORA_THRONE05;WORA_THRONE07;WORA_THRONE09;WORA_THRONE10";

    private static int warpTestDelay = 40; //one second delay
    public static WorldCoordinate? HoldSlugpupHostagePos = null;
    //chance of spawning warp each second
    public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        try
        {
            if (!Plugin.IsProtectorCampaign || self.abstractPhysicalObject.ID != Slugpup?.ID)
                return;

            if (HoldSlugpupHostagePos != null)
            {
                if (!self.abstractPhysicalObject.pos.CompareDisregardingTile(HoldSlugpupHostagePos.Value))
                    self.abstractCreature.Move(HoldSlugpupHostagePos.Value); //abstract slugpup moved; put him back in his place
                else
                {
                    self.mainBodyChunk.pos = (HoldSlugpupHostagePos.Value.Tile * 20).ToVector2(); //real slugpup moved; put him back in his place too
                    self.mainBodyChunk.vel *= 0;
                }
                return;
            }

            bool inThrone = self.room?.world?.name?.ToUpperInvariant() == "WORA" //Outer Rim
                && THRONE_WARP_ROOMS.Contains(self.room.abstractRoom.name.ToUpperInvariant()) //Throne warp room
                && self.IsBlacklistedRoomFromDynamicWarpPoints(self.room) == Player.BlackListReason.None; //Can warp

            if (WarpChance <= 0 && !inThrone)
                return;

            warpTestDelay--;
            if (warpTestDelay >= 0) return;
            warpTestDelay = 40;

            float currentChance = 1;

            //add several factors that increase/decrease the warp chance
            if (self.Stunned) currentChance *= 10;
            currentChance *= 1 + 2 * self.AI.threatTracker.Panic;

            currentChance *= (inThrone ? 5f : WarpChance) * WARP_CHANCE_PER_SECOND;
            //Debug("CurrentWarpChance: " + currentChance);

            if (UnityEngine.Random.value < currentChance
                && self.room.warpPoints.Count <= 0
                && self.IsBlacklistedRoomFromDynamicWarpPoints(self.room) == Player.BlackListReason.None) //don't fail spawning a warp; it pops up a message
            {
                //spawn a warp!
                Debug("Trying to spawn a warp!!! Chance of occurring this second: " + currentChance + ". In Throne: " + inThrone);

                var saveState = self.abstractPhysicalObject.world.game.GetStorySession.saveState;
                var miscData = saveState.miscWorldSaveData.GetSlugBaseData();
                int warpsSpawned = miscData.TryGet(SAVE_KEY_WARPS_SPAWNED, out int spawnCount) ? spawnCount : 0;
                Debug("Warps spawned so far: " + warpsSpawned);

                saveState.deathPersistentSaveData.rippleLevel = warpsSpawned + 1; //warp options are determined by rippleLevel; this opens up more options as time goes on
                saveState.deathPersistentSaveData.reinforcedKarma = true; //makes it a good warp instead of a bad one
                Plugin.SlugpupWarp = !inThrone; //if in the Throne, treat it like a normal warp
                self.SpawnDynamicWarpPoint();
                Plugin.SlugpupWarp = false; //just in case the warp failed

                saveState.deathPersistentSaveData.rippleLevel = 0; //reset rippleLevel

                if (self.room.warpPoints.Count > 0)
                {
                    saveState.deathPersistentSaveData.GetSlugBaseData().Set(SAVE_KEY_SKIPPED_WARP, true);
                    miscData.Set(SAVE_KEY_WARPS_SPAWNED, warpsSpawned + 1);

                    //note: this should happen twice; once here, once from going through the warp
                    miscData.Set(SAVE_KEY_WARP_CHANCE, POST_WARP_CHANCE);
                    WarpChance = 0;

                    self.abstractPhysicalObject.pos.Tile = new(Mathf.RoundToInt(self.mainBodyChunk.pos.x * 0.05f), Mathf.RoundToInt(self.mainBodyChunk.pos.y * 0.05f));
                    HoldSlugpupHostagePos = self.abstractPhysicalObject.pos;
                    self.ChangeRippleLayer(1); //ensure nothing can hurt the poor pup

                    Debug("Warp spawned. Save strings set. Pup held hostage.");
                }
            }
        }
        catch (Exception ex) { Error(ex); }
    }

    //Set warp chance to 0 for every warp taken
    //public static void WarpPoint_PerformWarp(On.Watcher.WarpPoint.orig_PerformWarp orig, WarpPoint self)
    public static void WarpPerformed(WarpPoint self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign)
            {
                var saveState = self.room.game.GetStorySession.saveState;
                saveState.miscWorldSaveData.GetSlugBaseData().Set(SAVE_KEY_WARP_CHANCE, POST_WARP_CHANCE);
                //saveState.deathPersistentSaveData.GetSlugBaseData().Set(SAVE_KEY_SKIPPED_WARP, false); //took a warp, so it probably wasn't skipped
                WarpChance = 0;
                Debug("Entered warp. Set warp chance to " + POST_WARP_CHANCE);
            }
        }
        catch (Exception ex) { Error(ex); }

        //orig(self);
    }

    //Reverse changes to the slugpup right before warping
    //ProvideAir is called right before SuckInCreatures. For some reason, the SuckInCreatures hook is broken, so I'm using this instead.
    public static void WarpPoint_ProvideAir(On.Watcher.WarpPoint.orig_ProvideAir orig, WarpPoint self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign && HoldSlugpupHostagePos != null && self.transportable && self.WarpPointAnimationPlaying)
            {
                HoldSlugpupHostagePos = null;
                Slugpup.rippleLayer = 0;
                Debug("Set slugpup's ripple layer back to 0");
            }
        } catch (Exception ex) { Error(ex); }

        orig(self);
    }


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
