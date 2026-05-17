using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Nebula.Utilities;
using System;
using UnityEngine;
using Virial.Media;
using static Hori.Scripts.Abilities.BottomMenu;
namespace Hori.Scripts.Abilities;

internal class BottomMenu
{
    public record BottomMenuElement
        (
        GUIWidgetSupplier Widget,
        Action OnClick,
        GUIWidgetSupplier? Overlay = null
    );

    GameObject obj = null!;
    Func<bool> showWhile = null!;
    BottomMenuElement[] elements = null!;
    (GameObject obj, Vector2 origScale)[] generated = null!;
    int currentSelection = -1;
    PassiveButton button = null!;

    static private Image backgroundSprite =
        SpriteLoader.FromResource("Nebula.Resources.RingMenu.png", 100f);

    public void Show(BottomMenuElement[] elements, Func<bool> showWhile, Vector2? menuPosition = null)
    {
        if (obj) UnityEngine.Object.Destroy(obj);

        obj = UnityHelper.CreateObject(
            "BottomMenu",
            HudManager.Instance.transform,
            menuPosition ?? Vector2.zero
        );

        obj.transform.SetLocalZ(-780f);

        this.elements = elements;
        this.showWhile = showWhile;

        generated = new (GameObject, Vector2)[elements.Length];

        // ===== レイアウト設定 =====
        float spacingX = 1.2f;
        float spacingY = 1.2f;
        int maxPerRow = 10;

        int columns = Math.Min(elements.Length, maxPerRow);
        float startX = -((columns - 1) * spacingX) / 2f;

        int totalRows = (elements.Length + maxPerRow - 1) / maxPerRow;
        float startY = -2.5f + ((totalRows - 1) * spacingY) / 2f;

        // ===== 要素生成 =====
        for (int i = 0; i < elements.Length; i++)
        {
            int row = i / maxPerRow;
            int col = i % maxPerRow;

            float posX = startX + col * spacingX;

            // 最終行だけ中央揃え
            if (row == totalRows - 1)
            {
                int lastRowCount = elements.Length % maxPerRow;
                if (lastRowCount == 0) lastRowCount = maxPerRow;

                float lastStartX = -((lastRowCount - 1) * spacingX) / 2f;
                posX = lastStartX + col * spacingX;
            }

            var widget = elements[i].Widget.Invoke().Instantiate(new(2f, 2f), out _);
            if (widget != null)
            {
                widget.transform.SetParent(obj.transform);
                widget.transform.localPosition = new Vector3(
                    posX,
                    startY - row * spacingY,
                    0f
                );

                generated[i] = (widget, widget.transform.localScale);

                widget.transform.localScale *= 0.9f;
            }
        }

        // ===== 背景 =====
        var bg = UnityHelper.CreateObject<SpriteRenderer>(
            "BG",
            obj.transform,
            new Vector3(0f, 0f, 0.1f)
        );

        bg.sprite = backgroundSprite.GetSprite();
        bg.color = new Color(0f, 1f, 0f, 0.15f);
        bg.transform.localScale = new Vector3(6f, 2.5f, 1f);

        // ===== ボタン =====
        button = obj.SetUpButton(false);
        button.OnClick.AddListener(() =>
        {
            if (currentSelection != -1)
            {
                VanillaAsset.PlaySelectSE();
                elements[currentSelection].OnClick.Invoke();

                UnityEngine.Object.Destroy(obj);
                obj = null!;
                showWhile = null!;
            }
        });

        currentSelection = -1;
    }

    public void Update()
    {
        if (!obj) return;

        // ===== 表示終了 =====
        if (!(showWhile?.Invoke() ?? true))
        {
            if (currentSelection != -1)
            {
                VanillaAsset.PlaySelectSE();
                elements[currentSelection].OnClick.Invoke();
            }

            UnityEngine.Object.Destroy(obj);
            obj = null!;
            showWhile = null!;
            return;
        }

        var pos = UnityHelper.ScreenToLocalPoint(
            Input.mousePosition,
            LayerExpansion.GetUILayer(),
            obj.transform
        );
        pos.z = 0f;

        int nextSelection = -1;

        // ===== ホバー判定 =====
        for (int i = 0; i < generated.Length; i++)
        {
            var elementPos = generated[i].obj.transform.localPosition;
            float dist = Vector2.Distance(pos, elementPos);

            if (dist < 0.6f)
            {
                nextSelection = i;
                break;
            }
        }

        // ===== 選択変更 =====
        if (nextSelection != currentSelection)
        {
            if (nextSelection != -1)
            {
                var overlay = elements[nextSelection].Overlay;
                if (overlay != null)
                    NebulaManager.Instance.SetHelpWidget(button, overlay.Invoke());
                else
                    NebulaManager.Instance.HideHelpWidgetIf(button);

                VanillaAsset.PlayHoverSE();
            }
            else
            {
                NebulaManager.Instance.HideHelpWidgetIf(button);
            }

            currentSelection = nextSelection;
        }

        // ===== スケール・前後 =====
        for (int i = 0; i < generated.Length; i++)
        {
            bool selected = (currentSelection == i);

            generated[i].obj.transform.localScale =
                (generated[i].origScale * (selected ? 1.4f : 0.9f)).AsVector3(1f);

            generated[i].obj.transform.SetLocalZ(selected ? -0.5f : -0.05f);
        }
    }
}

[NebulaPreprocess(PreprocessPhase.CompileAddons)]
[NebulaRPCHolder]
public class BottomMenuManager : MonoBehaviour
{
    private BottomMenu menu = new();

    internal void ShowBottomMenu(BottomMenuElement[] elements, Func<bool> showWhile, Action? ifEmpty, Vector2? menuPosition = null)
    {
        if (elements.Length == 0)
            ifEmpty?.Invoke();
        else
            menu.Show(elements, showWhile, menuPosition);
    }

    public void Update()
    {
        menu.Update();
    }
}