using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Core;

//別にPatchではない
public static class VentPatch
{
    public static Vent? SearchVentForId(int id)
    {
        return ShipStatus.Instance.AllVents.FirstOrDefault((x) => x != null && x.Id == id);
    }

    /// <summary>
    /// ベントをパカパカします。
    /// </summary>
    public static void PlayVentAnimation(this Vent? vent)
    {
        if (vent == null || vent.myRend == null) return;

        vent.myAnim.Play(vent.ExitVentAnim, 1f);
    }
}