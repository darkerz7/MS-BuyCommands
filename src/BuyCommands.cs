using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;
using Sharp.Modules.LocalizerManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.HookParams;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;
using System.Text.Json;

namespace MS_BuyCommands
{
    public class BuyCommands : IModSharpModule, IGameListener
    {
        public string DisplayName => "BuyCommand";
        public string DisplayAuthor => "DarkerZ[RUS]";

        public BuyCommands(ISharedSystem sharedSystem, string dllPath, string sharpPath, Version version, IConfiguration coreConfiguration, bool hotReload)
        {
            _modSharp = sharedSystem.GetModSharp();
            _modules = sharedSystem.GetSharpModuleManager();
            _clients = sharedSystem.GetClientManager();
            _hooks = sharedSystem.GetHookManager();
            _dllPath = dllPath;
            _logger = sharedSystem.GetLoggerFactory().CreateLogger<BuyCommands>();
            _convars = sharedSystem.GetConVarManager();
        }

        public static IModSharp? _modSharp;
        private static ISharpModuleManager? _modules;
        public static IClientManager? _clients;
        private readonly IHookManager _hooks;
        private readonly string _dllPath;
        private readonly ILogger<BuyCommands> _logger;
        public static IConVarManager? _convars;

        private static IModSharpModuleInterface<ILocalizerManager>? _localizer;
        private static IModSharpModuleInterface<IAdminManager>? _adminManager;
        private bool _AMInit = false;

        static ConfigJSON? cfg = null;
        static readonly PlayerValues[] g_PlayerValues = new PlayerValues[PlayerSlot.MaxPlayerCount];

        public bool Init()
        {
            for (int i = 0; i < g_PlayerValues.Length; i++) g_PlayerValues[i] = new PlayerValues();
            _modSharp!.InstallGameListener(this);
            _hooks.PlayerWeaponCanUse.InstallHookPre(OnPlayerWeaponCanUse);
            _hooks.PlayerCanAcquire.InstallHookPre(OnPlayerCanAcquire);
            return true;
        }

        private void InitializePermissions()
        {
            if (_adminManager?.Instance is not { } adminManager || _AMInit) return;

            try
            {
                var registry = adminManager.GetCommandRegistry(DisplayName);

                registry.RegisterPermissions(["buycommands:reload"]);
                registry.RegisterAdminCommand("buycommands_reload", OnReloadCallback, ["buycommands:reload"]);

                _AMInit = true;
            }
            catch (InvalidOperationException)
            {
                // CommandCenter isn't loaded yet — will retry when it connects.
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to initialize admin permissions.");
            }
        }

        public void PostInit()
        {
            TryResolveAdminManager();
        }

        public void OnLibraryConnected(string name)
        {
            if (name.Equals("Sharp.Modules.AdminManager", StringComparison.OrdinalIgnoreCase)) TryResolveAdminManager();
        }

        public void OnAllModulesLoaded()
        {
            GetLocalizer()?.LoadLocaleFile("BuyCommands");
            TryResolveAdminManager();
        }

        public void Shutdown()
        {
            cfg?.UnregisterCommands();
            cfg = null;
            _modSharp!.RemoveGameListener(this);
            _hooks.PlayerWeaponCanUse.RemoveHookPre(OnPlayerWeaponCanUse);
            _hooks.PlayerCanAcquire.RemoveHookPre(OnPlayerCanAcquire);
        }

        public void OnGameActivate()
        {
            LoadCFGs();
            cfg?.RegisterCommands();
        }

        public void OnGameDeactivate()
        {
            cfg?.UnregisterCommands();
            cfg = null;
        }

        public void OnRoundRestarted()
        {
            for (int i = 0; i < g_PlayerValues.Length; i++)
            {
                g_PlayerValues[i].NumBuyCT.Clear();
                g_PlayerValues[i].NumBuyT.Clear();
            }
        }

        void LoadCFGs()
        {
            if (_modSharp!.GetMapName() is { } mapname)
            {
                LoadCFG($"{Path.Join(_dllPath, $"/maps/{mapname.ToLower()}.json")}");
                if (cfg != null) return;
                else
                {
                    int iIndex = mapname.ToLower().IndexOf('_');
                    if (iIndex > 0)
                    {
                        LoadCFG($"{Path.Join(_dllPath, $"/prefix/{mapname.ToLower()[..iIndex]}.json")}");
                        if (cfg != null) return;
                    }
                }
            }
            LoadCFG($"{Path.Join(_dllPath, "config.json")}");
            if (cfg == null) PrintToConsole($"Config files not found");
        }

