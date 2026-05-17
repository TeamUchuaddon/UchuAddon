using AmongUs.Data;
using AmongUs.GameOptions;
using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Hazel;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Impostor;
using Hori.Scripts.Role.Modifier;
using Hori.Scripts.Role.Neutral;
using Il2CppSystem.Resources;
using Il2CppSystem.Runtime.Serialization.Formatters.Binary;
using Il2CppSystem.Threading;
using Nebula;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Patches;
using Nebula.Roles;
using Nebula.Roles;
using Nebula.Utilities;
using Rewired.UI.ControlMapper;
using Rewired.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Game;
using Virial.Events.Player;
using Virial.Media;
using Virial.Text;
using static Rewired.Glyphs.UnityUI.UnityUITextMeshProGlyphHelper;
using static UnityEngine.ProBuilder.UvUnwrapping;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Core;

static class PatchCompat
{
    static public GamePlayer nebulaPlayer(this PlayerControl player) => GamePlayer.GetPlayer(player.PlayerId);
    static public PlayerControl vanillaPlayer(this GamePlayer player) => player.VanillaPlayer;
}

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

        harmony.Patch(typeof(LobbyBehaviour).GetMethod(nameof(LobbyBehaviour.Start)),postfix: new HarmonyMethod(typeof(PatchManager).GetMethod("LobbyStart")));
        harmony.Patch(typeof(KeyboardJoystick).GetMethod(nameof(KeyboardJoystick.Update)), postfix:new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(LobbyDisableCollider))));

        //harmony.Patch(typeof(ModNewsHistory).GetMethod(nameof(ModNewsHistory.GetLoaderEnumerator)),prefix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(GetLoaderEnumeratorPatch))));
        harmony.Patch(typeof(AnnouncementPanel).GetMethod(nameof(AnnouncementPanel.SetUp)),postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(SetUpPatch))));

        //HastyU.LoadPatch(harmony);
        //LimpidoU.LoadPatch(harmony);

        harmony.Patch(typeof(MeetingHud).GetMethod(nameof(MeetingHud.Awake)),prefix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(StartMeeting))));
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

        GameOperatorManager.Instance!.Subscribe<GameStartEvent>(_ => GameObject.Destroy(addonLogoHolder), Virial.NebulaAPI.CurrentGame!);
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

    public static void StartMeeting()
    {
        GameOperatorManager.Instance?.Run(new MeetingEvent());
    }

}

public static class AddonScreen
{

