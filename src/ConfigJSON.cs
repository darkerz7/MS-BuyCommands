using Sharp.Shared.Enums;

namespace MS_BuyCommands
{
    public class ConfigJSON
    {
        public float CoolDownCommand { get; set; } = 0.0f;
        public Dictionary<string, ConfigWeapon> Weapons { get; set; } = [];
        public void Verify()
        {
            foreach (var weapon in Weapons.ToArray())
            {
                weapon.Value.Verify();
            }

            foreach (var weapon in Weapons.ToArray()) // Only once
            {
                if (weapon.Value.IsValid())
                {
                    foreach (var command in weapon.Value.Commands)
                    {
                        int iCount = 0;
                        foreach (var weapontest in Weapons.ToArray())
                        {
                            if (weapon.Value.IsValid())
                            {
                                foreach (var commandtest in weapontest.Value.Commands)
                                {
                                    if (command.Equals(commandtest, StringComparison.OrdinalIgnoreCase))
                                        iCount++;
                                }
                            }
                        }
                        if (iCount > 1)
                        {
                            BuyCommands.PrintToConsole($"Duplicate commands found in {weapon.Key}:{command}");
                            weapon.Value.SetFalseValid();
                        }
                    }
                }
            }
        }

        public ConfigWeapon? GetWeaponByCommandName(string sCommandName)
        {
            foreach (var weapon in Weapons.ToArray())
            {
                if (weapon.Value.IsValid())
                {
                    foreach (var command in weapon.Value.Commands)
                    {
                        if (sCommandName.Equals(command, StringComparison.OrdinalIgnoreCase)) return weapon.Value;
                    }
                }
            }
            return null;
        }

        public ConfigWeapon? GetWeaponByClassname(string sClassname)
        {
            foreach (var weapon in Weapons.ToArray())
            {
                if (weapon.Value.IsValid())
                {
                    if (sClassname.Equals(weapon.Value.EntityName, StringComparison.OrdinalIgnoreCase)) return weapon.Value;
                }
            }
            return null;
        }

        public ConfigWeapon? GetWeaponDefIndex(int iDefIndex)
        {
            if (iDefIndex > 0)
            {
                foreach (var weapon in Weapons.ToArray())
                {
                    if (weapon.Value.IsValid() && weapon.Value.DefIndex == iDefIndex)
                    {
                        return weapon.Value;
                    }
                }
            }
            return null;
        }

        public void RegisterCommands()
        {
            foreach (var weapon in Weapons.ToArray())
            {
                if (weapon.Value.IsValid())
                {
                    foreach (var command in weapon.Value.Commands)
                    {
                        BuyCommands._clients!.InstallCommandCallback(command, BuyCommands.OnBuyCommand);
                        BuyCommands._convars!.CreateConsoleCommand($"c_{command}", BuyCommands.OnBuyCommandC);
                    }
                }
            }
        }

        public void UnregisterCommands()
        {
            foreach (var weapon in Weapons.ToArray())
            {
                if (weapon.Value.IsValid())
                {
                    foreach (var command in weapon.Value.Commands)
                    {
                        BuyCommands._clients!.RemoveCommandCallback(command, BuyCommands.OnBuyCommand);
                        BuyCommands._convars!.ReleaseCommand($"c_{command}");
                    }
                }
            }
        }
    }

    public class ConfigWeapon
    {
        public List<string> Commands { get; set; } = [];
        public string EntityName { get; set; } = "";
        public uint Price_CT { get; set; } = 0;
        public uint Price_T { get; set; } = 0;
        public byte Limit_CT { get; set; } = 0;
        public byte Limit_T { get; set; } = 0;
        public bool Restrict_CT { get; set; } = false;
        public bool Restrict_T { get; set; } = false;
        
        public GearSlot Slot = GearSlot.ReservedSlot11;
        public int SlotIndex = -1;
        public int DefIndex = 0;

        public bool IsValid()
        {
            return Slot != GearSlot.ReservedSlot11;
        }

        public void SetFalseValid()
        {
            Slot = GearSlot.ReservedSlot11;
        }

        public void Verify()
        {
            bool b = true;
            if (Commands.Count >= 1)
            {
                foreach (var command in Commands)
                {
                    if (string.IsNullOrEmpty(command))
                    {
                        b = false;
                        break;
                    }
                }
            }
            else b = false;

            if (b)
            {
                foreach (var Ent in Arrays.EntValidArray)
                {
                    if (EntityName.Equals(Ent.Key))
                    {
                        Slot = Ent.Value;
                        SetSlotIndex();
                        SetDefIndex();

                        break;
                    }
                }
            }
        }

