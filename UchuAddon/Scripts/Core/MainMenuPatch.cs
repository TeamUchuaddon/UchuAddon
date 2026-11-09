using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Hori.Core;
using Il2CppSystem;
using Nebula;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Virial.Media;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Core;

public class MainMenuPatch
{
    static MainMenuPatch()
    {
        Init();
    }

    public static void Init()
    {
        var harmony = new Harmony("UchuAddonMainMenuPatch");
        harmony.PatchAll();
    }


    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
    public static class ButtonMainMenuPatch
    {
        static private readonly Image TitleLogo = NebulaAPI.AddonAsset.GetResource("TitleLogo.png")!.AsImage(400f)!;
        static private readonly Image IconSprite = NebulaAPI.AddonAsset.GetResource("UchuMenuIcon.png")!.AsImage(115f)!;
        const string DiscordUrl = "https://discord.gg/B5kqzhp5Zv";
        const int SocialPanelSortingOrder = 0;

        static readonly Vector3 StackOrigin = new Vector3(-0.2145f, -1.5236f, 0f);

        static Transform? MenuTransform;
        static bool Created = false;

        static readonly List<GameObject> ToggleTargets = new();
        static bool IsExpanded = true;
        static SpriteRenderer? MinimizeRenderer;

        static void Postfix(MainMenuManager __instance)
        {
            var uchuMenuRenderer = UnityHelper.CreateObject<SpriteRenderer>("UchuMenuButton", __instance.transform, Vector3.Zero);
            uchuMenuRenderer.gameObject.SetAsUIAspectContent(AspectPosition.EdgeAlignments.RightBottom, new Vector3(0.78f, 0.24f, -6f));
            uchuMenuRenderer.transform.localScale = new(0.9f, 0.9f, 1f);
            uchuMenuRenderer.sprite = IconSprite.GetSprite();
            var menuButton = uchuMenuRenderer.gameObject.SetUpButton(true, uchuMenuRenderer);
            menuButton.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(menuButton, Language.Translate("uchu.label.menu")));
            menuButton.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(menuButton));
            menuButton.OnClick.AddListener(() => Application.OpenURL(DiscordUrl));
            menuButton.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;

            var titleLogoRenderer = UnityHelper.CreateObject<SpriteRenderer>("TitleLogo", __instance.transform, Vector3.Zero);
            titleLogoRenderer.transform.localPosition = new Vector3(4.4424f, 1.4836f, -6f);
            titleLogoRenderer.transform.localScale = new(1f, 1f, 1f);
            titleLogoRenderer.sprite = TitleLogo.GetSprite();

            var version = NebulaAddon.GetAddon("UchuAddon")?.Version ?? "?.?.?";
            var versionObj = UnityHelper.CreateObject<TextMeshPro>("VersionText", __instance.transform, Vector3.Zero);
            versionObj.transform.localPosition = new Vector3(4.4424f, 1f, -6f);
            versionObj.text = $"v{version}";
            versionObj.fontSize = 2f;
            versionObj.fontStyle = FontStyles.Bold;
            versionObj.alignment = TextAlignmentOptions.Center;
            versionObj.color = new Color(1f, 1f, 1f, 1f);

            MenuTransform = __instance.transform;
            Created = false;
        }
    }
}