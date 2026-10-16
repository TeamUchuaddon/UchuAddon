using BepInEx.Unity.IL2CPP.Utils.Collections;
using Epic.OnlineServices.Presence;
using Hazel.Dtls;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Nebula.VoiceChat;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Virial;
using Virial;
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Components;
using Virial.Components;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Events.Role;
using Virial.Game;
using Virial.Game;
using Virial.Game;
using Virial.Helpers;
using Virial.Runtime;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class ObolusU : DefinedRoleTemplate, DefinedRole,IAssignableDocument
{
    public static Team MyTeam = new Team("teams.obolusU", new Virial.Color(189, 135, 26), TeamRevealType.OnlyMe);
    private ObolusU() : base("obolusU", new(189, 135, 26), RoleCategory.NeutralRole, MyTeam, [KillCoolDownOption,KillShieldTurn,CanKillHidingPlayer, ImpostorVision, BlackOutVision, ObolusBreakdown, BreakdownAction, ObolusBreakdownInDead,
    new GroupConfiguration("options.role.obolusU.group.win",[JackalOptions,TyrantOptions, RogueOptions],GroupConfigurationColor.ToDarkenColor(MyTeam.Color.ToUnityColor()))])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Obolus.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.obolusU.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static private BoolConfiguration CanKillHidingPlayer = NebulaAPI.Configurations.Configuration("options.role.obolusU.canKillHidingPlayer", false);
    public static IntegerConfiguration KillShieldTurn = NebulaAPI.Configurations.Configuration("options.role.obolusU.killShieldTurn", (0, 7), 2);
    static private BoolConfiguration ImpostorVision = NebulaAPI.Configurations.Configuration("options.role.obolusU.impostorVision", true);
    static private BoolConfiguration BlackOutVision = NebulaAPI.Configurations.Configuration("options.role.obolusU.blackOutVision", true);
    static internal BoolConfiguration ObolusBreakdown = NebulaAPI.Configurations.Configuration("options.role.obolusU.obolusBreakdown", false);
    static private ValueConfiguration<int> BreakdownAction = NebulaAPI.Configurations.Configuration("options.role.obolusU.breakdownAction", ["options.role.obolusU.breakdownAction.firstTurn", "options.role.obolusU.breakdownAction.unlockTurn"], 0 ,()=> ObolusBreakdown);
    static internal BoolConfiguration ObolusBreakdownInDead = NebulaAPI.Configurations.Configuration("options.role.obolusU.obolusBreakdownInDead", true, ()=> ObolusBreakdown);

    static private BoolConfiguration JackalOptions = NebulaAPI.Configurations.Configuration("options.role.obolusU.jackalOptions", false);
    static private BoolConfiguration TyrantOptions = NebulaAPI.Configurations.Configuration("options.role.obolusU.tyrantOptions", false);
    static private BoolConfiguration RogueOptions = NebulaAPI.Configurations.Configuration("options.role.obolusU.rogueOptions", false);

    static public float KillCooldown => KillCoolDownOption.Cooldown;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Obolus.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public ObolusU MyRole = new ObolusU();
    bool DefinedRole.IsKiller => true;

    private static Virial.Media.MultiImage Icon = NebulaAPI.AddonAsset.GetResource("NoticeIcon.png")!.AsMultiImage(4, 1, 250f)!;
    static private Image NoticeIconImage => Icon.AsLoader(1);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        static private readonly Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("KillButtonOrange.png")!.AsImage(100f)!;
        bool WinBlock = false;
        bool Win = false;
        bool Extra = false;
        int leftTurnKill = 1;
        int killLock = 0;
        SpriteRenderer? lockSprite = null;

        bool IsFirst = false;

        public Instance(GamePlayer player) : base(player)
        {
        }



        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                leftTurnKill = 1;
                ModAbilityButton killButton = null!;

                var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), null, CanKillHidingPlayer);
                myTracker.SetColor(MyRole.RoleColor);

                killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                    KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                    (target, _) =>
                    {

                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            
                            if (target.IsImpostor)
                            {
                                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.ObolusWin, 1 << MyPlayer.PlayerId);
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);                        
                            }
                            else
                            {
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }
                            if (JackalOptions && target.Role.Role is Jackal)
                            {
                                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.ObolusWin, 1 << MyPlayer.PlayerId);
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }
                            if (TyrantOptions && target.Role.Role is Tyrant)
                            {
                                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.ObolusWin, 1 << MyPlayer.PlayerId);
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }
                            if (RogueOptions && target.Role.Role is RogueU)
                            {
                                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.ObolusWin, 1 << MyPlayer.PlayerId);
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }

                            leftTurnKill--;
                            killButton.UpdateUsesIcon(leftTurnKill.ToString());
                        }
                        if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }, 
                    null,
                    _ => myTracker.CurrentTarget != null && !MyPlayer.IsDived,
                    _ => MyPlayer.AllowToShowKillButtonByAbilities
                    );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                killButton.OnUpdate = (button) =>
                {
                    killButton.UpdateUsesIcon(leftTurnKill.ToString());
                };
                killButton.ShowUsesIcon(2, leftTurnKill.ToString());
                killButton.Visibility = (button) => leftTurnKill > 0 && !MyPlayer.IsDead;
                killButton.Availability = (button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove && lockSprite == null;
                killButton.SetImage(KillImage);
                if (KillShieldTurn > killLock)
                {
                    lockSprite = (killButton as ModAbilityButtonImpl)!.VanillaButton.AddLockedOverlay();
                }

                GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(ev =>
                {
                    if (lockSprite != null)
                    {
                        killLock++;
                        if (KillShieldTurn <= killLock)
                        {
                            UnityEngine.Object.Destroy(lockSprite.gameObject);
                            lockSprite = null;

                            if (ObolusBreakdown && BreakdownAction.GetValue() == 1 && !MyPlayer.IsDead)
                            {
                                RpcObolusBreakdown.Invoke();
                            }
                        }
                    }
                }, this);
            }
        }

        void Leset(MeetingPreEndEvent ev)
        {
            leftTurnKill = 1;
        }

        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (ObolusBreakdown && BreakdownAction.GetValue() == 0 && !IsFirst)
            {
                IsFirst = true;
                RpcObolusBreakdown.Invoke();
            }
        }

        [OnlyMyPlayer]
        void OnDead(PlayerDieOrDisconnectEvent ev)
        {
            RpcObolusBreakdownInDead.Invoke();
        }

        bool RuntimeRole.HasImpostorVision => ImpostorVision;
        bool RuntimeRole.IgnoreBlackout => BlackOutVision;

        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.CanMoveInVent => true;
    }

    public static RemoteProcess RpcObolusBreakdown = new("RpcObolusBreakdown_Uchu", (_) =>
    {
        var local = GamePlayer.LocalPlayer;
        if (local == null) return;

        if (local.IsImpostor 
        || (JackalOptions && local.Role.Role is Jackal)
        || (TyrantOptions && local.Role.Role is Tyrant)
        || (RogueOptions && local.Role.Role is RogueU))
        {
            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.obolusU.message.header")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(Language.Translate("options.role.obolusU.message.content")))),
                NoticeIconImage, MyRole.RoleColor);
        }
    });


    public static RemoteProcess RpcObolusBreakdownInDead = new("RpcObolusBreakdownInDead_Uchu", (_) =>
    {
        var local = GamePlayer.LocalPlayer;
        if (local == null) return;

        if (local.IsImpostor
        || (JackalOptions && local.Role.Role is Jackal)
        || (TyrantOptions && local.Role.Role is Tyrant)
        || (RogueOptions && local.Role.Role is RogueU))
        {
            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaAPI.GUI.VerticalHolder(Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.obolusU.messageDead.header")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(Language.Translate("options.role.obolusU.messageDead.content")))),
                NoticeIconImage, MyRole.RoleColor);
        }
    });
}

/*[NebulaPreprocess(PreprocessPhase.PostRoles)]
internal class ObolusTeamAllocator : AbstractModule<Game>, IGameOperator
{
    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule(() => new ObolusTeamAllocator());
    }

    private ObolusTeamAllocator()
    {
        this.RegisterPermanently();
    }

    void OnFixAssignmentTable(PreFixAssignmentEvent ev)
    {
        var oboluses = ev.RoleTable.GetPlayers(ObolusU.MyRole).ToArray();

        for (int i = 0; i < oboluses.Length; i++)
        {
            ev.RoleTable.EditRole(oboluses[i], (last) =>
            {
                // argument[0] = TeamId
                if (last.argument.Length < 1)
                    return (last.role, [i]);

                last.argument[0] = i;
                return (last.role, last.argument);
            });
        }
    }
}*/