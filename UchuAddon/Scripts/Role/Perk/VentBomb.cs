using Hori.Core;
using Hori.Scripts.Core;
using Il2CppInterop.Runtime.Injection;
using Nebula.Modules.Cosmetics;
using Nebula.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Virial.Events.Game.Minimap;
using static Hori.Scripts.Role.Crewmate.AdmiralU;
using static Hori.Scripts.Role.Crewmate.SonarU;
using static Nebula.Roles.Impostor.Hadar;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class VentBombU : PerkFunctionalInstance
{
    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    [NebulaRPCHolder]
    public class VentBombManager : AbstractModule<Virial.Game.Game>, IGameOperator
    {
        static VentBombManager() => DIManager.Instance.RegisterModule(() => new VentBombManager());

        internal void RegisterBomb(int ventId, GamePlayer planter)
        {
            bombVentId[ventId] = planter;
        }

        private VentBombManager()
        {
            ModSingleton<VentBombManager>.Instance = this;
            this.RegisterPermanently();
        }

        private Dictionary<int, GamePlayer> bombVentId = []; // ばくれつボムが仕掛けられてるベントIDと仕掛けたプレイヤー

        void OnVentEnter(PlayerVentButtonUseEnterEvent ev)
        {
            if (!bombVentId.TryGetValue(ev.Vent.Id, out var planter)) return;

            bombVentId.Remove(ev.Vent.Id);

            ev.CancelUse = true; // ボムが仕掛けられてるならベント移動しないようにボタンをキャンセル
            Cannon.FireCannon(ev.Vent.transform.position, planter, -1);
            RpcBombEffect.Invoke((planter, ev.Player));
        }

        static internal RemoteProcess<(GamePlayer owner, GamePlayer target)> RpcBombEffect = new("RpcBombEffect_Uchu",
        (message, _) =>
        {
            if (message.owner.AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(Def.PerkDefinition.perkColor, 0f, 0.25f, 0.45f);
            }

            if (message.target.AmOwner)
            {
                SoundManager.Instance.PlaySound(VentDiscoveredAudio, false, 1f);
            }
        });

        static private AudioClip? VentDiscoveredAudio
        {
            get
            {
                if (!field)
                {
                    var console = UnityEngine.Object.FindObjectOfType<VentCleaningConsole>();

                    if (console != null) field = console.ImpostorDiscoveredSound;
                }

                return field;
            }
        }
    }

    static private float Cooldown => 10f;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front9.png")!.AsImage(100f)!;

    static PerkFunctionalDefinition Def = new("ventBombU", PerkFunctionalDefinition.Category.Standard,
       new PerkDefinition("ventBombU", PerkDefinition.GetPerkBackIcon(4), IconImage, new VColor(92, 113, 156).ToUnityColorUchu(), null).CooldownText("%CD%", () => Cooldown), (def, instance) => new VentBombU(def, instance));

    public VentBombU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
        ventTracker = ObjectTrackers.ForVents(this, 0.8f, MyPlayer, vent => true , MyPlayer.Role.Role.Color.ToUnityColorUchu());
    }

    private GameTimer cooldownTimer;
    bool used = false;
    private ObjectTracker<Vent> ventTracker;

    public override bool HasAction => true;

    public override void OnClick()
    {
        if (MyPlayer.IsDead) return;
        if (used) return;
        if (cooldownTimer.IsProgressing) return;
        if (ventTracker.CurrentTarget == null) return;

        ModSingleton<VentBombManager>.Instance.RegisterBomb(ventTracker.CurrentTarget.Id, MyPlayer);
        MyPlayer.VanillaPlayer.NetTransform.RpcSnapTo(ventTracker.CurrentTarget.transform.position);
        NebulaAsset.PlaySE(NebulaAudioClip.HadarDive, true);

        used = true;
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor((used || ventTracker.CurrentTarget == null) ? Color.gray : Color.white);
    }

    void OnMeetingEnd(TaskPhaseStartEvent ev)
    {
        cooldownTimer.Start(Cooldown);
    }
}

public class PlayerVentButtonUseEnterEvent : AbstractPlayerEvent
{
    internal Vent Vent { get; }
    public bool CancelUse { get; set; }

    internal PlayerVentButtonUseEnterEvent(Virial.Game.Player player, Vent vent) : base(player)
    {
        this.Vent = vent;
        CancelUse = false;
    }
}

public class PlayerVentButtonUseExitEvent : AbstractPlayerEvent
{
    internal Vent Vent { get; }
    public bool CancelUse { get; set; }

    internal PlayerVentButtonUseExitEvent(Virial.Game.Player player, Vent vent) : base(player)
    {
        Vent = vent;
        CancelUse = false;
    }
}

[HarmonyPatch(typeof(VentUsePatch), nameof(VentUsePatch.Prefix))]
public static class VentUseEventPatch
{
    public static bool Prefix(Vent __0)
    {
        var localPlayer = GamePlayer.LocalPlayer;
        var playerControl = PlayerControl.LocalPlayer;

        if (localPlayer == null) return true;
        if (playerControl.walkingToVent) return true;

        if (playerControl.inVent) // 出る
        {
            var ev = GameOperatorManager.Instance?.Run(new PlayerVentButtonUseExitEvent(localPlayer, __0));
            return ev?.CancelUse != true;
        }
        else // 入る
        {
            var ev = GameOperatorManager.Instance?.Run(new PlayerVentButtonUseEnterEvent(localPlayer, __0));
            return ev?.CancelUse != true;
        }
    }
}

[HarmonyPatch]
public static class CannonShowIconPatch
{
    private static MethodBase TargetMethod()
    {
        var type = typeof(Cannon).Assembly.GetTypes().FirstOrDefault(t => typeof(System.Collections.IEnumerator).IsAssignableFrom(t) && t.Name.Contains("CoShowIcon"));
        if (type == null) throw new MissingMethodException("CoShowIcon state machine not found.");

        return AccessTools.Method(type, "MoveNext") ?? throw new MissingMethodException("CoShowIcon.MoveNext not found.");
    }

    public static bool Prefix(object __instance, ref bool __result)
    {
        if (!IsVentBomb(__instance, 0)) return true;

        // VentBombから呼び出された場合はコルーチンを終了
        __result = false;
        return false;
    }

    private static bool IsVentBomb(object? obj, int depth)
    {
        if (obj == null || depth > 3) return false;

        if (obj is ValueTuple<Vector2, byte, int> message) return message.Item3 == -1;

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var field in obj.GetType().GetFields(flags))
        {
            if (field.FieldType == typeof(ValueTuple<Vector2, byte, int>))
            {
                if (field.GetValue(obj) is ValueTuple<Vector2, byte, int> captured && captured.Item3 == -1) return true;
                continue;
            }

            if (field.FieldType.IsValueType || field.FieldType == typeof(string)) continue;

            var fieldType = field.FieldType;
            if (!fieldType.IsDefined(typeof(CompilerGeneratedAttribute), false) && !fieldType.Name.Contains("DisplayClass")) continue;

            var nested = field.GetValue(obj);
            if (nested != null && IsVentBomb(nested, depth + 1)) return true;
        }

        return false;
    }
}