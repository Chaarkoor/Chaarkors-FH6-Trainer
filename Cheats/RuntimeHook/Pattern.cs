using System.Collections.Generic;

namespace FH6Mod.Cheats.RuntimeHook;

/// <summary>
/// AOB signature parser/scanner. "?" = wildcard byte (-1).
/// </summary>
internal static class Pattern
{
    public static int[] Parse(string signature)
    {
        var parts = signature.Split([' '], System.StringSplitOptions.RemoveEmptyEntries);
        var result = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            result[i] = parts[i] == "?" || parts[i] == "??"
                ? -1
                : int.Parse(parts[i], System.Globalization.NumberStyles.HexNumber);
        return result;
    }

    public static IEnumerable<int> FindAll(byte[] data, int[] pattern, int max)
    {
        if (pattern.Length == 0 || data.Length < pattern.Length) yield break;
        var found = 0;
        var end = data.Length - pattern.Length;
        for (var i = 0; i <= end; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (pattern[j] != -1 && data[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (!match) continue;
            yield return i;
            if (++found >= max) yield break;
        }
    }
}
