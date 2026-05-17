using Hori.Scripts.Role.Modifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Abilities;

public class NearPlayerArrowAbility : FlexibleLifespan, IGameOperator
{
    PlayerControl? targetPlayer = null;
    private Arrow? playerArrow = null;
    public bool ShowArrow { get; set; } = true;

    void UpdateTarget(GameUpdateEvent ev)
    {
        if (!PlayerControl.LocalPlayer) return;
        if (ShipStatus.Instance == null) return;

        Vector3 myPos = PlayerControl.LocalPlayer.transform.position;

        PlayerControl? closest = null;
        float minDistance = float.MaxValue;

        foreach (var all in PlayerControl.AllPlayerControls.ToArray())
        {
            if (all.AmOwner) continue;
            if (all.Data.IsDead) continue;

            float d = Vector3.Distance(myPos, all.transform.position);
            if (closest == null || d < minDistance)
            {
                minDistance = d;
                closest = all;
            }
        }

        targetPlayer = closest;

        if (targetPlayer != null)
        {
            Vector2 pos = targetPlayer.transform.position;

            if (playerArrow == null)
            {
                playerArrow = new Arrow(null)
                {
                    TargetPos = pos
                }.SetColor(RadarU.MyRole.UnityColor).Register(this);
            }
            else
            {
                playerArrow.TargetPos = pos;
                playerArrow.IsActive = ShowArrow;
            }
        }
        else
        {
            if (playerArrow != null)
            {
                playerArrow.Release();
                playerArrow = null;
            }
        }
    }
    [OnlyMyPlayer]
    void OnDead(PlayerDieEvent ev)
    {
        if (playerArrow != null)
        {
            playerArrow.Release();
            playerArrow = null;
        }
        targetPlayer = null;
    }
}