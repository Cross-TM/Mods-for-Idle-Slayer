# Minion Sender

Automatically claims completed minion missions and sends affordable minions back to work.

## Automatic prestige configuration

Use the Mod Manager Config Editor to enable automatic prestige globally and then opt in each minion separately. Every per-minion prestige setting is disabled by default. `DivinityPointsMultiplier` multiplies the normal reward from automatic prestige and accepts values from 1 through 100. `AutoResendAfterPrestige` controls whether a successfully prestiged minion is sent out again, and `ShowPrestigeNotifications` controls the additional short status messages. `PrestigeActiveMissions` may be enabled to prestige a max-level minion before its active mission finishes; doing so forfeits that pending mission reward.

`AutoDailyBonusEnabled` automatically sends available affordable minions, claims the available daily minion bonus, and then claims and resends missions completed by its time reduction.

## Installation

Install the mod through Idle Slayer Mod Manager, or place the DLL and manifest in the existing MinionSender mod structure.

Original mod by CrossTM. Version 2.4.0 improvements contributed by Joeydm6.
