using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Hazel;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Impostor;
using Hori.Scripts.Role.Neutral;
using Il2CppSystem.Resources;
using Il2CppSystem.Threading;
using Nebula;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Patches;
using Nebula.Roles;
using Nebula.Utilities;
using Rewired.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Game;
using Virial.Media;
using static Rewired.Glyphs.UnityUI.UnityUITextMeshProGlyphHelper;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Core;

public class PatchManager
{

    static PatchManager()
    {
        Init();
    }
    
    static Harmony? harmony;
    private static ConstructorInfo? _ctor;

    public static void Init()
    {
        harmony = new Harmony("UchuAddonPatch");

        harmony.Patch(
            typeof(LobbyBehaviour).GetMethod(nameof(LobbyBehaviour.Start)),
            postfix: new HarmonyMethod(typeof(PatchManager).GetMethod("LobbyStart"))
        );

        harmony.Patch(
            typeof(KeyboardJoystick).GetMethod(nameof(KeyboardJoystick.Update)), 
            postfix:new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(LobbyDisableCollider)))
        );

        harmony.Patch(
            typeof(ModNewsHistory).GetMethod(nameof(ModNewsHistory.GetLoaderEnumerator)),
            prefix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(GetLoaderEnumeratorPatch)))
        );

        harmony.Patch(
            typeof(AnnouncementPanel).GetMethod(nameof(AnnouncementPanel.SetUp)),
            postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(SetUpPatch)))
        );

    }

    public static void LobbyStart(LobbyBehaviour __instance)
    {
        var addonLogoHolder = UnityHelper.CreateObject("UchuAddonLogoHolder", HudManager.Instance.transform, new Vector3(-4.3f, 1.95f, 0f));
        addonLogoHolder.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        if (NebulaAPI.GetAddon("Plan17ResourcesPlana") != null) addonLogoHolder.transform.localPosition = new Vector3(-4.3f, 0.7f, 0f);

        var logo = UnityHelper.CreateObject<SpriteRenderer>("UchuAddonLogo", addonLogoHolder.transform, Vector3.zero);
        logo.sprite = NebulaAPI.AddonAsset.GetResource("TitleLogo.png")!.AsImage(100f)!.GetSprite();
        logo.color = new(1f, 1f, 1f, 0.75f);

        var logoButton = logo.gameObject.SetUpButton(true, logo, selectedColor: new Color(0.5f, 0.5f, 0.5f));
        logoButton.OnClick.AddListener(() => AddonScreen.OpenUchuAddonScreen(HudManager.Instance.transform));

        logo.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(7f, 3.5f);

        GameOperatorManager.Instance!.Subscribe<LobbyDestroyEvent>(_ => GameObject.Destroy(addonLogoHolder), Virial.NebulaAPI.CurrentGame!);
    }


    public static void LobbyDisableCollider()
    {
        if (LobbyBehaviour.Instance || GeneralConfigurations.CurrentGameMode == Virial.Game.GameModes.FreePlay)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift)) PlayerControl.LocalPlayer.Collider.enabled = false;
            if (Input.GetKeyUp(KeyCode.LeftShift)) PlayerControl.LocalPlayer.Collider.enabled = true;
        }
        else if (PlayerControl.LocalPlayer.Collider.enabled == false) PlayerControl.LocalPlayer.Collider.enabled = true;
    }

    private static Regex RoleRegex = new Regex("%ROLE:[A-Z]+\\([^)]+\\)%");
    private static Regex OptionRegex = new Regex("%LANG\\([a-zA-Z\\.0-9]+\\)\\,\\([^)]+\\)%");

    public static bool GetLoaderEnumeratorPatch(ref IEnumerator __result)
    {
        __result = CustomLoader();
        return false;
    }

    static IEnumerator CustomLoader()
    {
        if (!NebulaPlugin.AllowHttpCommunication) yield break;

        ModNewsHistory.AllModNews.Clear();

        var lang = Language.GetCurrentLanguage();

        string response = null!;
        yield return NebulaWebRequest.CoGet(Helpers.ConvertUrl($"https://raw.githubusercontent.com/Dolly1016/Nebula/master/Announcement_{lang}.json"), true, r => response = r);

        if (response == null) yield break;

        ModNewsHistory.AllModNews = JsonStructure.Deserialize<List<ModNews>>(response) ?? new();

        response = null!;
        yield return NebulaWebRequest.CoGet(Helpers.ConvertUrl($"https://raw.githubusercontent.com/catudon1276/UchuAddon/master/UchuAddonAnnouncement_{lang}.json"), true, r => response = r);

        if (response == null) yield break;

        ModNewsHistory.AllModNews.AddRange(JsonStructure.Deserialize<List<ModNews>>(response) ?? new());


        foreach (var news in ModNewsHistory.AllModNews)
        {
            foreach (Match match in RoleRegex.Matches(news.detail))
            {
                var split = match.Value.Split(':', '(', ')');
                FormatRoleString(match, ref news.detail, split[1], split[2]);
            }

            foreach (Match match in OptionRegex.Matches(news.detail))
            {
                var split = match.Value.Split('(', ')');

                var translated = Language.Find(split[1]);
                if (translated == null) translated = split[3];
                news.detail = news.detail.Replace(match.Value, translated);
            }
        }
    }

    private static void FormatRoleString(Match match, ref string str, string key, string defaultString)
    {
        foreach (var role in Roles.AllAssignables())
        {
            if (role.LocalizedName.ToUpper() == key)
            {
                str = str.Replace(match.Value, role.DisplayColoredName);
            }
        }
        str = str.Replace(match.Value, defaultString);
    }

    static Sprite UchuAddonLabel = NebulaAPI.AddonAsset.GetResource("UchuAddonTag.png")!.AsImage(100f)!.GetSprite();

    [HarmonyPriority(Priority.VeryLow)]
    public static void SetUpPatch(AnnouncementPanel __instance, [HarmonyArgument(0)] Announcement announcement)
    {
        if (announcement.Number < 200000) return;
        __instance.transform.FindChild("ModLabel").GetComponent<SpriteRenderer>().sprite = UchuAddonLabel;
    }




}

