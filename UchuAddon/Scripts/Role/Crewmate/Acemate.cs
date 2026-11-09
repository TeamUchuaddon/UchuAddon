using AmongUs.Data;
using Hori.Scripts.Role.Modifier;
using Hori.Scripts.Role.Neutral;
using Nebula.Behavior;
using Nebula.Patches;
using Nebula.Roles.Abilities;
using Nebula.Roles.Ghost.Neutral;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Runtime;
using static Hori.Scripts.Role.Neutral.EpidemicU;
using static Il2CppSystem.Xml.Schema.FacetsChecker.FacetsCompiler;
using static Nebula.Roles.Ghost.Neutral.Grudge;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

[NebulaPreprocess(PreprocessPhase.Roles)]
public class AcemateU : DefinedSingleAbilityRoleTemplate<AcemateU.Ability>, DefinedRole, IAssignableDocument
{
    public AcemateU() : base("acemateU", new(255, 200, 0), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam)
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Acemate.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public AcemateU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Acemate.png")!.AsImage(110f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    bool IAssignableDocument.HasAbility => false;
    bool IAssignableDocument.HasTips => true;

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "acemateU.common1")
        {
            RelatedRole = AcemateU.MyRole,
            Goal = 20,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.acemateU.common1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "acemateU.common1")
        {
            Goal = 20
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "acemateU.common2")
        {
            RelatedRole = AcemateU.MyRole,
            Goal = 300,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.acemateU.common2"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "acemateU.common2")
        {
            Goal = 300
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "acemateU.hard1")
        {
            RelatedRole = AcemateU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Gold
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "acemateU.challenge1")
        {
            RelatedRole = AcemateU.MyRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        private SpriteRenderer[]? cachedRenderers;

        static private readonly Image buttonSprite = SpriteLoader.FromResource("Nebula.Resources.Buttons.BalloonButton.png", 115f);

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                /*
                Virial.Components.ObjectTracker<Console> consoleTracker = new ObjectTrackerUnityImpl<Console, Console>(
                    MyPlayer.VanillaPlayer, AmongUsLLImpl.Instance.VanillaKillDistance, () => AmongUsLLImpl.ShipStatusInstance.AllConsoles,
                    c => true, c => !c.IsFast<VentCleaningConsole>() && !c.IsFast<AutoTaskConsole>() && !c.IsFast<StoreArmsTaskConsole>(), c => c,
    c => [c.transform.position], c => c.Image, UnityEngine.Color.yellow,
    true, false).Register(this);
                var balloonButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
              0f,"balloon", buttonSprite,
              _ => consoleTracker.CurrentTarget != null
              ).SetAsUsurpableButton(this);
                balloonButton.OnClick = (button) =>
                {
                    var console = consoleTracker.CurrentTarget;
                    if (console == null) return;
                    UchuDebug.Log($"[Console] {console.name}");
                };
                */
            }
        }

        [Local]
        void OnHudUpdate(GameHudUpdateEvent ev) //タスク画面を半透明にする
        {
            if (!AmOwner) return;

            var minigame = Minigame.Instance;
            if (minigame == null)
            {
                cachedRenderers = null;
                return;
            }

            if (IsExemptTask(minigame)) return;

            if (cachedRenderers == null)
            {
                cachedRenderers = minigame.GetComponentsInChildren<SpriteRenderer>(true);
                PlayerControl.LocalPlayer.moveable = true;
            }

            foreach (var rend in cachedRenderers)
            {
                if (rend == null) continue;

                var c = rend.color;
                if (c.a <= 0.5f) continue;

                rend.color = new (c.r, c.g, c.b, 0.5f);
            }
        }

        void Update(GameUpdateEvent ev) //タスク中に動けるようにする
        {
            if (!PlayerControl.LocalPlayer.AmOwner) return;
            if (Minigame.Instance == null) return;

            if (IsExemptTask(Minigame.Instance)) return;

            Vector2 vel = Vector2.Zero;

            if (DestroyableSingleton<HudManager>.Instance != null)
            {
                vel = DestroyableSingleton<HudManager>.Instance.joystick.DeltaL;
            }

            MyPlayer.VanillaPlayer.MyPhysics.body.velocity = vel * GameManager.Instance.LogicOptions.GetPlayerSpeedMod(PlayerControl.LocalPlayer) * 2.5f;
        }

        static bool IsExemptTask(Minigame minigame)
        {
            if (minigame.TryCast<MedScanMinigame>()) return true;
            if (minigame.TryCast<EmergencyMinigame>()) return true;
            if (minigame.TryCast<VitalsMinigame>()) return true;
            if (minigame.TryCast<SurveillanceMinigame>()) return true;
            if (minigame.TryCast<PlanetSurveillanceMinigame>()) return true;
            if (minigame.TryCast<ShapeshifterMinigame>()) return true;

            return false;
        }

        float IsTaskCompleteTime;
        private int challengeTaskCount;
        private float challengeDistance;
        private bool challengeDistanceActive;

        [Local]
        void OnTitleUpdate(GameUpdateEvent ev)
        {
            if (Minigame.Instance)
            {
                IsTaskCompleteTime = 1f;
            }
            else if (IsTaskCompleteTime >= 0)
            {
                IsTaskCompleteTime -= Time.deltaTime;
            }
            else
            {
                IsTaskCompleteTime = 0f;
            }
        }

        [OnlyMyPlayer]
        void OnTitle2(PlayerTaskCompleteLocalEvent ev)
        {
            TitleUnLock("uchuaddon.acemateU.common1");

            if (IsTaskCompleteTime >= 0)
            {
                TitleUnLock("uchuaddon.acemateU.hard1");
            }

            challengeTaskCount++;
        }

        private Vector2 lastPosition;
        private bool hasLastPosition;
        private float distanceBuffer;

        [Local]
        void OnDistanceUpdate(GameUpdateEvent ev)
        {
            if (!AmOwner) return;

            if (!Minigame.Instance)
            {
                hasLastPosition = false;
                challengeDistanceActive = false;
                return;
            }

            var current = MyPlayer.TruePosition;

            if (!hasLastPosition)
            {
                lastPosition = current;
                hasLastPosition = true;
                challengeDistanceActive = true;
                return;
            }

            float delta = (current - lastPosition).Magnitude;

            distanceBuffer += delta;
            challengeDistance += delta;
            lastPosition = current;

            while (distanceBuffer >= 1f)
            {
                distanceBuffer -= 1f;
                TitleUnLock("uchuaddon.acemateU.common2");
            }
        }

        private void CheckChallenge()
        {
            if (challengeTaskCount >= 6 && challengeDistance >= 50f)
            {
                TitleUnLock("uchuaddon.acemateU.challenge1");
            }
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}