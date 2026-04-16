using Nebula.Behavior;
using Nebula.Modules.ScriptComponents;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Player;
using Hori.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Nebula;
using Nebula.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Hori.Scripts.Abilities;

// var ability = new EmergencyButtonAbility().Register(this); をAmOwnerの中に置けば使える
public class EmergencyButtonAbility : FlexibleLifespan, IGameOperator
{
    static private readonly Virial.Media.Image EmergencyImage = NebulaAPI.AddonAsset.GetResource("EmergencyButton.png")!.AsImage(115f)!;

    public EmergencyButtonAbility()
    {
        GamePlayer? my = GamePlayer.LocalPlayer;
        var emergencyButton = NebulaAPI.Modules.AbilityButton(this, my!, false, true, Virial.Compat.VirtualKeyInput.None, null, 15f, "emergency.ability", EmergencyImage, null);
        emergencyButton.OnClick = (button) =>
        {
            if (!my!.CanMove) return;
            var prefab = ShipStatus.Instance.EmergencyButton.MinigamePrefab;

            PlayerControl.LocalPlayer.NetTransform.Halt();

            Minigame minigame = UnityEngine.Object.Instantiate(prefab);
            minigame.transform.SetParent(Camera.main.transform, false);
            minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
            minigame.Begin(null);

            ConsoleTimer.MarkAsNonConsoleMinigame();
        };
    }
}