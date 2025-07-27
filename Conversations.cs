using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtectorCampaign;

public static class Conversations
{
    //public static Conversation.ID Pebbles_White_Protector;

    public static void RegisterConversations()
    {
        //Pebbles_White_Protector = new("Pebbles_White_Protector", true);
    }



    public static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
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

        orig(self);
    }


    public static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        if (Plugin.IsProtectorCampaign)
        {
            if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("u stupid slugcat"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("go away"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("jk u good"), 0));

                Debug("Set up custom conversation replacing MoonFirstPostMarkConversation");
                return;
            }
        }

        orig(self);
    }


    private static void Debug(object obj) => Plugin.PublicLogger.LogDebug(obj);
    private static void Error(object obj) => Plugin.PublicLogger.LogError(obj);
}
