using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

namespace Hori.Scripts.Abilities;

/// <summary>
/// プレイヤーの名前の可視性に関するモジュール。
/// </summary>
internal class NameVisibleModule
{
    /// <summary>
    /// プレイヤーの名前を表示します。<br>基本的にリセット用の処理です。</br>
    /// </summary>
    public static readonly RemoteProcess<GamePlayer> RpcShowName = new("RpcShowPlayerName_UchuModule", (player, _) =>
    {
        var name = player.VanillaPlayer.transform.Find("Names");
        name.Find("NameText_TMP").gameObject.SetActive(true);
        name.Find("ColorblindName_TMP").gameObject.SetActive(true);
    });

    /// <summary>
    /// プレイヤーの名前を非表示にします。
    /// </summary>
    public static readonly RemoteProcess<GamePlayer> RpcHideName = new("RpcHidePlayerName_UchuModule", (player, _) =>
    {
        var name = player.VanillaPlayer.transform.Find("Names");
        name.Find("NameText_TMP").gameObject.SetActive(false);
        name.Find("ColorblindName_TMP").gameObject.SetActive(false);
    });
}