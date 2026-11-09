using BepInEx.Unity.IL2CPP.Utils.Collections;
using Epic.OnlineServices.RTCAudio;
using Hori.Core;
using Hori.Scripts.Abilities;
using Hori.Scripts.Core;
using Hori.Scripts.Role.Modifier;
using Il2CppSystem.Xml;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Impostor;
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Mathn = Virial.Utilities.Mathn;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class VisorU : DefinedSingleAbilityRoleTemplate<VisorU.Ability>, DefinedRole, IAssignableDocument, HasCitation
{
    public VisorU() : base("visorU", new(29, 48, 145), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [CanMoveWithMapWatchingOption, CanUseAdminOnMeetingOption,
    new GroupConfiguration("options.role.visorU.group.charge",[PortableAdminCharge, MaxPortableAdminCharge, ChargesPerTasks],GroupConfigurationColor.ToDarkenColor(new UnityEngine.Color(0.1137f, 0.1882f, 0.5686f)))])
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [VisorModifierU.MyRole.ConfigurationHolder!]);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate; //マッド化
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable) => CanLoadDefaultTemplate(assignable) && assignable is not VisorModifierU;


    static public BoolConfiguration CanMoveWithMapWatchingOption = NebulaAPI.Configurations.Configuration("options.role.visorU.canMoveWithMapWatching", false);
    static public BoolConfiguration CanUseAdminOnMeetingOption = NebulaAPI.Configurations.Configuration("options.role.visorU.canUseAdminOnMeeting", false);

    static public FloatConfiguration PortableAdminCharge = NebulaAPI.Configurations.Configuration("options.role.visorU.portableAdminCharge", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static public FloatConfiguration MaxPortableAdminCharge = NebulaAPI.Configurations.Configuration("options.role.visorU.maxPortableAdminCharge", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static public FloatConfiguration ChargesPerTasks = NebulaAPI.Configurations.Configuration("options.role.visorU.chargesPerTasks", (0.5f, 10f, 0.5f), 1f, FloatConfigurationDecorator.Second);

    static public VisorU MyRole = new();

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Visor.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    Citation? HasCitation.Citation => Hori.Core.Citations.ExtremeRoles;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(AdminImage, "role.visorU.ability.admin");
    }

    static private readonly Virial.Media.Image AdminImage = NebulaAPI.AddonAsset.GetResource("VisorAdminButton .png")!.AsImage(115f)!;
    static private readonly MultiImage Battery = NebulaAPI.AddonAsset.GetResource("Battery.png")!.AsMultiImage(3, 1, 150f)!;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        private VisorAdmin PortableAdmin = null!;
        private Transform holder = null!;
        private SpriteRenderer adminIcon = null!;
        private TMPro.TextMeshPro chargeText = null!;
        float AdminTimer;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                AdminTimer = Mathn.Min(PortableAdminCharge, MaxPortableAdminCharge);

                holder = HudContent.InstantiateStretchContent(this, "AdminTimerHud", true, true);
                adminIcon = UnityHelper.CreateObject<SpriteRenderer>("BatteryIconHud", holder, new UnityEngine.Vector3(0f, -0.3f, 0f), LayerExpansion.GetUILayer());
                adminIcon.sprite = Battery.GetSprite(0);

                chargeText = UnityHelper.CreateObject<TextMeshPro>("TimerTextHud", holder.transform, new Vector3(0.7f, -0.3f, -0.1f));
                chargeText.transform.localScale = new Vector3(1.1f, 1.1f, -0.1f);
                chargeText.fontSize = 2f;
                chargeText.fontStyle = FontStyles.Bold;
                chargeText.alignment = TMPro.TextAlignmentOptions.Center;
                chargeText.outlineColor = UnityEngine.Color.black;
                chargeText.outlineWidth = 0.3f;

                UpdateCharge();

                var admin = VisorAdmin.TryAddAndBind(() => !this.IsDeadObject && !this.IsUsurped);
                PortableAdmin = admin;

                var adminButton = NebulaAPI.Modules.AbilityButton(this, alwaysShow: true).BindKey(VirtualKeyInput.Ability).SetLabel("visorU.admin").SetImage(AdminImage);
                adminButton.Visibility = _ => !MyPlayer.IsDead && !AmongUsUtil.MapIsOpen && !ExileController.Instance && (!MeetingHud.Instance || CanUseAdminOnMeetingOption);
                adminButton.Availability = _ => (MyPlayer.CanMove || MeetingHud.Instance) && AdminTimer > 0f;
                adminButton.OnClick = (button) =>
                {
                    PortableAdmin.SetMapActive(true);

                    NebulaManager.Instance.ScheduleDelayAction(() =>
                    {
                        HudManager.Instance.InitMap();
                        MapBehaviour.Instance.ShowNormalMap();
                    });
                };
            }
        }

        // Timerの表示管理
        private float emptyBatteryAnimTimer = 0f;
        private bool emptyBatteryAnimState = false;
        [Local]
        void OnChargeUpdate(GameUpdateEvent ev)
        {
            if (AmongUsUtil.MapIsOpen && PortableAdmin.mapActive)
            {
                if(AdminTimer > 0f) AdminTimer -= ev.DeltaTime;

                if (AdminTimer <= 0f)
                {
                    if (AdminTimer < 0f) AdminTimer = 0f;

                    MapBehaviour.Instance.Close();
                }
            }

            // Spriteを変える
            if (AdminTimer > 0f)
            {
                adminIcon.sprite = Battery.GetSprite(0);

                emptyBatteryAnimTimer = 0f;
                emptyBatteryAnimState = false;
            }
            else
            {
                emptyBatteryAnimTimer += ev.DeltaTime;

                if (emptyBatteryAnimTimer >= 0.65f)
                {
                    emptyBatteryAnimTimer = 0f;
                    emptyBatteryAnimState = !emptyBatteryAnimState;
                }

                adminIcon.sprite = Battery.GetSprite(emptyBatteryAnimState ? 2 : 1);
            }

            UpdateCharge();
        }

        [Local]
        void MapClose(MapCloseEvent ev)
        {
            if (PortableAdmin.mapActive && AdminTimer <= 0f) // 充電切れでマップが閉じた時
            {
                foreach (RoleBehaviour role in RoleManager.Instance.AllRoles)
                {
                    if (role.Role == RoleTypes.Tracker)
                    {
                        SoundManager.Instance.PlaySound(role.gameObject.GetComponent<TrackerRole>().cancelSound, false);
                    }
                }
            }
            
            PortableAdmin.SetMapActive(false);
        }

        void OnTaskCompleteLocal(PlayerTaskCompleteLocalEvent ev)
        {
            AdminTimer = Mathn.Min(MaxPortableAdminCharge, AdminTimer + ChargesPerTasks);
            UpdateCharge();
        }

        [OnlyMyPlayer]
        void OnDead(PlayerDieOrDisconnectEvent ev)
        {
            holder.gameObject.SetActive(false);
        }

        [OnlyMyPlayer]
        void OnRevive(PlayerReviveEvent ev)
        {
            holder.gameObject.SetActive(true);
        }

        private void UpdateCharge()
        {
            chargeText.text = Language.Translate("role.visorU.battery").Replace("%SECOND%", string.Format("{0:f1}", this.AdminTimer));
        }
    }
}

