using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Impostor;
using Nebula.Utilities;
using System;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Minimap;
using Virial.Events.Player;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Virial;
using Virial.Assignable;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Game;

namespace Hori.Scripts.Abilities;

/*
var ability = new AllVentConnectAbility().Register(this); 
ability.AllVentConnects(true); 
AmOwnerで入れれば使える
*/

public class AllVentConnectAbility : FlexibleLifespan, IGameOperator
{
    private Vent? GetAllVent(string name)
    {
        return ShipStatus.Instance.AllVents.FirstOrDefault(v => v.name == name);
    }

    public void AllVentConnects(bool activate)
    {
        switch (NebulaAPI.AmongUs.MapId)
        {
            case 0:
                //Skeld
                GetAllVent("NavVentNorth")!.Right = activate ? GetAllVent("NavVentSouth") : null;
                GetAllVent("NavVentSouth")!.Right = activate ? GetAllVent("NavVentNorth") : null;

                GetAllVent("ShieldsVent")!.Left = activate ? GetAllVent("WeaponsVent") : null;
                GetAllVent("WeaponsVent")!.Center = activate ? GetAllVent("ShieldsVent") : null;

                GetAllVent("ReactorVent")!.Left = activate ? GetAllVent("UpperReactorVent") : null;
                GetAllVent("UpperReactorVent")!.Left = activate ? GetAllVent("ReactorVent") : null;

                GetAllVent("SecurityVent")!.Center = activate ? GetAllVent("ReactorVent") : null;
                GetAllVent("ReactorVent")!.Center = activate ? GetAllVent("SecurityVent") : null;

                GetAllVent("REngineVent")!.Center = activate ? GetAllVent("LEngineVent") : null;
                GetAllVent("LEngineVent")!.Center = activate ? GetAllVent("REngineVent") : null;

                if (GetAllVent("StorageVent") != null)
                {
                    GetAllVent("AdminVent")!.Center = activate ? GetAllVent("StorageVent") : null;
                    GetAllVent("StorageVent")!.Left = activate ? GetAllVent("ElecVent") : null;
                    GetAllVent("StorageVent")!.Right = activate ? GetAllVent("AdminVent") : null;

                    GetAllVent("StorageVent")!.Center = activate ? GetAllVent("CafeUpperVent") : null;
                }
                else
                {
                    GetAllVent("AdminVent")!.Center = activate ? GetAllVent("MedVent") : null;
                    GetAllVent("MedVent")!.Center = activate ? GetAllVent("AdminVent") : null;
                }

                if (GetAllVent("CafeUpperVent") != null)
                {
                    GetAllVent("CafeUpperVent")!.Left = activate ? GetAllVent("LEngineVent") : null;
                    GetAllVent("LEngineVent")!.Right = activate ? GetAllVent("CafeUpperVent") : null;

                    GetAllVent("CafeUpperVent")!.Center = activate ? GetAllVent("StorageVent") : null;

                    GetAllVent("CafeUpperVent")!.Right = activate ? GetAllVent("WeaponsVent") : null;
                    GetAllVent("WeaponsVent")!.Left = activate ? GetAllVent("CafeUpperVent") : null;
                }
                else
                {
                    GetAllVent("CafeVent")!.Center = activate ? GetAllVent("WeaponsVent") : null;
                    GetAllVent("WeaponsVent")!.Center = activate ? GetAllVent("CafeVent") : null;
                }

                break;
            case 2:
                //Polus
                GetAllVent("CommsVent")!.Center = activate ? GetAllVent("ElecFenceVent") : null;
                GetAllVent("ElecFenceVent")!.Center = activate ? GetAllVent("CommsVent") : null;

                GetAllVent("ElectricalVent")!.Center = activate ? GetAllVent("ElectricBuildingVent") : null;
                GetAllVent("ElectricBuildingVent")!.Center = activate ? GetAllVent("ElectricalVent") : null;

                GetAllVent("ScienceBuildingVent")!.Right = activate ? GetAllVent("BathroomVent") : null;
                GetAllVent("BathroomVent")!.Center = activate ? GetAllVent("ScienceBuildingVent") : null;

                GetAllVent("SouthVent")!.Center = activate ? GetAllVent("OfficeVent") : null;
                GetAllVent("OfficeVent")!.Center = activate ? GetAllVent("SouthVent") : null;

                if (GetAllVent("SpecimenVent") != null)
                {
                    GetAllVent("AdminVent")!.Center = activate ? GetAllVent("SpecimenVent") : null;
                    GetAllVent("SpecimenVent")!.Left = activate ? GetAllVent("AdminVent") : null;

                    GetAllVent("SubBathroomVent")!.Center = activate ? GetAllVent("SpecimenVent") : null;
                    GetAllVent("SpecimenVent")!.Right = activate ? GetAllVent("SubBathroomVent") : null;
                }
                break;
            case 4:
                //Airship
                GetAllVent("VaultVent")!.Right = activate ? GetAllVent("GaproomVent1") : null;
                GetAllVent("GaproomVent1")!.Center = activate ? GetAllVent("VaultVent") : null;

                GetAllVent("EjectionVent")!.Right = activate ? GetAllVent("KitchenVent") : null;
                GetAllVent("KitchenVent")!.Center = activate ? GetAllVent("EjectionVent") : null;

                GetAllVent("HallwayVent1")!.Center = activate ? GetAllVent("HallwayVent2") : null;
                GetAllVent("HallwayVent2")!.Center = activate ? GetAllVent("HallwayVent1") : null;

                GetAllVent("GaproomVent2")!.Center = activate ? GetAllVent("RecordsVent") : null;
                GetAllVent("RecordsVent")!.Center = activate ? GetAllVent("GaproomVent2") : null;

                if (GetAllVent("ElectricalVent") != null)
                {
                    GetAllVent("MeetingVent")!.Left = activate ? GetAllVent("GaproomVent1") : null;

                    GetAllVent("ElectricalVent")!.Left = activate ? GetAllVent("MeetingVent") : null;
                    //GetVent("MeetingVent").Right = activate ? GetVent("ElectricalVent") : null;

                    GetAllVent("ShowersVent")!.Center = activate ? GetAllVent("ElectricalVent") : null;
                    GetAllVent("ElectricalVent")!.Right = activate ? GetAllVent("ShowersVent") : null;
                }
                break;
            case 5:
                //Fungle
                GetAllVent("NorthWestJungleVent")!.Center = activate ? GetAllVent("SouthWestJungleVent") : null;
                GetAllVent("SouthWestJungleVent")!.Center = activate ? GetAllVent("NorthWestJungleVent") : null;

                GetAllVent("NorthEastJungleVent")!.Center = activate ? GetAllVent("SouthEastJungleVent") : null;
                GetAllVent("SouthEastJungleVent")!.Center = activate ? GetAllVent("NorthEastJungleVent") : null;

                GetAllVent("StorageVent")!.Center = activate ? GetAllVent("CommunicationsVent") : null;
                GetAllVent("CommunicationsVent")!.Center = activate ? GetAllVent("StorageVent") : null;

                if (GetAllVent("MiningPitVent") != null && GetAllVent("GemVent") != null)
                {
                    GetAllVent("MiningPitVent")!.Left = activate ? GetAllVent("CommunicationsVent") : null;
                    GetAllVent("CommunicationsVent")!.Center = activate ? GetAllVent("MiningPitVent") : null;

                    GetAllVent("GemVent")!.Right = activate ? GetAllVent("SouthEastJungleVent") : null;
                    GetAllVent("SouthEastJungleVent")!.Right = activate ? GetAllVent("GemVent") : null;
                }
                break;
        }
    }
}