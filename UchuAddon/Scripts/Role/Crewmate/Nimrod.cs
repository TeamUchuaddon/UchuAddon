using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cpp2IL.Core.Extensions;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using Virial;
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using Virial.Text;
using static Il2CppSystem.DateTimeParse;
using static Nebula.Roles.Impostor.Cannon;
using static Rewired.UnknownControllerHat;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class NimrodU : DefinedSingleAbilityRoleTemplate<NimrodU.Ability>, DefinedRole,HasCitation
{
    public NimrodU() : base("nimrodU", new(199, 162, 70), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfInvolve,MaxLeftTimer, MultiInvolve, NumOfInvolvePerMeeting])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Nimrod.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfHostY;

    static private readonly IntegerConfiguration NumOfInvolve = NebulaAPI.Configurations.Configuration("options.role.nimrodU.numOfInvolve", (1, 15), 6);
    static private readonly FloatConfiguration MaxLeftTimer = NebulaAPI.Configurations.Configuration("options.role.nimrodU.maxLeftTimer", (0f, 60f, 5f), 20f, FloatConfigurationDecorator.Second);
    static private readonly BoolConfiguration MultiInvolve = NebulaAPI.Configurations.Configuration("options.role.nimrodU.multiInvolve", true);
    static private readonly IntegerConfiguration NumOfInvolvePerMeeting = NebulaAPI.Configurations.Configuration("options.role.nimrodU.numOfInvolvePerMeting", (1, 15), 3, ()=> MultiInvolve);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public NimrodU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Nimrod.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private readonly GameStatsEntry StatsInvolve = NebulaAPI.CreateStatsEntry("stats.nimrodU.involve", GameStatsCategory.Roles, MyRole);

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "nimrodU.common1")
        {
            RelatedRole = NimrodU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "nimrodU.another1")
        {
            RelatedRole = NimrodU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "nimrodU.another2")
        {
            RelatedRole = NimrodU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Silver,
            IsSecret = true
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "nimrodU.hard1")
        {
            RelatedRole = NimrodU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "nimrodU.challenge1")
        {
            RelatedRole = NimrodU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private readonly Virial.Media.Image InvolveImage = NebulaAPI.AddonAsset.GetResource("NimrodButton.png")!.AsImage(115f)!;

        int left = NumOfInvolve;
        int leftPerMeeting = NumOfInvolvePerMeeting;
        bool InvolveCheck = false;
        bool ExiledCheck = false;
        List<GamePlayer> InvolvePlayer = new List<GamePlayer>();

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                Nebula.Utilities.Helpers.TextHudContent("NimrodUText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.nimrodU.hudText") + ": " + left, true);
            }
        }

        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            leftPerMeeting = NumOfInvolvePerMeeting;

            var involveButton = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            involveButton?.RegisterMeetingAction(new(InvolveImage,p =>
            {
                var target = p.MyPlayer;
                if (target != null)
                {
                    InvolvePlayer.Add(target);
                    left--;
                    leftPerMeeting--;
                    if (!MultiInvolve)
                    {
                        InvolveCheck = true;
                    }
                }
            }, p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner&& left > 0 && leftPerMeeting > 0 && !InvolveCheck && MeetingHudExtension.LeftTime > MaxLeftTimer && !PlayerControl.LocalPlayer.Data.IsDead && !InvolvePlayer.Contains(p.MyPlayer)));
        }

        private bool EmbroilLastImpostor = false;
        private float EmbroilLastImpostorTime;

        [OnlyMyPlayer,Local]
        void OnPlayerExiled(PlayerExiledEvent ev)
        {
            if (InvolvePlayer.Count == 0)
            {
                TitleUnLock("uchuaddon.nimrodU.another2");
                return;
            }

            if (InvolvePlayer.Count >= 3 && InvolvePlayer.All(target => !target.IsTrueCrewmate))
            {
                TitleUnLock("uchuaddon.nimrodU.challenge1");
            }

            foreach (var target in InvolvePlayer)
            {
                if (target.IsTrueCrewmate)
                {
                    TitleUnLock("uchuaddon.nimrodU.another1");
                }
                else
                {
                    TitleUnLock("uchuaddon.nimrodU.common1");
                }
                if (target.IsDead) continue;
                StatsInvolve.Progress();
                target.VanillaPlayer.ModMarkAsExtraVictim(ev.Player.VanillaPlayer,PlayerState.Embroiled,EventDetail.Embroil);
            }

            if (InvolvePlayer.Any(p => p.IsImpostor) &&
            !GamePlayer.AllPlayers.Any(p => !p.IsDead && p.IsImpostor && !InvolvePlayer.Contains(p)))
            {
                EmbroilLastImpostor = true;
                EmbroilLastImpostorTime = Time.time;
            }
        }

        [OnlyMyPlayer, Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (EmbroilLastImpostor && ev.EndState.Winners.Test(MyPlayer))
            {
                TitleUnLock("uchuaddon.nimrodU.hard1");
            }
        }

        [Local]
        void OnGameUpdateHard(GameUpdateEvent ev)
        {
            if (EmbroilLastImpostor)
            {
                if (Time.time - EmbroilLastImpostorTime >= 15f) //15以内に終われば大体おっけー
                {
                    EmbroilLastImpostor = false;
                }
            }
        }

        [Local]
        void MeetingLeset(MeetingPreStartEvent ev)
        {
            InvolvePlayer.Clear();
            InvolveCheck = false;
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}