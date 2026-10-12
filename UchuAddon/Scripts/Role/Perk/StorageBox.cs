using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Impostor;
using Nebula.Roles.Perks;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Impostor.Hadar;
using static Sentry.MeasurementUnit;
using static UnityEngine.ProBuilder.UvUnwrapping;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;


[NebulaRPCHolder]
internal class StorageBoxU : PerkFunctionalInstance
{
    const float CoolDown = 15f;
    static FloatConfiguration BoxDuration = NebulaAPI.Configurations.Configuration("perk.storageBoxU.duration", (5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("storageBoxU", PerkFunctionalDefinition.Category.NoncrewmateOnly, new PerkDefinition("storageBoxU", 3, 25, Virial.Color.ImpostorColor, Virial.Color.ImpostorColor).CooldownText("%CD%", () => CoolDown).DurationText("%D%", () => BoxDuration), (def, instance) => new StorageBoxU(def, instance), [BoxDuration]);

    bool used = false;
    public StorageBoxU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, CoolDown);
        cooldownTimer.Start();
        PerkInstance.BindTimer(cooldownTimer);
    }

    private GameTimer cooldownTimer;

    public override bool HasAction => true;

    
    [NebulaPreprocess(PreprocessPhase.PostRoles)]
    public class StorageBox : NebulaSyncStandardObject, IGameOperator
    {
        public const string MyTag = "StorageBox";
        private static MultiImage BoxImage = NebulaAPI.AddonAsset.GetResource("StorageBox.png")!.AsMultiImage(1, 1, 55f)!;

        public StorageBox(Vector2 pos) : base(pos, ZOption.Just, true, BoxImage.GetSprite(0))
        {
            var colliderObj = UnityHelper.CreateObject<CircleCollider2D>(MapData.NonMapColliderName,null, pos.AsVector3(0f), LayerExpansion.GetShortObjectsLayer() );

            colliderObj.radius = 0.73f;
            colliderObj.isTrigger = false;

            this.BindGameObject(colliderObj.gameObject);
        }

        static StorageBox()
        {
            NebulaSyncObject.RegisterInstantiater(MyTag, (args) => new StorageBox(new Vector2(args[0], args[1])));
        }
    }

    public override void OnClick()
    {
        if (used) return;
        if (cooldownTimer.IsProgressing) return;
        if (MyPlayer.IsDead) return;

        NebulaManager.Instance.StartDelayAction(0.05f, () =>
        {
            var result = NebulaSyncObject.RpcInstantiate(StorageBox.MyTag, [
            PlayerControl.LocalPlayer.transform.localPosition.x,
            PlayerControl.LocalPlayer.transform.localPosition.y -0.2f]);

            var box = result.SyncObject as NebulaSyncStandardObject;
            if (box == null) return;

            used = true;

            NebulaManager.Instance.StartDelayAction(BoxDuration, () =>
            {
                NebulaSyncObject.RpcDestroy(box.ObjectId);
            });
        });
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor((used) ? Color.gray : Color.white);
    }
}