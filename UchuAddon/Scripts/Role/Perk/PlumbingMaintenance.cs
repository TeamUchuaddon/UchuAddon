using Hori.Scripts.Role.Neutral;
using Nebula.Map;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Helpers;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class PlumbingMaintenanceU : PerkFunctionalInstance
{
    static private float Cooldown => CooldownOption;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("Perk/Front1.png")!.AsImage(100f)!;

    static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("perk.plumbingMaintenanceU.cooldown", (5f, 30f, 5f), 15f, FloatConfigurationDecorator.Second);
    static PerkFunctionalDefinition Def = new("plumbingMaintenanceU", PerkFunctionalDefinition.Category.Standard,
        new PerkDefinition("plumbingMaintenanceU", PerkDefinition.GetPerkBackIcon(4), IconImage, ObolusU.MyTeam.Color.ToUnityColor(),null).
        CooldownText("%CD%", () => Cooldown), (def, instance) => new PlumbingMaintenanceU (def, instance), [CooldownOption]);

    private GameTimer cooldownTimer;

    public PlumbingMaintenanceU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }

    public override bool HasAction => true;
    public override void OnClick()
    {
        if (cooldownTimer.IsProgressing) return;

        RpcPlumbingUchu.Invoke();

        cooldownTimer.Start();
    }

    void OnUpdate(GameUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor(!cooldownTimer.IsProgressing ? UnityEngine.Color.white : UnityEngine.Color.gray);
    }

    static private Virial.Media.Image PlumbingIcon = NebulaAPI.AddonAsset.GetResource("PlumbingIcon.png")!.AsImage(200f)!;
    public static RemoteProcess RpcPlumbingUchu = new("RpcPlumbing_Uchu", (_) =>
    {
        if (PlayerControl.LocalPlayer.inVent)
        {
            var arrow = new Arrow(PlumbingIcon.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true, OnJustPoint = true };
            arrow.Register(arrow);
            arrow.TargetPos = PlayerControl.LocalPlayer.transform.position;
            NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
        }

        PlayerControl.LocalPlayer.MyPhysics.ExitAllVents();
    });
}


