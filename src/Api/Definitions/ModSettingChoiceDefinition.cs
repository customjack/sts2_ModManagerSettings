using System.Collections.Generic;

namespace ModManagerSettings.Api;

/// <summary>
/// Defines one single-choice setting row contributed by another mod.
/// </summary>
public sealed class ModSettingChoiceDefinition : ModSettingDefinition<string>
{
    public required IReadOnlyList<string> Options { get; init; }
}
