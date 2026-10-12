using Hori.Core;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using Hori.Scripts.Role.Neutral;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Virial.Assignable;
using Virial.Helpers;
using static Il2CppSystem.DateTimeParse;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

public class EraserU : DefinedSingleAbilityRoleTemplate<EraserU.Ability>, HasCitation, DefinedRole,IAssignableDocument
{
    public EraserU() : base("EraserU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [EraseCooldown, NumOfErase])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Eraser.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        ConfigurationHolder?.ScheduleAddRelated(() => [SeedU.MyRole.ConfigurationHolder!]);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;

    Citation? HasCitation.Citation => Nebula.Roles.Citations.TheOtherRoles;

    static private FloatConfiguration EraseCooldown = NebulaAPI.Configurations.Configuration("options.role.EraserU.EraseCooldown", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration NumOfErase = NebulaAPI.Configurations.Configuration("options.role.EraserU.numofErase", (1, 15), 3);
    static public EraserU MyRole = new EraserU();
    public override EraserU.Ability CreateAbility(GamePlayer player, int[] arguments)
    {
        bool isUsurped = arguments.Length > 0 && arguments[0] != 0;
        return new EraserU.Ability(player, isUsurped);
    }
    static private Virial.Media.Image EraseImage = NebulaAPI.AddonAsset.GetResource("EraserEraseButton.png")!.AsImage(115f)!;
    private Nebula.Modules.ScriptComponents.ModAbilityButtonImpl? EraseButton = null;
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Eraser.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static private readonly GameStatsEntry StatsSample = NebulaAPI.CreateStatsEntry("stats.EraserU.EraseSkill", GameStatsCategory.Roles, MyRole);

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(EraseImage, "role.EraserU.ability.erase");
    }
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        private List<GamePlayer>? EraseTargets = new List<GamePlayer>();

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                int leftErase = NumOfErase;
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);

                var EraseButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    EraseCooldown, "Erase", EraseImage, _ => playerTracker.CurrentTarget != null && playerTracker.CurrentTarget.RealPlayer.Role.Role.Team != NebulaTeams.ImpostorTeam).SetAsUsurpableButton(this);
                EraseButton.Visibility = (button) => !MyPlayer.IsDead && leftErase > 0;
                EraseButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor);
                EraseButton.OnClick = (button) =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    if (target != null)
                    {
                        leftErase--;
                        EraseButton.UpdateUsesIcon(leftErase.ToString());
                        EraseTargets!.Add(target);
                        EraseButton.StartCoolDown();
                    }
                };
                EraseButton.ShowUsesIcon(0, leftErase.ToString());
            }
        }
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (EraseTargets != null)
            {
                foreach (var target in EraseTargets)
                {
                    if (target.IsTrueCrewmate)
                    {
                        target.RealPlayer.SetRole(Nebula.Roles.Crewmate.Crewmate.MyRole);
                    }
                    else
                    {
                        target.RealPlayer.SetRole(Hori.Scripts.Role.Neutral.SeedU.MyRole);
                    }
                }
                EraseTargets.Clear();
            }
        }
    }
}