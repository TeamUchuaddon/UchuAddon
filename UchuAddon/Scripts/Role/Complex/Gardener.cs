using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Scripts.Core;
using Hori.Scripts.Role.Crewmate;
using Il2CppInterop.Runtime.Injection;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
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
using Virial;
using Virial.Achievements;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Runtime;
using Virial.Text;
using static Hori.Scripts.Role.Neutral.EpidemicU;
using static Nebula.Roles.Complex.Nexus;
using static Nebula.Roles.Complex.Trapper;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Impostor.Hadar;
using static Nebula.Roles.Impostor.Thurifer;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Complex;


file static class GardenerSystem
{
    private static MultiImage buttonSprite = NebulaAPI.AddonAsset.GetResource("GardenerButton.png")!.AsMultiImage(2, 1, 115f)!;
    static public Image EvilImage => buttonSprite.AsLoader(0);
    static public Image NiceImage => buttonSprite.AsLoader(1);

    private static MultiImage waterSprite = NebulaAPI.AddonAsset.GetResource("GardenerWaterButton.png")!.AsMultiImage(2, 1, 115f)!;
    static public Image WaterEvilImage => waterSprite.AsLoader(0);
    static public Image WaterNiceImage => waterSprite.AsLoader(1);

    
    public static void OnActivated(IUsurpableAbility myRole, bool isEvil, int left, List<Vector2>? myPerkPos = null)
    {
        var plantButton = NebulaAPI.Modules.AbilityButton(myRole, myRole.MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "gardenerU.plant",
            GardenerU.PlantCooldown, "plant", NiceImage, _ => left > 0).SetAsUsurpableButton(myRole);
        if (isEvil) plantButton.SetImage(EvilImage);
        int iconVariation = isEvil ? 0 : 3;
        plantButton.ShowUsesIcon(iconVariation, left.ToString());
        plantButton.OnClick = (button) =>
        {
            var p = PlayerControl.LocalPlayer.transform.position;
            Vector2 pos = new Vector2(p.x, p.y);

            if (isEvil) //人外パークを植える
            {
                NebulaSyncObject.RpcInstantiate(
                ItemSupplier.MyNoncrewmateTag,
                [pos.x, pos.y - 0.25f]);
                TitleUnLock("uchuaddon.evilGardenerU.common1");
                myPerkPos?.Add(pos);
            }
            else
            {
                NebulaSyncObject.RpcInstantiate(
                ItemSupplier.MyAllplayersTag,
                [pos.x, pos.y - 0.25f]);
                TitleUnLock("uchuaddon.niceGardenerU.common1");
            }
            
            plantButton.StartCoolDown();
            left--;
            button.UpdateUsesIcon(left.ToString());

            GameOperatorManager.Instance?.Run(new GardenerPlantEvent(myRole.MyPlayer));
        };

        ObjectTracker<ItemSupplier> flowerTracker = new ObjectTrackerUnityImpl<ItemSupplier, ItemSupplier>(myRole.MyPlayer.VanillaPlayer, 0.8f, () => ModSingleton<ItemSupplierManager>.Instance.allSuppliers, flower => flower.age != 3, flower => !NebulaPhysicsHelpers.AnyShadowBetween(myRole.MyPlayer.TruePosition, flower.Position, out _), f => f, f => [f.Position], f => f.MyRenderer, isEvil? Palette.ImpostorRed : GardenerU.MyNiceRole.UnityColor, true).Register(myRole);
        var waterButton = NebulaAPI.Modules.EffectButton(myRole, myRole.MyPlayer, Virial.Compat.VirtualKeyInput.SecondaryAbility, "gardenerU.water",
            GardenerU.WaterCooldown, GardenerU.WaterDuration, "water", WaterNiceImage, _ => flowerTracker.CurrentTarget != null).SetAsUsurpableButton(myRole);
        if (isEvil) waterButton.SetImage(WaterEvilImage);
        waterButton.OnEffectStart = _ => flowerTracker.KeepAsLongAsPossible = true;
        waterButton.OnEffectEnd = (button) =>
        {
            flowerTracker.KeepAsLongAsPossible = false;
            if (flowerTracker.CurrentTarget == null) return;
            if (MeetingHud.Instance) return;

            if (!button.EffectTimer!.IsProgressing)
            {
                var supplier = flowerTracker.CurrentTarget;
                if (supplier != null)
                {
                    string perkId = "";

                    var perk = ModSingleton<ItemSupplierManager>.Instance?.SelectPerk(supplier.NoncrewmateOnly);

                    if (perk != null) perkId = perk.Id;

                    NebulaManager.Instance.StartCoroutine(CoWaterEffect(supplier, perkId).WrapToIl2Cpp());
                }

                waterButton.StartCoolDown();

                if (isEvil)
                {
                    TitleUnLock("uchuaddon.evilGardenerU.common2");
                }
                else
                {
                    TitleUnLock("uchuaddon.niceGardenerU.common2");
                }

                GameOperatorManager.Instance?.Run(new GardenerWaterEvent(myRole.MyPlayer));
            }
        };
        waterButton.OnUpdate = (button) => 
        {
            if (!button.IsInEffect) return;
            if (flowerTracker.CurrentTarget == null) button.InterruptEffect();
        };

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }

    private static IEnumerator CoWaterEffect(ItemSupplier supplier, string perkId)
    {
        if (supplier == null) yield break;

        int currentAge = supplier.age;

        while (currentAge < 3)
        {
            currentAge++;

            ItemSupplier.RpcUpdateAge.Invoke((supplier.ObjectId, currentAge, currentAge >= 3 ? perkId : ""));

            yield return Effects.Wait(0.065f);
        }
    }
}

