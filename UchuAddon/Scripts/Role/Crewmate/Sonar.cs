using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Core;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Impostor;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
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
using static Nebula.Roles.Impostor.Hadar;
using static StarGen;
using Color = Virial.Color;
using Image = Virial.Media.Image;
using Vector2 = Virial.Compat.Vector2;
using Vector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class SonarU : DefinedSingleAbilityRoleTemplate<SonarU.Ability>, DefinedRole,IAssignableDocument
{
    public SonarU() : base("sonarU", new(52, 115, 68), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfPlace,PlaceCooldown,SonarRange,NoiseDuration, DeadPositionNoise, RpcBreakAntenna])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Sonar.png")!.AsImage(115f);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    static private FloatConfiguration PlaceCooldown = NebulaAPI.Configurations.Configuration("options.role.sonarU.placeCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private IntegerConfiguration NumOfPlace = NebulaAPI.Configurations.Configuration("options.role.sonarU.numOfPlace", (1, 10), 2);
    static private FloatConfiguration SonarRange = NebulaAPI.Configurations.Configuration("options.role.sonarU.sonarRange", (0.25f, 10f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);
    static public readonly FloatConfiguration NoiseDuration = NebulaAPI.Configurations.Configuration("options.role.sonarU.noiseDuration", (1f, 10f, 1f), 3f, FloatConfigurationDecorator.Second);
    static public BoolConfiguration DeadPositionNoise = NebulaAPI.Configurations.Configuration("options.role.sonarU.noisePosition", true);
    static public BoolConfiguration RpcBreakAntenna = NebulaAPI.Configurations.Configuration("options.role.sonarU.rpcBreakAntenna", true);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static public SonarU MyRole = new();
    static private readonly GameStatsEntry StatsPlace = NebulaAPI.CreateStatsEntry("stats.sonarU.place", GameStatsCategory.Roles, MyRole);
    static private readonly GameStatsEntry StatsNoise = NebulaAPI.CreateStatsEntry("stats.sonarU.noise", GameStatsCategory.Roles, MyRole);

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class SonarAntenna : NebulaSyncStandardObject, IGameOperator
    {
        public const string MyTag = "SonarAntenna";
        private static MultiImage AntennaImage = NebulaAPI.AddonAsset.GetResource("SonarAntenna.png")!.AsMultiImage(1, 1, 310f)!;
        //private static SpriteLoader AntennaImage = SpriteLoader.FromResource("SonarAntenna.png", 100f);

        public SonarAntenna(Vector2 pos) : base(pos, ZOption.Just, true, AntennaImage.GetSprite(0))
        {
        }
        static SonarAntenna()
        {
            RegisterInstantiater(MyTag, (args) => new SonarAntenna(new Vector2(args[0], args[1])));
        }
    }

    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class SonarAntennaBreak : NebulaSyncStandardObject, IGameOperator
    {
        public const string MyTag = "SonarAntennaBreak";
        private static MultiImage AntennaImage = NebulaAPI.AddonAsset.GetResource("SonarAntennaBreak.png")!.AsMultiImage(1, 1, 310f)!;
        //private static SpriteLoader AntennaImage = SpriteLoader.FromResource("SonarAntenna.png", 100f);

        public SonarAntennaBreak(Vector2 pos) : base(pos, ZOption.Just, true, AntennaImage.GetSprite(0))
        {
        }
        static SonarAntennaBreak()
        {
            RegisterInstantiater(MyTag, (args) => new SonarAntennaBreak(new Vector2(args[0], args[1])));
        }
    }
    static private readonly Virial.Media.Image PlaceImage = NebulaAPI.AddonAsset.GetResource("SonarPlaceButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image AntennaDocument = NebulaAPI.AddonAsset.GetResource("SonarAntennaDocument.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image AntennaDocumentBreak = NebulaAPI.AddonAsset.GetResource("SonarAntennaBreakDocument.png")!.AsImage(115f)!;
    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(PlaceImage, "role.sonarU.ability.place");
        yield return new(AntennaDocument, "role.sonarU.ability.antenna");
        yield return new(AntennaDocumentBreak, "role.sonarU.ability.break");
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Sonar.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {  
        int leftPlace = NumOfPlace;
        List<Vector2> sonarPositions = new();
        List<NebulaSyncStandardObject> sonarAntennas = new();
        Vector2 myPos = GamePlayer.LocalPlayer!.Position;

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var PlaceButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, PlaceCooldown, "sonarU.place", PlaceImage).SetAsUsurpableButton(this);
                PlaceButton.ShowUsesIcon(3, leftPlace.ToString());
                PlaceButton.Visibility = (button) => !MyPlayer.IsDead && leftPlace > 0;
                PlaceButton.OnClick = (button) =>
                {
                    var pos = new Vector2(
                    PlayerControl.LocalPlayer.transform.localPosition.x,
                    PlayerControl.LocalPlayer.transform.localPosition.y - 0.05f);

                    var obj = NebulaSyncObject.LocalInstantiate(SonarAntenna.MyTag,[pos.x, pos.y]).SyncObject as NebulaSyncStandardObject;
                    sonarAntennas.Add(obj!);
                    sonarPositions.Add(pos);

                    leftPlace--;
                    PlaceButton.UpdateUsesIcon(leftPlace.ToString());
                    StatsPlace.Progress();
                    PlaceButton.StartCoolDown();
                };
                /*var TestButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, PlaceCooldown, "sonarU.place", PlaceImage).SetAsUsurpableButton(this);
                TestButton.OnClick = (button) =>
                {
                    RpcSonarNoise.Invoke(myPos);
                };*/
            }
        }

        void SonarCheck(PlayerMurderedEvent ev)
        {
            if (sonarAntennas.Count == 0) return;
            if (MeetingHud.Instance) return;
            if (MyPlayer.IsDead && ev.Dead != MyPlayer) return;

            Vector2 deadPos = ev.Dead.Position;

            foreach (var antenna in sonarAntennas)
            {
                Vector2 antennaPos = antenna.Position; 

                if (antennaPos.Distance(deadPos) <= SonarRange)
                {
                    if (DeadPositionNoise)
                    {
                        RpcSonarNoise.Invoke(deadPos);
                    }
                    else
                    {
                        RpcSonarNoise.Invoke(antennaPos);
                    }
                    StatsNoise.Progress();

                    var target = sonarAntennas.OrderBy(a => a.Position.Distance(deadPos)).First();
                    sonarAntennas.Remove(target);

                    if (RpcBreakAntenna)
                    {
                        NebulaSyncObject.RpcInstantiate(SonarAntennaBreak.MyTag, new float[] { target.Position.x, target.Position.y });
                    }
                    else
                    {
                        NebulaSyncObject.LocalInstantiate(SonarAntennaBreak.MyTag, new float[] { target.Position.x, target.Position.y });
                    }
                    NebulaSyncObject.LocalDestroy(target.ObjectId);
                }
            }
        }

        static RemoteProcess<Vector2> RpcSonarNoise = new("RpcShowSonarNoise",(pos, _) =>
        {
            AmongUsUtil.InstantiateNoisemakerArrow(pos, true, 310f).arrow.SetDuration(NoiseDuration);
        });
    }
}
