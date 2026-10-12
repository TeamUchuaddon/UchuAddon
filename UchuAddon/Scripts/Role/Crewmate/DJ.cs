using Nebula.Modules;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules.ScriptComponents;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
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
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;
using Nebula.Roles.Abilities;

namespace Hori.Scripts.Role.Crewmate;

public class DJu : DefinedSingleAbilityRoleTemplate<DJu.Ability>, DefinedRole
{
    public DJu() : base("djU", new(173, 3, 252), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [DJSkillCooldown,NumOfFlash,DJSkillRange])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/DJ.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    static private readonly FloatConfiguration DJSkillCooldown = NebulaAPI.Configurations.Configuration("options.role.djU.djSkillCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration NumOfFlash = NebulaAPI.Configurations.Configuration("options.role.djU.numOfFlash", (1, 99), 10);
    static private readonly FloatConfiguration DJSkillRange = NebulaAPI.Configurations.Configuration("options.role.djU.djSkillRange", (1f, 10f, 1f), 7f, FloatConfigurationDecorator.Ratio);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static public DJu MyRole = new();
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/DJ.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    /*[NebulaRPCHolder]
    public class BadEmote : EquipableAbility, IGameOperator
    {
        static private readonly IDividedSpriteLoader badSprite = DividedSpriteLoader.FromResource( "Nebula.Resources.EmoteHand.png",100f,2,2);
        protected override float Size => 0.68f;
        protected override float Distance => 0.7f;
        bool animing;

        public BadEmote(GamePlayer owner) : base(owner, false, "DJEmote")
        {
            animing = false;
        }
        void Update(GameUpdateEvent ev)
        {
            Renderer.sprite = badSprite.AsLoader(animing ? 3 : 2).GetSprite();
        }
        System.Collections.IEnumerator CoSwitch()
        {
            animing = false;
            yield return Effects.Wait(0.2f);
            animing = true;
        }
    }*/

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image DJImage = NebulaAPI.AddonAsset.GetResource("DJButton.png")!.AsImage(115f)!;

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var djButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, DJSkillCooldown, "scratch", DJImage).SetAsUsurpableButton(this);
                djButton.Visibility = (button) => !MyPlayer.IsDead;
                djButton.OnClick = (b) =>
                {
                    NebulaManager.Instance.StartCoroutine(CoShowDJRange(MyPlayer.Position).WrapToIl2Cpp());
                    RpcDJFlash.Invoke((MyPlayer.Position, DJSkillRange));
                    djButton.StartCoolDown();
                };
            }
        }

        /*static RemoteProcess RpcDjSkill = new RemoteProcess("RpcDJSkillU", (_) =>
        {
            var player = GamePlayer.LocalPlayer;
            if (player == null) return;
            if (!playersInRange.Contains(player)) return;

            NebulaManager.Instance.StartCoroutine(DJFlash().WrapToIl2Cpp());
        });*/

        static RemoteProcess<(Vector2 center, float range)> RpcDJFlash = new("RpcDJSkillU", (message, _) =>
        {
            var localPlayer = GamePlayer.LocalPlayer;
            if (localPlayer == null) return;
            //if (localPlayer.IsDead) return;
            
            float distance = localPlayer.Position.Distance(message.center);
            if (distance > message.range) return;
            SoundManager.instance.PlaySound(APICompat.GetSound("DJScratch"), false, 1.3f);
            NebulaManager.Instance.StartCoroutine(DJFlash().WrapToIl2Cpp());
        });

        static IEnumerator DJFlash()
        {
            for (int i = 0; i < NumOfFlash; i++)
            {
                var color = new Color(UnityEngine.Random.value,UnityEngine.Random.value,UnityEngine.Random.value,0.6f);
                //AmongUsUtil.PlayQuickFlash(color);
                AmongUsUtil.PlayCustomFlash(color, 0.1f, 0.4f);
                yield return Effects.Wait(0.2f);
            }
        }

        IEnumerator CoShowDJRange(Vector2 position)
        {
            var circle = EffectCircle.SpawnEffectCircle(null, position.AsVector3(), Color.Magenta, DJSkillRange,  null,true);
            //var circle = EffectCircle.SpawnEffectCircle(null, new Vector3(position.x, position.y, 0f), color,DJSkillRange,  null,true);

            this.BindGameObject(circle.gameObject);

            yield return Effects.Wait(0.3f);

            circle.Disappear();
        }

        [OnlyMyPlayer]
        void Dance(PlayerDanceEvent ev)
        {
            //new StaticAchievementToken("djU.another1"); //ダンスクラブ
        }
    }
}