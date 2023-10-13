using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MintyCore.Generator.Registry;

public record RegisterMethodInfo
{
    public RegisterMethodType RegisterType { get; set; } = 0;
    public int NumericRegisterType => (int)RegisterType;

    public string MethodName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Namespace { get; set; } = "";

    public int RegistryPhase { get; set; } = -1;

    public bool HasFile { get; set; } = false;

    public string[] GenericConstraintTypes { get; set; } = Array.Empty<string>();
    public GenericConstraints Constraints { get; set; } = GenericConstraints.None;
    public int NumericConstraints => (int)Constraints;

    public string? InvocationReturnType { get; set; } = null;

    public string CategoryId { get; set; } = "";

    public string CategoryName =>
        string.Concat(CategoryId.Split('_').Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));

    public string? ResourceSubFolder { get; set; } = null;

    public string GameType { get; set; } = "";
}