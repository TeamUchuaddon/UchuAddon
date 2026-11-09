using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
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


namespace Hori.Scripts.Role.Modifier;

public class TimerU : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
    private TimerU() : base("timerU", "TMR", new(3, 252, 194))
    {
        base.ConfigurationHolder!.Illustration = NebulaAPI.AddonAsset.GetResource("RoleImage/Timer.png")!.AsImage(115f);
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Timer.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;
    static public TimerU MyRole = new TimerU();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        private float timer = 0f;
        private bool isRunning = false;
        private TMPro.TextMeshPro? timerHud;

        public Instance(GamePlayer player) : base(player)
        {
            if (AmOwner)
            {
                timerHud = Nebula.Utilities.Helpers.TextHudContent("TimerHUD", this, (tmPro) =>
                {
                    tmPro.text = "";
                }, true);
            }
        }

        void RuntimeAssignable.OnActivated()
        {

        }

        /*void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo)
                name += " ∞".Color(MyRole.RoleColor.ToUnityColor());
        }*/
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo, bool inEndScene)
        {
            if (AmOwner || canSeeAllInfo) name += MyRole.GetRoleIconTagSmall();
        }

        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            isRunning = false;
        }
        [Local]
        void MeetingEnd(MeetingEndEvent ev)
        {
            timer = 0f;
        }

        [Local]
        void TaskPhase(TaskPhaseRestartEvent ev)
        {
            isRunning = true;
        }
        void Update(GameUpdateEvent ev)
        {
            if (timerHud != null)
            {
                string label = NebulaAPI.Language.Translate("role.timerU.TimerTxt");
                timerHud.text = $"{label}: {timer:0.0}";
            }
            if (!isRunning) return;
            timer += Time.deltaTime;
        }
        void GameStart(GameStartEvent ev)
        {
            isRunning = true;
        }
    }
}