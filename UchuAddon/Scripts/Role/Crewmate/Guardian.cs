using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
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
public class GuardianU : DefinedSingleAbilityRoleTemplate<GuardianU.Ability>, DefinedRole
{
    public GuardianU() : base("guardianU", new(222, 223, 255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [ShieldTask,ShieldLimit,NumOfShield/*,BlessingCooldown,NumOfBlessing*/])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Guardian.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static FloatConfiguration ShieldTask = NebulaAPI.Configurations.Configuration("options.role.guardianU.shieldTask", (0f, 100f, 10f), 100f, FloatConfigurationDecorator.Percentage);
    static BoolConfiguration ShieldLimit = NebulaAPI.Configurations.Configuration("options.role.guardianU.shieldLimit", false);
    static IntegerConfiguration NumOfShield = NebulaAPI.Configurations.Configuration("options.role.guardianU.numOfShield", (1, 15), 2, () => ShieldLimit);
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(GamePlayer player, int[] arguments)
    {
        return null;
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Guardian.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public GuardianU MyRole = new();

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "guardianU.common1")
        {
            RelatedRole = GuardianU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "guardianU.another1")
        {
            RelatedRole = GuardianU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "guardianU.another2")
        {
            RelatedRole = GuardianU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "guardianU.challenge1")
        {
            RelatedRole = GuardianU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        bool hasGuarded = false;
        int leftShield = NumOfShield;
        bool Guard = false;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {

            }
        }

        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            hasGuarded = true;
            if (Guard)
            {
                if (ShieldLimit)
                {
                    if (leftShield == 0) return;
                }
                if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse) return;
                if (ev.Killer.PlayerId == MyPlayer.PlayerId) return;
                ev.Result = KillResult.ObviousGuard;
               
                leftShield--;

                //キルを防ぐ称号
                TitleUnLock("uchuaddon.guardianU.challenge1");
            }
            else
            {
                //キル防げなかった称号
                TitleUnLock("uchuaddon.guardianU.another1");
            }
        }

        [Local,OnlyMyPlayer]
        void OnExiled(PlayerExiledEvent ev)
        {
            //追放称号
            TitleUnLock("uchuaddon.guardianU.another2");
        }

        [Local,OnlyMyPlayer]
        void OnGameEnd(GameEndEvent ev)
        {
            if (!MyPlayer.IsDead && ev.EndState.Winners.Test(MyPlayer) && !hasGuarded)
            {
                //一度もキルされずに勝利
                TitleUnLock("uchuaddon.guardianU.common1");
            }
        }

        [Local, OnlyMyPlayer]
        void DecorateOtherPlayerName(PlayerDecorateNameEvent ev)
        {
            if (!Guard) return;

            if (ShieldLimit)
            {
                ev.Name += $" ({leftShield})".Color("#ffff00");
            }
        }

        void GuardTask(GameUpdateEvent ev)
        {
            float taskRatio = ShieldTask / 100f;

            if (MyPlayer.Tasks.CurrentCompleted / (float)MyPlayer.Tasks.CurrentTasks >= taskRatio)
            {
                Guard = true;
            }
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}