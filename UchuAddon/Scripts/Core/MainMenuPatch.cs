using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem;
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
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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
        static private readonly Image IconSprite = NebulaAPI.AddonAsset.GetResource("UchuMenuIcon.png")!.AsImage(115f)!;

        static void Postfix(MainMenuManager __instance)
        {
            var uchuMenuRenderer = UnityHelper.CreateObject<SpriteRenderer>("UchuMenuButton", __instance.transform, Vector3.zero);
            uchuMenuRenderer.gameObject.SetAsUIAspectContent(AspectPosition.EdgeAlignments.RightBottom, new Vector3(0.78f, 0.24f, -6f));
            uchuMenuRenderer.transform.localScale = new(0.9f, 0.9f, 1f);
            uchuMenuRenderer.sprite = IconSprite.GetSprite();
            var menuButton = uchuMenuRenderer.gameObject.SetUpButton(true, uchuMenuRenderer);
            menuButton.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(menuButton, Language.Translate("uchu.label.menu")));
            menuButton.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(menuButton));
            menuButton.OnClick.AddListener(() => AddonScreen.OpenUchuAddonScreen(HudManager.Instance.transform));
            menuButton.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;
        }
    }

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