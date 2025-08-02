using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeAchievements;

namespace ProtectorCampaign;

class FakeAchievementCompat
{
    public static void ShowAchievement(string id) => AchievementsManager.ShowAchievement(id);
}
