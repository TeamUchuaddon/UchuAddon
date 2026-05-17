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

[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
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
    public static TranslatableTag VoyagerParadox = new("state.voyagerParadox");
    public static TranslatableTag HjkBalloonDead = new("state.hjkBalloonDead");
    public static TranslatableTag tunaDead = new("state.tuna.dead"); //マグロ調理
}