/*
    public static RemoteProcess<(int ventId, byte plumberId)> RpcFlushUchu = new("RpcFlush_Uchu", (message, _) =>
    {
        if (!PlayerControl.LocalPlayer.inVent) return;

        int VentId = message.ventId;
        byte PlumberId = message.plumberId;

        var player = PlayerControl.LocalPlayer;

        switch (AmongUsUtil.CurrentMapId)
        {
            case 0:
                if (VentId == 0 || VentId == 1 || VentId == 2)
                {
                    if (Vent.currentVent.Id == 0) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(0);
                    if (Vent.currentVent.Id == 1) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(1);
                    if (Vent.currentVent.Id == 2) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(2);
                }
                else if (VentId == 3 || VentId == 5 || VentId == 6)
                {
                    if (Vent.currentVent.Id == 3) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(3);
                    if (Vent.currentVent.Id == 5) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(5);
                    if (Vent.currentVent.Id == 6) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(6);
                }
                else if (VentId == 4 || VentId == 11)
                {
                    if (Vent.currentVent.Id == 4) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(4);
                    if (Vent.currentVent.Id == 11) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(11);
                }
                else if (VentId == 8 || VentId == 9)
                {
                    if (Vent.currentVent.Id == 8) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(8);
                    if (Vent.currentVent.Id == 9) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(9);
                }
                else if (VentId == 7 || VentId == 12)
                {
                    if (Vent.currentVent.Id == 7) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(7);
                    if (Vent.currentVent.Id == 12) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(12);
                }
                else if (VentId == 10 || VentId == 13)
                {
                    if (Vent.currentVent.Id == 10) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(10);
                    if (Vent.currentVent.Id == 13) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(13);
                }
                if (GetAllVent("CafeUpperVent") != null)
                {
                    if (Vent.currentVent.Id == 14) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(14);
                }
                if (GetAllVent("StorageVent") != null)
                {
                    if (Vent.currentVent.Id == 15) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(15);
                }
                break;

            case 1: //Miraは全ベント接続なので全部
                PlayerControl.LocalPlayer.MyPhysics.ExitAllVents();
                break;

            case 2:
                if (VentId == 0 || VentId == 1 || VentId == 2)
                {
                    if (Vent.currentVent.Id == 0) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(0);
                    if (Vent.currentVent.Id == 1) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(1);
                    if (Vent.currentVent.Id == 2) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(2);
                }
                else if (VentId == 3 || VentId == 4 || VentId == 8)
                {
                    if (Vent.currentVent.Id == 3) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(3);
                    if (Vent.currentVent.Id == 4) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(4);
                    if (Vent.currentVent.Id == 8) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(8);
                }
                else if (VentId == 5 || VentId == 6 || VentId == 7 || VentId == 11)
                {
                    if (Vent.currentVent.Id == 5) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(5);
                    if (Vent.currentVent.Id == 6) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(6);
                    if (Vent.currentVent.Id == 7) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(7);
                    if (Vent.currentVent.Id == 11) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(11);
                }
                else if (VentId == 9 || VentId == 10)
                {
                    if (Vent.currentVent.Id == 9) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(9);
                    if (Vent.currentVent.Id == 10) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(10);
                }
                if (GetAllVent("SpecimenVent") != null)
                {
                    if (Vent.currentVent.Id == 12) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(12);
                }
                break;

            case 4: //ドレスク飛ばしてエアシップ
                if (VentId == 0 || VentId == 1 || VentId == 2)
                {
                    if (Vent.currentVent.Id == 0) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(0);
                    if (Vent.currentVent.Id == 1) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(1);
                    if (Vent.currentVent.Id == 2) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(2);
                }
                else if (VentId == 3 || VentId == 4 || VentId == 5)
                {
                    if (Vent.currentVent.Id == 3) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(3);
                    if (Vent.currentVent.Id == 4) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(4);
                    if (Vent.currentVent.Id == 5) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(5);
                }
                else if (VentId == 6 || VentId == 7 || VentId == 8)
                {
                    if (Vent.currentVent.Id == 6) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(6);
                    if (Vent.currentVent.Id == 7) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(7);
                    if (Vent.currentVent.Id == 8) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(8);
                }
                else if (VentId == 9 || VentId == 10 || VentId == 11)
                {
                    if (Vent.currentVent.Id == 9) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(9);
                    if (Vent.currentVent.Id == 10) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(10);
                    if (Vent.currentVent.Id == 11) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(11);
                }
                if (GetAllVent("MeetingVent") != null)
                {
                    if (Vent.currentVent.Id == 12) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(12);
                }
                if (GetAllVent("ElectricalVent") != null)
                {
                    if (Vent.currentVent.Id == 13) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(13);
                }
                break;

            case 5:
                if (VentId == 3 || VentId == 4 || VentId == 8 || VentId == 9)
                {
                    if (Vent.currentVent.Id == 3) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(3);
                    if (Vent.currentVent.Id == 4) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(4);
                    if (Vent.currentVent.Id == 8) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(8);
                    if (Vent.currentVent.Id == 9) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(9);
                }
                else if (VentId == 1 || VentId == 6 || VentId == 7)
                {
                    if (Vent.currentVent.Id == 1) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(1);
                    if (Vent.currentVent.Id == 6) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(6);
                    if (Vent.currentVent.Id == 7) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(7);
                }
                else if (VentId == 5 || VentId == 0 || VentId == 2)
                {
                    if (Vent.currentVent.Id == 5) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(5);
                    if (Vent.currentVent.Id == 0) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(0);
                    if (Vent.currentVent.Id == 2) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(2);
                }
                if (GetAllVent("MiningPitVent") != null && GetAllVent("GemVent") != null)
                {
                    if (VentId == 10 || VentId == 11)
                    {
                        if (Vent.currentVent.Id == 10) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(10);
                        if (Vent.currentVent.Id == 11) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(11);
                    }
                }
                break;
        }
        //RpcFlashPlumber.Invoke(PlumberId);
    });

    static private Vent? GetAllVent(string name)
    {
        return ShipStatus.Instance.AllVents.FirstOrDefault(v => v.name == name);
    }
*/