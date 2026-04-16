using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using AmongUs.Matchmaking;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Abilities;

// 停電無効前提
public class GloomVisionAbility : FlexibleLifespan, IGameOperator
{
    bool ElectricSabo;
    float normalVision;
    float blackoutVision;

    float releaseVision = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.CrewLightMod) + 3f;

    float currentVision;
    float targetVision;

    void Update(GameUpdateEvent ev)
    {
        if (GamePlayer.LocalPlayer == null) return;
        if (!GamePlayer.LocalPlayer.AmOwner) return;
        if (GamePlayer.LocalPlayer.IsDead) return;

        ElectricSabo = PlayerTask.PlayerHasTaskOfType<ElectricTask>(PlayerControl.LocalPlayer);
        normalVision = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.CrewLightMod) + 3f;
        blackoutVision = normalVision * 0.5f;

        targetVision = ElectricSabo ? normalVision : blackoutVision;

        currentVision = Mathf.MoveTowards(currentVision, targetVision, Time.deltaTime * 2f);

        ShipStatus.Instance.MaxLightRadius = currentVision;
    }

    void IGameOperator.OnReleased()
    {
        if (GamePlayer.LocalPlayer == null) return;
        if (!GamePlayer.LocalPlayer.AmOwner) return;

        if (ShipStatus.Instance)
            ShipStatus.Instance.MaxLightRadius = releaseVision;
    }
}