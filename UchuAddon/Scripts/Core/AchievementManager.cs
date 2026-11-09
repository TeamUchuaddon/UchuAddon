using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Achievements;

namespace Hori.Scripts.Core;

public class AchievementUnLock
{
    /// <summary>
    /// 称号の進捗を進めます。
    /// </summary>
    /// <param name="id">称号のID</param>
    public static void TitleUnLock(string id)
    {
        var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
        progress.Progress(id);
    }
}