        static void LoadCFG(string sConfig)
        {
            if (File.Exists(sConfig))
            {
                try
                {
                    string sData = File.ReadAllText(sConfig);
                    cfg = JsonSerializer.Deserialize<ConfigJSON>(sData);
                    cfg?.Verify();
                    PrintToConsole($"Config loaded from {sConfig}");
                }
                catch
                {
                    cfg = null;
                    PrintToConsole($"Bad Config file ({sConfig})");
                }
            }
        }

        private HookReturnValue<bool> OnPlayerWeaponCanUse(IPlayerWeaponCanUseHookParams @params, HookReturnValue<bool> value)
        {
            if (cfg is { } && string.IsNullOrEmpty(@params.Weapon.HammerId) && cfg.GetWeaponByClassname(@params.Weapon.Classname) is { } weapon)
            {
                if (@params.Pawn.Team == CStrikeTeam.CT && weapon.Restrict_CT || @params.Pawn.Team == CStrikeTeam.TE && weapon.Restrict_T)
                {
                    return new HookReturnValue<bool>(EHookAction.SkipCallReturnOverride);
                }
            }
            
            return default;
        }

        private HookReturnValue<EAcquireResult> OnPlayerCanAcquire(IPlayerCanAcquireHookParams @params, HookReturnValue<EAcquireResult> value)
        {
            if (@params.Method == EAcquireMethod.Buy && cfg is { } && cfg.GetWeaponDefIndex(@params.ItemDefinitionIndex) is { } weapon)
            {
                if (@params.Pawn.Team == CStrikeTeam.CT && weapon.Restrict_CT || @params.Pawn.Team == CStrikeTeam.TE && weapon.Restrict_T)
                {
                    return new HookReturnValue<EAcquireResult>(EHookAction.SkipCallReturnOverride, EAcquireResult.NotAllowedForPurchase);
                }
            }

            return default;
        }

        public static ECommandAction OnBuyCommandC(IGameClient? client, StringCommand command)
        {
            if (client == null) return ECommandAction.Stopped;
            StringCommand newcommand = new(command.CommandName[2..], command.ChatTrigger, command.ArgString);

            return OnBuyCommand(client, newcommand);
        }

        public static ECommandAction OnBuyCommand(IGameClient client, StringCommand command)
        {
            if (!client.IsValid || !client.IsInGame || cfg is not { } || client.GetPlayerController() is not { } player) return ECommandAction.Stopped;

            if (!player.IsAlive || player.Team < CStrikeTeam.TE)
            {
                ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.Alive");
                return ECommandAction.Stopped;
            }
            if (!g_PlayerValues[client.Slot].IsCanBuy())
            {
                ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.Cooldown");
                return ECommandAction.Stopped;
            }

            if (cfg.CoolDownCommand > 0.0f) g_PlayerValues[client.Slot].SetCooldown(cfg.CoolDownCommand);

            if (cfg.GetWeaponByCommandName(command.CommandName) is { } weapon)
            {
                if (player.Team == CStrikeTeam.CT && weapon.Restrict_CT || player.Team == CStrikeTeam.TE && weapon.Restrict_T)
                {
                    ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.Restrict", weapon.EntityName);
                    return ECommandAction.Stopped;
                }

                g_PlayerValues[client.Slot].NumBuyCT.TryAdd(weapon.EntityName, 0);
                g_PlayerValues[client.Slot].NumBuyT.TryAdd(weapon.EntityName, 0);

                g_PlayerValues[client.Slot].NumBuyCT.TryGetValue(weapon.EntityName, out int iBuyNumCT);
                g_PlayerValues[client.Slot].NumBuyT.TryGetValue(weapon.EntityName, out int iBuyNumT);

                if (player.Team == CStrikeTeam.CT && weapon.Limit_CT > 0 && weapon.Limit_CT <= iBuyNumCT || player.Team == CStrikeTeam.TE && weapon.Restrict_T && weapon.Limit_T > 0 && weapon.Limit_T <= iBuyNumT)
                {
                    ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.MaxPurchase", weapon.EntityName);
                    return ECommandAction.Stopped;
                }

                if (player.GetInGameMoneyService() is { } moneyservice)
                {
                    if (player.Team == CStrikeTeam.CT && moneyservice.Account < weapon.Price_CT || player.Team == CStrikeTeam.TE && moneyservice.Account < weapon.Price_T)
                    {
                        ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.NoMoney");
                        return ECommandAction.Stopped;
                    }

                    if (weapon.Slot != GearSlot.Invalid)
                    {
                        while(player.GetPlayerPawn()!.GetWeaponBySlot(weapon.Slot, weapon.SlotIndex) is { } wpn)
                        {
                            if (string.IsNullOrEmpty(wpn.HammerId))
                            {
                                if (weapon.Slot == GearSlot.Grenades)
                                {
                                    ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.HaveNade");
                                    return ECommandAction.Stopped;
                                }
                                player.GetPlayerPawn()!.DropWeapon(wpn);
                            } else
                            {
                                ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.SpecialWeapon");
                                return ECommandAction.Stopped;
                            }
                        }
                    }
                    
                    int iLimit = 0;
                    int iBuyNum = 0;

                    if (player.Team == CStrikeTeam.CT)
                    {
                        g_PlayerValues[client.Slot].NumBuyCT[weapon.EntityName] += 1;
                        moneyservice.Account -= (int)weapon.Price_CT;
                        iLimit = weapon.Limit_CT;
                        iBuyNum = g_PlayerValues[client.Slot].NumBuyCT[weapon.EntityName];
                    }

                    if (player.Team == CStrikeTeam.TE)
                    {
                        g_PlayerValues[client.Slot].NumBuyT[weapon.EntityName] += 1;
                        moneyservice.Account -= (int)weapon.Price_T;
                        iLimit = weapon.Limit_T;
                        iBuyNum = g_PlayerValues[client.Slot].NumBuyT[weapon.EntityName];
                    }

                    player.GetPlayerPawn()?.GiveNamedItem(weapon.EntityName);

                    if (iLimit > 0)
                    {
                        ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.PurchaseWithLimit", weapon.EntityName, iBuyNum, iLimit);
                    }
                    else
                    {
                        ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.Purchase", weapon.EntityName);
                    }
                }
            }

            return ECommandAction.Stopped;
        }

