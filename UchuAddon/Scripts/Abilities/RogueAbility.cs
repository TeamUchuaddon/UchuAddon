using Hori.Scripts.Role.Crewmate;
using Hori.Scripts.Role.Modifier;
using Hori.Scripts.Role.Neutral;
using Hori.Scripts.Role.Sample;
using Nebula.Map;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
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

// 一部のinstance型や奪ったときに色々おかしい能力はこっちで作成する。

public class MadmateAbility : FlexibleLifespan, IGameOperator, IBindPlayer //instance型だからこっちで作る。黒猫能力は未定
{
    static internal Image SuicideButtonImage = SpriteLoader.FromResource("Nebula.Resources.Buttons.MadmateSuicideButton.png", 115f);

    private GamePlayer myPlayer;
    GamePlayer IBindPlayer.MyPlayer => myPlayer;

    public MadmateAbility(GamePlayer player)
    {
        this.myPlayer = player;

        if (player.AmOwner)
        {
            var suicideButton = NebulaAPI.Modules.AbilityButton(this, player, Virial.Compat.VirtualKeyInput.Ability, Madmate.SuicideCoolDownOption.Cooldown, "madmate.suicide", SuicideButtonImage);
            suicideButton.OnClick = (button) =>
            {
                player.Suicide(PlayerState.Suicide, PlayerState.Suicide, KillParameter.RemoteKill);
            };
            suicideButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
        }
    }
}

public class VultureAbility : FlexibleLifespan, IGameOperator, IBindPlayer //instance型だから、死体消しだけ付与。矢印は未定
{
    static private Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.EatButton.png", 115f);

    private GamePlayer myPlayer;
    GamePlayer IBindPlayer.MyPlayer => myPlayer;

    public VultureAbility(GamePlayer player)
    {
        this.myPlayer = player;

        if (player.AmOwner)
        {
            var eatTracker = ObjectTrackers.ForDeadBody(this, null, player, (d) => true);
            var eatButton = NebulaAPI.Modules.AbilityButton(this, player, Virial.Compat.VirtualKeyInput.Ability,
                Vulture.EatCoolDownOption.Cooldown, "eat", buttonSprite, _ => eatTracker.CurrentTarget != null);
            eatButton.OnClick = (button) =>
            {
                NebulaGameManager.Instance?.RpcDoGameAction(player, player.Position, GameActionTypes.EatCorpseAction);

                AmongUsUtil.RpcCleanDeadBody(eatTracker.CurrentTarget!, player.PlayerId, EventDetail.Eat);

                eatButton.StartCoolDown();
            };
        }
    }
}