        private void SetSlotIndex()
        {
            switch (EntityName)
            {
                case "weapon_hegrenade": { SlotIndex = 0; break; }
                case "weapon_flashbang": { SlotIndex = 1; break; }
                case "weapon_smokegrenade": { SlotIndex = 2; break; }
                case "weapon_decoy": { SlotIndex = 3; break; }
                case "weapon_incgrenade": { SlotIndex = 4; break; }
                case "weapon_molotov": { SlotIndex = 4; break; }
                case "weapon_knife": { SlotIndex = 0; break; }
                case "weapon_taser": { SlotIndex = 1; break; }
                default: { SlotIndex = -1; break; }
            }
        }

        private void SetDefIndex()
        {
            foreach (var Ent in Arrays.EntDefIndexArray)
            {
                if (EntityName.Equals(Ent.Key))
                {
                    DefIndex = Ent.Value;
                    break;
                }
            }
        }
    }

    public static class Arrays
    {
        public static readonly Dictionary<string, GearSlot> EntValidArray = new()
        {
            {"weapon_deagle",                   GearSlot.Pistol},
            {"weapon_elite",                    GearSlot.Pistol},
            {"weapon_fiveseven",                GearSlot.Pistol},
            {"weapon_glock",                    GearSlot.Pistol},
            {"weapon_ak47",                     GearSlot.Rifle},
            {"weapon_aug",                      GearSlot.Rifle},
            {"weapon_awp",                      GearSlot.Rifle},
            {"weapon_famas",                    GearSlot.Rifle},
            {"weapon_g3sg1",                    GearSlot.Rifle},
            {"weapon_galilar",                  GearSlot.Rifle},
            {"weapon_m249",                     GearSlot.Rifle},
            {"weapon_m4a1",                     GearSlot.Rifle},
            {"weapon_mac10",                    GearSlot.Rifle},
            {"weapon_p90",                      GearSlot.Rifle},
            {"weapon_mp5sd",                    GearSlot.Rifle},
            {"weapon_ump45",                    GearSlot.Rifle},
            {"weapon_xm1014",                   GearSlot.Rifle},
            {"weapon_bizon",                    GearSlot.Rifle},
            {"weapon_mag7",                     GearSlot.Rifle},
            {"weapon_negev",                    GearSlot.Rifle},
            {"weapon_sawedoff",                 GearSlot.Rifle},
            {"weapon_tec9",                     GearSlot.Rifle},
            {"weapon_taser",                    GearSlot.Knife},
            {"weapon_hkp2000",                  GearSlot.Pistol},
            {"weapon_mp7",                      GearSlot.Rifle},
            {"weapon_mp9",                      GearSlot.Rifle},
            {"weapon_nova",                     GearSlot.Rifle},
            {"weapon_p250",                     GearSlot.Pistol},
            {"weapon_scar20",                   GearSlot.Rifle},
            {"weapon_sg556",                    GearSlot.Rifle},
            {"weapon_ssg08",                    GearSlot.Rifle},
            {"weapon_knifegg",                  GearSlot.Knife},
            {"weapon_knife",                    GearSlot.Knife},
            {"weapon_flashbang",                GearSlot.Grenades},
            {"weapon_hegrenade",                GearSlot.Grenades},
            {"weapon_smokegrenade",             GearSlot.Grenades},
            {"weapon_molotov",                  GearSlot.Grenades},
            {"weapon_decoy",                    GearSlot.Grenades},
            {"weapon_incgrenade",               GearSlot.Grenades},
            {"weapon_c4",                       GearSlot.C4},
            {"weapon_knife_t",                  GearSlot.Knife},
            {"weapon_m4a1_silencer",            GearSlot.Rifle},
            {"weapon_usp_silencer",             GearSlot.Pistol},
            {"weapon_cz75a",                    GearSlot.Pistol},
            {"weapon_revolver",                 GearSlot.Pistol},
            {"weapon_bayonet",                  GearSlot.Knife},
            {"weapon_knife_css",                GearSlot.Knife},
            {"weapon_knife_flip",               GearSlot.Knife},
            {"weapon_knife_gut",                GearSlot.Knife},
            {"weapon_knife_karambit",           GearSlot.Knife},
            {"weapon_knife_m9_bayonet",         GearSlot.Knife},
            {"weapon_knife_tactical",           GearSlot.Knife},
            {"weapon_knife_falchion",           GearSlot.Knife},
            {"weapon_knife_survival_bowie",     GearSlot.Knife},
            {"weapon_knife_butterfly",          GearSlot.Knife},
            {"weapon_knife_push",               GearSlot.Knife},
            {"weapon_knife_cord",               GearSlot.Knife},
            {"weapon_knife_canis",              GearSlot.Knife},
            {"weapon_knife_ursus",              GearSlot.Knife},
            {"weapon_knife_gypsy_jackknife",    GearSlot.Knife},
            {"weapon_knife_outdoor",            GearSlot.Knife},
            {"weapon_knife_stiletto",           GearSlot.Knife},
            {"weapon_knife_widowmaker",         GearSlot.Knife},
            {"weapon_knife_skeleton",           GearSlot.Knife},
            {"weapon_knife_kukri",              GearSlot.Knife},
            {"item_kevlar",                     GearSlot.Invalid},
            {"item_assaultsuit",                GearSlot.Invalid},
            {"item_defuser",                    GearSlot.Invalid},
            {"ammo_50ae",                       GearSlot.Invalid},
        };

