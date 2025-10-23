using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtectorCampaign;

public static class Constants
{
    public const string SAVE_KEY_HEALTH = "LZC_Protector_Health";
    public const int MIN_HEALTH = -20;
    public const int MAX_HEALTH = 30;
    public const int FOOD_GOAL = 6;
    public const int STARVE_PENALTY = -10;

    public const float BASE_SHOCK_CHANCE = 1; //100%
    public const float SHOCK_CHANCE_PER_HEALTH = 0.05f; //30 * 0.05 = 1.5f = 150%
    public const float SHOCK_CHANCE_PER_WARP = 1; //chance goes up by 100% each time we warp
    public const float DEATH_BLOCK_WEIGHT = 1; //subtracts 100% from the shock chance
    public const float CREATURE_SHOCK_WEIGHT = 0.3f;

    private const float MAX_CYCLES_BEFORE_WARP = 40; //it takes 40 cycles for chance to go from 0 to 1
    public const float STARTING_WARP_CHANCE = -10f / MAX_CYCLES_BEFORE_WARP; //10 cycles before warp is possible
    public const float POST_WARP_CHANCE = -5f / MAX_CYCLES_BEFORE_WARP; //5 cycles after a warp before it's possible again
    public const float WARP_CHANCE_INCREASE = 1f / MAX_CYCLES_BEFORE_WARP;
    public const float SKIPPED_WARP_OVERRIDE_CHANCE = 2f; //warp is extremely likely
    public const float MEET_MOON_WARP_CHANCE_INCREASE = 0.5f;
    public const float WARP_CHANCE_PER_SECOND = 1f / 60f / 15f; //15 minutes on average for warp (unless pup gets scared)
    //public const float MAX_WARP_CHANCE = 1;

    public const string SAVE_KEY_SLUGPUP_ID = "LZC_Protector_Slugpup_ID";
    public const string SAVE_KEY_PUP_REQUIRED = "LZC_Protector_Slugpup_Required";

    public const string SAVE_KEY_WARP_CHANCE = "LZC_Protector_Warp_Chance";
    public const string SAVE_KEY_SKIPPED_WARP = "LZC_Protector_Skipped_Warp"; //deathPersistant save data
    public const string SAVE_KEY_WARPS_SPAWNED = "LZC_Protector_Warps_Spawned";

    public const string SAVE_KEY_ABANDONS = "LZC_Protector_Abandons";
    public const string SAVE_KEY_HUNTER_NEURON = "LZC_Protector_Hunter_Neuron";

    public const string SAVE_KEY_OE_OPEN = "LZC_Protector_OE_Open";

        //used for the fake passage
    public const string Protector_Health_String = "LZC_Protector_Health";

    //iterators
    //public const string SAVE_KEY_PEBBLES_CONVS = "LZC_Protector_SS_Convs";
    //public const string SAVE_KEY_PEBBLES_THROWS = "LZC_Protector_SS_Throws";
    public const string SAVE_PREFIX_PEBBLES_DATA = "LZC_Protector_SSData_";
    public const string SAVE_PREFIX_MOON_STATE = "LZC_Protector_SLState_";
    public const string MOON_SAVE_KEY_READ_PEARL = "LZC_Protector_Read_Pearl";
    public const string MOON_SAVE_KEY_DM_GOT_NEURON = "LZC_Protector_DM_Got_Neuron";
}
