using AppEducativa.Shared.Dtos;
using AppEducativa.Shared.Enums;

namespace AppEducativa.Api.Services.PlanningSequence;

public sealed class BloomProgressionService
{
    public IReadOnlyList<NivelBloom> SuggestForObjective(
        int sessionCount,
        BloomProgressionSettingsRequest settings)
    {
        var allowed = (settings.AllowedLevels is { Count: > 0 }
            ? settings.AllowedLevels
            : Enum.GetValues<NivelBloom>()).OrderBy(x => (int)x).ToList();

        var initial = Clamp(settings.InitialLevel, allowed);
        var target = Clamp(settings.TargetLevel, allowed);
        if ((int)target < (int)initial)
            target = initial;

        return sessionCount switch
        {
            <= 0 => [],
            1 => [target],
            2 => [PreviousOrSame(initial, target, allowed), target],
            3 => [PreviousOrSame(initial, target, allowed), target, Consolidate(target, allowed)],
            _ => BuildLongProgression(sessionCount, initial, target, allowed)
        };
    }

    public bool IsExcessiveJump(NivelBloom from, NivelBloom to, int maxJump) =>
        (int)to - (int)from > maxJump;

    public bool IsStagnation(IReadOnlyList<NivelBloom> sequence, int maxRepeat = 3)
    {
        if (sequence.Count < maxRepeat) return false;
        var last = sequence[^1];
        var count = 0;
        for (var i = sequence.Count - 1; i >= 0; i--)
        {
            if (sequence[i] != last) break;
            count++;
        }
        return count >= maxRepeat;
    }

    private static IReadOnlyList<NivelBloom> BuildLongProgression(
        int count, NivelBloom initial, NivelBloom target, IReadOnlyList<NivelBloom> allowed)
    {
        var result = new List<NivelBloom>(count);
        var intro = PreviousOrSame(initial, target, allowed);
        var mid = initial;
        if ((int)mid < (int)intro) mid = intro;
        if ((int)mid > (int)target) mid = target;

        for (var i = 0; i < count; i++)
        {
            var ratio = count == 1 ? 1d : i / (double)(count - 1);
            if (ratio < 0.25) result.Add(intro);
            else if (ratio < 0.55) result.Add(mid);
            else if (ratio < 0.8) result.Add(target);
            else result.Add(Consolidate(target, allowed));
        }

        return result;
    }

    private static NivelBloom PreviousOrSame(NivelBloom initial, NivelBloom target, IReadOnlyList<NivelBloom> allowed)
    {
        var prev = (NivelBloom)Math.Max((int)NivelBloom.Recordar, (int)target - 1);
        if ((int)prev < (int)initial) prev = initial;
        return Clamp(prev, allowed);
    }

    private static NivelBloom Consolidate(NivelBloom target, IReadOnlyList<NivelBloom> allowed) =>
        Clamp(target, allowed);

    private static NivelBloom Clamp(NivelBloom level, IReadOnlyList<NivelBloom> allowed)
    {
        if (allowed.Contains(level)) return level;
        return allowed.OrderBy(a => Math.Abs((int)a - (int)level)).First();
    }
}
