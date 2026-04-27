using Hori.Core;
using Hori.Scripts.Role.Modifier;
using Nebula.Configuration;
using Nebula.Modules;
using Nebula.Patches;
using Nebula.Roles;
using Nebula.Roles.Complex;
using Nebula.Roles.Impostor;
using Nebula.Roles.Modifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Attributes;
using Virial.Events.Player;
using static UnityEngine.GraphicsBuffer;

namespace Hori.Scripts.Role.Neutral;

public class EnchanterU : DefinedRoleTemplate, DefinedRole, IAssignableDocument
{
    static readonly public RoleTeam MyTeam = NebulaAPI.Preprocessor!.CreateTeam("teams.enchanter", new(66, 245, 114), TeamRevealType.OnlyMe);

    private EnchanterU() : base("enchanterU", MyTeam.Color, RoleCategory.NeutralRole, MyTeam, [enchantCountOption, enchantCoolDownOption, VentConfiguration, EnchanterGiveableFilter])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Enchanter.png")!.AsImage(115f);
    }

    static List<DefinedModifier> giveableModifiers = Roles.AllModifiers.ToList();
    public static AssignableFilter<DefinedModifier> enchanterGiveableFilter = new ModifierFilterImpl("role.enchanterU.modifierFilter");
    static public List<DefinedModifier> allowedModifiers = new List<DefinedModifier>()
    {
        AlertU.MyRole,
        AutopsyU.MyRole,
        Bloody.MyRole,
        ChickenModifierU.MyRole,
        Confused.MyRole,
        Disclosure.MyRole,
        DrunkU.MyRole,
        ExpressU.MyRole,
        GloomU.MyRole,
        GutsU.MyRole,
        HexU.MyRole,
        HighLowU.MyRole,
        HunchU.MyRole,
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
    };

    static private IntegerConfiguration enchantCountOption = NebulaAPI.Configurations.Configuration("options.role.enchanterU.enchantCount", (1, 10), 5);
    static private FloatConfiguration enchantCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.enchanterU.enchantCoolDown", (0f, 45f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private readonly IConfiguration EnchanterGiveableFilter = NebulaAPI.Configurations.Configuration(() => null, () => NebulaAPI.GUI.LocalizedButton(Virial.Media.GUIAlignment.Center, NebulaAPI.GUI.GetAttribute(Virial.Text.AttributeAsset.OptionsTitleHalf), "options.role.enchanterU.giveableFilter", _ => RoleOptionHelper.OpenFilterScreen<DefinedModifier>("EnchanterGiveableFilter", allowedModifiers, m => enchanterGiveableFilter)));
    static private IVentConfiguration VentConfiguration = NebulaAPI.Configurations.NeutralVentConfiguration("options.role.enchanterU.vent", false);
    static public EnchanterU MyRole = new EnchanterU();

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private GameStatsEntry StatsEnchanted = NebulaAPI.CreateStatsEntry("stats.enchanter.enchanted", GameStatsCategory.Roles, MyRole);
    static private readonly Virial.Media.Image EnchantImage = NebulaAPI.AddonAsset.GetResource("EnchanterButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(EnchantImage, "role.EnchanterU.ability.enchant");
    }

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {

        int leftenchant = enchantCountOption;
        static List<GamePlayer>? EnchanterTargets = new List<GamePlayer>();
        private Dictionary<byte, DefinedModifier> lastEnchantedMod = new Dictionary<byte, DefinedModifier>();


        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var playerTracker = NebulaAPI.Modules.PlayerTracker(this, MyPlayer);
                var EnchantButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    enchantCoolDownOption, "EnchanterU.enchant", EnchantImage, _ => playerTracker.CurrentTarget != null);
                EnchantButton.Visibility = (button) => !MyPlayer.IsDead && leftenchant > 0;
                EnchantButton.ShowUsesIcon(0, leftenchant.ToString());
                EnchantButton.OnClick = (button) =>
                {
                    var target = playerTracker.CurrentTarget;
                    if (target != null)
                    {
                        byte targetId = target.RealPlayer.PlayerId;
                        var allModifiers = allowedModifiers.Where(m => enchanterGiveableFilter.Test(m)).ToList();
                        if (allModifiers.Count == 0) allModifiers = allowedModifiers;
                        if (lastEnchantedMod.ContainsKey(targetId))
                        {
                            target.RealPlayer.RemoveModifier(lastEnchantedMod[targetId]);
                            var filtered = allModifiers.Where(m => m.Id != lastEnchantedMod[targetId].Id).ToList();
                            if (filtered.Count > 0) allModifiers = filtered;
                        }

                        EnchantButton.Visibility = (button) => !MyPlayer.IsDead;
                        var selectedModifier = allModifiers[UnityEngine.Random.Range(0, allModifiers.Count)];
                        lastEnchantedMod[targetId] = selectedModifier;
                        target.RealPlayer.AddModifier(selectedModifier);
                        button.StartCoolDown();
                        StatsEnchanted.Progress();
                        leftenchant--;
                        RpcSyncLeftEnchant.Invoke(MyPlayer);
                        if (leftenchant > 0)
                        {
                            EnchantButton.UpdateUsesIcon(leftenchant.ToString());
                        }
                        else
                        {
                            EnchantButton.HideUsesIcon();
                        }
                    }
                };
            }
        }
        [OnlyHost]
        void CheckEnchanterExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (leftenchant > 0) return;
            ev.SetWin(true);
            ev.ExtraWinMask.Add(UchuGameEnd.EnchanterExtra);
        }
        static RemoteProcess<GamePlayer> RpcSyncLeftEnchant = new("SyncLeftEnchant",
    (message, _) =>
    {
        if (message.Role is Instance instance)
            instance.leftenchant--;
    });
    }
}