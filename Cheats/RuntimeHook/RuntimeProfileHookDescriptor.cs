namespace FH6Mod.Cheats.RuntimeHook;

public enum RuntimeProfileFeature
{
    Credits,
    Wheelspins,
    SuperWheelspins,
    SkillPoints,
    DriftScoreMultiplier,
    NoSkillBreak,
    SellFactor,
}

/// <summary>
/// Describes a function detour: where to find it (AOB), what to install (ASM bytes),
/// and where in the cave to write the runtime toggle/value.
/// Ported 1:1 from Autoshow Unlocker v1.3.0.
/// </summary>
internal sealed class RuntimeProfileHookDescriptor
{
    public string Key = "";
    public string Name = "";
    public string Signature = "";
    public int MatchOffset;
    public bool ResolveCallTarget;
    public int CallTargetOffset;
    public int HookSize;
    public byte[] Asm = [];
    public byte[] ExpectedOriginal = [];
    public int ToggleOffset;
    public int ValueOffset = -1;
}

internal sealed class RuntimeDetour
{
    public string Name = "";
    public ulong Address;
    public ulong DetourAddress;
    public int Size;
    public byte[] Original = [];
    public byte[] Patch = [];
}

internal static class ProfileFeatureCatalog
{
    public static RuntimeProfileHookDescriptor Get(RuntimeProfileFeature feature) => feature switch
    {
        RuntimeProfileFeature.Credits => new()
        {
            Key = "Credits", Name = "Credits",
            Signature = "E8 ? ? ? ? 89 84 ? ? ? ? ? 4C 8D ? ? ? ? ? 48 8B",
            ResolveCallTarget = true, CallTargetOffset = 24,
            HookSize = 6,
            ExpectedOriginal = [72, 139, 79, 8, 51, 210],
            ToggleOffset = 49, ValueOffset = 50,
            Asm =
            [
                72, 139, 79, 8, 128, 61, 38, 0, 0, 0,
                1, 117, 29, 72, 139, 84, 36, 32, 72, 184,
                67, 114, 101, 100, 105, 116, 115, 0, 72, 57,
                66, 180, 117, 8, 139, 21, 10, 0, 0, 0,
                137, 23, 49, 210,
            ],
        },
        RuntimeProfileFeature.Wheelspins => new()
        {
            Key = "Wheelspins", Name = "Wheelspins",
            Signature = "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 10",
            MatchOffset = 28, HookSize = 5,
            ExpectedOriginal = [51, 210, 139, 95, 8],
            ToggleOffset = 28, ValueOffset = 29,
            Asm =
            [
                128, 61, 21, 0, 0, 0, 1, 117, 9, 139,
                21, 14, 0, 0, 0, 137, 87, 8, 51, 210,
                139, 95, 8,
            ],
        },
        // Super Wheelspins: identical to Wheelspins but the player struct stores
        // the count at [rdi+0x18] instead of [rdi+0x08]. Signature ends 4F 18 vs 4F 10.
        // ASM is byte-for-byte the Wheelspins payload with the three 0x08 displacements
        // bumped to 0x10 (mov [rdi+0x10] / mov ebx,[rdi+0x10]).
        RuntimeProfileFeature.SuperWheelspins => new()
        {
            Key = "SuperWheelspins", Name = "Super Wheelspins",
            Signature = "48 89 5C 24 08 57 48 83 EC 20 48 8B FA 33 D2 48 8B 4F 18",
            MatchOffset = 28, HookSize = 5,
            ExpectedOriginal = [51, 210, 139, 95, 16],
            ToggleOffset = 28, ValueOffset = 29,
            Asm =
            [
                128, 61, 21, 0, 0, 0, 1, 117, 9, 139,
                21, 14, 0, 0, 0, 137, 87, 16, 51, 210,
                139, 95, 16,
            ],
        },
        RuntimeProfileFeature.SkillPoints => new()
        {
            Key = "SkillPoints", Name = "Skill Points",
            Signature = "85 D2 78 32 48 89 5C 24 08 57 48 83 EC 20 8B DA 48 8B F9 48 8B 49 48",
            MatchOffset = 34, HookSize = 5,
            ExpectedOriginal = [51, 210, 137, 95, 64],
            ToggleOffset = 25, ValueOffset = 26,
            Asm =
            [
                128, 61, 18, 0, 0, 0, 1, 117, 6, 139,
                29, 11, 0, 0, 0, 51, 210, 137, 95, 64,
            ],
        },
        RuntimeProfileFeature.DriftScoreMultiplier => new()
        {
            Key = "DriftScoreMultiplier", Name = "Drift Score Multiplier",
            Signature = "E8 ? ? ? ? F3 0F ? ? 0F 28 ? ? ? 0F 28",
            MatchOffset = 5, HookSize = 9,
            ExpectedOriginal = [243, 15, 88, 247, 15, 40, 124, 36, 32],
            ToggleOffset = 31, ValueOffset = 32,
            Asm =
            [
                128, 61, 24, 0, 0, 0, 1, 117, 8, 243,
                15, 89, 61, 15, 0, 0, 0, 243, 15, 88,
                247, 15, 40, 124, 36, 32,
            ],
        },
        RuntimeProfileFeature.NoSkillBreak => new()
        {
            Key = "NoSkillBreak", Name = "No Skill Break",
            Signature = "0F B6 ? 40 38 ? ? ? ? ? 74 ? 84 C0",
            MatchOffset = 0, HookSize = 10,
            ExpectedOriginal = [15, 182, 240, 64, 56, 171, 116, 2, 0, 0],
            ToggleOffset = 26, ValueOffset = -1,
            Asm =
            [
                128, 61, 19, 0, 0, 0, 1, 117, 2, 48,
                192, 15, 182, 240, 64, 56, 171, 116, 2, 0,
                0,
            ],
        },
        RuntimeProfileFeature.SellFactor => new()
        {
            Key = "SellFactor", Name = "Sell Payout",
            Signature = "44 8B ? ? ? ? ? 33 D2 48 8B ? ? ? ? ? E8 ? ? ? ? 90",
            MatchOffset = 0, HookSize = 7,
            ExpectedOriginal = [68, 139, 183, 8, 1, 0, 0],
            ToggleOffset = 28, ValueOffset = 29,
            Asm =
            [
                68, 139, 183, 8, 1, 0, 0, 128, 61, 14,
                0, 0, 0, 1, 117, 7, 68, 139, 53, 6,
                0, 0, 0,
            ],
        },
        _ => throw new System.InvalidOperationException("Unsupported runtime profile feature."),
    };
}
