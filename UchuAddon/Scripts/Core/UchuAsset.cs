using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace Hori.Scripts.Core;

public class UchuAsset
{
    static private TMP_FontAsset? barlowFont = null;

    static public TMP_FontAsset BarlowFont
    {
        get
        {
            if (barlowFont == null)
                barlowFont = UnityHelper.FindAsset<TMP_FontAsset>("Barlow-SemiBold SDF")!;

            return barlowFont;
        }
    }
}