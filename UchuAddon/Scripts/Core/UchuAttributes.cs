using Cpp2IL.Core.Extensions;
using Hori.Core;
using Il2CppInterop.Runtime.Injection;
using System.IO;
using UnityEngine.UIElements;
using Virial.Compat;
using Virial.Helpers;
using static Hori.Scripts.Role.Impostor.BoxerU;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Mathn = Virial.Utilities.Mathn;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Core;

/// <summary>
/// UchuAddonが提供するアトリビュートです。
/// </summary>
public static class UchuAttributes
{
    /// <summary>
    /// 虹色になります。
    /// </summary>
    static public IPlayerAttribute Rainbow { get; internal set; }
    /// <summary>
    /// キルを防ぐアトリビュートです。<br>キルエフェクトがガードした側も見え、自覚できます。</br>
    /// </summary>
    static public IPlayerAttribute ObviousGuard { get; internal set; }
    /// <summary>
    /// キルを防ぐアトリビュートです。<br>キルエフェクトはキルした側しか見えません。</br>
    /// </summary>
    static public IPlayerAttribute Guard { get; internal set; }

    static UchuAttributes()
    {
        Rainbow = new PlayerAttributeImpl(3, "$rainbow", "rainbow") { Cognizable = _ => false };
        ObviousGuard = new PlayerAttributeImpl(5, "$obviousGuard", "obviousGuard");
        Guard = new PlayerAttributeImpl(5, "$guard", "guard") { Cognizable = _ => false };
    }
}

[NebulaPreprocess(PreprocessPhase.PostRoles)]
[NebulaRPCHolder]
public class AttributesManager : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static AttributesManager() => DIManager.Instance.RegisterModule(() => new AttributesManager());

    private AttributesManager()
    {
        ModSingleton<AttributesManager>.Instance = this;
        this.RegisterPermanently();
    }

    /// <summary>
    /// ガード系のアトリビュートを処理
    /// </summary>
    void OnMurder(PlayerCheckKilledEvent ev)
    {
        if (ev.Player.MyKiller == ev.Player) return; // 自殺は例外

        if (ev.Player.HasAttribute(UchuAttributes.Guard) || ev.Player.HasAttribute(UchuAttributes.ObviousGuard))
        {
            ev.Result = ev.Player.HasAttribute(UchuAttributes.Guard) ? KillResult.Guard : KillResult.ObviousGuard;
        }
    }
}

// RainbowAttributeの処理
[HarmonyPatch(typeof(PlayerModInfo), nameof(PlayerModInfo.UpdateVisibilityInner))]
public class PlayerControlPatch
{
    private const float RainbowSpeed = 0.25f;

    private static void Postfix(PlayerModInfo __instance, float? deltaTime, bool update, bool ignoreShadow, bool showNameText, ref float alpha, ref float alphaIgnoresWall)
    {
        if (__instance.HasAttribute(UchuAttributes.Rainbow))
        {
            var myCosmetics = __instance.MyLayer;

            float hue = Mathf.Repeat(Time.time * RainbowSpeed, 1f);
            Color rainbowColor = Color.HSVToRGB(hue, 0.85f, 1f);

            if (myCosmetics.currentBodySprite.BodySprite.AsBoolFast(out var bodySprite))
            {
                Color originalColor = bodySprite.color;
                Color mixedColor = Color.Lerp(originalColor, rainbowColor, 0.7f);

                mixedColor.a = alpha;
                bodySprite.color = mixedColor;
            }
        }
    }
}