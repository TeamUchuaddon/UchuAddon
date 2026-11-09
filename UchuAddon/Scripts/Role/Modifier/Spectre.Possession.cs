using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hazel;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Assignment;
using Nebula.Roles.Complex;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine;
using Virial;
using Virial;
using Virial;
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Compat;
using Virial.Components;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Game;
using Virial.Game;
using Virial.Helpers;
using Virial.Text;
using Virial.Text;
using Virial.Utilities;
using static Nebula.Roles.Impostor.Cannon;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Modifier;


public class SpectrePossessionU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private SpectrePossessionU() : base("spectrePossessionU", "HUT", new(185, 152, 197),[Clairvoyance,
        new GroupConfiguration("options.role.spectrePossessionU.group.fanaticism", [Fanaticism,FanaticismLimit,NumOfTaskFanaticism,NumOfKillFanaticism], new UnityEngine.Color(0.7255f, 0.5961f, 0.7725f)),],  
        allocateToNeutral: false)
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/SpectrePossession.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }

    static private BoolConfiguration Clairvoyance = NebulaAPI.Configurations.Configuration("options.role.spectre.clairvoyance", true);
    //static private BoolConfiguration ShowDishesOnMap =NebulaAPI.Configurations.Configuration("options.role.spectreImmoralist.showDishesOnMap",true);
    static private BoolConfiguration Fanaticism = NebulaAPI.Configurations.Configuration("options.role.spectrePossessionU.fanaticism", true);
    static private BoolConfiguration FanaticismLimit = NebulaAPI.Configurations.Configuration("options.role.spectrePossessionU.fanaticismLimit", false,()=> Fanaticism);
    static private IntegerConfiguration NumOfTaskFanaticism = NebulaAPI.Configurations.Configuration("options.role.spectrePossessionU.numOfTaskFanaticism", (1, 10), 4, ()=> FanaticismLimit);
    static private IntegerConfiguration NumOfKillFanaticism = NebulaAPI.Configurations.Configuration("options.role.spectrePossessionU.numOfKillFanaticism", (1, 10), 1, () => FanaticismLimit);

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Spectre.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public SpectrePossessionU MyRole = new SpectrePossessionU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        string? RuntimeModifier.DisplayIntroBlurb=> Language.Translate("role.spectrePossessionU.blurb");
        public bool EyesightIgnoreWalls { get; private set; } = Clairvoyance;
        bool fanaticism = !FanaticismLimit;
        int leftKill = 0;
        static List<GamePlayer> spectreName = new List<GamePlayer>();

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {

            }
        }

        /*void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo)
                name += " ＊".Color(MyRole.RoleColor.ToUnityColor());
        }*/
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        [OnlyMyPlayer]
        void OnTaskUpdate(PlayerTaskUpdateEvent ev)
        {
            if (!MyPlayer.IsCrewmate) return;
            if (MyPlayer.Tasks.CurrentTasks == 0) return;
            if (MyPlayer.Tasks.CurrentCompleted >= NumOfTaskFanaticism)
            {
                fanaticism = true;
            }
        }

        [OnlyMyPlayer]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (!MyPlayer.IsImpostor) return;
            leftKill++;
            if (leftKill >= NumOfKillFanaticism)
            {
                fanaticism = true;
            }
        }

        void DecorateFollowersColor(PlayerDecorateNameEvent ev)
        {
            if (AmOwner)
            {
                if (!fanaticism) return;
                if (!spectreName.Contains(ev.Player)) return;
                ev.Color = MyRole.RoleColor;
            }
        }

        void List(GameStartEvent ev)
        {
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (p.Role.Role == Spectre.MyRole)
                {
                    spectreName.Add(p);
                }
            }
        }


        /*DishMapLayer? mapLayer = null;
        [Local]
        void OnOpenNormalMap(MapOpenNormalEvent ev)
        {
            if (!ShowDishesOnMap) return;

            if (mapLayer is null)
            {
                mapLayer = UnityHelper.CreateObject<DishMapLayer>("DishLayer", MapBehaviour.Instance.transform, new(0, 0, -1f));
                this.BindGameObject(mapLayer.gameObject);
            }
            mapLayer.gameObject.SetActive(true);
        }*/



        [OnlyMyPlayer]
        void BlockWins(PlayerBlockWinEvent ev) => ev.IsBlocked |= ev.GameEnd != NebulaGameEnd.SpectreWin;
        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev) => ev.SetWinIf(ev.GameEnd == NebulaGameEnd.SpectreWin);
        bool RuntimeAssignable.MyCrewmateTaskIsIgnored => true;
    }
}