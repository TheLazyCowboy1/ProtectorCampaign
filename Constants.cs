using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtectorCampaign;

public partial class Plugin
{
    public const string SAVE_KEY_HEALTH = "Protector_Health";
    public const int MIN_HEALTH = -20;
    public const int MAX_HEALTH = 30;
    public const int FOOD_GOAL = 6;
    public const int STARVE_PENALTY = -10;

    public const string SAVE_KEY_SLUGPUP_ID = "Protector_Slugpup_ID";
    public const string SAVE_KEY_PUP_REQUIRED = "Protector_Slugpup_Required";

    public const string SAVE_KEY_WARP_CHANCE = "Protector_Warp_Chance";
    public const string SAVE_KEY_SKIPPED_WARP = "Protector_Skipped_Warp"; //deathPersistant save data
    public const float STARTING_WARP_CHANCE = 0.0f;
    public const float POST_WARP_CHANCE = 0.1f;
    public const float WARP_CHANCE_INCREASE = 0.3f;
    public const float SKIPPED_WARP_OVERRIDE_CHANCE = 1.5f;
    //public const float MAX_WARP_CHANCE = 1;
}
