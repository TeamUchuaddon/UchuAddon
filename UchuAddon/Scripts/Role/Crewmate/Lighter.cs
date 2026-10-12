using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Core;
using Hori.Scripts.Role.Modifier;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
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
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Runtime;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class LighterU : DefinedSingleAbilityRoleTemplate<LighterU.Ability>, DefinedRole, HasCitation
{
    public LighterU() : base("lighterU", new(219, 203, 55), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [LightCoolDownOption, LightDurationOption, LightRatioOption, VisibileIsLighting])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Lighter.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration LightCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration LightDurationOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightDuration", (2.5f, 30f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration LightRatioOption = NebulaAPI.Configurations.Configuration("options.role.lighterU.lightRatio", (0.25f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    static private BoolConfiguration VisibileIsLighting = NebulaAPI.Configurations.Configuration("options.role.lighterU.visibileIsLighting", true);

    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.TheOtherRoles; } }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Lighter.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public LighterU MyRole = new();
    static private readonly GameStatsEntry StatsLight = NebulaAPI.CreateStatsEntry("stats.lighterU.light", GameStatsCategory.Roles, MyRole);
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;

    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable) => CanLoadDefaultTemplate(assignable) && assignable is not MoonU /*&& assignable is not MatchU*/;

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "lighterU.common1")
        {
            RelatedRole = LighterU.MyRole,
            Goal = 10,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.lighterU.common1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "lighterU.common1")
        {
            Goal = 10
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "lighterU.another1")
        {
            RelatedRole = LighterU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "lighterU.hard1")
        {
            RelatedRole = LighterU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "lighterU.challenge1")
        {
            RelatedRole = LighterU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image lightImage = NebulaAPI.AddonAsset.GetResource("LightButton.png")!.AsImage(115f)!;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        public bool IgnoreBlackout { get; private set; } = true;

        bool isLighting = false;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var lightButton = NebulaAPI.Modules.AbilityButton(this).BindKey(Virial.Compat.VirtualKeyInput.Ability).SetImage(lightImage).SetLabel("light").SetAsUsurpableButton(this);
                lightButton.Availability = (button) => MyPlayer.CanMove;
                lightButton.Visibility = (button) => !MyPlayer.IsDead;
                lightButton.OnClick = (button) => button.StartEffect();
                lightButton.OnEffectStart = (button) =>
                {
                    using (RPCRouter.CreateSection("LighterULight"))
                    {
                        MyPlayer.GainAttribute(PlayerAttributes.Eyesight, LightDurationOption, LightRatioOption, false, 100);
                    }

                    StatsLight.Progress();

                    isLighting = true;

                    TitleUnLock("uchuaddon.lighterU.common1");

                    if (LightRatioOption < 1)
                    {
                        TitleUnLock("uchuaddon.lighterU.another1");
                    }
                };
                lightButton.OnEffectEnd = (button) =>
                {
                    isLighting = false;
                    lightButton.StartCoolDown();
                };
                lightButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, LightCoolDownOption).SetAsAbilityTimer().Start();
                lightButton.EffectTimer = NebulaAPI.Modules.Timer(this, LightDurationOption);
            }
        }

        [OnlyMyPlayer]
        void UpdateVisibilyty(PlayerUpdateVisibilityEvent ev)
        {
            if (!VisibileIsLighting) return;
            if (!isLighting) return;

            if (ev.Player.IsInvisible && !ev.Player.IsDead)
            {
                ev.SetSemitransparent();
            }
            else if (!ev.Player.IsInvisible && (!ev.Player.IsDead || !ev.Player.IsDisconnected))
            {
                ev.SetVisible();
            }
            else if (ev.Player.IsDead || ev.Player.IsDisconnected)
            {
                ev.SetInvisible();
            }
        }

        [Local]
        void OnUpdateCamera(CameraUpdateEvent ev)
        {
            if (!isLighting) return;
            var lightplayer = GamePlayer.AllPlayers.Where(p => !p.IsDead && p != MyPlayer && Vector2.Distance(MyPlayer.TruePosition, p.TruePosition) <= LightRatioOption);
            int count = lightplayer.Count();

            if (count >= 5)
            {
                TitleUnLock("uchuaddon.lighterU.challenge1");
            }

            foreach (var p in lightplayer)
            {
                if (p.IsInvisible)
                {
                    TitleUnLock("uchuaddon.lighterU.hard1");
                }
            }
        }
        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}