using Hori.Core;
using Hori.Scripts.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;
namespace Hori.Scripts.Patch;

[HarmonyPatch(typeof(ShapeshifterMinigame), nameof(ShapeshifterMinigame.Begin))]
public static class ShapeshifterBeginPatch
{
    private static void Postfix(ShapeshifterMinigame __instance)
    {
        int count = __instance.potentialVictims.Count; // パネルの人数をチェック

        if (count <= 15) return; // 15人以下はそのまま

        int columns;
        float scale;
        float xStart;

        if (count <= 18) // 16～18人 : 3列 × 6行
        {
            columns = 3;
            scale = 0.95f;

            xStart = -2.87f;
        }
        else // 19～24人 : 4列 × 6行
        {
            columns = 4;
            scale = 0.76f;

            // 中央を揃えるために、4列の場合はxStartを-3.306fに設定
            xStart = -3.306f;
        }

        for (int i = 0; i < __instance.potentialVictims.Count; i++)
        {
            var panel = __instance.potentialVictims[i];

            int x = i % columns;
            int y = i / columns;

            panel.transform.localPosition = new Vector3(xStart + __instance.XOffset * scale * x, __instance.YStart + __instance.YOffset * scale * y, panel.transform.localPosition.z);

            panel.transform.localScale = Vector3.one * scale;
        }
    }
}