        public static readonly Dictionary<string, int> EntDefIndexArray = new()
        {
            {"weapon_deagle",                   1},
            {"weapon_elite",                    2},
            {"weapon_fiveseven",                3},
            {"weapon_glock",                    4},
            {"weapon_ak47",                     7},
            {"weapon_aug",                      8},
            {"weapon_awp",                      9},
            {"weapon_famas",                    10},
            {"weapon_g3sg1",                    11},
            {"weapon_galilar",                  13},
            {"weapon_m249",                     14},
            {"weapon_m4a1",                     16},
            {"weapon_mac10",                    17},
            {"weapon_p90",                      19},
            {"weapon_mp5sd",                    23},
            {"weapon_ump45",                    24},
            {"weapon_xm1014",                   25},
            {"weapon_bizon",                    26},
            {"weapon_mag7",                     27},
            {"weapon_negev",                    28},
            {"weapon_sawedoff",                 29},
            {"weapon_tec9",                     30},
            {"weapon_taser",                    31},
            {"weapon_hkp2000",                  32},
            {"weapon_mp7",                      33},
            {"weapon_mp9",                      34},
            {"weapon_nova",                     35},
            {"weapon_p250",                     36},
            {"weapon_scar20",                   38},
            {"weapon_sg556",                    39},
            {"weapon_ssg08",                    40},
            {"weapon_knifegg",                  41},
            {"weapon_knife",                    42},
            {"weapon_flashbang",                43},
            {"weapon_hegrenade",                44},
            {"weapon_smokegrenade",             45},
            {"weapon_molotov",                  46},
            {"weapon_decoy",                    47},
            {"weapon_incgrenade",               48},
            {"weapon_c4",                       49},
            {"weapon_knife_t",                  59},
            {"weapon_m4a1_silencer",            60},
            {"weapon_usp_silencer",             61},
            {"weapon_cz75a",                    63},
            {"weapon_revolver",                 64},
            {"weapon_bayonet",                  500},
            {"weapon_knife_css",                503},
            {"weapon_knife_flip",               505},
            {"weapon_knife_gut",                506},
            {"weapon_knife_karambit",           507},
            {"weapon_knife_m9_bayonet",         508},
            {"weapon_knife_tactical",           509},
            {"weapon_knife_falchion",           512},
            {"weapon_knife_survival_bowie",     514},
            {"weapon_knife_butterfly",          515},
            {"weapon_knife_push",               516},
            {"weapon_knife_cord",               517},
            {"weapon_knife_canis",              518},
            {"weapon_knife_ursus",              519},
            {"weapon_knife_gypsy_jackknife",    520},
            {"weapon_knife_outdoor",            521},
            {"weapon_knife_stiletto",           522},
            {"weapon_knife_widowmaker",         523},
            {"weapon_knife_skeleton",           525},
            {"weapon_knife_kukri",              526},
            {"item_kevlar",                     0},
            {"item_assaultsuit",                0},
            {"item_defuser",                    0},
            {"ammo_50ae",                       0},
        };
    }
}
