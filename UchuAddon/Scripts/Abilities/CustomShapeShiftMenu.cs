using Hori.Core;
using Hori.Scripts.Core;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Abilities;

[HarmonyPatch(typeof(ShapeshifterMinigame), nameof(ShapeshifterMinigame.Shapeshift))]
public static class ShapeshifterPatch
{
    private static bool Prefix(ShapeshifterMinigame __instance, PlayerControl target)
    {
        if (!__instance.IsCustom()) return true;

        PlayerControl selectedPlayer = target;

        var selected = GamePlayer.GetPlayer(target.PlayerId);

        GameOperatorManager.Instance?.Run(new CustomShapeshiftEvent(GamePlayer.LocalPlayer, selected, __instance));

        return false;
    }
}

/// <summary>
/// ShapeShifterのミニゲームでプレイヤーを選択した時に発火します。<br>
/// IsCustom()がtrueの時のみ発火します。</br>
/// </summary>
public class CustomShapeshiftEvent : AbstractPlayerEvent
{
    public Virial.Game.Player Target;
    public ShapeshifterMinigame Panel;

    internal CustomShapeshiftEvent(Virial.Game.Player player, Virial.Game.Player target, ShapeshifterMinigame panel) : base(player)
    {
        this.Target = target;
        this.Panel = panel;
    }
}

/// <summary>
/// ShapeShifterMinigameの拡張メゾットです。
/// </summary>
public static class ShapeShiftExtensions
{
    private static readonly ConditionalWeakTable<ShapeshifterMinigame, BoolHolder> Data = new();

    private sealed class BoolHolder
    {
        public bool Value;
    }

    /// <summary>
    /// ShapeShifterMinigameがカスタムメニューかどうかを取得します。<br>
    /// カスタムメニューではイベントを通して独自の処理をさせることができます。</br>
    /// </summary>
    public static bool IsCustom(this ShapeshifterMinigame instance)
    {
        return Data.GetOrCreateValue(instance).Value;
    }

    /// <summary>
    /// ShapeShifterMinigameがカスタムメニューかどうかを設定します。
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="value"></param>
    public static void SetIsCustom(this ShapeshifterMinigame instance, bool value)
    {
        Data.GetOrCreateValue(instance).Value = value;
    }
}