public static class AddonScreen
{
    static public MetaScreen OpenUchuAddonScreen(Transform parent)
    {
        var window = MetaScreen.GenerateWindow(new Vector2(7.5f, 4.6f), parent, new Vector3(0f, 0f, -200f), true, false, true, BackgroundSetting.Modern);
        window.SetWidget(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Center,
            NebulaAPI.GUI.Image(GUIAlignment.Center, NebulaAPI.AddonAsset.GetResource("TitleLogo.png")!.AsImage(100f)!, new(1.5f, 1.5f)),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>UchuAddon ver-β1.3.3</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "\n<size=110%>このアドオンはNebula on the Shipを基に製作されています。</size>"),
            NebulaAPI.GUI.VerticalMargin(0.15f),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentTitle, "Credit"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>\n<b>Idea</b>\n</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>ネコうどん(Catudon)\n</size>\n"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%><b>Code</b>\n</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>ごま。(goma_), あらいもん(araimon), アンハッピーセット(unhappyset), たこ焼き(takoyaki)\n</size>\n"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%><b>Illustration</b>\n</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>ねこかぼちゃ(nekokabocha), マカロン(macaron), シート(Sheat), りょい(ryoi)\n</size>\n"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%><b>Language</b>\n</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>回往(HW)  客串齐(KCQ)  Plana\n</size>"),
            NebulaAPI.GUI.VerticalMargin(0.15f)//,
            //new  GUIModernButton(GUIAlignment.Center, AttributeAsset.OptionsButtonMedium, new RawTextComponent("wiki"))
            //{
            //    OnClick = clickable =>
            //    {
            //        Application.OpenURL("https://hackmd.io/@uchuaddon/home");
            //    },
            //}
            ), 
            new Vector2(0.5f, 1f), out _);

        return window;
    }
}

public static class AnonymousVoteBypass
{
    public static void AnonymousVotesFix(ref bool __result)
    {
        if (__result && NebulaAPI.CurrentGame != null && GamePlayer.LocalPlayer != null)
        {
            if (GamePlayer.LocalPlayer.Role.Role == Hori.Scripts.Role.Crewmate.AdmiralU.MyRole)
            {
                __result = false;
            }
        }
    }

    public static void Patch(Harmony harmony)
    {
        harmony.Patch(
            original: typeof(LogicOptionsNormal).GetMethod("GetAnonymousVotes"),
            postfix: new HarmonyMethod(typeof(AnonymousVoteBypass).GetMethod(nameof(AnonymousVotesFix)))
        );
    }
}