[NebulaPreprocess(PreprocessPhase.Roles)]
[NebulaRPCHolder]
public class GardenerU : DefinedSingleAbilityRoleTemplate<IUsurpableAbility>, DefinedRole, IAssignableDocument
{
    private GardenerU(bool isEvil) : base(
    isEvil ? "evilGardenerU" : "niceGardenerU",
    isEvil ? Nebula.Roles.Impostor.Impostor.MyTeam.Color : new(32, 212, 56),
    isEvil ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole,
    isEvil ? Nebula.Roles.Impostor.Impostor.MyTeam : Nebula.Roles.Crewmate.Crewmate.MyTeam,
    [PlantCooldown, NumOfPlant, WaterCooldown, WaterDuration])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        ConfigurationHolder?.ScheduleAddRelated(() => [isEvil ? MyNiceRole.ConfigurationHolder! : MyEvilRole.ConfigurationHolder!]);
        base.ConfigurationHolder!.Illustration = isEvil ? NebulaAPI.AddonAsset.GetResource("RoleImage/EvilGardener.png")!.AsImage(115f) : NebulaAPI.AddonAsset.GetResource("RoleImage/NiceGardener.png")!.AsImage(115f);
    }


    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Gardener.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public bool IsEvil => Category == RoleCategory.ImpostorRole;
    public override IUsurpableAbility CreateAbility(GamePlayer player, int[] arguments) => IsEvil ? new EvilAbility(player, arguments.GetAsBool(0)) : new NiceAbility(player, arguments.GetAsBool(0));

    static internal readonly FloatConfiguration PlantCooldown = NebulaAPI.Configurations.Configuration("options.role.gardenerU.plantCooldown", (5f, 60f, 5f), 15f, FloatConfigurationDecorator.Second);
    static internal readonly IntegerConfiguration NumOfPlant = NebulaAPI.Configurations.Configuration("options.role.gardenerU.numOfPlant", (1, 99), 4);
    static internal readonly FloatConfiguration WaterCooldown = NebulaAPI.Configurations.Configuration("options.role.gardenerU.waterCooldown", (5f, 60f, 5f), 20f, FloatConfigurationDecorator.Second);
    static internal readonly FloatConfiguration WaterDuration = NebulaAPI.Configurations.Configuration("options.role.gardenerU.waterDuration", (1f, 10f, 0.5f), 3f, FloatConfigurationDecorator.Second);

    static public readonly GardenerU MyNiceRole = new(false);
    static public readonly GardenerU MyEvilRole = new(true);

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        if (IsEvil)
        {
            yield return new(GardenerSystem.EvilImage, "role.gardenerU.ability.evil.plant");
            yield return new(GardenerSystem.WaterEvilImage, "role.gardenerU.ability.water");
        }
        else
        {
            yield return new(GardenerSystem.NiceImage, "role.gardenerU.ability.nice.plant");
            yield return new(GardenerSystem.WaterNiceImage, "role.gardenerU.ability.water");
        }
    }

    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "niceGardenerU.common1")
        {
            RelatedRole = GardenerU.MyNiceRole,
            Goal = 15,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.niceGardenerU.common1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "niceGardenerU.common1")
        {
            Goal = 15
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "evilGardenerU.common1")
        {
            RelatedRole = GardenerU.MyEvilRole,
            Goal = 15,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.evilGardenerU.common1"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "evilGardenerU.common1")
        {
            Goal = 15
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "niceGardenerU.common2")
        {
            RelatedRole = GardenerU.MyNiceRole,
            Goal = 10,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.niceGardenerU.common2"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "niceGardenerU.common2")
        {
            Goal = 10
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "evilGardenerU.common2")
        {
            RelatedRole = GardenerU.MyEvilRole,
            Goal = 10,
            Rank = TrophyLevel.Silver,
            RelatedRecord = "uchuaddon.evilGardenerU.common2"
        });
        preprocessor.Titles.Register(new RecordBuilder("uchuaddon", "evilGardenerU.common2")
        {
            Goal = 10
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "niceGardenerU.challenge1")
        {
            RelatedRole = GardenerU.MyNiceRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
        preprocessor.Titles.Register(new TitleBuilder("uchuaddon", "evilGardenerU.challenge1")
        {
            RelatedRole = GardenerU.MyEvilRole,
            Goal = 1,
            Rank = TrophyLevel.Rainbow
        });
    }

    public class NiceAbility : AbstractPlayerUsurpableAbility, IPlayerAbility, IGameOperator
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        int ValueChallengePlant = 0;
        int ValueChallengeWater = 0;

        public NiceAbility(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                GardenerSystem.OnActivated(this, false, NumOfPlant);
            }
        }

        [Local, OnlyMyPlayer]
        void OnValuePlant(GardenerPlantEvent ev)
        {
            ValueChallengePlant++;
        }

        [Local, OnlyMyPlayer]
        void OnValueWater(GardenerWaterEvent ev)
        {
            ValueChallengeWater++;
        }

        [Local, OnlyMyPlayer]
        void OnGameEvent(GameEndEvent ev)
        {
            if (WaterCooldown < 20) return;
            
            if (ValueChallengePlant >= 3 && ValueChallengeWater >= 5)
            {
                TitleUnLock("uchuaddon.niceGardenerU.challenge1");
            }
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }

    public class EvilAbility : AbstractPlayerUsurpableAbility, IPlayerAbility, IGameOperator
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];

        List<Vector2>? MyPerkPos = new List<Vector2>();
        int ValueChallenge = 0;

        public EvilAbility(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                GardenerSystem.OnActivated(this, true, NumOfPlant, MyPerkPos);

                GameOperatorManager.Instance?.Subscribe<PlayerKillPlayerEvent>(ev =>
                {
                    if (ev.Murderer.AmOwner)
                    {
                        if (ValueChallenge < 2) return;

                        Vector2 killPos = ev.Murderer.TruePosition;

                        foreach (var perkPos in MyPerkPos!)
                        {
                            if ((killPos - perkPos).SqrMagnitude <= 2f * 2f)
                            {
                                TitleUnLock("uchuaddon.evilGardenerU.challenge1");
                                break;
                            }
                        }
                    }
                }, this);
            }
        }

        [Local, OnlyMyPlayer]
        void OnValuePlant(GardenerPlantEvent ev)
        {
            ValueChallenge++;
        }

        void TitleUnLock(string id)
        {
            var progress = NebulaAPI.CurrentGame?.GetModule<ITitleHandler>();
            progress.Progress(id);
        }
    }
}

public class GardenerPlantEvent : AbstractPlayerEvent
{
    internal GardenerPlantEvent(Virial.Game.Player player) : base(player) { }
}

public class GardenerWaterEvent : AbstractPlayerEvent
{
    internal GardenerWaterEvent(Virial.Game.Player player) : base(player) { }
}