using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Utilities;
using Rewired.Internal;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Hori.Scripts.Role.Crewmate;

namespace Hori.Scripts.Role.Modifier;

public class ChickenModifierU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private ChickenModifierU() : base("chickenModifierU", "TNA", new(255, 238, 0), [DeadPercentVent, DeadPercentLadder, DeadPercentZipLine, DeadPercentPlatform, DeadPercentDoor, DeadElectricSecond, DeadNumOfVote, VentRange])
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Chicken.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }


    static public readonly FloatConfiguration DeadPercentVent = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentVent", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentLadder = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentLadder", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentZipLine = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentZipLine", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentPlatform = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentPlatform", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadPercentDoor = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadPercentDoor", (10f, 100f, 10f), 20f, FloatConfigurationDecorator.Percentage);
    static public readonly FloatConfiguration DeadElectricSecond = NebulaAPI.Configurations.Configuration("options.role.chickenU.deadElectricSecond", (10f, 180f, 5f), 40f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration DeadNumOfVote = NebulaAPI.Configurations.Configuration("options.role.admiralU.deadNumOfVote", (1, 10), 2);
    static public readonly FloatConfiguration VentRange = NebulaAPI.Configurations.Configuration("options.role.chickenU.ventRange", (0.25f, 5f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);

    static public ChickenModifierU MyRole = new ChickenModifierU();


    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Chicken.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        bool FrameVent = false;
        float VentDead = DeadPercentVent;
        float LadderDead = DeadPercentLadder;
        float ZipLineDead = DeadPercentZipLine;
        float PlatformDead = DeadPercentPlatform;
        float DoorDead = DeadPercentDoor;
        float Electric = 0;
        private ObjectTracker<Vent>? ventTracker;


        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {

        }

        void Update(GameUpdateEvent ev)
        {
            ventTracker ??= ObjectTrackers.ForVents(this, VentRange, MyPlayer, v => true, Color.yellow, true);
            bool isTrackingNow = ventTracker.CurrentTarget != null;
            if (FrameVent && isTrackingNow)
            {
                FrameVent = isTrackingNow;
                return;
            }
            if (isTrackingNow)
            {
                if (ventTracker.CurrentTarget != null)
                {
                    Vent targetVent = ventTracker.CurrentTarget;
                    float Vent = UnityEngine.Random.Range(1f, 100f);
                    if (Vent <= VentDead)
                    {
                        MyPlayer.Suicide(PlayerState.Suicide, ChickenU.Ability.ChickenVentDead, KillParameter.RemoteKill);
                    }
                }
            }

            FrameVent = isTrackingNow;
        }

        [OnlyMyPlayer]
        void LadderCheck(PlayerClimbLadderEvent ev)
        {
            float Ladder = UnityEngine.Random.Range(1f, 100f);
            if (Ladder <= LadderDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenU.Ability.ChickenFallDead, KillParameter.RemoteKill);
            }
        }

        [OnlyMyPlayer]
        void ZipCheck(PlayerUseZiplineEvent ev)
        {
            float ZipLine = UnityEngine.Random.Range(1f, 100f);
            if (ZipLine <= ZipLineDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenU.Ability.ChickenDoorDead, KillParameter.RemoteKill);
            }
        }

        [OnlyMyPlayer]
        void Platform(PlayerUseMovingPlatformEvent ev)
        {
            float Platform = UnityEngine.Random.Range(1f, 100f);
            if (Platform <= PlatformDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenU.Ability.ChickenDropDead, KillParameter.RemoteKill);
            }
        }

        [OnlyMyPlayer]
        void Door(PlayerBeginMinigameByDoorLocalEvent ev)
        {
            float Door = UnityEngine.Random.Range(1f, 100f);
            if (Door <= DoorDead)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenU.Ability.ChickenDoorDead, KillParameter.RemoteKill);
            }
        }

        void OnVotedForMeLocal(PlayerVotedLocalEvent ev)
        {
            if (ev.Voters.Count >= DeadNumOfVote)
            {
                MyPlayer.Suicide(PlayerState.Suicide, ChickenU.Ability.ChickenWussDead, KillParameter.RemoteKill);
            }
        }

        void update(GameUpdateEvent ev)
        {
            bool ElectricSabo = PlayerTask.PlayerHasTaskOfType<ElectricTask>(PlayerControl.LocalPlayer);
            if (MyPlayer.IsDead) return;
            if (ElectricSabo)
            {
                Electric += Time.deltaTime;
                if (Electric >= DeadElectricSecond)
                {
                    MyPlayer.Suicide(PlayerState.Suicide, ChickenU.Ability.ChickenMessDead, KillParameter.RemoteKill);
                }
            }
            else
            {
                Electric = 0f;
            }
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }
    }
}