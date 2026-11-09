using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Events.Lobby;
using static Nebula.Modules.ConsoleRestriction;

namespace Hori.Scripts.Core;

/// <summary>
/// チャット送信時に呼び出されるイベントです。
/// テキストを書き換えたりテキストを追加したりできます。
/// </summary>
public class PlayerSendChatEvent : AbstractPlayerEvent
{
    public string Text;
    internal PlayerSendChatEvent(Virial.Game.Player player, string text) : base(player)
    {
        this.Text = text;
    }

    public void SetText(string text) => Text = text;

    public void AddText(string text) => Text += text;
}

public class LobbyUpdateEvent : Virial.Events.Event
{
    internal LobbyUpdateEvent() { }
}

/// <summary>
/// サボタージュボタンを使用できるかどうか更新するイベントです。
/// Garnet用の設計のため非常に使いにくいです。
/// </summary>
public class SabotageUpdateEvent : Virial.Events.Event
{
    /// <summary>
    /// flaseを返している場合サボタージュが使用できません。
    /// </summary>

    public bool IsUseSabotage = true;

    internal SabotageUpdateEvent() { }
}

public class PlayerFixedUpdateEvent : AbstractPlayerEvent
{
    public PlayerControl Control;

    internal PlayerFixedUpdateEvent(Virial.Game.Player player, PlayerControl control) : base(player)
    {
        this.Control = control;
    }
}

/// <summary>
/// 懐中電灯モードの可否をチェックする時に発火します。
/// </summary>
internal class PlayerFlashLightEvent : AbstractPlayerEvent
{
    /// <summary>
    /// 今懐中電灯モードかどうかを返します。
    /// </summary>
    public bool IsFlashLight { get; set; } = false;

    /// <summary>
    /// 懐中電灯のサイズです。
    /// </summary>
    public float LightSize { get; set; } = 1f;

    internal PlayerFlashLightEvent(Virial.Game.Player player) : base(player)
    {

    }
}