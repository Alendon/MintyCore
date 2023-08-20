using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MintyCore.Generator.Registry;

public class RegistryHelper
{
    internal const string RegistryInterfaceName = "MintyCore.Modding.IRegistry";
    internal const string RegistryClassAttributeName = "MintyCore.Modding.Attributes.RegistryAttribute";
    internal const string RegistryMethodAttributeName = "MintyCore.Modding.Attributes.RegisterMethodAttribute";
    internal const string RegisterBaseAttributeName = "MintyCore.Modding.Attributes.RegisterBaseAttribute";
    internal const string RegisterMethodInfoBaseName = "MintyCore.Modding.Attributes.RegisterMethodInfo";
    internal const string ReferenceRegisterMethodName = "MintyCore.Modding.Attributes.ReferencedRegisterMethod";
    internal const string IdentificationName = "MintyCore.Utils.Identification";
    internal const string ModName = "MintyCore.Modding.IMod";

    

   

    public static RegisterMethod GetRegistryAttributeData(ISymbol attribute)
    {
        throw new NotImplementedException();
    }
}