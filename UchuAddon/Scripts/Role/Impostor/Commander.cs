using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hazel.Dtls;
using Hori.Core;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Runtime.Remoting.Messaging;
using MS.Internal.Xml.XPath;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Abilities;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Impostor;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Newtonsoft.Json.Bson;
using PowerTools;
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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Virial;
using Virial;
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Components;
using Virial.Configuration;
using Virial.Configuration;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Game;
using Virial.Helpers;
using Virial.Media;
using Virial.Text;
using static Nebula.Modules.ScriptComponents.NebulaSyncStandardObject;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Impostor.Thurifer;
using static Nebula.Roles.Impostor.Whammy;
using static Rewired.Utils.Classes.Data.SerializedObject;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Impostor;

public class CommanderU : DefinedSingleAbilityRoleTemplate<CommanderU.Ability>, DefinedRole, IAssignableDocument
{
    public CommanderU() : base("commanderU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [NumOfCommand,CommandCooldown,CommandKillCooldown,CommandEndedDuration,KillButtonLostTime])
    {
    }

    static private IntegerConfiguration NumOfCommand = NebulaAPI.Configurations.Configuration("options.role.commanderU.numOfCommand", (1, 15), 2);
    static private FloatConfiguration CommandCooldown = NebulaAPI.Configurations.Configuration("options.role.commanderU.commandCooldown", (2.5f, 60f, 2.5f), 30f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration CommandKillCooldown = NebulaAPI.Configurations.Configuration("options.role.commanderU.commandKillCooldown", (0f, 15f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration CommandEndedDuration = NebulaAPI.Configurations.Configuration("options.role.commanderU.commandDuration", (2.5f, 30f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration KillButtonLostTime = NebulaAPI.Configurations.Configuration("options.role.commanderU.killButtonLostTime", (5f, 120f, 5f), 30f, FloatConfigurationDecorator.Second);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));


    static public CommanderU MyRole = new();

    static private readonly Virial.Media.Image CommandImage = NebulaAPI.AddonAsset.GetResource("CommanderCommandButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(CommandImage, "role.commanderU.ability.command");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new("%COOL%", CommandKillCooldown.GetValue().ToString());
        yield return new("%LOST%", KillButtonLostTime.GetValue().ToString());
    }

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Commander.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private Virial.Media.Image CutinImageAd1 = NebulaAPI.AddonAsset.GetResource("CommanderCutin_Ad1.png")!.AsImage(100f)!;
        static private Virial.Media.Image CutinImageAd2 = NebulaAPI.AddonAsset.GetResource("CommanderCutin_Ad2.png")!.AsImage(100f)!;
        static SpriteRenderer? commandCutinAd1;
        static SpriteRenderer? commandCutinAd2;

        bool Command = false;
        int leftCommand = NumOfCommand;
        static List<GamePlayer> ImpostorPlayers = new List<GamePlayer>();
        static List<GamePlayer> MadmatePlayers = new List<GamePlayer>();

        List<TrackingArrowAbility> activeArrows = new();
        private TMPro.TextMeshPro CommandTxt = null!;
        static bool KillLost = false;


        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var commandButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, CommandCooldown, "commanderU.command", CommandImage).SetAsUsurpableButton(this);
                commandButton.EffectTimer = NebulaAPI.Modules.Timer(this, CommandEndedDuration);
                commandButton.Availability = (button) => MyPlayer.CanMove;
                commandButton.Visibility = (button) => !MyPlayer.IsDead && leftCommand > 0;
                commandButton.OnClick = (button) => button.StartEffect();
                commandButton.OnEffectStart = (button) =>
                {
                    //RpcAllImpostor.Invoke(MyPlayer);
                    RpcCooldown.Invoke(MyPlayer);

                    leftCommand--;
                    Command = true;
                    commandButton.UpdateUsesIcon(leftCommand.ToString());

                    if (MyPlayer.IsImpostor)
                    {
                        foreach (var p in ImpostorPlayers.Where(p => !p.AmOwner && !p.IsDead))
                        {
                            if (!activeArrows.Any(a => a.MyPlayer == p))
                                activeArrows.Add(new TrackingArrowAbility(p.Unbox(), 0f, Palette.ImpostorRed).Register(this));
                        }
                    }
                };

                commandButton.OnEffectEnd = (button) =>
                {
                    Command = false;
                    RpcKillLost.Invoke(MyPlayer);
                    commandButton.StartCoolDown();

                    foreach (var arrow in activeArrows) arrow.Release();
                    activeArrows.Clear();
                };
                commandButton.ShowUsesIcon(0, leftCommand.ToString());

            }
        }

        [Local]
        void OnPlayerDead(PlayerDieOrDisconnectEvent ev)
        {
            activeArrows.RemoveAll(a => { if (a.MyPlayer == ev.Player) { a.Release(); return true; } else return false; });
        }

        RemoteProcess<GamePlayer> RpcCooldown = new("RpcCooldown", (message, _) =>
        {
            var imp = GamePlayer.LocalPlayer!;
            if (!(imp.IsImpostor || imp.IsMadmate)) return;

            NebulaAPI.CurrentGame?.KillButtonLikeHandler.SetCooldown(CommandKillCooldown);
            NebulaAsset.PlaySE(NebulaAudioClip.Justice1, false);
            NebulaManager.Instance.StartCoroutine(CoCommanderCutin(imp).WrapToIl2Cpp());
            NebulaManager.Instance.StartCoroutine(FlashRoutine().WrapToIl2Cpp());
            NebulaManager.Instance.StartCoroutine(CommandTimer().WrapToIl2Cpp());

        });

        RemoteProcess<GamePlayer> RpcKillLost = new("RpcKillLost", (message, _) =>
        {
            var imp = GamePlayer.LocalPlayer!;
            if (!imp.IsImpostor) return;

            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
            imp.GainAttribute(PlayerAttributes.CooldownSpeed, KillButtonLostTime, 0, false, 1, "commander::stopcooldown");
            NebulaManager.Instance.StartCoroutine(KillLostTime().WrapToIl2Cpp());
        });

        static IEnumerator FlashRoutine()
        {
            for (int i = 0; i < 3; i++)
            {
                AmongUsUtil.PlayFlash(Color.red);
                yield return new WaitForSeconds(1.56f);   
            }
        }

        static IEnumerator CoCommanderCutin(GamePlayer player)
        {
            var commanderCutinHolder = UnityHelper.CreateObject("CommanderCutinHolder", HudManager.Instance.transform, Vector3.zero);

            var cutinBG = UnityHelper.CreateObject<SpriteRenderer>("BackGround", commanderCutinHolder.transform, new Vector3(0f, 0f, -70f));
            cutinBG.maskInteraction = UnityEngine.SpriteMaskInteraction.VisibleInsideMask;
            cutinBG.sprite = NebulaAPI.AddonAsset.GetResource("CommanderCutin.png")!.AsImage(50f)!.GetSprite();
            cutinBG.transform.localScale = new Vector3(1.1106f, 1.1106f, 1f);
            cutinBG.color = new UnityEngine.Color(1f, 1f, 1f, 0.95f);

            var cutinMask = UnityHelper.CreateObject<SpriteMask>("CutinMask", commanderCutinHolder.transform, new Vector3(-10.662f, 0f, -70f));
            cutinMask.sprite = NebulaAPI.AddonAsset.GetResource("CommanderCutinMask.png")!.AsImage(50f)!.GetSprite();
            cutinMask.transform.localScale = new Vector3(1.1106f, 1.1106f, 1f);

            for (float t = 0f; t < 10.662; t += Time.deltaTime * 90f)
            {
                cutinMask.transform.localPosition = new Vector3(-11f + t, 0f, 0f);
                yield return null;
            }
            cutinBG.maskInteraction = SpriteMaskInteraction.None;
            GameObject.Destroy(cutinMask.gameObject);

            yield return new WaitForSeconds(0.1f);

            var cutinImpostor = UnityHelper.CreateObject<SpriteRenderer>("CutinImpostor", commanderCutinHolder.transform, new Vector3(0f, 0f, -70f));
            cutinImpostor.sprite = NebulaAPI.AddonAsset.GetResource("CommanderCutinImpostor.png")!.AsImage(50f)!.GetSprite();
            cutinImpostor.transform.localScale = new Vector3(3.5f, 3.5f, 1);
            cutinImpostor.material = HatManager.Instance.PlayerMaterial;
            PlayerMaterial.SetColors(player.CurrentOutfit.outfit.ColorId, cutinImpostor);

            for (float t = 0f; t < 2.3894f; t += Time.deltaTime * 13)
            {
                cutinImpostor.transform.localScale = new Vector3(3.5f - t, 3.5f - t, 1);
                yield return null;
            }
            cutinImpostor.transform.localScale = new Vector3(1.1106f, 1.1106f, 1f);

            yield return new WaitForSeconds(0.2f);

            cutinBG.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            cutinImpostor.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            var cutinExitMask1 = UnityHelper.CreateObject<SpriteMask>("CutinExitMask1", commanderCutinHolder.transform, new Vector3(0f, 4.33f, -75f));
            var cutinExitMask2 = UnityHelper.CreateObject<SpriteMask>("CutinExitMask2", commanderCutinHolder.transform, new Vector3(0f, -3.85f, -75f));
            cutinExitMask1.sprite = NebulaAPI.AddonAsset.GetResource("CommanderCutinMask.png")!.AsImage(50f)!.GetSprite();
            cutinExitMask2.sprite = NebulaAPI.AddonAsset.GetResource("CommanderCutinMask.png")!.AsImage(50f)!.GetSprite();
            cutinExitMask1.transform.localScale = new Vector3(1.1106f, 1.1106f, 1f);
            cutinExitMask2.transform.localScale = new Vector3(1.1106f, 1.1106f, 1f);

            for (float t = 0f; t < 4.33; t += Time.deltaTime * 10)
            {
                cutinExitMask1.transform.localPosition = new Vector3(0f, 4.33f - t, -75f);
                cutinExitMask2.transform.localPosition = new Vector3(0f, -3.85f + t, -75f);
                yield return null;
            }

            GameObject.Destroy(commanderCutinHolder.gameObject);
        }

        static IEnumerator CommandTimer()
        {
            var CommandTxt = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, HudManager.Instance.transform);
            new TextAttributeOld(TextAttributeOld.NormalAttr) { Size = new Vector2(5f, 0.9f) }.EditFontSize(2.7f, 2.7f, 2.7f).Reflect(CommandTxt);
            CommandTxt.transform.localPosition = new Vector3(0f, -1.7f, -4f);
            CommandTxt.gameObject.SetActive(true);

            float timer = CommandEndedDuration;

            while (timer > 0f)
            {
                CommandTxt.color = Palette.ImpostorRed;
                CommandTxt.text =
                    Language.Translate("role.commander.CommandText")+ $" {Mathf.CeilToInt(timer)}";

                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }

            if (CommandTxt != null)
            {
                GameObject.Destroy(CommandTxt.gameObject);
            }
        }

        static IEnumerator KillLostTime()
        {
            var CommandEndTxt = GameObject.Instantiate(VanillaAsset.StandardTextPrefab, HudManager.Instance.transform);
            new TextAttributeOld(TextAttributeOld.NormalAttr){Size = new Vector2(5f, 0.9f)}.EditFontSize(2.7f, 2.7f, 2.7f).Reflect(CommandEndTxt);
            CommandEndTxt.transform.localPosition = new Vector3(0f, -1.7f, -4f);
            CommandEndTxt.gameObject.SetActive(true);


            float Losttimer = KillButtonLostTime;

            KillLost = true;

            while (Losttimer > 0f)
            {
                CommandEndTxt.color = Palette.ImpostorRed;
                CommandEndTxt.text =
                    Language.Translate("role.commander.CommandEndText") + $" {Mathf.CeilToInt(Losttimer)}";

                yield return new WaitForSeconds(1f);
                Losttimer -= 1f;
            }

            if (CommandEndTxt != null)
            {
                GameObject.Destroy(CommandEndTxt.gameObject);
                KillLost = false;
            }
        }


    }
}