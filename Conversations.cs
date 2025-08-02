using MoreSlugcats;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;

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


    public static void OverWorld_InitiateSpecialWarp_WarpPoint(On.OverWorld.orig_InitiateSpecialWarp_WarpPoint orig, OverWorld self, ISpecialWarp callback, WarpPoint.WarpPointData warpData, bool useNormalWarpLoader)
    {
        try
        {
            if (Plugin.IsProtectorCampaign && self.game.IsStorySession && warpData.destTimeline != null)
            {
                var newTimeline = warpData.destTimeline;
                var saveState = self.game.GetStorySession.saveState;
                //only update iterator data if we're CHANGING timelines
                if (saveState.currentTimelinePosition != newTimeline)
                {
                    Debug($"Switching timelines from {saveState.currentTimelinePosition} to {newTimeline}");

                    var miscSave = saveState.miscWorldSaveData;
                    var slugData = miscSave.GetSlugBaseData();

                    //save data for the old timeline
                    int[] data = new int[] { miscSave.SSaiConversationsHad, miscSave.SSaiThrowOuts, miscSave.cyclesSinceSSai };
                    slugData.Set(Plugin.SAVE_PREFIX_PEBBLES_DATA + saveState.currentTimelinePosition.value, data);
                    slugData.Set(Plugin.SAVE_PREFIX_MOON_STATE + saveState.currentTimelinePosition.value, miscSave.SLOracleState.ToString());

                    //load data for the new timeline
                    if (!slugData.TryGet(Plugin.SAVE_PREFIX_PEBBLES_DATA + newTimeline.value, out int[] pebData))
                        pebData = new int[0];
                    miscSave.SSaiConversationsHad = pebData.Length > 0 ? pebData[0] : 0;
                    miscSave.SSaiThrowOuts = pebData.Length > 1 ? pebData[1] : 0;
                    miscSave.cyclesSinceSSai = pebData.Length > 2 ? pebData[2] : 0;

                    miscSave.SLOracleState.ForceResetState(new(newTimeline.value));
                    if (slugData.TryGet(Plugin.SAVE_PREFIX_MOON_STATE + newTimeline.value, out string moonState))
                    {
                        miscSave.SLOracleState.FromString(moonState);
                        Debug("Found Moon State!");
                    }
                }
            }
        } catch (Exception ex) { Error(ex); }

        orig(self, callback, warpData, useNormalWarpLoader);
    }


    //Make Pebbles react according to timeline, not slugcat
    public static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        try
        {
            if (Plugin.IsProtectorCampaign)
            {
                var storySession = self.oracle.room.game.GetStorySession;
                var tempSaveNum = storySession.saveState.saveStateNumber;
                storySession.saveStateNumber = new(storySession.saveState.currentTimelinePosition.value);
                orig(self);
                storySession.saveStateNumber = tempSaveNum;
                Debug("Initiating Pebbles behavior for " + storySession.saveState.currentTimelinePosition);
                return;
            }
        } catch (Exception ex) { Error(ex); }
        orig(self);
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
                storySession.saveStateNumber = new(storySession.saveState.currentTimelinePosition.value);
                orig(self);
                storySession.saveStateNumber = tempSaveNum;
                Debug("Initiating Moon conversation for " + storySession.saveState.currentTimelinePosition);
                return;
            }
        }
        catch (Exception ex) { Error(ex); }
        orig(self);
    }


    public static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
    {
        try
        {
            //throw new NotImplementedException();
            if (Plugin.IsProtectorCampaign)
            {
                if (self.id == Conversation.ID.Pebbles_White)
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

                    return; //don't add the default events; only add my own
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
                /*if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
                {
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("u stupid slugcat"), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("go away"), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("jk u good"), 0));

                    Debug("Set up custom conversation replacing MoonFirstPostMarkConversation");
                    return;
                }*/

                if (self.id == Conversation.ID.Moon_Pearl_Red_stomach)
                {
                    self.PearlIntro();

                    if (!self.State.unrecognizedSaveStrings.Contains(Plugin.MOON_SAVE_KEY_READ_PEARL))
                    { //new reading
                        if (self.myBehavior.CheckSlugpupsInRoom())
                        { //with slugpup
                            self.State.likesPlayer = Mathf.Max(self.State.likesPlayer, 1f);
                            PupTracker.MetMoon(self.myBehavior);
                            AchievementManager.GavePearlToMoon();
                            self.State.unrecognizedSaveStrings.Add(Plugin.MOON_SAVE_KEY_READ_PEARL); //only mark it as read if we get full dialogue

                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This pearl... is addressed as a message to me?<LINE>Ah..."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 40, self.Translate("Little creature... I am sorry."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I cannot help you with this. Surely you can see why."), 20));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("In my current state, I cannot possibly provide the care you seek for your child.<LINE>This little one needs food, safety, instruction in the ways of the wild...<LINE>I can offer none of these things."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It pains me that I cannot serve you in this way. Truly, I have dreamed of an opportunity like this for some time.<LINE>I crave company, tutelage, and - more than anything - purpose."), 20));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You likely are frustrated by your predicament. You travelled all this way hoping for the completion of your task,<LINE>yet you seem no closer to that end. Little servant, I hope that you are not resentful of this burden placed upon you."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("In the end, the most I can offer is this (a small comfort - if comfort it can be called):<LINE>Your hardships are a hidden blessing that others desire. Because of your task, you have purpose, freedom, and company.<LINE>Do not lose heart. Cherish your - yes, your - child."), 0)); ;
                            self.events.Add(new Conversation.TextEvent(self, 40, self.Translate("In truth, I envy you. If I were in any other state, I would gladly care for this little one."), 10));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("But I trust that you will continue to guard this child, and I trust that you will find peace doing so."), 0));

                            Debug("Set up custom pearl reading (new with slugpup) replacing Moon_Pearl_Red_stomach");
                            return;
                        }
                        else
                        { //without slugpup
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This pearl... is addressed as a message to me?<LINE>The message is freshly written, but it doesn't make sense in this situation."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It mentions two creatures, a messenger and a child. Are you the messenger or the child? Or did you find this somewhere?<LINE>I sincerely hope that you are not the messenger mentioned here, because that would mean you lost the little one entrusted to you."), 20));
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Regardless, I clearly cannot do what this pearl requests."), 0));
                            self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("Perhaps you would like me to read it to you?"), 0));

                            Debug("Set up custom pearl reading (new withOUT slugpup) replacing Moon_Pearl_Red_stomach");
                        }
                    }
                    else
                    { //repeat reading
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah, this pearl again.<LINE>Little creature, you know that I cannot assist you in caring for the child."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Would you like me to read the contents of the pearl to you?"), 0));

                        Debug("Set up custom pearl reading (repeat reading) replacing Moon_Pearl_Red_stomach");
                    }

                    //read the pearl
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let's see... \"PEARL CONTENTS GO HERE\""), 0));

                    return;
                }

                if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
                {
                    if (self.State.unrecognizedSaveStrings.Contains(Plugin.MOON_SAVE_KEY_READ_PEARL))
                    {
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh, hello again!"), 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I wonder what it is that you want?"), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("There is nothing here. Not even my memories remain."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Perhaps you seek guidance? Or maybe you're curious to see me?"), 0));
                        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("If you desire direction, I cannot help you much, little one."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Maybe you can return to Five Pebbles. He might know how to help, if he's willing. He seemed glad to be rid of you.<LINE>I think you'll have to wander with your child in search of a home. I'm sure that, somewhere,<LINE>you can find a suitable abode. Maybe you can even find a larger family?"), 0));
                        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("I know this place has little to offer you, but you're always welcome to stay here.<LINE>I do appreciate the company."), 20));

                        Debug("Set up custom conversation replacing MoonSecondPostMarkConversation");
                        return;
                    }
                }
            }
        }
        catch (Exception ex) { Error(ex); }

        orig(self);
    }


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
