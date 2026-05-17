using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Complex;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Abilities;

internal class UchuPlayerIconInfo
{
    public GamePlayer Player;
    internal PoolablePlayer Icon;

    public UchuPlayerIconInfo(GamePlayer player, Transform parent)
    {
        Player = player;
        Icon = AmongUsUtil.GetPlayerIcon(player.Unbox().DefaultOutfit.Outfit.outfit, parent, Vector3.zero, Vector3.one * 0.31f);
        Icon.ToggleName(false);
    }

    public void SetText(string? text, float size = 4f)
    {
        if (text == null)
            Icon.ToggleName(false);
        else
        {
            Icon.ToggleName(true);
            Icon.SetName("", Vector3.one * size, Color.white, -1f);
            Icon.cosmetics.nameText.text = text;
        }
    }

    public void SetAlpha(bool semitransparent)
    {
        Icon.SetAlpha(semitransparent ? 0.35f : 1f);
    }
}

internal class UchuPlayersIconHolder : FlexibleLifespan, IGameOperator
{
    HudContent myContent;
    GameObject adjuster;
    List<UchuPlayerIconInfo> icons = new();
    public float XInterval = 0.29f;
    public UchuPlayersIconHolder(bool isStaticContent = true)
    {
        myContent = HudContent.InstantiateContent("PlayerIconsUchu", true, true, false, isStaticContent);
        adjuster = UnityHelper.CreateObject("AdjusterUchu", myContent.transform, Vector3.zero);
    }

    private void UpdateIcons()
    {
        for (int i = 0; i < icons.Count; i++) icons[i].Icon.transform.localPosition = new(i * XInterval - 0.3f, -0.1f, -i * 0.01f);
    }

    public void Remove(UchuPlayerIconInfo icon)
    {
        if (icons.Remove(icon)) GameObject.Destroy(icon.Icon.gameObject);
        UpdateIcons();
    }

    public UchuPlayerIconInfo AddPlayer(GamePlayer player)
    {
        UchuPlayerIconInfo info = new(player, adjuster.transform);
        icons.Add(info);
        UpdateIcons();
        return info;
    }

    public IEnumerable<UchuPlayerIconInfo> AllIcons => icons;

    void IGameOperator.OnReleased()
    {
        if (myContent) GameObject.Destroy(myContent.gameObject);
    }

    void OnUpdate(GameUpdateEvent ev)
    {
        if (myContent.IsStaticContent && MeetingHud.Instance)
        {
            adjuster.transform.localScale = new(0.65f, 0.65f, 1f);
            adjuster.transform.localPosition = new(-0.45f, -0.37f, 0f);
        }
        else
        {
            adjuster.transform.localScale = Vector3.one;
            adjuster.transform.localPosition = Vector3.zero;
        }
    }
}

internal class UchuPlayersIconHolderSummoner : FlexibleLifespan, IGameOperator
{
    HudContent myContent;
    GameObject adjuster;
    List<UchuPlayerIconInfo> icons = new();
    public float XInterval = 0.29f;
    public Action<GamePlayer>? OnPlayerClicked;

    public UchuPlayersIconHolderSummoner(bool isStaticContent = true)
    {
        myContent = HudContent.InstantiateContent("PlayerIconsUchu", true, true, false, isStaticContent);
        adjuster = UnityHelper.CreateObject("AdjusterUchu", myContent.transform, Vector3.zero);
    }

    private void UpdateIcons()
    {
        for (int i = 0; i < icons.Count; i++) icons[i].Icon.transform.localPosition = new(i * XInterval - 0.3f, -0.1f, -i * 0.01f);
    }

    public void Remove(UchuPlayerIconInfo icon)
    {
        if (icons.Remove(icon)) GameObject.Destroy(icon.Icon.gameObject);
        UpdateIcons();
    }

    public UchuPlayerIconInfo AddPlayer(GamePlayer player)
    {
        UchuPlayerIconInfo info = new(player, adjuster.transform);

        var obj = info.Icon.gameObject;

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.2f, 1.2f);

        var clickable = obj.AddComponent<PlayerIconClickableSummoner>();
        clickable.Info = info;
        clickable.Holder = this;

        icons.Add(info);
        UpdateIcons();
        return info;
    }

    public IEnumerable<UchuPlayerIconInfo> AllIcons => icons;

    void IGameOperator.OnReleased()
    {
        if (myContent) GameObject.Destroy(myContent.gameObject);
    }

    void OnUpdate(GameUpdateEvent ev)
    {
        if (myContent.IsStaticContent && MeetingHud.Instance)
        {
            adjuster.transform.localScale = new(0.65f, 0.65f, 1f);
            adjuster.transform.localPosition = new(-0.45f, -0.37f, 0f);
        }
        else
        {
            adjuster.transform.localScale = Vector3.one;
            adjuster.transform.localPosition = Vector3.zero;
        }
    }

    internal class PlayerIconClickableSummoner : MonoBehaviour
    {
        public UchuPlayerIconInfo Info = null!;
        public UchuPlayersIconHolderSummoner Holder = null!;

        void OnMouseDown()
        {
            if (Info == null || Holder == null) return;

            Holder.OnPlayerClicked?.Invoke(Info.Player);
        }
    }
}
