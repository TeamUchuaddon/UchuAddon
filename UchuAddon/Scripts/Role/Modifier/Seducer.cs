using Hori.Scripts.Role.Sample;
using Mono.Cecil;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Nebula.Behavior;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using static UnityEngine.GraphicsBuffer;

namespace Hori.Scripts.Role.Modifier;

public class SeducerU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, IAssignableDocument
{
    private SeducerU() : base("seducerU", "SDU", new(166, 27, 113))
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);


    static public SeducerU MyRole = new SeducerU();

    static public HashSet<DefinedModifier> allowed = new()
    {
        AlertU.MyRole,
        AutopsyU.MyRole,
        Bloody.MyRole,
        ChickenModifierU.MyRole,
        Confused.MyRole,
        Disclosure.MyRole,
        DrunkU.MyRole,
        ExpressU.MyRole,
        GutsU.MyRole,
        HexU.MyRole,
        HunchU.MyRole,
        InvisibleU.MyRole,
        HighLowU.MyRole,
        GuesserModifier.MyRole,
        MatchU.MyRole,
        MiasmaU.MyRole,
        MoonU.MyRole,
        RadarU.MyRole,
        ScaleU.MyRole,
        StarU.MyRole,
        StaticU.MyRole,
        TieBreaker.MyRole,
        TimerU.MyRole,
        GuardingU.MyRole,
        TunaModiU.MyRole,
        WatchingU.MyRole,
        Lover.MyRole,
        ScarletLover.MyRole,
        SidekickModifier.MyRole
    };
    static private readonly Virial.Media.Image MeetingIcon = NebulaAPI.AddonAsset.GetResource("StealIcon.png")!.AsImage(115f)!;


    bool IAssignableDocument.HasWinCondition => true;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(MeetingIcon, "role.seducerU.ability.thief");
    }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Seducer.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;    
        int leftMeeting = 1;
        bool stolen = false;
        bool playTargetEffect = false;
        RuntimeModifier? mod;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                leftMeeting = 1;
            }
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            leftMeeting = 1;

            var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
            buttonManager?.RegisterMeetingAction(new(MeetingIcon, p =>
            {
                var target = p.MyPlayer;

                var candidates = target.Modifiers.Where(m => SeducerU.allowed.Contains(m.Modifier)).ToList();

                stolen = false;
                playTargetEffect = false;
                leftMeeting--;

                if (candidates.Count > 0)
                {
                    mod = candidates[UnityEngine.Random.Range(0, candidates.Count)];

                    if (mod is Lover.Instance lover)  // Lover専用処理
                    {
                        int loverId = lover.LoversId;

                        if (!MyPlayer.Modifiers.Any(m => m.Modifier == Lover.MyRole))
                        {
                            MyPlayer.AddModifier(Lover.MyRole, new int[] { loverId });
                        }

                        target.RemoveModifier(Lover.MyRole);
                        target.AddModifier(LostLoverU.MyRole, new int[] { loverId, 0, 0, 0 });

                        stolen = true;
                        playTargetEffect = false;
                    }
                    else if(mod is ScarletLover.Instance scarlet)
                    {
                        int flirtatiousId = scarlet.FlirtatiousId;
                        bool amFavorite = scarlet.AmFavorite;

                        if (!MyPlayer.Modifiers.Any(m => m.Modifier == ScarletLover.MyRole))
                        {
                            MyPlayer.AddModifier(ScarletLover.MyRole, new int[] { flirtatiousId, amFavorite ? 1 : 0 });
                        }

                        target.RemoveModifier(ScarletLover.MyRole);
                        target.AddModifier(LostLoverU.MyRole, new int[] { 0, flirtatiousId, 1, amFavorite ? 1 : 0 });

                        stolen = true;
                        playTargetEffect = false;
                    }
                    else if (mod is SidekickModifier.Instance sidekick)
                    {
                        int teamId = sidekick.JackalTeamId;

                        if (!MyPlayer.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole))
                        {
                            MyPlayer.AddModifier(SidekickModifier.MyRole, new int[] { 1, teamId });
                        }

                        target.RemoveModifier(SidekickModifier.MyRole);

                        stolen = true;
                        playTargetEffect = true; 
                    }
                    else 
                    {
                        var defined = mod!.Modifier;

                        bool alreadyHave = MyPlayer.Modifiers.Any(m => m.Modifier == defined);

                        target.RemoveModifier(defined);

                        if (!alreadyHave)
                        {
                            MyPlayer.AddModifier(defined, []);
                        }

                        stolen = true;
                        playTargetEffect = true;
                    }
                }
                
                if (stolen)
                {
                    NebulaAsset.PlaySE(NebulaAudioClip.SnatcherSuccess); //自分自身にSEとエフェクト

                    if (MeetingHud.Instance.TryGetPlayer(MyPlayer.PlayerId, out var pva))
                    {
                        pva.NameText.StartCoroutine(AnimationEffects.CoPlayRoleNameEffect(pva.NameText.transform.parent, new(0.2f, 0f, -0.2f), Color.grey, LayerExpansion.GetUILayer(), 2.4f).WrapToIl2Cpp());
                    }

                    if (playTargetEffect)
                    {
                        RpcThiefEffectUchu.Invoke(target);
                    }
                }  
            }, p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner  && leftMeeting > 0 && !PlayerControl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess));
        }

        static RemoteProcess<GamePlayer> RpcThiefEffectUchu = new("RpcThiefEffect_Uchu", (message, _) =>
        {
            if (GamePlayer.LocalPlayer == null) return;
            if (message != GamePlayer.LocalPlayer) return;

            NebulaAsset.PlaySE(NebulaAudioClip.SnatcherSuccess);

            if (MeetingHud.Instance.TryGetPlayer(GamePlayer.LocalPlayer.PlayerId, out var pva))
            {
                pva.NameText.StartCoroutine(AnimationEffects.CoPlayRoleNameEffect(pva.NameText.transform.parent, new(0.2f, 0f, -0.2f), Color.grey, LayerExpansion.GetUILayer(), 2.4f).WrapToIl2Cpp());
            }
        });

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}


