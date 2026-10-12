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

// TOH K ありがとうゾーン

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
        static private readonly Image LinkLogo = NebulaAPI.AddonAsset.GetResource("LinkLogo.png")!.AsImage(400f)!;
        static private readonly Image MinimizeImage = NebulaAPI.AddonAsset.GetResource("minimize.png")!.AsImage(115f)!;
        static private readonly Image MaximizeImage = NebulaAPI.AddonAsset.GetResource("maximize.png")!.AsImage(115f)!;

        const string YouTubeUrl = "https://www.youtube.com/@TeamUchuaddon";
        const string TwitterUrl = "https://x.com/uchu_addons";
        const string WikiUrl = "https://teamuchuaddon.github.io/UchuAddonWiki/";
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

        /*

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        class SocialButtonsStartPatch
        {
            public static void Postfix()
            {
                if (MenuTransform == null) return;
                if (Created) return;
                Created = true;
                CreateSocialButtons(MenuTransform);
            }
        }

        static void CreateSocialButtons(Transform parent)
        {
            var template = FindPlayButtonTemplate();
            const float sizeFactor = 0.55f;
            var refScale = template != null ? template.transform.localScale : UnityEngine.Vector3.one;
            var scale = new Vector3(refScale.x * sizeFactor, refScale.y * sizeFactor, refScale.z * sizeFactor);
            const float spacingFactor = 0.78f;
            var verticalSpacing = Mathf.Abs(scale.y) * spacingFactor;

            var group = new GameObject("SocialButtonGroup");
            group.transform.SetParent(parent);
            group.transform.localPosition = StackOrigin;
            group.transform.localScale = Vector3.One;

            ToggleTargets.Clear();
            IsExpanded = true;

            var bg = CreateSocialPanelBackground(group.transform, template);
            if (bg != null) ToggleTargets.Add(bg);

            (string url, string name, string label, Color color)[] buttons =
            [
                (YouTubeUrl, "YouTube", "YouTube", new Color(0.92f, 0.12f, 0.12f)),
                (TwitterUrl, "Twitter", "Twitter", new Color(0.35f, 0.82f, 0.98f)),
                (WikiUrl,    "Wiki",    "Wiki",    Color.white),
                (DiscordUrl, "Discord", "Discord", new Color(0.34f, 0.4f,  0.95f)),
            ];

            var centerOffset = (buttons.Length - 1) / 2f * verticalSpacing;

            var logoRenderer = UnityHelper.CreateObject<SpriteRenderer>("SocialGroupLogo", parent, Vector3.Zero);
            logoRenderer.sprite = LinkLogo.GetSprite();
            logoRenderer.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            logoRenderer.transform.localPosition = new Vector3(-0.1309f, -0.2111f, 0f);
            logoRenderer.sortingOrder = SocialPanelSortingOrder;
            ToggleTargets.Add(logoRenderer.gameObject);

            for (var i = 0; i < buttons.Length; i++)
            {
                var (url, name, label, color) = buttons[i];
                var localPos = new Vector3(0f, centerOffset - i * verticalSpacing, 0f);
                if (template != null)
                    CreateSocialButtonFromPlayTemplate(template, group.transform, name, url, label, color, localPos, scale);
                else
                    CreateSocialButtonFallback(group.transform, name, url, label, color, localPos, scale);
            }

            for (var i = 0; i < group.transform.childCount; i++)
            {
                var child = group.transform.GetChild(i).gameObject;
                if (child.name.EndsWith("Button"))
                    ToggleTargets.Add(child);
            }
            CreateMinimizeButton(group.transform);
        }

        static void CreateMinimizeButton(Transform group)
        {
            var minRenderer = UnityHelper.CreateObject<SpriteRenderer>("MinimizeButton", group, Vector3.Zero);
            minRenderer.sprite = MinimizeImage.GetSprite();
            minRenderer.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            minRenderer.transform.localPosition = new Vector3(-0.8673f, 1.778f, -4f);
            minRenderer.sortingOrder = 0;
            MinimizeRenderer = minRenderer;

            var minButton = minRenderer.gameObject.SetUpButton(true, minRenderer);
            var col = minRenderer.gameObject.AddComponent<BoxCollider2D>();
            var sp = minRenderer.sprite;
            if (sp != null) col.size = sp.bounds.size;
            minButton.OnClick.AddListener((System.Action)(() => ToggleMinimize()));
            minButton.OnMouseOver.AddListener((System.Action)(() => NebulaManager.Instance.SetHelpWidget(minButton, Language.Translate("uchu.label.minimize"))));
            minButton.OnMouseOut.AddListener((System.Action)(() => NebulaManager.Instance.HideHelpWidgetIf(minButton)));
            minButton.OnMouseOver.AddListener((System.Action)(() => NebulaManager.Instance.SetHelpWidget(minButton, Language.Translate("uchu.label.minimize"))));
            minButton.OnMouseOut.AddListener((System.Action)(() => NebulaManager.Instance.HideHelpWidgetIf(minButton)));
        }

        static void ToggleMinimize()
        {
            IsExpanded = !IsExpanded;
            foreach (var t in ToggleTargets)
            {
                if (t != null) t.SetActive(IsExpanded);
            }

            if (MinimizeRenderer != null)
                MinimizeRenderer.sprite = IsExpanded ? MinimizeImage.GetSprite() : MaximizeImage.GetSprite();
        }

        static GameObject? CreateSocialPanelBackground(Transform parent, GameObject? buttonTemplate)
        {
            var sourceRenderer = FindSpriteRenderer("ReworkedLeftPanel");
            if (sourceRenderer == null) return null;

            var buttonRenderer = buttonTemplate != null ? buttonTemplate.GetComponentInChildren<SpriteRenderer>(true) : null;
            var background = UnityHelper.CreateObject<SpriteRenderer>("SocialPanelBackground", parent, Vector3.Zero);
            background.transform.localPosition = new Vector3(-0.0255f, 0.3091f, 0.01f);
            background.transform.localScale = new Vector3(0.5691f, 0.6054f, 1f);
            background.sprite = sourceRenderer.sprite;
            background.color = sourceRenderer.color;
            background.sortingLayerID = buttonRenderer != null ? buttonRenderer.sortingLayerID : sourceRenderer.sortingLayerID;
            background.sortingOrder = SocialPanelSortingOrder;
            return background.gameObject;
        }

        static SpriteRenderer? FindSpriteRenderer(string objectName)
        {
            var source = GameObject.Find(objectName);
            var renderer = source != null ? source.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (renderer != null) return renderer;

            foreach (var obj in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<SpriteRenderer>()))
            {
                var candidate = obj.TryCast<SpriteRenderer>();
                if (candidate == null) continue;
                if (candidate.name == objectName || candidate.gameObject.name == objectName)
                    return candidate;
            }

            return null;
        }

        static Color GetSocialLabelColor(string name)
            => name is "YouTube" or "Discord" ? Color.white
             : name == "GitHub" ? new Color(0.15f, 0.15f, 0.15f)
             : Color.white;

        static GameObject? FindPlayButtonTemplate()
        {
            foreach (var name in new[] { "PlayButton", "playButton", "Play Button" })
            {
                var found = GameObject.Find(name);
                if (found != null) return found;
            }

            var mainButtons = GameObject.Find("Main Buttons");
            if (mainButtons == null) return null;

            for (var i = 0; i < mainButtons.transform.childCount; i++)
            {
                var child = mainButtons.transform.GetChild(i);
                if (child.name is "BottomButtonBounds" or "Divider") continue;
                if (child.name.Contains("play", System.StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }

            for (var i = 0; i < mainButtons.transform.childCount; i++)
            {
                var child = mainButtons.transform.GetChild(i);
                if (child.name is "BottomButtonBounds" or "Divider") continue;
                if (child.GetComponent<PassiveButton>() != null)
                    return child.gameObject;
            }

            return null;
        }

        static void CreateSocialButtonFromPlayTemplate(GameObject template, Transform parent, string name, string url, string label, Color color, Vector3 localPos, Vector3 scale)
        {
            var btn = UnityEngine.Object.Instantiate(template, parent);
            btn.name = name + "Button";
            btn.SetActive(true);

            foreach (var ap in btn.GetComponentsInChildren<AspectPosition>(true))
                UnityEngine.Object.DestroyImmediate(ap);

            var passive = btn.GetComponent<PassiveButton>();
            if (passive == null) return;

            CopyMatchingSpriteRenderers(template.transform, btn.transform);
            ApplySocialButtonColor(btn, passive, color);
            ApplySortingOrder(btn, SocialPanelSortingOrder);
            RemoveIconObjects(btn);

            passive.OnClick = new();
            passive.OnClick.AddListener((System.Action)(() => Application.OpenURL(url)));

            ApplySocialButtonLabel(btn, name, label);

            var labelColor = GetSocialLabelColor(name);
            var texts = btn.GetComponentsInChildren<TMP_Text>(true);

            var helpKey = "uchu.label." + name.ToLower();
            passive.OnMouseOver = new();
            passive.OnMouseOver.AddListener((System.Action)(() =>
            {
                NebulaManager.Instance.SetHelpWidget(passive, Language.Translate(helpKey));
                foreach (var t in texts) if (t != null) t.color = labelColor;
            }));
            passive.OnMouseOut = new();
            passive.OnMouseOut.AddListener((System.Action)(() =>
            {
                NebulaManager.Instance.HideHelpWidgetIf(passive);
                foreach (var t in texts) if (t != null) t.color = labelColor;
            }));

            btn.transform.localScale = scale;
            btn.transform.localPosition = localPos;
        }

        static void RemoveIconObjects(GameObject btn)
        {
            var icons = new List<GameObject>();
            CollectIconObjects(btn.transform, icons);
            foreach (var icon in icons)
                UnityEngine.Object.DestroyImmediate(icon);
        }

        static void CollectIconObjects(Transform root, List<GameObject> result)
        {
            if (root.name == "Icon")
            {
                result.Add(root.gameObject);
                return;
            }

            for (var i = 0; i < root.childCount; i++)
                CollectIconObjects(root.GetChild(i), result);
        }

        static void ApplySortingOrder(GameObject obj, int sortingOrder)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
                renderer.sortingOrder = sortingOrder;
        }

        static void ApplySocialButtonLabel(GameObject btn, string name, string label)
        {
            var labelColor = GetSocialLabelColor(name);
            foreach (var text in btn.GetComponentsInChildren<TMP_Text>(true))
            {
                var translator = text.GetComponent<TextTranslatorTMP>();
                if (translator != null) UnityEngine.Object.Destroy(translator);
                text.text = label;
                text.SetText(label);
                text.color = labelColor;
            }
        }

        static void ApplySocialButtonColor(GameObject btn, PassiveButton passive, Color color)
        {
            if (btn.transform.childCount > 0)
                ApplyColorToRenderers(btn.transform.GetChild(0), color);

            if (passive.inactiveSprites != null)
                ApplyColorToRenderers(passive.inactiveSprites.transform, color);

            if (passive.activeSprites != null)
                ApplyColorToRenderers(passive.activeSprites.transform, color);
        }

        static void ApplyColorToRenderers(Transform root, Color color)
        {
            if (root.name == "Icon") return;

            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = color;

            for (var i = 0; i < root.childCount; i++)
                ApplyColorToRenderers(root.GetChild(i), color);
        }

        static void CopyMatchingSpriteRenderers(Transform template, Transform clone)
        {
            var templateRenderer = template.GetComponent<SpriteRenderer>();
            var cloneRenderer = clone.GetComponent<SpriteRenderer>();
            if (templateRenderer != null && cloneRenderer != null)
            {
                cloneRenderer.sprite = templateRenderer.sprite;
                cloneRenderer.color = templateRenderer.color;
            }

            var childCount = Mathf.Min(template.childCount, clone.childCount);
            for (var i = 0; i < childCount; i++)
                CopyMatchingSpriteRenderers(template.GetChild(i), clone.GetChild(i));
        }

        static void CreateSocialButtonFallback(Transform parent, string name, string url, string label, Color color, Vector3 localPos, Vector3 scale)
        {
            var template = FindPlayButtonTemplate();
            if (template != null)
            {
                CreateSocialButtonFromPlayTemplate(template, parent, name, url, label, color, localPos, scale);
                return;
            }

            var btnRenderer = UnityHelper.CreateObject<SpriteRenderer>(name + "Button", parent, Vector3.Zero);
            btnRenderer.transform.localScale = scale;
            btnRenderer.transform.localPosition = localPos;
            btnRenderer.color = color;

            var labelText = UnityHelper.CreateObject<TextMeshPro>(name + "Label", btnRenderer.transform, Vector3.Zero);
            labelText.text = label;
            labelText.fontSize = 1.6f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = GetSocialLabelColor(name);

            var btn = btnRenderer.gameObject.SetUpButton(true, btnRenderer);
            ApplySortingOrder(btnRenderer.gameObject, SocialPanelSortingOrder);
            btn.OnClick.AddListener((System.Action)(() => Application.OpenURL(url)));
        }

        //クレジット:Town Of Us Mira
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        class MainMenuManagerStartPatch
        {
            public static void Postfix(MainMenuManager __instance)
            {
                var menuBg = GameObject.Find("BackgroundTexture");

                if (menuBg != null)
                {
                    var render = menuBg.GetComponent<SpriteRenderer>();
                    render.flipY = true;
                    render.color = new Color(1f, 1f, 1f, 0.65f);
                }

                var tint = GameObject.Find("MainUI").transform.GetChild(0).gameObject;
                if (tint != null)
                {
                    tint.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.1f);
                    tint.transform.localScale = new Vector3(7.5f, 7.5f, 1f);
                }
            }
        }
        */

        /*
        //クレジット:Town Of Next
        [HarmonyPatch]
        public class MainMenuButtonHoverAnimation
        {
            [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Start_Postfix(MainMenuManager __instance)
            {
                var mainButtons = GameObject.Find("Main Buttons");
                mainButtons.ForEachChild((Il2CppSystem.Action<GameObject>)Init);
            }
            public static void RefreshButtons(GameObject obj)
            {
                AllButtons = new();
                obj.ForEachChild((Il2CppSystem.Action<GameObject>)Init);
            }
            public static void Init(GameObject obj)
            {
                if (obj.name is "BottomButtonBounds" or "Divider") return;
                if (obj.name.EndsWith("Button") && (obj.name.StartsWith("YouTube") || obj.name.StartsWith("Twitter") || obj.name.StartsWith("Wiki") || obj.name.StartsWith("") || obj.name.StartsWith("Minimize"))) return;
                if (AllButtons.ContainsKey(obj)) return;
                SetButtonStatus(obj, false);
                var pb = obj.GetComponent<PassiveButton>();
                if (pb == null) return;
                pb.OnMouseOver = new();
                pb.OnMouseOver.AddListener((System.Action)(() => SetButtonStatus(obj, true)));
                pb.OnMouseOut = new();
                pb.OnMouseOut.AddListener((System.Action)(() => SetButtonStatus(obj, false)));
            }

            private static Dictionary<GameObject, (Vector3, bool)> AllButtons = new();
            private static void SetButtonStatus(GameObject obj, bool active)
            {
                AllButtons.TryAdd(obj, (obj.transform.position, active));
                AllButtons[obj] = (AllButtons[obj].Item1, active);
            }

            [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
            private static void Update_Postfix(MainMenuManager __instance)
            {
                if (GameObject.Find("MainUI") == null) return;

                foreach (var kvp in AllButtons.Where(x => x.Key != null && x.Key.active))
                {
                    var button = kvp.Key;
                    var pos = button.transform.position;
                    var targetPos = kvp.Value.Item1 + new Vector3(kvp.Value.Item2 ? 0.35f : 0f, 0f, 0f);
                    if (kvp.Value.Item2 && pos.x > (kvp.Value.Item1.x + 0.2f)) continue;
                    button.transform.position = kvp.Value.Item2
                        ? UnityEngine.Vector3.Lerp(pos, targetPos, Time.deltaTime * 8f)
                        : UnityEngine.Vector3.MoveTowards(pos, targetPos, Time.deltaTime * 8f);
                }
            }
        }
        */

        [HarmonyPatch(typeof(EjectMainMenu), nameof(EjectMainMenu.EjectCrewmate))]
        class EjectMainMenuEjectCrewmatePatch
        {
            public static int i = 0;

            public static void Postfix(EjectMainMenu __instance)
            {
                try
                {
                    i++;
                    __instance.pressState.SetActive(false);
                    __instance.ejectButton.SetActive(true);
                    __instance.onCooldown = false;


                    if (10 < i && i < 15)
                    {
                        __instance.EjectCrewmate();
                    }

                    if (35 < i)
                    {
                        i = 0;
                    }

                    if (UnityEngine.Random.Range(0, 2) == 1)
                    {
                        __instance.EjectCrewmate();
                    }
                }

                catch { }
            }
        }

        [HarmonyPatch(typeof(EjectMainMenu), nameof(EjectMainMenu.PlacePlayer))]
        class EjectMainMenuEjectPlacePlayerPatch
        {
            public static MultiImage ConfigurationImage = NebulaAPI.AddonAsset.GetResource("HjkConfiguration.png")!.AsMultiImage(3, 6, 65f)!;

            public static void Postfix(EjectMainMenu __instance, PlayerParticle part)
            {
                if (part == null || part.myRend == null) return;

                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool hKey = Input.GetKey(KeyCode.H);

                if (!(shift && hKey)) return;

                int index = UnityEngine.Random.Range(0, 15);
                float scale = UnityEngine.Random.Range(0.5f, 1.5f);

                var sprite = ConfigurationImage.GetSprite(index);
                if (sprite == null) return;

                part.myRend.sprite = sprite;
                part.transform.localScale = UnityEngine.Vector3.one * scale;
            }
        }
    }
}