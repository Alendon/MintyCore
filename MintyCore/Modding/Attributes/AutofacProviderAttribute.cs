using System;
using MintyCore.Modding.Providers;

namespace MintyCore.Modding.Attributes;

/// <summary>
///  Annotate your <see cref="IAutofacProvider"/> implementation with this attribute to make it discoverable
/// </summary>
public class AutofacProviderAttribute : Attribute;