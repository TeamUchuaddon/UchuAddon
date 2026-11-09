using Nebula.Modules.ScriptComponents;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Abilities;

public class VentArrowAbility : FlexibleLifespan, IGameOperator
{
    float min = 0f;
    Vent? ventLocal = null;
    private Arrow? ventArrow = null;
    public bool ShowArrow { get; set; } = true;

    void UpdateVent(GameUpdateEvent ev)
    {
        Vent? closestVent = null;
        float minDistance = float.MaxValue;
        Vector3 myPos = PlayerControl.LocalPlayer.transform.position;

        foreach (var v in ShipStatus.Instance.AllVents)
        {
            float d = UnityEngine.Vector3.Distance(myPos, v.transform.position);
            if (closestVent == null || d < minDistance)
            {
                minDistance = d;
                closestVent = v;
            }
        }
        ventLocal = closestVent;

        if (ventLocal != null)
        {
            if (ventArrow == null)
            {
                ventArrow = new Arrow(null)
                {
                    TargetPos = new Vector2(ventLocal.transform.position.x, ventLocal.transform.position.y)
                }.SetColor(Color.Yellow)
                 .Register(this);
            }
            else
            {
                ventArrow.TargetPos = new Vector2(ventLocal.transform.position.x, ventLocal.transform.position.y);
                ventArrow.IsActive = ShowArrow;
            }
        }
    }
    [OnlyMyPlayer]
    void OnDead(PlayerDieEvent ev)
    {
        if (ventArrow != null)
        {
            ventArrow.Release();
            ventArrow = null;
        }
        ventLocal = null;
    }
}