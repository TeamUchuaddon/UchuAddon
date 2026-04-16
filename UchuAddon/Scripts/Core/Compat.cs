using Cpp2IL.Core.Extensions;
using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Impostor;
using Hori.Scripts.Role.Neutral;
using Il2CppInterop.Runtime.InteropTypes;
using Nebula.Modules.GUIWidget;
using Nebula.Roles;
using Nebula.Utilities;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Media;

namespace Hori.Core;

public static class APICompat
{
    /*static public UnityEngine.Color ToUnityColor(this Virial.Color color) => new UnityEngine.Color(color.R, color.G, color.B, color.A);
    static public Virial.Color ToNebulaColor(this UnityEngine.Color color) => new Virial.Color(color.r, color.g, color.b, color.a);
    static public UnityEngine.Vector2 ToUnityVector(this Virial.Compat.Vector2 v) => new UnityEngine.Vector2(v.x, v.y);
    static public UnityEngine.Vector3 ToUnityVector(this Virial.Compat.Vector3 v) => new UnityEngine.Vector3(v.x, v.y, v.z);
    static public Virial.Compat.Vector2 ToNebulaVector(this UnityEngine.Vector2 v) => new Virial.Compat.Vector2(v.x, v.y);
    static public Virial.Compat.Vector3 ToNebulaVector(this UnityEngine.Vector3 v) => new Virial.Compat.Vector3(v.x, v.y, v.z);
    static public GamePlayer ToNebulaPlayer(this PlayerControl player) => GamePlayer.GetPlayer(player.PlayerId)!;
    static public PlayerControl ToAUPlayer(this GamePlayer player)
    {
        throw new NotSupportedException("Player.VanillaPlayer プロパティはアクセスできません。PlayerControl への変換方法を実装してください。");
    }
    public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
    {
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }
    public static void AddValueV2(this Dictionary<byte, int> self, byte target, int num)
    {
        if (self.TryGetValue(target, out var last))
            self[target] = last + num;
        else
            self[target] = num;
    }
    public static KeyValuePair<byte, int> MaxPairV2(this Dictionary<byte, int> self, out bool tie)
    {
        tie = true;
        KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(PlayerVoteArea.SkippedVote, 0);
        foreach (KeyValuePair<byte, int> keyValuePair in self)
        {
            if (keyValuePair.Value > result.Value)
            {
                result = keyValuePair;
                tie = false;
            }
            else if (keyValuePair.Value == result.Value)
            {
                tie = true;
            }
        }
        return result;
    }
    /static public AllocationParameters MoriartizedParamaters(this DefinedRole role)
    {
        if (PatchManager.MoriartizedRoleDic.TryGetValue(role.Id, out var p))
        {
            return p;
        }
        return null;

    }
    static public AllocationParameters YandereRoleParamaters(this DefinedRole role)
    {
        if (PatchManager.YandereRoleDic.TryGetValue(role.Id, out var p))
        {
            return p;
        }
        return null;
    }/
    static public bool IsModMadmate(this GamePlayer player)
    {
        if (player.IsMadmate)
        {
            return true;
        }
        return false;
    }
    public static UnityEngine.Color RGBMultiplied(this UnityEngine.Color color, float multiplier)
    {
        return new UnityEngine.Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
    }
    public static UnityEngine.Color RGBMultiplied(this UnityEngine.Color color, UnityEngine.Color multiplier)
    {
        return new UnityEngine.Color(color.r * multiplier.r, color.g * multiplier.g, color.b * multiplier.b, color.a);
    }
    static public UnityEngine.Color AlphaMultiplied(this UnityEngine.Color color, float multiplier)
    {
        return new UnityEngine.Color(color.r, color.g, color.b, color.a * multiplier);
    }
    static public Virial.Color AlphaMultiplied(this Virial.Color color, float multiplier)
    {
        return new Virial.Color(color.R, color.G, color.B, color.A * multiplier);
    }
    static public FieldInfo GetPrivateFieldInfo(this object instance, string fieldname)
    {
        return instance.GetType().GetField(fieldname, BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
    static public T GetPrivateField<T>(this object instance, string fieldname)
    {
        return (T)(object)instance.GetPrivateFieldInfo(fieldname).GetValue(instance)!;
    }
    static public void SetPrivateField(this object instance, string fieldname, object value)
    {
        instance.GetPrivateFieldInfo(fieldname).SetValue(instance, value);
    }
    static public MethodInfo GetPrivateMethodInfo(this object instance, string method)
    {
        if (instance is Type)
        {
            return (instance as Type)!.GetPrivateMethodInfoType(method);
        }
        return instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
    static public MethodInfo GetPrivateMethodInfoType(this Type type, string method)
    {
        return type.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
    static public MethodInfo GetPrivateStaticMethodInfo(this object instance, string method)
    {
        return instance.GetType().GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!;
    }
    static public MethodInfo GetPrivateStaticMethodInfoType(this Type type, string method)
    {
        return type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!;
    }
    static public T CallPrivateMethod<T>(this object instance, string method, params object[] param)
    {
        return (T)(object)instance.GetPrivateMethodInfo(method).Invoke(instance, param)!;
    }
    static public T CallPrivateStaticMethod<T>(this object instance, string method, params object[] param)
    {
        return (T)(object)instance.GetPrivateStaticMethodInfo(method).Invoke(instance, param)!;
    }
    static public Type GetPrivateChildType(this Type t, string name)
    {
        return t.GetNestedType(name, BindingFlags.NonPublic)!;
    }*/

    private static AssetBundle? ab;

    public static AudioClip GetSound(string name)
    {
        if (ab == null)
        {
            ab = AssetBundle.LoadFromMemory(NebulaAPI.AddonAsset.GetResource("uchuaddon_asset.bundle")!.AsStream()!.ReadBytes());
        }
        return ab!.LoadAsset(name + ".wav").Cast<AudioClip>();
    }

}

public class AddonConfigurationTags
{
    private static Virial.Media.Image AddonTagSprite = NebulaAPI.AddonAsset.GetResource("UchuAddonTag.png").AsImage(100f);
    static private GUIWidget GetTagTextWidget(string translationKey) => new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new TranslateTextComponent("configuration.tag." + translationKey));
    static public ConfigurationTag TagUchuAddon { get; private set; } = new(AddonTagSprite, GetTagTextWidget("UchuAddonTag"));
}

[NebulaPreprocess(PreprocessPhase.BuildNoSModule)]
public class UchuTranslatable
{
    public static TranslatableTag ChickenVentDead = new("state.chickenDeadVent"); //転倒死  ベント
    public static TranslatableTag ChickenFallDead = new("state.chickenDeadFall"); //転落死　梯子・ジップライン
    public static TranslatableTag ChickenDropDead = new("state.chickenDeadDrop"); //落下死　昇降機
    public static TranslatableTag ChickenDoorDead = new("state.chickenDeadDoor"); //挟圧死　ドア
    public static TranslatableTag ChickenMessDead = new("state.chickenDeadWuss"); //錯乱死　停電
    public static TranslatableTag ChickenWussDead = new("state.chickenDeadMess"); //恐怖死　投票
    public static TranslatableTag EclipseBomb = new("state.eclipseBomb"); //エクリプス　爆撃
    public static TranslatableTag RocketLaunch = new("state.rocketLaunch"); //ロケット 発射
    public static TranslatableTag WitchSpell = new("state.witchSpell"); //ウィッチ 魔術
}