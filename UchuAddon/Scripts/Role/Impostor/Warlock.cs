using Hori.Core;
using Nebula;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using Rewired.Utils.Classes.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Player;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;


namespace Hori.Scripts.Role.Impostor;

public class WarlockU : DefinedSingleAbilityRoleTemplate<WarlockU.Ability>, DefinedRole, HasCitation,IAssignableDocument
{
    public static TranslatableTag CurseKill = new("statistics.curseKill");

    public WarlockU() : base("warlockU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [KillCooldownOption, CurseCooldownOption, CanKillTeamPlayerOption, KillFreezeTimeOption])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Warlock.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    Citation? HasCitation.Citation { get { return Nebula.Roles.Citations.TheOtherRoles; } }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.Killers;


    static private readonly IRelativeCooldownConfiguration KillCooldownOption = NebulaAPI.Configurations.KillConfiguration("options.role.warlockU.curseKillCooldown", CoolDownType.Immediate, (0f, 60f, 2.5f), 30f, (-30f, 30f, 2.5f), 0f, (0.5f, 5f, 0.125f), 1.0f);
    static private readonly FloatConfiguration CurseCooldownOption = NebulaAPI.Configurations.Configuration("options.role.warlockU.curseCooldown", (5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private readonly BoolConfiguration CanKillTeamPlayerOption = NebulaAPI.Configurations.Configuration("options.role.warlockU.canKillTeamPlayer", true);
    static private readonly FloatConfiguration KillFreezeTimeOption = NebulaAPI.Configurations.Configuration("options.role.warlockU.killFreezeTime", (0f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Second);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Warlock.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public WarlockU MyRole = new();
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(SampleImage, "role.warlockU.ability.curse");
    }
    static private readonly Virial.Media.Image SampleImage = NebulaAPI.AddonAsset.GetResource("WarlockCurseButton.png")!.AsImage(115f)!;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        
        GamePlayer? cursePlayer = null;
        ObjectTracker<Player> killTracker = null;
        PoolablePlayer? curseTargetIcon = null;
        bool canKill = false;



        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {

            if (AmOwner)
            {

                var playerTracker = NebulaAPI.Modules.KillTracker(this, MyPlayer);
                playerTracker.SetColor(MyRole.RoleColor);

                ObjectTracker<Player> killTracker = null;

                ModAbilityButton? curseButton = null;

                var killButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, true, false, Virial.Compat.VirtualKeyInput.Kill, "", KillCooldownOption.GetCooldown(MyPlayer.TeamKillCooldown), "CurseKill", null, _ => killTracker != null ? killTracker.CurrentTarget != null : false);
                killButton.OnClick = (button) =>
                {
                    if (killTracker.CurrentTarget == null) return;
                    MyPlayer.MurderPlayer(killTracker.CurrentTarget, CurseKill, null, KillParameter.RemoteKill);
                    MyPlayer.GainSpeedAttribute(0f, KillFreezeTimeOption, false, 10);
                    MyPlayer.GainAttribute(PlayerAttributes.CooldownSpeed, KillFreezeTimeOption, 0, false, 10, "warlockU:curseKill");
                    RpcBlink.Invoke((cursePlayer, killTracker.CurrentTarget));
                    cursePlayer = null;
                    if (curseTargetIcon) GameObject.Destroy(curseTargetIcon.gameObject);
                    curseTargetIcon = null;
                    killTracker = null;
                    canKill = false;
                    button.StartCoolDown();
                    curseButton.StartCoolDown();

                };
                killButton.Availability = _ => (killTracker != null ? killTracker.CurrentTarget != null : false) && canKill;
                killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);


                curseButton = NebulaAPI.Modules.InteractButton(this, MyPlayer, playerTracker, new PlayerInteractParameter(true, false, true), Virial.Compat.VirtualKeyInput.Ability, null,
                CurseCooldownOption, "Curse", SampleImage, (p, button) =>
                {
                    if (playerTracker.CurrentTarget == null) return;
                    cursePlayer = playerTracker.CurrentTarget;
                    curseTargetIcon = (killButton as ModAbilityButtonImpl)?.GeneratePlayerIcon(playerTracker.CurrentTarget);
                    killTracker = NebulaAPI.Modules.PlayerTracker(this, cursePlayer, p => p != cursePlayer && (CanKillTeamPlayerOption ? true : MyPlayer.Role.Role.Team != p.Role.Role.Team));
                    killTracker.SetColor(MyRole.RoleColor); 
                    canKill = true;
                    button.StartCoolDown();
                }).SetAsUsurpableButton(this);

                curseButton.Visibility = _ => cursePlayer == null && !MyPlayer.IsDead;
                curseButton.SetLabelType(ModAbilityButton.LabelType.Impostor);


            }

        }
        RemoteProcess<(GamePlayer killer, GamePlayer victim)> RpcBlink = new("Blink", (message, _) => {
            message.killer.VanillaPlayer.NetTransform.SnapTo(message.victim.VanillaPlayer.transform.position);
        });


        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            cursePlayer = null;
            if (curseTargetIcon) GameObject.Destroy(curseTargetIcon.gameObject);
            curseTargetIcon = null;
            killTracker = null;
            canKill = false;
        }

        [Local]
        void OnPlayerDie(PlayerDieOrDisconnectEvent ev)
        {
            if (ev.Player == cursePlayer)
            {
                cursePlayer = null;
                if (curseTargetIcon) GameObject.Destroy(curseTargetIcon.gameObject);
                curseTargetIcon = null;
                killTracker = null;
                canKill = false;
            }
        }


        bool IPlayerAbility.HideKillButton => true;
    }

}