public class VisorModifierU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, IAssignableDocument
{
    private VisorModifierU() : base("visorU", "VSR", VisorU.MyRole.Color, [VisorU.CanMoveWithMapWatchingOption, VisorU.CanUseAdminOnMeetingOption], allocateToImpostor: false)
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [VisorU.MyRole.ConfigurationHolder!]);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    string DefinedAssignable.InternalName => "visorModifierU";
    string DefinedAssignable.GeneralBlurb => (VisorU.MyRole as DefinedAssignable).GeneralBlurb;
    Image? DefinedAssignable.IconImage => VisorU.MyRole.GetRoleIcon();

    int HasAssignmentRoutine.AssignPriority => 2;

    static public VisorModifierU MyRole = new VisorModifierU();

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    bool IAssignableDocument.HasTips => true;

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {

        public Instance(GamePlayer myPlayer) : base(myPlayer)
        {
        }

        DefinedModifier RuntimeModifier.Modifier => MyRole;
        private VisorAdmin PortableAdmin = null!;

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var admin = VisorAdmin.TryAddAndBind(() => !this.IsDeadObject);
                PortableAdmin = admin;
                PortableAdmin.SetMapActive(true);
            }
        }

        [Local]
        void MapOpen(MeetingStartEvent ev)
        {
            MapBehaviour.Instance.Close();
            if (!VisorU.CanUseAdminOnMeetingOption) PortableAdmin.SetMapActive(false);
        }

        [Local]
        void OnMeetingEnd(TaskPhaseStartEvent ev)
        {
            PortableAdmin.SetMapActive(true);
        }
    }
}