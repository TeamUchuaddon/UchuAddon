using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Modifier;
using Hori.Scripts.Role.Neutral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hori.Scripts.Patch;

[HarmonyPatch(typeof(LogicOptionsNormal), nameof(LogicOptionsNormal.GetAnonymousVotes))]
public static class AnonymousVotesPatch
{
    private static void Postfix(LogicOptionsNormal __instance, ref bool __result)
    {
        var myPlayer = GamePlayer.LocalPlayer;
        if (__result && myPlayer != null && NebulaAPI.CurrentGame != null)
        {
            if (myPlayer.Role.Role == AdmiralU.MyRole && AdmiralU.VoteWatching)
            {
                __result = false;
            }

            if (myPlayer.Role.Role == MoiraU.MyRole && MoiraU.CanSeeVoteOption)
            {
                __result = false;
            }

            if (myPlayer.TryGetModifier<WatcherU.Instance>(out _))
            {
                __result = false;
            }
        }
    }
}