public class LostLoverU : DefinedModifierTemplate, DefinedModifier
{
    private LostLoverU() : base("seducer.LostLoverU", new(130, 7, 111), [], true, () => false)
    {
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments)
     => new Instance(
         player,
         arguments.Get(0, 0), // loversId
         arguments.Get(1, 0), // flirtatiousId
         arguments.Get(2, 0), // type
         arguments.Get(3, 0) == 1 // amFavorite
     );


    static public LostLoverU MyRole = new LostLoverU();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/LostLover.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        int loversId;
        int flirtatiousId;
        int type;
        bool amFavorite;
        public Cache<GamePlayer> OriginalLover;

        public Instance(GamePlayer player, int loversId, int flirtatiousId, int type, bool amFavorite) : base(player)
        {
            this.loversId = loversId;
            this.flirtatiousId = flirtatiousId;
            this.type = type;
            this.amFavorite = amFavorite;

            OriginalLover = new(() =>
            {
                return NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p =>
                p.PlayerId != MyPlayer.PlayerId && p.GetModifiers<Lover.Instance>().Any(l => l.LoversId == loversId) && !p.GetModifiers<SeducerU.Instance>().Any());
            });
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            bool amOriginalOrSelf = (OriginalLover.Get()?.PlayerId == GamePlayer.LocalPlayer?.PlayerId || AmOwner);

            if (inEndScene)
            {
                if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
            }
            else
            {
                if (type == 0)
                {
                    if (!(AmOwner || amOriginalOrSelf)) return;

                    Color loverColor = Lover.Colors[canSeeAllInfo ? loversId : 0];
                    name += "♥".Color(loverColor);
                }
                else
                {
                    Color loverColor = Scarlet.MyRole.UnityColor;

                    if (AmOwner || canSeeAllInfo)
                        name += "♡".Color(loverColor); //LostLoverではチェック必要なし
                }
            }
        }

        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp
        {
            get
            {
                if (type == 0) return [Lover.MyRole];
                else return [ScarletLover.MyRole];
            }
        }

        void RuntimeAssignable.OnActivated()
        {

        }
    }
}