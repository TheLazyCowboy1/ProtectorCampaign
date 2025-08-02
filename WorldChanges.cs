using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtectorCampaign;

public partial class Plugin
{
    private const string BrokenGravRooms = "WORA_THRONE02;WORA_THRONE03;WORA_THRONE04;WORA_THRONE05;WORA_THRONE06;WORA_THRONE07;WORA_THRONE08;WORA_THRONE09;WORA_THRONE10;WORA_THRONE11;WORA_THRONE12;WORA_AI";

    //Load room settings for LZC_Protector if they exist
    //This makes LZC_Protector its own timeline but ONLY for room settings
    private void RoomSettings_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame(On.RoomSettings.orig_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame orig, RoomSettings self, Room room, string name, Region region, bool template, bool firstTemplate, SlugcatStats.Timeline timelinePoint, RainWorldGame game)
    {
        try
        {
            if (!template && WorldLoader.FindRoomFile(name, false, "_settings-LZC_Protector.txt", false) != null)
            {
                Logger.LogDebug("Found settings file " + name + "_settings-LZC_Protector.txt");
                orig(self, room, name, region, template, firstTemplate, new("LZC_Protector"), game);
                return;
            }
        } catch (Exception ex) { Logger.LogError(ex); }

        orig(self, room, name, region, template, firstTemplate, timelinePoint, game);

        try
        {
            //add BrokenZeroG for Throne leadup
            if (BrokenGravRooms.Contains(name.ToUpperInvariant()) && self.GetEffect(RoomSettings.RoomEffect.Type.BrokenZeroG) == null)
                self.effects.Add(new(RoomSettings.RoomEffect.Type.BrokenZeroG, 0.5f, false));
        } catch (Exception ex) { Logger.LogError(ex); }
    }
}
