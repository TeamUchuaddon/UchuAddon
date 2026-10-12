using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Media;
using static UnityEngine.GraphicsBuffer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class LoversBreakerU : DefinedRoleTemplate, HasCitation, DefinedRole,IAssignableDocument
{
    static readonly public RoleTeam MyTeam = new Team("teams.loversbreakerU", new Virial.Color(235, 0, 192), TeamRevealType.OnlyMe);
    private LoversBreakerU() : base("loversbreakerU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [ExpCooldown, NumOfWinExp, NumOfDeathExp, TakeoverWin, BecomingLoversMeansDeath])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/LoversBreaker.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    Citation? HasCitation.Citation => Nebula.Roles.Citations.SuperNewRoles;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private FloatConfiguration ExpCooldown = NebulaAPI.Configurations.Configuration("options.role.loversbreakerU.ExpCooldown", (0f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration NumOfWinExp = NebulaAPI.Configurations.Configuration("options.role.loversbreakerU.NumOfExp", (1, 15), 3);
    static public readonly IntegerConfiguration NumOfDeathExp = NebulaAPI.Configurations.Configuration("options.role.loversbreakerU.NumOfDeathExp", (1, 15), 2);
    static internal BoolConfiguration TakeoverWin = NebulaAPI.Configurations.Configuration("options.role.loversbreakerU.TakeoverWinOption", true);
    static private readonly BoolConfiguration BecomingLoversMeansDeath = NebulaAPI.Configurations.Configuration("options.role.loversbreakerU.BecomingLoversMeansDeath", true);
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/LoversBreaker.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public LoversBreakerU MyRole = new LoversBreakerU();
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable) => CanLoadDefaultTemplate(assignable) && assignable is not Lover;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(ExpImage, "role.loversbreakerU.ability.exp");
    }
    static private Virial.Media.Image ExpImage = NebulaAPI.AddonAsset.GetResource("ExpButton.png")!.AsImage(115f)!;
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        static int leftExp;
        static int deathExp;

        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.CanMoveInVent => true;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                leftExp = (int)NumOfWinExp;
                deathExp = (int)NumOfDeathExp;

                Nebula.Utilities.Helpers.TextHudContent("LeftDeathExp", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.loversbreakerU.hudText") + ": " + deathExp, true);
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                var ExpButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Kill, "LoversBreakerU.Explosion", ExpCooldown, "Explosion", ExpImage, _ => playerTracker.CurrentTarget != null );
                ExpButton.Visibility = (button) => !MyPlayer.IsDead && leftExp > 0;

                ExpButton.OnClick = (button) =>
                {
                    GamePlayer? target = playerTracker.CurrentTarget;
                    bool isLoverTarget = target != null && (target.TryGetModifier<Lover.Instance>(out _) || target.TryGetModifier<ScarletLover.Instance>(out _));

                    if (isLoverTarget)
                    {
                        //リア充爆破ぁ！
                        MyPlayer.MurderPlayer(playerTracker.CurrentTarget, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill | KillParameter.WithoutSelfSE);
                        RpcExp.Invoke((MyPlayer.Position, MyPlayer, 3f, 0f));
                        NebulaAsset.PlaySE(NebulaAudioClip.ExplosionNear);
                        RpcExpCount.Invoke((MyPlayer.Position, MyPlayer, 3f, 0f));
                        ExpButton.UpdateUsesIcon(leftExp.ToString());
                        ExpButton.StartCoolDown();

                        if (!TakeoverWin)
                        {
                            if (leftExp <= 0)
                            {
                                // 回数勝利
                                NebulaGameManager.Instance.RpcInvokeSpecialWin(UchuGameEnd.LoversBreakerUTeamWin, 1 << MyPlayer.PlayerId);
                            }
                        }
                    }
                    else
                    {
                        ExpButton.StartCoolDown();
                        deathExp--;
                        if (deathExp <= 0)
                        {
                            // ミスった時の処理
                            MyPlayer.Suicide(PlayerStates.Suicide, PlayerStates.Suicide, KillParameter.RemoteKill);
                            RpcExp.Invoke((MyPlayer.Position, MyPlayer, 3f, 0f));
                            NebulaAsset.PlaySE(NebulaAudioClip.ExplosionNear);
                        }
                    }
                };
                ExpButton.ShowUsesIcon(4, leftExp.ToString());
            }
        }

        void OnCheckGameEnd(EndCriteriaMetEvent ev)
        {
            if (TakeoverWin)
            {
                if (ev.OriginalEndReason != GameEndReason.Sabotage && !MyPlayer.IsDead && leftExp <= 0)
                {
                    ev.TryOverwriteEnd(UchuGameEnd.LoversBreakerUTeamWin, GameEndReason.Special);
                }
            }
        }

        void PlayerModifierSetEvent(PlayerModifierSetEvent ev)
        {
            if (BecomingLoversMeansDeath && (MyPlayer.TryGetModifier<Lover.Instance>(out _) || MyPlayer.TryGetModifier<ScarletLover.Instance>(out _)))
            {
                // 非リア脱却
                MyPlayer.Suicide(PlayerStates.Suicide, PlayerStates.Suicide, KillParameter.RemoteKill);
                RpcExp.Invoke((MyPlayer.Position, MyPlayer, 3f, 0f));
                NebulaAsset.PlaySE(NebulaAudioClip.ExplosionNear);
            }
        }

        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == UchuGameEnd.LoversBreakerUTeamWin);

        static private void Explosion(GamePlayer invoker, Vector2 pos, float angle)
        {
            //参考:死のメテオストーム
            //爆破エフェクト
            IDividedSpriteLoader ExplosionSprite = DividedSpriteLoader.FromResource("Nebula.Resources.ExplosionAnim.png", 100f, 4, 2);

            IEnumerator CoExp(float delay, Vector2 pos)
            {
                yield return Effects.Wait(delay);

                if (MeetingHud.Instance) yield break;

                var explosion = UnityHelper.CreateObject<SpriteRenderer>("Explosion", null, pos.AsVector3(-10f));

                explosion.transform.localScale = Vector3.One * 1.8f;
                explosion.transform.localEulerAngles = new(0f, 0f, System.Random.Shared.NextSingle() * 360f);

                for (int i = 0; i < 8; i++)
                {
                    explosion.sprite = ExplosionSprite.GetSprite(i);
                    yield return Effects.Wait(0.06f);
                }

                GameObject.Destroy(explosion.gameObject);
            }

            NebulaManager.Instance.StartCoroutine(CoExp(0f, pos).WrapToIl2Cpp());
        }

        static private readonly RemoteProcess<(Vector2 pos, GamePlayer invoker, float size, float angle)> RpcExp = new("Exp", static (message, _) =>
        {
            Explosion(message.invoker, message.pos, message.angle);
        });
        static private readonly RemoteProcess<(Vector2 pos, GamePlayer invoker, float size, float angle)> RpcExpCount = new("leftExpCount", static (message, _) =>
        {
            leftExp--;
        });
    }
}