using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nebula.Roles.Crewmate;

namespace Hori.Scripts.Core;

public class UchuEventManager
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class PlayerFixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var player = GamePlayer.GetPlayer(__instance.PlayerId);
            if (player == null) return;

            GameOperatorManager.Instance?.Run(new PlayerFixedUpdateEvent(player, __instance));
        }
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    public static class SendChatPatch
    {
        public static void Prefix(PlayerControl __instance, ref string chatText)
        {
            var player = GamePlayer.GetPlayer(__instance.PlayerId);
            if (player == null) return;

            var ev = new PlayerSendChatEvent(player, chatText);

            GameOperatorManager.Instance?.Run(ev);

            chatText = ev.Text;
        }
    }

    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
    public static class LobbyUpdatePatch
    {
        public static void Postfix()
        {
            GameOperatorManager.Instance?.Run(new LobbyUpdateEvent());
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdatePatch
    {
        public static void Postfix(HudManager __instance)
        {
            SabotageUpdateEventPatch();
            //ChatButtonUpdateEventPatch(__instance);
        }

        public static void SabotageUpdateEventPatch()
        {
            var ev = new SabotageUpdateEvent();

            GameOperatorManager.Instance?.Run(ev);

            if (ev.IsUseSabotage == false)
            {
                var ship = ShipStatus.Instance;

                if (ship == null) return;

                var system = ship.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
                system.Timer = 30f;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.AdjustLighting))]
    public static class PlayerFlashLightPatch
    {
        public static bool Prefix(PlayerControl __instance)
        {
            if (PlayerControl.LocalPlayer != __instance) return true;

            var player = GamePlayer.GetPlayer(__instance.PlayerId);
            if (player == null) return true;

            var ev = new PlayerFlashLightEvent(player);
            GameOperatorManager.Instance?.Run(ev);

            bool flashFlag = ev.IsFlashLight;
            float num = ev.LightSize;

            __instance.lightSource.SetupLightingForGameplay(flashFlag, num, __instance.TargetFlashlight.transform);

            return false;
        }
    }
}