using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Ghost.Neutral;
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

namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class ProsecutorU : DefinedSingleAbilityRoleTemplate<ProsecutorU.Ability>, DefinedRole, HasCitation,IAssignableDocument
{
    public ProsecutorU() : base("prosecutorU", new(52, 119, 235), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfProsecute,ProsecuteCrewmateCheck, CanCallEmergencyMeeting])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Prosecutor.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Hori.Core.Citations.TownOfUs;

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static private IntegerConfiguration NumOfProsecute = NebulaAPI.Configurations.Configuration("options.role.prosecutorU.numOfProsecute", (1, 15), 2);
    public static BoolConfiguration ProsecuteCrewmateCheck = NebulaAPI.Configurations.Configuration("options.role.prosecutorU.prosecuteCrewmateCheck", true);
    public static BoolConfiguration CanCallEmergencyMeeting = NebulaAPI.Configurations.Configuration("options.role.prosecutorU.canCallEmergencyMeeting", true);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static public ProsecutorU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Prosecutor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static private readonly Virial.Media.Image MeetingButton = NebulaAPI.AddonAsset.GetResource("ProsecutorIcon.png")!.AsImage(115f)!;
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(MeetingButton, "role.prosecutorU.ability.prosecute");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%EXILED%", Language.Translate(ProsecuteCrewmateCheck ? "role.prosecutorU.ability.main.exiled" : "role.prosecutorU.ability.main.notExiled"));
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int leftProsecute = NumOfProsecute;
        bool IsProsecute = false;
        
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                Nebula.Utilities.Helpers.TextHudContent("ProsecutorUText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.prosecutorU.hudText") + ": " + leftProsecute, true);
            }
        }

        bool IPlayerAbility.BlockCallingEmergencyMeeting => !CanCallEmergencyMeeting;

        //private GamePlayer? target = null;

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)　//Miraは神
        {
            IsProsecute = false;
            //target = null;

            var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            buttonManager?.RegisterMeetingAction(new(MeetingButton,p =>
            {
                leftProsecute--;
                IsProsecute = true;

                //target = p.MyPlayer;

                var meeting = MeetingHud.Instance;
                byte targetId = p.MyPlayer.PlayerId;

                if (meeting == null) return;

                meeting.ResetPlayerState();

                meeting.ModCastVote(MyPlayer.PlayerId, targetId, 100);
                
                foreach (var player in GamePlayer.AllPlayers)
                {
                    if (player.IsDead) continue; // 必要？
                    if (player == MyPlayer) continue;

                    meeting.ModCastVote(player.PlayerId, targetId, 0);
                }

                RpcHandMaterial.Invoke(targetId);

            },p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && leftProsecute > 0 && !IsProsecute && !PlayerControl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess));
        }

        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (IsProsecute) // 強制追放したかどうかの有無を確認
            {
                if (ev.Player.IsTrueCrewmate && ProsecuteCrewmateCheck) // Swapperなどで追放先が変わる可能性もあるため追放者をチェックする。
                {
                    MyPlayer.VanillaPlayer.ModMarkAsExtraVictim(null, PlayerState.Suicide, PlayerState.Suicide);
                }
            }
        }

        static RemoteProcess<byte> RpcHandMaterial = new("RpcHandMaterial _Uchu", (targetId, _) =>
        {
            var targetVoteArea = MeetingHud.Instance.playerStates.FirstOrDefault(v => v.PlayerId == targetId);
            if (targetVoteArea == null) return;

            var judgeUsed = targetVoteArea.transform.Find("JudgeUsed");

            if (judgeUsed != null)
            {
                judgeUsed.gameObject.SetActive(true);
            }
        });
    }
}