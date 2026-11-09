using Hori.Core;
using Hori.Scripts.Core;
using Il2CppInterop.Common.Host;
using Il2CppInterop.Runtime.Injection;
using Nebula.Behavior;
using Nebula.Game.Statistics;
using Nebula.Modules;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Il2CppSystem.Globalization.CultureInfo;
using static Nebula.Roles.Impostor.Hadar;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Abilities;

public class GhostVisibleAbility : FlexibleLifespan, IGameOperator
{
    void UpdateVent(PlayerFixedUpdateEvent ev)
    {
        if (ev.Player.IsDead)
        {
            ev.Control.Visible = true;
            ev.Control.cosmetics.Visible = true;
            ev.Control.cosmetics.currentBodySprite.Visible = true;
        }
    }
}