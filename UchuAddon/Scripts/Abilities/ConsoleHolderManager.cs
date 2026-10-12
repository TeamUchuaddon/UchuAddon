using Epic.OnlineServices;
using Hori.Core;
using Hori.Scripts.Core;
using Il2CppInterop.Runtime.Injection;
using Nebula.Map;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Events.Game.Minimap;
using Virial.Helpers;
using static Hori.Scripts.Role.Crewmate.AdmiralU;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using VColor = Virial.Color;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using VVector2 = Virial.Compat.Vector2;
using VVector3 = Virial.Compat.Vector3;

namespace Hori.Scripts.Abilities;

[NebulaPreprocess(PreprocessPhase.PostRoles)]
[NebulaRPCHolder]
internal class ConsoleHolderManager : AbstractModule<Virial.Game.Game>, IGameOperator
{
    static ConsoleHolderManager() => DIManager.Instance.RegisterModule(() => new ConsoleHolderManager());
    private ConsoleHolderManager()
    {
        ModSingleton<ConsoleHolderManager>.Instance = this;
    }
    protected override void OnInjected(Virial.Game.Game container)
    {
        this.Register(container);
    }

    public Dictionary<int, GamePlayer> ConsoleHolders = [];
    private Console? currentLocalHolding = null;

    public void RequestHoldConsole(Console console)
    {
        RpcCheckHoldConsole.Invoke((GamePlayer.LocalPlayer, console.GetInstanceID()));
    }
    public void RequestReleaseConsole(Console console)
    {
        RpcReleaseConsole.Invoke((GamePlayer.LocalPlayer, console.GetInstanceID(), console.transform.GetPositionFast()));
    }
    private void CheckHoldConsole(GamePlayer player, Console console)
    {
        if (ConsoleHolders.ContainsKey(console.GetInstanceID())) return;
        if (ConsoleHolders.Any(entry => entry.Value.PlayerId == player.PlayerId)) return;//同時に1つのコンソールしか掴めない

        RpcHoldConsole.Invoke((player, console.GetInstanceID()));
    }

    private void HoldConsole(GamePlayer player, Console console)
    {
        ConsoleHolders.Add(console.GetInstanceID(), player);
        console.gameObject.SetActive(true);

        foreach (var collider in console.GetComponents<Collider2D>())
        {
            collider.isTrigger = true;
        }

        if (player.AmOwner) currentLocalHolding = console;
    }

    private void CheckAndReleaseConsole(GamePlayer player, Console console, Vector3 pos)
    {
        if (ConsoleHolders.TryGetValue(console.GetInstanceID(), out var holder) && player.PlayerId == holder.PlayerId)
        {
            ConsoleHolders.Remove(console.GetInstanceID());
            console.gameObject.SetActive(true);
            console.transform.position = pos;

            if (player.AmOwner && currentLocalHolding?.GetInstanceID() == console.GetInstanceID()) currentLocalHolding = null;
        }
    }

    public bool AnyoneHolds(Console console) => ConsoleHolders.ContainsKey(console.GetInstanceID());
    static private bool TryGetConsole(int id, [MaybeNullWhen(false)] out Console console) => AmongUsLLImpl.ShipStatusInstance.AllConsoles.Find(c => c.GetInstanceID() == id, out console);
    static private RemoteProcess<(GamePlayer player, int consoleId)> RpcCheckHoldConsole = new("RpcCheckHoldConsole_Uchu", (message, _) =>
    {
        if (Helpers.AmHost(AmongUsLLImpl.LocalPlayer) && TryGetConsole(message.consoleId, out var console)) ModSingleton<ConsoleHolderManager>.Instance.CheckHoldConsole(message.player, console);
    });

    static private RemoteProcess<(GamePlayer player, int consoleId)> RpcHoldConsole = new("RpcHoldConsole_Uchu", (message, _) =>
    {
        if (TryGetConsole(message.consoleId, out var console)) ModSingleton<ConsoleHolderManager>.Instance.HoldConsole(message.player, console);
    });

    static private RemoteProcess<(GamePlayer player, int consoleId, Vector3 pos)> RpcReleaseConsole = new("RpcReleaseConsole_Uchu", (message, _) =>
    {
        if (TryGetConsole(message.consoleId, out var console)) ModSingleton<ConsoleHolderManager>.Instance.CheckAndReleaseConsole(message.player, console, message.pos);
    });

    private bool CheckConsolePosition(VVector2 position)
    {
        var data = MapData.GetCurrentMapData();
        return data.CheckMapArea(position, 0.3f);
    }

    static private RemoteProcess<(GamePlayer player, int consoleId, VVector2 pos)> RpcSyncConsolePos = new("RpcSyncConsolePos_Uchu", (message, _) =>
    {
        if (message.player.AmOwner) return;

        if (TryGetConsole(message.consoleId, out var console))
        {
            VVector2 currentPos = console.transform.GetPositionFast(); // 今のポジション
            VVector2 targetPos = message.pos;
            VVector2 nextPos;

            if (currentPos.Distance(targetPos) < 0.7f)
                nextPos = currentPos + (targetPos - currentPos).Delta(5.5f, 0.02f);
            else
                nextPos = targetPos;

            if (ModSingleton<ConsoleHolderManager>.Instance.CheckConsolePosition(nextPos)) console.transform.position = nextPos.AsUnityVector3(nextPos.y / 1000f + 0.01f);
        }
    });

    float lastShareTime = -10f;

    void OnUpdate(GameHudUpdateEvent ev)
    {
        if (!MeetingHud.Instance.AsBoolFast())
        {
            if (currentLocalHolding != null)
            {
                VVector2 currentPos = currentLocalHolding.transform.GetPositionFast();
                VVector2 targetPos = GamePlayer.LocalPlayer.TruePosition;

                VVector2 nextPos;
                if (currentPos.Distance(targetPos) < 0.7f)
                    nextPos = currentPos + (targetPos - currentPos).Delta(5.5f, 0.02f);
                else
                    nextPos = targetPos;

                if (CheckConsolePosition(nextPos)) currentLocalHolding.transform.position = nextPos.AsUnityVector3(nextPos.y / 1000f + 0.01f);

                if (NebulaGameManager.Instance!.CurrentTime > lastShareTime + 0.2f)
                {
                    lastShareTime = NebulaGameManager.Instance!.CurrentTime;

                    RpcSyncConsolePos.Invoke((GamePlayer.LocalPlayer, currentLocalHolding.GetInstanceID(), GamePlayer.LocalPlayer.TruePosition));
                }

                currentLocalHolding.gameObject.SetActive(!GamePlayer.LocalPlayer.Logic.InVent); // ベントに入ってると見えない
            }
        }
    }

    void OnDead(PlayerDieEvent ev)
    {
        if (ev.Player.AmOwner && currentLocalHolding != null) RequestReleaseConsole(currentLocalHolding);
    }

    void OnMeetingStart(MeetingPreStartEvent ev)
    {
        if (currentLocalHolding != null) RequestReleaseConsole(currentLocalHolding);
    }
}