using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace MintyCore.Generator.Registry;

public record RegisterObject
{
    public RegisterMethodInfo RegisterMethodInfo { get; set; } = new();
    public string Id { get; set; } = "";

    [UsedImplicitly]
    public string Name => string.Concat(Id.Split('_').Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));

    [UsedImplicitly] public string? File { get; set; }

    [UsedImplicitly] public string? RegisterType { get; set; }

    [UsedImplicitly] public string? RegisterProperty { get; set; }

    [UsedImplicitly] public string? RegisterMethod { get; set; }

    [UsedImplicitly] public string[]? RegisterMethodParameters { get; set; }
}