# MS-BuyCommands for ModSharp
Purchase weapon commands and restrict

## Required packages:
1. [ModSharp](https://github.com/Kxnrl/modsharp-public)
2. [LocalizerManager](https://github.com/Kxnrl/modsharp-public/tree/master/Sharp.Modules/LocalizerManager)

## Installation:
1. Install `LocalizerManager`
2. Compile or copy MS-BuyCommands to `sharp/modules/MS-BuyCommands` folger
3. Copy BuyCommands.json to `sharp/locales` folger
4. Configure config files `config.json`, `prefix/*.json`, `maps/*.json`

## Commands:
`ms_buycommands_reload` - Reload config file of BuyCommands(`root`)

## Arguments in configs:
`CoolDownCommand` - (float) the time after which the command will be available again<br>
`Commands` - (List<string>) list of commands to type in chat<br>
`EntityName` - (string) entity name to give<br>
`Price_CT` - (uint) The purchase price of weapons for Counter-Terrorists(via command)<br>
`Price_T` - (uint) The purchase price of weapons for Terrorists(via command)<br>
`Limit_CT` - (byte) Limitation on the purchase of this weapon per round for Counter-Terrorists(via command)<br>
`Limit_T` - (byte) Limitation on the purchase of this weapon per round for Terrorists(via command)<br>
`Restrict_CT` - (bool) Restrict the purchase of this weapon for Counter-Terrorists. Exclude weapons from the map<br>
`Restrict_T` - (bool) Restrict the purchase of this weapon for Terrorists. Exclude weapons from the map