        private void OnReloadCallback(IGameClient? client, StringCommand command)
        {
            cfg?.UnregisterCommands();
            cfg = null;
            LoadCFGs();
            cfg?.RegisterCommands();
            if (client?.GetPlayerController() is { } player) ReplyToCommand(client, player, command.ChatTrigger, "BuyCommands.Reload");
        }

        static void ReplyToCommand(IGameClient client, IPlayerController player, bool bChatTrigger, string sMessage, params object[] arg)
        {
            if (GetLocalizer() is { } lm)
            {
                var localizer = lm.For(client);
                player.Print(bChatTrigger ? HudPrintChannel.Chat : HudPrintChannel.Console, $" {ChatColor.Blue}[{ChatColor.Green}BuyCommands{ChatColor.Blue}] {ChatColor.White} {ReplaceColorTags(localizer.Text(sMessage, arg))}");
            }
        }

        public static void PrintToConsole(string sValue)
        {
            Console.ForegroundColor = (ConsoleColor)8;
            Console.Write("[");
            Console.ForegroundColor = (ConsoleColor)6;
            Console.Write("BuyCommands");
            Console.ForegroundColor = (ConsoleColor)8;
            Console.Write("] ");
            Console.ForegroundColor = (ConsoleColor)3;
            Console.WriteLine(sValue);
            Console.ResetColor();
        }

        public static string ReplaceColorTags(string input)
        {
            for (var i = 0; i < colorPatterns.Length; i++)
                input = input.Replace(colorPatterns[i], colorReplacements[i]);

            return input;
        }
        static readonly string[] colorPatterns =
        [
            "{default}", "{darkred}", "{purple}", "{green}", "{lightgreen}", "{lime}", "{red}", "{grey}",
            "{olive}", "{a}", "{lightblue}", "{blue}", "{d}", "{pink}", "{darkorange}", "{orange}",
            "{white}", "{yellow}", "{magenta}", "{silver}", "{bluegrey}", "{lightred}", "{cyan}", "{gray}"
        ];
        static readonly string[] colorReplacements =
        [
            "\x01", "\x02", "\x03", "\x04", "\x05", "\x06", "\x07", "\x08",
            "\x09", "\x0A", "\x0B", "\x0C", "\x0D", "\x0E", "\x0F", "\x10",
            "\x01", "\x09", "\x0E", "\x0A", "\x0D", "\x0F", "\x03", "\x08"
        ];

        private static ILocalizerManager? GetLocalizer()
        {
            if (_localizer?.Instance is null)
            {
                _localizer = _modules!.GetOptionalSharpModuleInterface<ILocalizerManager>(ILocalizerManager.Identity);
            }
            return _localizer?.Instance;
        }

        private void TryResolveAdminManager()
        {
            if (_adminManager?.Instance is not null)  return;

            _adminManager = _modules!.GetOptionalSharpModuleInterface<IAdminManager>(IAdminManager.Identity);

            if (_adminManager?.Instance is null)
            {
                _logger.LogWarning("AdminManager is not installed. Admin commands will not work.");
                return;
            }

            InitializePermissions();
        }

        int IGameListener.ListenerVersion => IGameListener.ApiVersion;
        int IGameListener.ListenerPriority => 0;
    }
}
