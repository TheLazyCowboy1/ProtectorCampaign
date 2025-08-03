using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ProtectorCampaign;

public class ConfigOptions : OptionInterface
{
    public ConfigOptions()
    {
        //Warp = this.config.Bind<float>("Warp", 25, new ConfigAcceptableRange<float>(-500, 500));
    }

    //General
    //public readonly Configurable<float> Warp;

    public override void Initialize()
    {
        var optionsTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            optionsTab
        };

        OpHoldButton clearAchievementButton;

        float t = 150f, y = 550f, h = -40f, H = -70f, x = 50f, w = 80f, c = 50f;
        float t2 = 400f, x2 = 300f;

        optionsTab.AddItems(

            clearAchievementButton = new OpHoldButton(new(50, 50), new Vector2(150, 40), "Clear Achievements") { description = "Clears all achievements for the currently active save file." }
            );

        clearAchievementButton.OnPressDone += (trigger) =>
        {
            AchievementManager.ClearAllAchievements();
            trigger?.Menu?.PlaySound(SoundID.MENU_Checkbox_Check);
        };
    }

}