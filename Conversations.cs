using MoreSlugcats;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;
using static ProtectorCampaign.Constants;

namespace ProtectorCampaign;

public static class Conversations
{
    //public static Conversation.ID Pebbles_White_Protector;

    public static void RegisterConversations()
    {
        //Pebbles_White_Protector = new("Pebbles_White_Protector", true);
    }


    //Make iterator saves unique for each timeline
    //public static void WorldLoader_ctor(On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, SlugcatStats.Timeline timelinePosition, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)


    //public static void OverWorld_InitiateSpecialWarp_WarpPoint(On.OverWorld.orig_InitiateSpecialWarp_WarpPoint orig, OverWorld self, ISpecialWarp callback, WarpPoint.WarpPointData warpData, bool useNormalWarpLoader)
    public static void UpdateTimeline(WarpPoint self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign && self.room.game.IsStorySession && self.Data.destTimeline != null)
            {
                var newTimeline = self.Data.destTimeline;
                var saveState = self.room.game.GetStorySession.saveState;
                //only update iterator data if we're CHANGING timelines
                if (saveState.currentTimelinePosition != newTimeline)
                {
                    Debug($"Switching timelines from {saveState.currentTimelinePosition} to {newTimeline}");

                    var miscSave = saveState.miscWorldSaveData;
                    var slugData = miscSave.GetSlugBaseData();

                    //save data for the old timeline
                    int[] data = new int[] { miscSave.SSaiConversationsHad, miscSave.SSaiThrowOuts, miscSave.cyclesSinceSSai };
                    slugData.Set(SAVE_PREFIX_PEBBLES_DATA + saveState.currentTimelinePosition.value, data);
                    slugData.Set(SAVE_PREFIX_MOON_STATE + saveState.currentTimelinePosition.value, miscSave.SLOracleState.ToString());

                    //load data for the new timeline
                    if (!slugData.TryGet(SAVE_PREFIX_PEBBLES_DATA + newTimeline.value, out int[] pebData))
                        pebData = new int[0];
                    miscSave.SSaiConversationsHad = pebData.Length > 0 ? pebData[0] : 0;
                    miscSave.SSaiThrowOuts = pebData.Length > 1 ? pebData[1] : 0;
                    miscSave.cyclesSinceSSai = pebData.Length > 2 ? pebData[2] : 0;

                    miscSave.SLOracleState.ForceResetState(Plugin.TimelineToSlugcat(newTimeline));
                    if (slugData.TryGet(SAVE_PREFIX_MOON_STATE + newTimeline.value, out string moonState))
                    {
                        miscSave.SLOracleState.FromString(moonState);
                        Debug("Found Moon State!");
                    }
                }
            }
        } catch (Exception ex) { Error(ex); }

        //orig(self, callback, warpData, useNormalWarpLoader);
    }


    //Make Pebbles react according to timeline, not slugcat
    public static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign)
            {
                var storySession = self.oracle.room.game.GetStorySession;
                var timeline = storySession.saveState.currentTimelinePosition;

                //So far, only override behavior for Gourmand timeline. Treat every other timeline like Survivor's
                //also override for Spearmaster LttM for SUBSEQUENT VISITS ONLY
                if (timeline == SlugcatStats.Timeline.Gourmand
                    || (timeline == SlugcatStats.Timeline.Spear && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM && self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState.playerEncountersWithMark > 0))
                {
                    var tempSaveNum = storySession.saveState.saveStateNumber;
                    storySession.saveStateNumber = Plugin.TimelineToSlugcat(timeline);
                    orig(self);
                    storySession.saveStateNumber = tempSaveNum;
                    Debug("Initiating Pebbles behavior for " + timeline);

                    return;
                }
            }
        } catch (Exception ex) { Error(ex); }
        orig(self);

        if (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM) //react to player if meeting DM for the first time
            self.SlugcatEnterRoomReaction();
    }


    //Change reaction text
    public static void SSSleepoverBehavior_ctor(On.SSOracleBehavior.SSSleepoverBehavior.orig_ctor orig, SSOracleBehavior.SSSleepoverBehavior self, SSOracleBehavior owner)
    {
        orig(self, owner);

        try
        {
            if (Plugin.IsProtectorCampaign && owner.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
            {
                if (owner.currSubBehavior.ID == SSOracleBehavior.SubBehavior.SubBehavID.GetNeuron
                    || owner.currSubBehavior.ID == SSOracleBehavior.SubBehavior.SubBehavID.MeetWhite)
                {
                    Debug("Preventing Moon from greeting the player when switching to slumber party.");
                    self.dialogBox.Interrupt("", -200); //negatively short text box. hopefully unnoticeable...?
                    self.dialogBox.lingerCounter = 200; //lie and say the box has been open for 5 seconds
                    return;
                }

                var slState = owner.oracle.room.world.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState;

                /*if (slState.playerEncountersWithMark <= 0)
                {
                    self.dialogBox.Interrupt("...", 0); //get rid of whatever message was shown

                    //initiate interaction with player, including turning on gravity and stuff
                    self.owner.getToWorking = 0f;
                    //self.gravOn = true;
                    self.firstMetOnThisCycle = true;
                    self.owner.SlugcatEnterRoomReaction();
                    self.owner.voice = self.oracle.room.PlaySound(SoundID.SL_AI_Talk_4, self.oracle.firstChunk);
                    self.owner.voice.requireActiveUpkeep = true;
                    self.owner.LockShortcuts();

                    //self.owner.InitateConversation(Conversation.ID.Pebbles_White, self);
                    self.owner.conversation?.Destroy();
                    self.owner.conversation = null;

                    //self.owner.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_AfterGiveMark); //causes an infinite loop, lol
                    //self.owner.action = MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_AfterGiveMark;
                    //self.owner.inActionCounter = 0;
                }
                else */
                if (slState.unrecognizedSaveStrings.Contains(MOON_SAVE_KEY_READ_PEARL))
                {
                    int num = slState.playerEncountersWithMark;
                    if (num > 3) num = UnityEngine.Random.Range(0, 3);
                    switch (num)
                    {
                        case 1:
                            self.dialogBox.Interrupt("Hello again, little creature!", 0);
                            break;
                        case 2:
                            self.dialogBox.Interrupt("Back again? Can I help you with anything, little one?", 0);
                            break;
                        default:
                            self.dialogBox.Interrupt("Hello! It's good to see you again, little creature.", 0);
                            break;
                    }
                }

                slState.playerEncounters++;
                slState.playerEncountersWithMark++;
            }
        } catch (Exception ex) { Error(ex); }

        //orig(self, owner);
    }


    //Make Moon talk according to timeline, not slugcat
    public static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign)
            {
                var storySession = self.oracle.room.game.GetStorySession;
                var tempSaveNum = storySession.saveState.saveStateNumber;
                storySession.saveStateNumber = Plugin.TimelineToSlugcat(storySession.saveState.currentTimelinePosition);
                orig(self);
                storySession.saveStateNumber = tempSaveNum;
                Debug("Initiating Moon conversation for " + storySession.saveState.currentTimelinePosition);
                return;
            }
        }
        catch (Exception ex) { Error(ex); }

        orig(self);
    }

    //Fix annoyingly refusing to read Hunter pearl sometimes
    public static void SLOracleBehaviorHasMark_GrabObject(On.SLOracleBehaviorHasMark.orig_GrabObject orig, SLOracleBehaviorHasMark self, PhysicalObject item)
    {
        try
        {
            if (Plugin.IsProtectorCampaign && item.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.DataPearl
                && (item.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Red_stomach
                && self.CheckSlugpupsInRoom()
                && !self.State.unrecognizedSaveStrings.Contains(MOON_SAVE_KEY_READ_PEARL))
            {
                self.State.significantPearls.Remove(DataPearl.AbstractDataPearl.DataPearlType.Red_stomach); //make sure we can still read the pearl
                Debug("Cleared Red_stomach from read pearls list, if it was already there.");
            }
        } catch (Exception ex) { Error(ex); }

        orig(self, item);
    }


    public static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
    {
        try
        {
            //throw new NotImplementedException();
            if (Plugin.IsProtectorCampaign)
            {
                if (self.id == MoreSlugcatsEnums.ConversationID.MoonGiveMarkAfter) //SHOULD NEVER BE TRIGGERED
                {
                    //Moon Spearmaster behavior
                    if (self.owner.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                    {
                        MoonMeetSpear(self);

                        return;
                    }
                }
                else if (self.id == Conversation.ID.Pebbles_White)
                {
                    var timeline = self.owner.oracle.room.game.TimelinePoint;

                    if (timeline == SlugcatStats.Timeline.Spear)
                    {
                        //Moon Spearmaster behavior
                        if (self.owner.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                        {
                            MoonMeetSpear(self);

                            return;
                        }
                        else //Pebbles
                        {
                            PebblesMeetSpear(self);

                            return;
                        }
                    }
                    else if (timeline == SlugcatStats.Timeline.Artificer)
                    {
                        PebblesMeetSpear(self); //It might be good to make this unique later, but this should be good enough

                        return;
                    }
                    else if (timeline == SlugcatStats.Timeline.Sofanthiel)
                    {
                        PebblesMeetInv(self);

                        return;
                    }
                    else if (timeline == SlugcatStats.Timeline.Red)
                    {
                        PebblesMeetRed(self);

                        return;
                    }
                    /*else if (timeline == SlugcatStats.Timeline.Gourmand) //Not Applicable here; Gourmand uses separate conversation ID
                    {
                        PebblesMeetRed(self);

                        return;
                    }*/
                    else if (timeline == SlugcatStats.Timeline.White)
                    {
                        PebblesMeetWhite(self);

                        return; //don't add the default events; only add my own
                    }
                    else if (SlugcatStats.AtOrBeforeTimeline(timeline, SlugcatStats.Timeline.White)) //Hopefully impossible catch-case
                    {
                        PebblesMeetRed(self);

                        return;
                    }
                    else //Monk + catch-case for any custom timelines
                    {
                        PebblesMeetYellow(self);

                        return;
                    }
                }
                else if (self.id == Conversation.ID.Pebbles_Red_Green_Neuron)
                {
                    //functional Moon get neuron
                    if (self.owner.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                    {
                        MoonMeetSpearWithNeuron(self);

                        return;
                    }
                    else //Pebbles
                    {
                        var timeline = self.owner.oracle.room.game.TimelinePoint;
                        //Post-collapse dialogue
                        if (SlugcatStats.AtOrAfterTimeline(timeline, SlugcatStats.Timeline.Artificer))
                        {
                            PebblesDiscussNeuron(self);

                            return;
                        }
                        else //Pre-collapse dialogue
                        {
                            PebblesDiscussNeuronEarly(self);

                            return;
                        }
                    }
                }
                else if (self.id == MoreSlugcatsEnums.ConversationID.Pebbles_Gourmand)
                {
                    PebblesMeetGourmand(self);

                    return;
                }
            }
        }
        catch (Exception ex) { Error(ex); }

        orig(self);
    }


    public static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign)
            {

                if (self.id == Conversation.ID.Moon_Pearl_Red_stomach)
                {
                    if (!self.State.unrecognizedSaveStrings.Contains(MOON_SAVE_KEY_READ_PEARL))
                    { //new reading
                        if (self.myBehavior.CheckSlugpupsInRoom())
                        { //with slugpup
                            MoonGivenStomachPearl(self);

                            return;
                        }
                        else
                        { //without slugpup
                            MoonGivenStomachPearlNoPup(self);

                            return;
                        }
                    }
                    else
                    { //repeat reading
                        MoonGivenStomachPearlRepeat(self);

                        return;
                    }
                }

                if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
                {
                    if (self.State.unrecognizedSaveStrings.Contains(MOON_SAVE_KEY_READ_PEARL))
                    {
                        MoonSecondEncounterAfterPearl(self);

                        return;
                    }
                }
            }
        }
        catch (Exception ex) { Error(ex); }

        orig(self);
    }


    #region Pebbles

    private static void PebblesMeetSpear(SSOracleBehavior.PebblesConversation self)
    {
        //Pebbles is stressed and unusually abrupt with his conversation

        self.events.Add(new Conversation.TextEvent(self, 100, self.Translate("...is this reaching you?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Im busy so get out"), 0));

        Debug("Set up custom conversation replacing Pebbles_White");
    }

    private static void PebblesMeetInv(SSOracleBehavior.PebblesConversation self)
    {
        //Hehe funny joke Pebbles
        //Probably make him say some "meta" stuff or explain the lore or something funny like that
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Why have you gone through all this pain? You're weird."), 0));
    }

    private static void PebblesDiscussNeuronEarly(SSOracleBehavior.PebblesConversation self)
    {
        //Slag keys? Funny. Why do we have them, though? They seemingly serve no purpose.
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("what on earth is this? Why would I want slag keys?"), 0));
    }

    private static void PebblesMeetRed(SSOracleBehavior.PebblesConversation self)
    {
        //Significantly calmer than for Spearmaster
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("hi there I don't like you much but we kinda chill ig"), 0));
    }

    private static void PebblesDiscussNeuron(SSOracleBehavior.PebblesConversation self)
    {
        //Basically same as for Hunter, but omit the talk about Hunter's sickness
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("yo sup"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("is this ur brain?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("its rly small. Are you stupid?"), 0));
    }

    private static void PebblesMeetGourmand(SSOracleBehavior.PebblesConversation self)
    {
        //"Stupid slugcats everywhere GET OUT OF MY FACILITY!!"

        //open OE gate
        try
        {
            self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().Set(SAVE_KEY_OE_OPEN, true);
            Debug("Opened Outer Expanse gate!");
        } catch (Exception ex) { Error(ex); }

        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("yo sup"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("good day"), 0));
    }

    //INITIAL WAKEUP SEQUENCE
    private static void PebblesMeetWhite(SSOracleBehavior.PebblesConversation self)
    {
        //self.events.Add(new SSOracleBehavior.PebblesConversation.WaitEvent(self, 100)); //wait 2.5 seconds
        self.events.Add(new Conversation.TextEvent(self, 100, self.Translate("...is this reaching you?"), 0));
        self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 10));

        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This is good. At last, you function."), 0)); //alien phrasing - intentional
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I began to consider your body another failed experiment. Fortunately, your awakening interrupted such contemplations,<LINE>and it shall soon put to rest many other bothers."), 0));
        self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 60));

        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("The child before you has incessantly distracted my attention for several cycles. You are ordered to remove it from my presence and deliver it to a suitable location."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I have given my word that the child will not be harmed - a careless mistake of mine.<LINE>As a result, activities such as disposing of the child and neglecting its care are expressly forbidden."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("However, I care not where you take the child. I suggest you deliver it to my neighbor, Looks to the Moon. My overseers have instructions to guide you in that direction."), 0));
        self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 20));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...Considering her situation, I doubt that she will refuse to assume ownership of the problem.<LINE>Unlike myself, she is likely incapable of important work."), 0));
        self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 60));

        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Travel with speed and caution, little servant. Your services, although minute, shall be appreciated.<LINE>In the meantime, I must resume my work."), 0));

        Debug("Set up custom conversation replacing Pebbles_White");
    }

    private static void PebblesMeetYellow(SSOracleBehavior.PebblesConversation self)
    {
        //"Why are you back here? I'm not a homeless shelter for slugcats."
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("guess what?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("im not a homeless shelter for slugcats"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("get out"), 0));
    }

    #endregion



    #region Moon

    //withOUT neuron
    private static void MoonMeetSpear(SSOracleBehavior.PebblesConversation self)
    {
        //player encountered Moon
        var slState = self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState;
        slState.playerEncounters++;
        slState.playerEncountersWithMark++;

        //Pretty basic greeting, I assume
        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("Greetings, strange creature!"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Your body shows marks of being modified. I can only assume that it was done by Five Pebbles.<LINE>Is this what he has been consuming so much water for? To create you? HA!"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), -20));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("My apologies, little creature. My scorn was unjustified."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Due to the excessive usage of water by my neighbor, I must hurry to protect my systems from total failure.<LINE>As a result, I do not have the luxury to examine you closely."), 0));
        self.events.Add(new Conversation.SpecialEvent(self, 0, "panic"));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I ca... c-c-c..."), 0));
        self.events.Add(new Conversation.TextEvent(self, 60, self.Translate("I'm sorry, but I cannot help you at the moment."), 0));
        if (self.convBehav.owner.CheckSlugpupsInRoom())
        {
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("But you are welcome to stay nearby. I will tend to you and your little one as I have time to do so."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I must work, but in the meantime, best of luck to you both."), 20));
        }
        else
        {
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("But you are welcome to stay nearby. I will tend to you as I have time to do so."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I must work, but in the meantime, best of luck to you."), 20));
        }

        //switch to slumber party behavior instead of throwing the player out
        self.events.Add(new LambdaEvent(self, 0, () => self.owner.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty)));

        Debug("MoonMetSpear conversation replacing " + self.id);
    }

    private static void MoonMeetSpearWithNeuron(SSOracleBehavior.PebblesConversation self)
    {
        //Confused about neuron's purpose
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("What is this?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 40, self.Translate("Remarkable! A neuron encoding - how many? Ah - Sixteen slag reset keys!<LINE>Wherever did you find such a thing?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Most of the methods encoded here are rather extreme, even for my circumstances.<LINE>Regardless, I will copy these keys for reference in case of an emergency.<LINE>Actually, I will implement all of these in a backup protocall immediately!"), 0));
        self.events.Add(new Conversation.TextEvent(self, 80, self.Translate("...Thank you for this, little creature! I never dared to hope for assistance amidst this dire situation, yet a strange animal came to help me.<LINE>Did someone send you? Did you find this somewhere? Ah, I wish I knew! But I greatly appreciate your service nonetheless, little one!<LINE>...although, I deeply hope that your help is not needed. These slag resets would not be ideal."), 20));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I do not know how to repay you for this. I hope that I can help you if you need anything of your own.<LINE>Please, stay here and rest a while. I appreciate the company."), 0));

        //switch to slumber party behavior instead of "politely" throwing the player out
        self.events.Add(new LambdaEvent(self, 0, () => self.owner.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty)));

        var slState = self.convBehav.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SLOracleState;
        if (!slState.unrecognizedSaveStrings.Contains(MOON_SAVE_KEY_DM_GOT_NEURON))
            slState.unrecognizedSaveStrings.Add(MOON_SAVE_KEY_DM_GOT_NEURON); //for reaction to the pearl
        
        Debug("MoonMetSpearWithNeuron conversation replacing " + self.id);
    }

    private static void MoonGivenStomachPearl(SLOracleBehaviorHasMark.MoonConversation self)
    {
        //affect like; give achievement; mark as read
        self.State.likesPlayer = Mathf.Max(self.State.likesPlayer, 1f);
        PupTracker.MetMoon(self.myBehavior);
        //AchievementManager.GavePearlToMoon(); //do this within conversation instead
        self.State.unrecognizedSaveStrings.Add(MOON_SAVE_KEY_READ_PEARL); //only mark it as read if we get full dialogue

        self.PearlIntro();

        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This pearl... is addressed as a message to me?<LINE>Ah..."), 0));

        if (self.myBehavior.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
        {
            DMMoonPearlEnding(self);
        }
        else //Shoreline Moon!
        {
            self.events.Add(new Conversation.TextEvent(self, 40, self.Translate("Little creature... I am sorry."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I cannot help you with this. Surely you can see why."), 20));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("In my current state, I cannot possibly provide the care you seek for your child.<LINE>This little one needs food, safety, instruction in the ways of the wild...<LINE>I can offer none of these things."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It pains me that I cannot serve you in this way. Truly, I have dreamed of an opportunity like this for some time.<LINE>I crave company, tutelage, and - more than anything - purpose."), 20));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You likely are frustrated by your predicament. You travelled all this way hoping for the completion of your task,<LINE>yet you seem no closer to that end. Little servant, I hope that you are not resentful of this burden placed upon you."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("In the end, the most I can offer is this (a small comfort - if comfort it can be called):<LINE>Your hardships are a hidden blessing that others desire. Because of your task, you have purpose, freedom, and company.<LINE>Do not lose heart. Cherish your - yes, your - child."), 0)); ;
            self.events.Add(new Conversation.TextEvent(self, 40, self.Translate("In truth, I envy you. If I were in any other state, I would gladly care for this little one."), 10));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("But I trust that you will continue to guard this child, and I trust that you will find peace doing so."), 0));
            self.events.Add(new AchievementManager.AchievementEvent(self, 0, AchievementManager.GivePearlID)); //give achievement
        }

        Debug("Set up custom pearl reading (new with slugpup) replacing Moon_Pearl_Red_stomach");
    }
    private static void DMMoonPearlEnding(SLOracleBehaviorHasMark.MoonConversation self)
    {
        self.events.Add(new AchievementManager.AchievementEvent(self, 0, AchievementManager.GivePearlID)); //give achievement for the pearl immediately
        self.events.Add(new Conversation.TextEvent(self, 40, self.Translate("..."), 0));
        self.events.Add(new Conversation.TextEvent(self, 40, self.Translate("You Win!"), 0));

        self.events.Add(new AchievementManager.AchievementEvent(self, 0, AchievementManager.PupAtDMMoon)); //give achievement for the ending last
        self.events.Add(new FadeOutEvent(self, 10, 80));
        self.events.Add(new EndGameEvent(self, 10, CustomEnding.SpearmasterMoon));
    }

    private static void MoonGivenStomachPearlNoPup(SLOracleBehaviorHasMark.MoonConversation self)
    {
        self.PearlIntro();

        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This pearl... is addressed as a message to me?<LINE>The message is freshly written, but it doesn't make sense in this situation."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It mentions two creatures, a messenger and a child. Are you the messenger or the child? Or did you find this somewhere?<LINE>I sincerely hope that you are not the messenger mentioned here, because that would mean you lost the little one entrusted to you."), 20));
        if (self.myBehavior.oracle.ID != MoreSlugcatsEnums.OracleID.DM)
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Regardless, I clearly cannot do what this pearl requests."), 0));
        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("Perhaps you would like me to read it to you?"), 0));

        Debug("Set up custom pearl reading (new withOUT slugpup) replacing Moon_Pearl_Red_stomach");

        MoonReadStomachPearl(self);
    }

    private static void MoonGivenStomachPearlRepeat(SLOracleBehaviorHasMark.MoonConversation self)
    {
        self.PearlIntro();

        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
        if (self.myBehavior.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah, this pearl again."), 0));
        else
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah, this pearl again.<LINE>Little creature, you know that I cannot assist you in caring for the child."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Would you like me to read the contents of the pearl to you?"), 0));

        Debug("Set up custom pearl reading (repeat reading) replacing Moon_Pearl_Red_stomach");

        MoonReadStomachPearl(self);
    }

    private static void MoonReadStomachPearl(SLOracleBehaviorHasMark.MoonConversation self)
    {
        //read the pearl
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let's see... \"PEARL CONTENTS GO HERE\""), 0));
    }

    private static void MoonSecondEncounterAfterPearl(SLOracleBehaviorHasMark.MoonConversation self)
    {
        //similar to original dialogue
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh, hello again!"), 10));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I wonder what it is that you want?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("There is nothing here. Not even my memories remain."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Perhaps you seek guidance? Or maybe you're curious to see me?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("If you desire direction, I cannot help you much, little one."), 0));
        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Maybe you can return to Five Pebbles. He might know how to help, if he's willing. He seemed glad to be rid of you.<LINE>I think you'll have to wander with your child in search of a home. I'm sure that, somewhere,<LINE>you can find a suitable abode. Maybe you can even find a larger family?"), 0));
        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("I know this place has little to offer you, but you're always welcome to stay here.<LINE>I do appreciate the company."), 20));

        Debug("Set up custom conversation replacing MoonSecondPostMarkConversation");
    }

    #endregion


    #region SpecialEvents

    private class LambdaEvent : Conversation.DialogueEvent
    {
        Action action;

        public LambdaEvent(Conversation owner, int initialWait, Action action) : base(owner, initialWait)
        {
            this.action = action;
        }

        public override void Activate()
        {
            base.Activate();

            try { action(); }
            catch (Exception ex) { Error(ex); }
        }

        public override bool IsOver => owner == null || age > initialWait; //if this isn't here, initialWait doesn't work!
    }

    private class FadeOutEvent : Conversation.DialogueEvent
    {
        public int duration;
        public bool fadeIn;

        public FadeOutEvent(Conversation owner, int initialWait, int duration, bool fadeIn = false) : base(owner, initialWait)
        {
            this.duration = duration;
            this.fadeIn = fadeIn;
        }

        public override void Activate()
        {
            base.Activate();

            try
            {
                Room room = null;
                if (owner is SLOracleBehaviorHasMark.MoonConversation moonConv)
                    room = moonConv.myBehavior.oracle.room;
                else if (owner is SSOracleBehavior.PebblesConversation pebbConv)
                    room = pebbConv.convBehav.oracle.room;

                if (room == null)
                    Error("Could not find room for fade out event!!!");
                else
                    room.AddObject(new FadeOut(room, Color.black, duration, fadeIn));
            } catch (Exception ex) { Error(ex); }
        }

        public override bool IsOver => owner == null || age > initialWait + duration;
    }

    /**
     * TODO: MAKE THIS A SEPARATE FILE FOR ENDINGS INSTEAD OF RANDOMLY ADDING IT IN HERE
     */
    private enum CustomEnding
    {
        SpearmasterMoon,
        FutureMoon
    }

    private class EndGameEvent : Conversation.DialogueEvent
    {
        public CustomEnding ending;

        public EndGameEvent(Conversation owner, int initialWait, CustomEnding ending) : base(owner, initialWait)
        {
            this.ending = ending;
        }

        public override void Activate()
        {
            base.Activate();

            Debug("Initiating custom ending " + ending);

            try
            {
                switch(ending)
                {
                    case CustomEnding.SpearmasterMoon:
                        var c = owner as SLOracleBehaviorHasMark.MoonConversation;
                        var g = c.myBehavior.oracle.room.game;

                        g.manager.statsAfterCredits = true;

                        //alter savedata
                        //g.rainWorld.progression.miscProgressionData.
                            //the pup is no longer required
                        g.GetStorySession.saveState.miscWorldSaveData.GetSlugBaseData().Set(SAVE_KEY_PUP_REQUIRED, false);

                        g.GetStorySession.saveState.skipNextCycleFoodDrain = true;
                        g.AppendCycleToStatisticsForPlayers();
                        RainWorldGame.ForceSaveNewDenLocation(g, "DM_AI", true); //vanilla endings set last bool false; maybe true will save the slugpup?
                            //...or we could just respawn the slugpup manually, lol. Works fine.
                        //try saving just like in a shelter?
                        //g.GetStorySession.saveState.denPosition = "DM_AI";
                        //g.GetStorySession.saveState.SessionEnded(g, true, false);

                        g.manager.nextSlideshow = WatcherEnums.SlideShowID.EndingSpinningTop;
                        g.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);

                        break;
                    default:
                        Error("ENDING NOT IMPLEMENTED YET!!!");
                        break;
                }
            } catch (Exception ex) { Error(ex); }
        }

        public override bool IsOver => owner == null || age > initialWait; //if this isn't here, initialWait doesn't work!
    }

    #endregion


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
