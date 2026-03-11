using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Ghost.Complex;

public class MiracleU : DefinedGhostRoleTemplate, DefinedGhostRole, IAssignableDocument
{
    public MiracleU() : base("miracleU", new(243, 255, 163), RoleCategory.CrewmateRole | RoleCategory.ImpostorRole, [SwapCooldown,NumOfSwap,MiracleAction])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    string ICodeName.CodeName => "MIR";

    RuntimeGhostRole RuntimeAssignableGenerator<RuntimeGhostRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private readonly FloatConfiguration SwapCooldown = NebulaAPI.Configurations.Configuration("options.role.miracleU.swapCooldown", (5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private readonly IntegerConfiguration NumOfSwap = NebulaAPI.Configurations.Configuration("options.role.miracleU.numOfSwap", (1, 7), 1);
    static private ValueConfiguration<int> MiracleAction = NebulaAPI.Configurations.Configuration("options.role.miracleU.miracleAction", ["options.role.miracleU.miracleAction.myTeam", "options.role.miracleU.miracleAction.otherNeutral", "options.role.miracleU.miracleAction.all"], 0);

    static public readonly MiracleU MyRole = new();

    static private readonly Virial.Media.Image MiracleImage = NebulaAPI.AddonAsset.GetResource("WitchSpellButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(MiracleImage, "role.miracleU.ability.swap");
    }
    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        if (MiracleAction.GetValue() == 0) yield return new("%ACTION%", Language.Translate("role.miracleU.ability.main.myTeam"));
        if (MiracleAction.GetValue() == 1) yield return new("%ACTION%", Language.Translate("role.miracleU.ability.main.otherNeutral"));
        if (MiracleAction.GetValue() == 2) yield return new("%ACTION%", Language.Translate("role.miracleU.ability.main.all"));
    }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Miracle.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    public class Instance : RuntimeAssignableTemplate, RuntimeGhostRole
    {
        DefinedGhostRole RuntimeGhostRole.Role => MyRole;
        private GamePlayer? swapTarget = null;
        bool swapCheck = false;
        int left = NumOfSwap;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                PoolablePlayer? swapIcon = null;

                GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(ev =>
                {
                    if (swapIcon) GameObject.Destroy(swapIcon!.gameObject);
                    swapIcon = null;
                }, this);

                if (MyPlayer.IsImpostor && MiracleAction.GetValue() == 0)
                {
                    var ImswapButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                        SwapCooldown, "miracleU.swap", MiracleImage, _ => playerTracker.CurrentTarget != null && swapTarget == null && left > 0 && playerTracker.CurrentTarget.RealPlayer.Role.Role.Team == NebulaTeams.ImpostorTeam, null, true);
                    ImswapButton.ShowUsesIcon(3, left.ToString());
                    ImswapButton.OnClick = (button) =>
                    {
                        var target = playerTracker.CurrentTarget;
                        if (target != null)
                        {
                            swapTarget = playerTracker.CurrentTarget;
                            left--;
                            ImswapButton.UpdateUsesIcon(left.ToString());
                            swapIcon = (ImswapButton as ModAbilityButtonImpl)?.GeneratePlayerIcon(swapTarget);
                            RpcCheckSwap.Invoke((MyPlayer, swapTarget!));
                        }
                    };
                }
                else if (MyPlayer.IsCrewmate && MiracleAction.GetValue() == 0)
                {
                    var CrswapButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                        SwapCooldown, "miracleU.swap", MiracleImage, _ => playerTracker.CurrentTarget != null && swapTarget == null && left > 0 && playerTracker.CurrentTarget.RealPlayer.Role.Role.Team == NebulaTeams.CrewmateTeam, null, true);
                    CrswapButton.ShowUsesIcon(3, left.ToString());
                    CrswapButton.OnClick = (button) =>
                    {
                        var target = playerTracker.CurrentTarget;
                        if (target != null)
                        {
                            swapTarget = playerTracker.CurrentTarget;
                            left--;
                            CrswapButton.UpdateUsesIcon(left.ToString());
                            swapIcon = (CrswapButton as ModAbilityButtonImpl)?.GeneratePlayerIcon(swapTarget);
                            RpcCheckSwap.Invoke((MyPlayer, swapTarget!));
                        }
                    };
                }
                else if(MiracleAction.GetValue() == 1)
                {
                    var OtherswapButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                        SwapCooldown, "miracleU.swap", MiracleImage, _ => playerTracker.CurrentTarget != null && swapTarget == null && left > 0 && 
                        (playerTracker.CurrentTarget.RealPlayer.Role.Role.Team == NebulaTeams.CrewmateTeam || playerTracker.CurrentTarget.RealPlayer.Role.Role.Team == NebulaTeams.CrewmateTeam), null, true);
                    OtherswapButton.ShowUsesIcon(3, left.ToString());
                    OtherswapButton.OnClick = (button) =>
                    {
                        var target = playerTracker.CurrentTarget;
                        if (target != null)
                        {
                            swapTarget = playerTracker.CurrentTarget;
                            left--;
                            OtherswapButton.UpdateUsesIcon(left.ToString());
                            swapIcon = (OtherswapButton as ModAbilityButtonImpl)?.GeneratePlayerIcon(swapTarget);
                            RpcCheckSwap.Invoke((MyPlayer, swapTarget!));
                        }
                    };
                }
                else if (MiracleAction.GetValue() == 2)
                {
                    var AllswapButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                        SwapCooldown, "miracleU.swap", MiracleImage, _ => playerTracker.CurrentTarget != null && swapTarget == null && left > 0, null, true);
                    AllswapButton.ShowUsesIcon(3, left.ToString());
                    AllswapButton.OnClick = (button) =>
                    {
                        var target = playerTracker.CurrentTarget;
                        if (target != null)
                        {
                            swapTarget = playerTracker.CurrentTarget;
                            left--;
                            AllswapButton.UpdateUsesIcon(left.ToString());
                            swapIcon = (AllswapButton as ModAbilityButtonImpl)?.GeneratePlayerIcon(swapTarget);
                            RpcCheckSwap.Invoke((MyPlayer, swapTarget!));
                        }
                    };
                }
            }
        }

        IEnumerator MiracleSystem()
        {
            if (swapTarget == null) yield break;
            if (swapCheck) yield break;
            if (!(swapTarget.VanillaPlayer)) yield break;
            if (MyPlayer.PlayerState == PlayerState.Misguessed) yield break;

            var player = swapTarget;

            if (player == null || player.IsDead) yield break;

            int[] targetArgument = [];
            var targetRole = player.Role.Role;
            var myRole = MyPlayer.Role.Role;
            yield return player.Unbox().CoGetRoleArgument((args) => targetArgument = args);

            using (RPCRouter.CreateSection("MiracleSwap_Uchu"))
            {
                MyPlayer.SetRole(targetRole, targetArgument);
                player.SetRole(myRole, null);

                PlayerExtension.SendRoleSwapping(MyPlayer, player, myRole, PlayerRoleSwapEvent.SwapType.Swap);
                PlayerExtension.SendRoleSwapping(player, MyPlayer, targetRole, PlayerRoleSwapEvent.SwapType.Swap);
            }

            swapCheck = true;

            yield return new WaitForSeconds(0.2f);

            yield break;
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            swapTarget = null;
            currentTargetList.Clear();
        }
        [Local]
        void OnMeetingPreEnd(MeetingPreEndEvent ev)
        {
            ev.PushCoroutine(MiracleSystem());
        }
    }

    static private List<(GamePlayer miracle, GamePlayer target)> currentTargetList = [];
    static private RemoteProcess<(GamePlayer miracle, GamePlayer target)> RpcFixSwapMiracle = new("FixSwapMiracle_Uchu", (message, _) => currentTargetList.Add(message));
    static private RemoteProcess<(GamePlayer shifter, GamePlayer target)> RpcCheckSwap = new("CheckSwap_Uchu", (message, _) =>
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (currentTargetList.Any(entry => entry.target == message.target)) return;

        RpcFixSwapMiracle.Invoke(message);
    });
}