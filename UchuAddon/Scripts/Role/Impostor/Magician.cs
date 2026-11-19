using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
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
using UnityEngine.UI;
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
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Impostor;

//役職の定義をしています。競合を避けるために役職の最後にUを付けてます。
public class MagicianU : DefinedSingleAbilityRoleTemplate<MagicianU.Ability>, DefinedRole,HasCitation
{
    //役職の定義をしています。　　　　　　　　　　
    public MagicianU() : base("magicianU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [MagicCooldown,NumOfMagic])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Magician.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon); //タグをつけてます。絶対付けてください。
    }

    //Jackal化などに対応させてます。KillersSideだとマッド化まで対応してくれます。
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.KillersSide;

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    //オプションの定義をしています。
    static private FloatConfiguration MagicCooldown = NebulaAPI.Configurations.Configuration("options.role.magicianU.magicCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration NumOfMagic = NebulaAPI.Configurations.Configuration("options.role.magicianU.numOfMagic", (1, 99), 4);

    //役職の引用です。無い場合はなくてokです。
    Citation? HasCitation.Citation => Hori.Core.Citations.ExtremeRoles;

    //RoleIconを設定しています。
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Magician.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    //必須
    static public MagicianU MyRole = new(); 

    //AmalgamにMap系能力として登録しています。
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;

    //Abilityを作ってます。

    static private readonly Virial.Media.Image MagicImage = NebulaAPI.AddonAsset.GetResource("MagicButton.png")!.AsImage(115f)!;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];//必須
        int leftMagic = NumOfMagic;
        int Magic;
        float KillTimer = 7f;
        bool KillJudge = false;
        float SkillTimer = 7f;
        bool SkillJudge = false;
        int KillCount = 0;
        bool KillAchi = false;
        bool KillAchiStop = false;
        bool OneturnMagic = false;

        //役職セット時に発火します。能力ボタンなどはここで登録してます。
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                //ボタン自体の定義
                var magicButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, MagicCooldown, "magic", MagicImage).SetAsUsurpableButton(this);
                magicButton.SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor); //ボタンのテキストのラベルを設定。
                magicButton.Visibility = (button) => !MyPlayer.IsDead && leftMagic > 0; //ボタンの表示条件を設定。
                magicButton.OnClick = (button) => //ボタンを押した時に発火します。
                {
                    leftMagic--;
                    Magic++;
                    KillTimer = 7f;
                    KillJudge = true;
                    OneturnMagic = true;

                    MagicRpc.Invoke();

                    //ボタンの使用回数の表示を更新しています。
                    magicButton.UpdateUsesIcon(leftMagic.ToString());
                    magicButton.StartCoolDown(); //ボタンのクールダウンをスタートしています。

                    if (Magic < 3) return;
                    //new StaticAchievementToken("magicianU.common1");
                };

                //ボタンに使用回数を表示させています。
                magicButton.ShowUsesIcon(0, leftMagic.ToString()); 
            }
        }

        //Rpcを作成しています。Rpcで実行すると全プレイヤーに同期されます。
        static RemoteProcess MagicRpc = new RemoteProcess("MagicRpc", (_) =>
        {
            var player = GamePlayer.LocalPlayer;
            var localplayer = PlayerControl.LocalPlayer;

            if (player == null || localplayer == null)
            {
                return;
            }

            if (player.IsDead || player.IsDisconnected)
            {
                return;
            }
            if (Minigame.Instance)
            {
                try
                {
                    Minigame.Instance.Close();
                }
                catch
                {
                }
            }
            AmongUsUtil.PlayFlash(Color.Red);


            if (localplayer.inVent)
            {
                localplayer.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                localplayer.MyPhysics.ExitAllVents();
            }

            var pcs = GamePlayer.AllPlayers.Where(p => p != null && p != null && !p.IsDead  && !p.IsDisconnected).ToArray();
            int count = pcs.Length;

            List<Vector2> posList = new();
            foreach (var pc in pcs)
            {
                Vector3 pos = pc.VanillaPlayer.transform.position;
                posList.Add(new Vector2(pos.x, pos.y));
            }

            posList = posList.OrderBy(_ => UnityEngine.Random.value).ToList();

            for (int i = 0; i < count; i++)
            {
                pcs[i].VanillaPlayer.NetTransform.RpcSnapTo(posList[i]);
            }         
        });

        public static List<Vector2> GetAllPlayersPositions()
        {
            List<Vector2> result = new();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null) continue;
                if (pc == null || pc.Data == null) continue;
                if (pc.Data.IsDead || pc.Data.Disconnected) continue;

                Vector3 pos = pc.transform.position;
                result.Add(new Vector2(pos.x, pos.y));
            }

            return result;

        }
        /*
        [Local,OnlyMyPlayer]
        void Kill(PlayerKillPlayerEvent ev)
        {
            KillJudge = true;
            KillTimer = 7f;
            if (SkillJudge) 
            {
                new StaticAchievementToken("magicianU.hard1");
            }
        }

        void AllKill(PlayerKillPlayerEvent ev)
        {
            KillCount++;
        }

        [Local]
        void Update(GameUpdateEvent ev)
        {
            if (KillJudge)
            {
                if (KillTimer > 0f)
                {
                    KillTimer -= Time.deltaTime;
                    if (KillTimer < 0f) KillTimer = 0f;
                }
                else
                {
                    KillJudge = false;
                }
            }

            if (SkillJudge)
            {
                if (SkillTimer > 0f)
                {
                    SkillTimer -= Time.deltaTime;
                    if (SkillTimer < 0f) SkillTimer = 0f;
                }
                else
                {
                    SkillJudge = false;
                }
            }

            if (KillAchiStop) return;
            if (!KillAchi) return;
            new StaticAchievementToken("magicianU.challenge1");
            KillAchi = false;
            KillAchiStop = true;
        }

        [Local]
        void Report(ReportDeadBodyEvent ev)
        {
            if (SkillJudge)
            {
                new StaticAchievementToken("magicianU.another1");
            }
        }
        [Local,OnlyMyPlayer]
        void AchievementReport(ReportDeadBodyEvent ev)
        {
            if (OneturnMagic) return;
            KillCount--;
            if (KillCount >= 3)
            {
                KillAchi = true;
            }
            KillCount = 0;
        }
        [Local, OnlyMyPlayer]
        void Meeting(CalledEmergencyMeetingEvent ev)
        {
            if (OneturnMagic) return;
            if (KillCount >= 3)
            {
                KillAchi = true;
            }
            KillCount = 0;
        }

        [Local,OnlyMyPlayer]
        void EndMeeting(MeetingEndEvent ev)
        {
            OneturnMagic = false;
        }
        */
    }
}