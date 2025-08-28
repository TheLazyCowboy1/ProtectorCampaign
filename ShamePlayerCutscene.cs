using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ProtectorCampaign;

public class ShamePlayerCutscene : UpdatableAndDeletable
{
    private static (string, int)[] Dialogue => new (string, int)[]
    {
        ("...", -20),
        ("...Why?", 0),
        ("How could you abandon such an innocent creature?", -20),
        ("You had one task.", 0),
        ("How could it be simpler? Just protect the pup; that's all.<LINE>How selfish can you be? Is your life worth more than that child's?", -20),
        ("...", 0),
        ("Because you prioritized your own life, you will lose it.", -10),
        ("I send you back, until your mission is done.<LINE>For the sake of both the pup and you, I do this.", 0)
    };

    /*
     * COLOR NOTE:
     * Color is determined by Effect Color A. The default for void sea stuff appears to be 13. 15 would also look good.
     * This means that the color can be anything, depending on where the shelter is. I think this is kinda cool.
     * ...Actually most rooms have EffectColorA = 0 which is purplish. I don't like that.
     */

    public ShamePlayerCutscene(Room room) : base()
    {
        _room = room;

        RoomSettings.RoomEffect effect = room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.VoidMelt);
        if (effect == null)
        {
            effect = new(RoomSettings.RoomEffect.Type.VoidMelt, MinVoidEffect, false);
            room.roomSettings.effects.Add(effect);
            MeltLights ml = new(effect, room);
            room.AddObject(ml);
        }
        else
            effect.amount = MinVoidEffect; //start at min here due to RoomCamera shenanigans

        room.roomSettings.EffectColorA = 15; //make the ripples yellow!

        //room.AddObject(this);

        //reset all the cameras to fully apply the effect
        foreach (var cam in room.game.cameras)
        {
            if (cam != null)
            {
                cam.voidSeaGoldFilter = 1f; //hopefully makes it more full-screen
                cam.MoveCamera(cam.room, cam.currentCameraPosition); //causes it to initialize void effect, etc.
            }
        }

        for (int i = 0; i < 20; i++) //add initial burst of light
            room.AddObject(new MeltLights.MeltLight(1f, room.RandomPos(), room, RainWorld.GoldRGB));

        room.PlaySound(SoundID.SB_A14);
    }

    private Room _room;
    private const float MinVoidEffect = 0.85f;
    private int textDelay = 40; //1 second delay before text starts
    private const int timerLength = 40 * 6; //6 seconds
    private int effectTimer = timerLength;
    private float soundChance = 1f;
    private const float soundChanceIncrease = 1f / 40f / 4f; //sound every >4 seconds

    public override void Update(bool eu)
    {
        base.Update(eu);

        try
        {
            //set void effect
            if (effectTimer-- >= 0)
            {
                RoomSettings.RoomEffect effect = _room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.VoidMelt);
                effect.amount = Mathf.Lerp(MinVoidEffect, 1f, Custom.SCurve((float)effectTimer / (float)timerLength, 0.6f));
            }

            //manage text
            if (--textDelay == 0)
            {
                var hud = _room.game.cameras[0].hud;
                if (hud.dialogBox == null)
                {
                    hud.InitDialogBox();
                }
                foreach (var msg in Dialogue)
                {
                    hud.dialogBox.NewMessage(msg.Item1, msg.Item2);
                }
            }

            //end end the "conversation" and go to the death screen if all messages are finished
            if (textDelay < -400) //must wait at least 10 seconds
            {
                var hud = _room.game.cameras[0].hud;
                if (hud.dialogBox == null || hud.dialogBox.messages.Count <= 0)
                {
                    this.slatedForDeletetion = true;
                    _room.game.GoToDeathScreen();
                }
            }

            //sounds
            if (soundChance > 0 && UnityEngine.Random.value < soundChance)
            {
                switch (UnityEngine.Random.Range(0, 6))
                {
                    case 0: _room.PlaySound(SoundID.SS_AI_Talk_1, 0, 1, 0.6f); break;
                    case 1: _room.PlaySound(SoundID.SS_AI_Talk_2, 0, 1, 0.6f); break;
                    case 2: _room.PlaySound(SoundID.SS_AI_Talk_3, 0, 1, 0.6f); break;
                    case 3: _room.PlaySound(SoundID.SS_AI_Talk_5, 0, 1, 0.6f); break;
                    default: _room.PlaySound(SoundID.SS_AI_Talk_4, 0, 1, 0.6f); break; //twice as likely
                }
                //add two extra lights, cuz why not
                room.AddObject(new MeltLights.MeltLight(1f, room.RandomPos(), room, RainWorld.GoldRGB));
                room.AddObject(new MeltLights.MeltLight(1f, room.RandomPos(), room, RainWorld.GoldRGB));
                soundChance = -1f;
            }
            soundChance += soundChanceIncrease;

        } catch (Exception ex) { Plugin.PublicLogger.LogError(ex); }
    }
}
