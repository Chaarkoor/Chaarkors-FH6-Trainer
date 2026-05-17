# Chaarkor's FH6 Trainer

All-in-one trainer for **Forza Horizon 6** — runtime hooks for player profile + live SQL access to the game's in-memory database. Single-file `.exe`, no extra runtime needed.

> ⚠️ **Use at your own risk.** This trainer modifies game memory. Microsoft / Turn 10 can ban your account. **Solo / Free Roam only — never online (Rivals, Eventlab, Multiplayer, leaderboards).**

## ⬇️ Download

Latest release: **[GitHub Releases](../../releases/latest)** — grab `ChaarkorFH6Mod.exe`. Run as administrator (the trainer needs to `OpenProcess` on the game).

## ✅ Working features (FH6 — current build)

### Runtime hooks (Unlocks page)
- 💰 **Credits** — custom value, locked at your number
- 🎰 **Wheelspins** — custom value (unlock first in tutorial)
- ⭐ **Skill Points** — custom value
- 💸 **Sell Payout x** — multiply car sell price

### SQL database actions (Database page)
- 💰 **Free Cars (LOCK)** — BaseCost stays at 0 forever (re-applied every 10s)
- 👁 **Autoshow All Visible (LOCK)** — every car stays in showroom
- ✅ **Install Flags (LOCK)** — IsInstalled / IsPurchased / IsDrivable stay at 1
- 🏷 **Clear NEW Tag** — remove persistent NEW! badges
- 🎁 **Add All Cars** — grant every car free (reopen game to claim)

Each LOCK toggle re-applies its SQL every 10 seconds so the game can't restore the values from save. Backup tables are created automatically before the first lock — flipping the toggle OFF restores originals.

## ⚠️ Broken in current FH6 build (game patched)

These also don't work in Autoshow Unlocker v1.3.0 — Turn 10 changed the underlying functions:
- ❌ Drift Score Multiplier
- ❌ No Skill Break

## 🛡️ Stability

- **CRC bypass** auto-armed before any hook (vtable function pointer swap + 10s re-arm timer)
- **Hook self-healing** — every 10s the engine re-applies any patch the game tries to roll back
- **ExpectedOriginal sanity check** — refuses to inject if the target bytes don't match (no crashes from outdated sigs)
- **Auto-detach** when the game exits / crashes — no writes to dead processes

## 🔧 Build from source

Requires **.NET 10 SDK**, builds on Windows or WSL2 (cross-compile to win-x64):

```bash
dotnet publish -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Output: `bin/Release/net10.0-windows/win-x64/publish/ChaarkorFH6Mod.exe`

## 📝 License

GPL-3.0 — source must remain open. See [LICENSE](LICENSE).

## 🙏 Credits

This trainer wouldn't exist without **[Forza Mods](https://github.com/ForzaMods)** — the entire memory injection foundation (AOB scanning, function detouring, code caves, x64 shellcode generation, CRC bypass for FH6) comes from their open-source projects. All runtime hooks and the SQL executor mechanism are derivative work adapted to our UI and extended with original toggle-lock infrastructure.

Memory scanning powered by **[Reloaded.Memory](https://github.com/Reloaded-Project/Reloaded.Memory.Sigscan)** — SIMD-accelerated AOB scanner.

## 💬 Community

Bug reports, feature requests, and updates: [Chaarkor's FH6 Trainer Discord](#) *(invite link soon)*.

---

**by [Chaarkor](https://github.com/Chaarkoor) · 2026 · GPL-3.0**
