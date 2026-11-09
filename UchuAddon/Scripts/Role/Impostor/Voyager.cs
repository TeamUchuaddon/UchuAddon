using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles.Impostor;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
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
using Virial.Media;
using Virial.Text;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;
using compat = Hori.Core.UchuTranslatable;

namespace Hori.Scripts.Role.Impostor;
public class VoyagerU : DefinedSingleAbilityRoleTemplate<VoyagerU.Ability>, DefinedRole,IAssignableDocument
{
    public VoyagerU() : base("voyagerU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [DiveCooldown,DiveDuration,SpeedRatio])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Voyager.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration DiveCooldown = NebulaAPI.Configurations.Configuration("options.role.voyagerU.diveCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration DiveDuration = NebulaAPI.Configurations.Configuration("options.role.voyagerU.diveDuration", (2.5f, 30f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SpeedRatio = NebulaAPI.Configurations.Configuration("options.role.voyagerU.speedRatio", (1f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);

    static public VoyagerU MyRole = new();

    static private readonly Virial.Media.Image DiveImage = NebulaAPI.AddonAsset.GetResource("PDiveButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image AwayImage = NebulaAPI.AddonAsset.GetResource("PAwayButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(DiveImage, "role.voyagerU.ability.dive");
        yield return new(AwayImage, "role.voyagerU.ability.away");
    }
    MultipleAssignmentType DefinedRole.MultipleAssignment => MultipleAssignmentType.Allowed;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Voyager.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public bool KillIgnoreTeam { get; private set; } = false;
        bool isDiving = false;
        float ParadoxTimer = DiveDuration;
        private SpriteRenderer? fullScreen;
        ModAbilityButton diveButton = null!;
        ModAbilityButton awayButton = null!;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                diveButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, DiveCooldown, "pdiver.dive", DiveImage).SetAsUsurpableButton(this);
                awayButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "pdiver.away", AwayImage).SetAsUsurpableButton(this);
                diveButton.Visibility = (button) => !isDiving && !MyPlayer.IsDead;
                awayButton.Visibility = (button) => isDiving && !MyPlayer.IsDead;

                diveButton.OnClick = (button) => button.StartEffect();
                diveButton.OnEffectStart = (button) =>
                {
                    MyPlayer.GainAttribute(PlayerAttributes.InvisibleElseImpostor, 9999999999999f, false, 1, "pdiverU:invisible");
                    MyPlayer.GainSpeedAttribute(SpeedRatio, 9999999999999f, true, 0, "pdiverU:speed");
                    MyPlayer.GainAttribute(PlayerAttributes.Roughening, 1f, true, 5, "pdiverU:mosaic");
                    isDiving = true;
                    KillIgnoreTeam = true;
                    AmongUsUtil.PlayCustomFlash(Color.Magenta, 0f, 0.25f, 0.4f);
                    NebulaAsset.PlaySE(NebulaAudioClip.SnatcherSuccess, false);

                    awayButton.StartEffect();
                    ShufflePlayerOutfits();
                };

                awayButton.EffectTimer = NebulaAPI.Modules.Timer(this, DiveDuration);
                awayButton.Availability = button => MyPlayer.CanMove;
                awayButton.PlayFlashWhile = button => true;
                awayButton.OnClick = (button) => reset();

                fullScreen = GameObject.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.transform);
                fullScreen.color = UnityEngine.Color.magenta.AlphaMultiplied(0f); // 初期は透明
                fullScreen.gameObject.SetActive(true);
                this.BindGameObject(fullScreen.gameObject);
            }
        }

        void reset()
        {
            isDiving = false;
            KillIgnoreTeam = false;
            ParadoxTimer = DiveDuration;

            MyPlayer.RemoveAttributeByTag("pdiverU:speed");
            MyPlayer.RemoveAttributeByTag("pdiverU:invisible");

            diveButton.StartCoolDown();

            ResetShuffledOutfits();

            if (fullScreen != null)
            {
                fullScreen.color = UnityEngine.Color.clear;
            }
        }


        void Update(GameUpdateEvent ev)
        {
            if (!isDiving) return;

            if (fullScreen != null)
            {
                float targetAlpha = isDiving ? 0.25f : 0f;
                float currentAlpha = fullScreen.color.a;
                currentAlpha += (targetAlpha - currentAlpha) * Time.deltaTime * 4f; 
                fullScreen.color = UnityEngine.Color.magenta.AlphaMultiplied(currentAlpha);
            }

            if (MyPlayer == null || MyPlayer.IsDead || !MyPlayer.CanMove || MeetingHud.Instance) return;
            ParadoxTimer -= Time.deltaTime;
            if (ParadoxTimer <= 0f)
            {
                MyPlayer.Suicide(PlayerState.Suicide, compat.VoyagerParadox, KillParameter.NormalKill, null);
                reset();
                return;
            }
        }

        [Local]
        void Meeting(MeetingStartEvent ev)
        {
            reset();
        }

        void IGameOperator.OnReleased()
        {
            reset();
        }

        private void ShufflePlayerOutfits()
        {
            var alives = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => !p.IsDead && !p.AmOwner).ToArray();
            var randomArray = Helpers.GetRandomArray(alives.Length);
            int maxPairs = Mathf.Min(4, alives.Length / 2);

            for (int i = 0; i < maxPairs; i++)
            {
                var player1 = alives[randomArray[i * 2]];
                var player2 = alives[randomArray[i * 2 + 1]];
                var outfit1 = player1.GetOutfit(50);
                var outfit2 = player2.GetOutfit(50);

                player1.Unbox().RemoveOutfit("Confused");
                player2.Unbox().RemoveOutfit("Confused");

                player1.Unbox().AddOutfit(new(outfit2, "Confused", OutfitPriority.Confused, false));
                player2.Unbox().AddOutfit(new(outfit1, "Confused", OutfitPriority.Confused, false));
            }
        }

        private void ResetShuffledOutfits()
        {
            var alives = NebulaGameManager.Instance!.AllPlayerInfo
                .Where(p => !p.IsDead && !p.AmOwner).ToArray();

            foreach (var player in alives)
            {
                player.Unbox().RemoveOutfit("Confused");
            }
        }
    }
}