    static public MetaScreen OpenUchuAddonScreen(Transform parent)
    {
        var window = MetaScreen.GenerateWindow(new Vector2(7.5f, 4.6f), parent, new Vector3(0f, 0f, -200f), true, false, true, BackgroundSetting.Modern);

        window.SetWidget(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Center,
            NebulaAPI.GUI.Image(GUIAlignment.Center, NebulaAPI.AddonAsset.GetResource("TitleLogo.png")!.AsImage(100f)!, new(1.5f, 1.5f),
            onClick: _ =>
            {
                Application.OpenURL("https://catudon1276.github.io/UchuAddonWiki/");
            }),
            //NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>UchuAddon ver-β1.3.8</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "\n<size=110%>このアドオンはNebula on the Shipを基に製作されています。</size>"),
            NebulaAPI.GUI.VerticalMargin(0.15f),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentTitle, "Credit"),
 NebulaAPI.GUI.VerticalMargin(0.15f),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%><b>Code</b>\n</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>ごま。(goma_), あらいもん(araimon), アンハッピーセット(unhappyset), たこ焼き(takoyaki), いおむ(iom)\n</size>\n"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%><b>Illustration</b>\n</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>ねこかぼちゃ(nekokabocha), マカロン(macaron), シート(Sheat), りょい(ryoi), こむぎこ(komugipan)\n</size>\n"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%><b>Language</b>\n</size>"),
            NebulaAPI.GUI.RawText(GUIAlignment.Center, AttributeAsset.DocumentStandard, "<size=110%>KpCam, HW, Plana, 回往\n</size>"),
            NebulaAPI.GUI.VerticalMargin(0.3f),
            new  GUIModernButton(GUIAlignment.Center, AttributeAsset.OptionsButtonMedium, new TranslateTextComponent("uchu.report"))
            {
                OnClick = clickable =>
                {
                    OpenReporGUI(HudManager.Instance.transform);
                },
            }), 
            new Vector2(0.5f, 1f), out _);

        return window;
    }

    static public MetaScreen OpenReporGUI(Transform parent)
    {
        var Repowindow = MetaScreen.GenerateWindow(new Vector2(7.5f, 4.6f), parent, new Vector3(0f, 0f, -200f), true, false, true, BackgroundSetting.Modern);
        var inputField = new GUITextField(Virial.Media.GUIAlignment.Center, new(6f, 2.5f)) { IsSharpField = false, MaxLines = 12, FontSize = 1.4f, HintText = Language.Translate("uchu.report.guide").Color(Color.gray) };
        var nameField = new GUITextField(Virial.Media.GUIAlignment.Left, new(4.2f, 0.21f)) { IsSharpField = false, MaxLines = 12, FontSize = 1.4f, HintText = Language.Translate("uchu.report.name").Color(Color.gray) };
        var checkBoxRequest = new NoSGUICheckbox(Virial.Media.GUIAlignment.Center, false);
        var checkBoxThoughts = new NoSGUICheckbox(Virial.Media.GUIAlignment.Center, false);
        var checkBoxBug = new NoSGUICheckbox(Virial.Media.GUIAlignment.Center, false);

        Repowindow.SetWidget(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Center,
            NebulaAPI.GUI.VerticalMargin(0.2f),
            NebulaAPI.GUI.LocalizedText(GUIAlignment.Center, AttributeAsset.DocumentTitle, "uchu.report.title"),
            NebulaAPI.GUI.VerticalMargin(0.1f),
            nameField,
            NebulaAPI.GUI.VerticalMargin(0.1f),
            inputField,
            NebulaAPI.GUI.HorizontalHolder(Virial.Media.GUIAlignment.Left,
            checkBoxBug,
            NebulaAPI.GUI.HorizontalMargin(0.2f),
            NebulaAPI.GUI.LocalizedText(Virial.Media.GUIAlignment.Center, AttributeAsset.DocumentBold, "uchu.report.checkBug"),
            NebulaAPI.GUI.HorizontalMargin(0.4f),
            checkBoxRequest,
            NebulaAPI.GUI.HorizontalMargin(0.2f),
            NebulaAPI.GUI.LocalizedText(Virial.Media.GUIAlignment.Center, AttributeAsset.DocumentBold, "uchu.report.checkRequest"),
            NebulaAPI.GUI.HorizontalMargin(0.4f),
            checkBoxThoughts,
            NebulaAPI.GUI.HorizontalMargin(0.2f),
            NebulaAPI.GUI.LocalizedText(Virial.Media.GUIAlignment.Center, AttributeAsset.DocumentBold, "uchu.report.checkThoughts")
            ),
            new GUIModernButton(GUIAlignment.Center, AttributeAsset.OptionsButtonMedium, new TranslateTextComponent("uchu.report.send"))
            {
                OnClick = clickable =>
                {
                    var field = inputField.Artifact.FirstOrDefault();
                    var name = nameField.Artifact.FirstOrDefault();

                    var bugArtifact = checkBoxBug.Artifact.FirstOrDefault();
                    var requestArtifact = checkBoxRequest.Artifact.FirstOrDefault();
                    var thoughtsArtifact = checkBoxThoughts.Artifact.FirstOrDefault();

                    bool isBug = bugArtifact.getter();
                    bool isRequest = requestArtifact.getter();
                    bool isThoughts = thoughtsArtifact.getter();

                    var text = field.Text;
                    var nameText = name.Text;
                    string secretText = "匿名";

                    if (text.Length == 0)
                    {
                        field.SetHint(Language.Translate("uchu.report.emptyError").Color(UnityEngine.Color.red.RGBMultiplied(0.7f)).Bold());
                        return;
                    }
                    
                    List<string> tags = new();
                    if (isBug) tags.Add("バグ報告");
                    if (isRequest) tags.Add("リクエスト");
                    if (isThoughts) tags.Add("感想");
                    if (tags.Count == 0) tags.Add("未分類");

                    string tagText = string.Join(" ", tags);
                    string finalText;

                    if (nameText.Length == 0)
                    {
                        finalText =
                        $"[{tagText}]" +
                        $"{text}\n" +
                        $"by {secretText}";
                    }
                    else
                    {
                        finalText =
                        $"[{tagText}]" +
                        $"{text}\n" +
                        $"by {nameText}";
                    }

                    if (Repowindow) Repowindow.CloseScreen();
                    var confirmDialog = MetaUI.ShowConfirmDialog(parent, new TranslateTextComponent("uchu.report.wait"));

                    Webhook(finalText, response =>
                    {
                        if (confirmDialog) confirmDialog.CloseScreen();

                        MetaUI.ShowConfirmDialog(parent, new TranslateTextComponent("uchu.report.finished"));
                    });
                }
            }), 
            new Vector2(0.5f, 1f), out _);

        return Repowindow;
    }

    static private HttpContent GenerateContent(string text)
    {
        return new FormUrlEncodedContent([new("content", text)]);
    }

    private const string u1 = "1488531335927828715";
    private const string u2 = "kBXtTcv32nq4YcxE4lNfZ2612QLNqChxjFT_vgHxkGd9uOzUguvG1Y6PPbd7GtDYeiOe";

    static private bool Webhook(string text, Action<System.Net.Http.HttpResponseMessage> onFinished)
    {
        try
        {
            HttpContent content = GenerateContent(text);

            var task = NebulaPlugin.HttpClient.PostAsync("https://discord.com/api/webhooks/" + u1 + "/" + u2, content);
            NebulaManager.Instance.StartCoroutine(ManagedEffects.Sequence(
                task.WaitAsCoroutine(),
                ManagedEffects.Action(() =>
                {
                    content.Dispose();
                    onFinished.Invoke(task.Result);
                })
                ).WrapToIl2Cpp());

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}

public class MeetingEvent : Virial.Events.Event
{
    internal MeetingEvent()
    {
    }
}

public static class AnonymousVoteBypass
{
    public static void AnonymousVotesFix(ref bool __result)
    {
        if (__result && NebulaAPI.CurrentGame != null && GamePlayer.LocalPlayer != null)
        {
            if ((GamePlayer.LocalPlayer.Role.Role == Hori.Scripts.Role.Crewmate.AdmiralU.MyRole && AdmiralU.VoteWatching)　|| GamePlayer.LocalPlayer.TryGetModifier<WatcherU.Instance>(out _) || (GamePlayer.LocalPlayer.Role.Role == MoiraU.MyRole && MoiraU.CanSeeVoteOption))
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

internal class  SabotageCheck
{
    public static bool IsComms()
    {
        try
        {
            if (!ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Comms, out ISystemType system)) return false;
            HudOverrideSystemType hudOverride;
            HqHudSystemType hqHud;
            if ((hudOverride = system.TryCast<HudOverrideSystemType>()) != null) return hudOverride.IsActive;
            if ((hqHud = system.TryCast<HqHudSystemType>()) != null) return hqHud.IsActive;

            return false;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    public static bool IsElectrical()
    {
        try
        {
            if (!ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Electrical, out ISystemType system)) return false;
            SwitchSystem electrical;
            if ((electrical = system.TryCast<SwitchSystem>()) != null) return electrical.IsActive;
            return false;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}

static public class FreeColorRoleIcon
{
    static private Dictionary<string, Material> materialCache = new();

    static private Material CreateMaterial(Color roleColor, Color subColor)
    {
        var mat = new Material(NebulaAsset.RoleIconShader);

        mat.SetColor("_RedTo", roleColor);
        mat.SetColor("_GreenTo", subColor);
        mat.SetFloat("_Outline", 1.07f - 0.45f);

        return mat;
    }

    static public string GetRoleIconCustomColorTag( this DefinedAssignable assignable, Color roleColor, Color subColor, bool masked = false, int size = 100)
    {
        if (assignable == null) return "";

        string key = assignable.InternalName + roleColor.ToString() + subColor.ToString() +masked;

        if (!materialCache.TryGetValue(key, out var mat))
        {
            mat = CreateMaterial(roleColor, subColor);
            materialCache[key] = mat;
        }

        var sprite = assignable.GetRoleIcon()?.GetSprite();
        if (sprite == null) return "";

        var tag = RuntimeDynamicSpriteGenerator.GetSpriteTag(sprite, mat, assignable.InternalName, masked);

        if (size == 100) return tag;
        return tag.Sized(size);
    }

    static public string GetRoleIconCustomColorTagSmall(this DefinedAssignable assignable, Color roleColor, Color subColor, bool masked = false) => GetRoleIconCustomColorTag(assignable, roleColor, subColor, masked, 70);
}

static public class RuntimeDynamicSpriteGenerator
{
    static private Dictionary<string, int> idMap = new();

    static public TMP_SpriteAsset SpriteAsset { get; private set; }

    static public string GetSpriteTag( Sprite sprite, Material mat, string name, bool masked)
    {
        int id = Register(sprite, mat, name);

        if (masked)
            return $"<sprite name=\"masked_{name}_{id}\">";
        else
            return $"<sprite name=\"dynamic_{name}_{id}\">";
    }

    static private int Register(Sprite sprite, Material mat, string name)
    {
        string key = name + mat.GetHashCode();

        if (idMap.TryGetValue(key, out var id))
            return id;

        id = idMap.Count;
        idMap[key] = id;

        return id;
    }
}