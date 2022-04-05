using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable All

namespace MintyCoreGenerator
{
    [Generator]
    class ComponentQueryGenerator : IIncrementalGenerator
    {
        private const string ComponentQueryAttributeName = "MintyCore.ECS.ComponentQueryAttribute";
        private const string IComponentInterfaceName = "MintyCore.ECS.IComponent";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var componentQueryProvider = context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => IsSyntaxTarget(node),
                    static (context, _) => GetSemanticTargetForGeneration(context))
                .Where(query => query is not null);

            context.RegisterSourceOutput(componentQueryProvider, static (context, information) => GenerateComponentQuery(context, information));
        }

        private static bool IsSyntaxTarget(SyntaxNode node)
        {
            //By this we get only field declarations syntaxes which lives in a not nested partial class and haves at least one attribute
            return node is FieldDeclarationSyntax fieldNode &&
                   node.Parent is ClassDeclarationSyntax classNode &&
                   classNode.Parent is BaseNamespaceDeclarationSyntax &&
                   classNode.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)) &&
                   fieldNode.AttributeLists.Count() != 0;
        }

        private static QueryInformation? GetSemanticTargetForGeneration(
            GeneratorSyntaxContext context)
        {
            if (context.Node is not FieldDeclarationSyntax queryField || queryField.Parent is null) return null;

            if (context.SemanticModel.GetDeclaredSymbol(queryField.Parent) is not INamedTypeSymbol parentClassSymbol)
                return null;
            //Get the semantic symbol for the query field
            if (context.SemanticModel.GetDeclaredSymbol(queryField.Declaration.Variables[0]) is not IFieldSymbol
                querySymbol) return null;
            if (querySymbol.Type is not INamedTypeSymbol querySymbolType) return null;

            //Check if the field declaration has the [ComponentQuery] Attribute
            bool hasComponentQueryAttribute = false;
            var attributes = querySymbol.GetAttributes();

            foreach (var attributeData in attributes)
            {
                if (attributeData.AttributeClass is not null &&
                    (attributeData.AttributeClass.ToString()?.Equals(ComponentQueryAttributeName)).GetValueOrDefault())
                {
                    hasComponentQueryAttribute = true;
                    break;
                }
            }

            if (!hasComponentQueryAttribute) return null;

            //Check if the field has generics
            if (!querySymbolType.IsGenericType) return null;
            //Collect all Generic Parameters

            var typeArguments = querySymbolType.TypeArguments;

#pragma warning disable RS1024 // Symbols should be compared for equality     False positiv "error"
            var writeComponents = new HashSet<INamedTypeSymbol>();
            var readComponents = new HashSet<INamedTypeSymbol>();
            var excludeComponents = new HashSet<INamedTypeSymbol>();


            //This should be always true
            //Collect the write components
            if (querySymbolType.Arity > 0)
                FillComponentList(writeComponents, typeArguments[0], context);

            if (querySymbolType.Arity > 1)
                FillComponentList(readComponents, typeArguments[1], context);

            if (querySymbolType.Arity > 2)
                FillComponentList(excludeComponents, typeArguments[2], context);

            if (writeComponents.Overlaps(readComponents) ||
                writeComponents.Overlaps(excludeComponents) ||
                readComponents.Overlaps(excludeComponents))
            {
                return null;
            }

            return new()
            {
                parentClassSymbol = parentClassSymbol,
                querySymbol = querySymbol,
                queryTypeSymbol = querySymbolType,
                writeComponents = writeComponents.ToArray(),
                readComponents = readComponents.ToArray(),
                excludeComponents = excludeComponents.ToArray()
            };

#pragma warning restore RS1024 // Symbols should be compared for equality
        }

        private static void FillComponentList(HashSet<INamedTypeSymbol> componentList, ITypeSymbol baseType,
            GeneratorSyntaxContext context)
        {
            if (baseType is not INamedTypeSymbol baseNamedType) return;

            var componentsToProcess = new Queue<INamedTypeSymbol>();

            componentsToProcess.Enqueue(baseNamedType);

            while (componentsToProcess.Count > 0)
            {
                var potentialComponentSymbol = componentsToProcess.Dequeue();

                //Unpack all tuples in the generic type
                if (potentialComponentSymbol.IsTupleType)
                {
                    foreach (var tupleSymbol in potentialComponentSymbol.TypeArguments)
                    {
                        if (tupleSymbol is INamedTypeSymbol namedTupleSymbol)
                            componentsToProcess.Enqueue(namedTupleSymbol);
                    }

                    continue;
                }

                //Check for IComponentInterface, IsUnmanaged
                if (!potentialComponentSymbol.IsUnmanagedType) continue;

                var interfaces = potentialComponentSymbol.AllInterfaces;
                bool interfaceFound = false;
                foreach (var @interface in interfaces)
                {
                    var interfaceName = @interface.ToString();
                    if (interfaceName is not null && interfaceName.Equals(IComponentInterfaceName))
                    {
                        interfaceFound = true;
                        break;
                    }
                }

                if (!interfaceFound) continue;

                componentList.Add(potentialComponentSymbol);
            }
        }

        private static void GenerateComponentQuery(SourceProductionContext context, QueryInformation? information)
        {
            //Should never happen
            if (information is not QueryInformation info) return;

            string classAccessor = GetAccessabilityAsString(info.parentClassSymbol.DeclaredAccessibility);
            string queryAccessor = GetAccessabilityAsString(info.querySymbol.DeclaredAccessibility);

            string className = info.parentClassSymbol.Name;
            string namespaceName = info.parentClassSymbol.ContainingNamespace.ToString() ?? String.Empty;
            string componentQueryName = info.queryTypeSymbol.Name;

            string[] writeComponents = new string[info.writeComponents.Length];
            for (var i = 0; i < info.writeComponents.Length; i++)
            {
                var writeComponent = info.writeComponents[i];
                writeComponents[i] = writeComponent.ToString();
            }
            
            string[] readComponents = new string[info.readComponents.Length];
            for (var i = 0; i < info.readComponents.Length; i++)
            {
                var readComponent = info.readComponents[i];
                readComponents[i] = readComponent.ToString();
            }
            
            string[] excludeComponents = new string[info.excludeComponents.Length];
            for (var i = 0; i < info.excludeComponents.Length; i++)
            {
                var excludeComponent = info.excludeComponents[i];
                excludeComponents[i] = excludeComponent.ToString();
            }

            var builder = new ComponentQueryBuilder()
                .SetNamespaceName(namespaceName)
                .SetClassAccessor(classAccessor)
                .SetClassName(className)
                .SetComponentQueryName(componentQueryName)
                .SetComponentQueryAccessor(queryAccessor)
                .SetWriteComponents(writeComponents)
                .SetReadComponents(readComponents)
                .SetExcludeComponents(excludeComponents);

            var classText = builder.BuildComponentQuery();
            var fileExtensionLocation = $"{namespaceName}.{className}.{componentQueryName}.g.cs";
            context.AddSource(fileExtensionLocation, classText);
        }

        private static string GetAccessabilityAsString(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Private:
                    {
                        return "private";
                    }
                case Accessibility.ProtectedAndInternal:
                    {
                        return "protected internal";
                    }
                case Accessibility.Protected:
                    {
                        return "protected";
                    }
                case Accessibility.Internal:
                    {
                        return "internal";
                    }
                case Accessibility.Public:
                    {
                        return "public";
                    }
            }
            return String.Empty;

        }

        private struct QueryInformation
        {
            public IFieldSymbol querySymbol;
            public INamedTypeSymbol queryTypeSymbol;
            public INamedTypeSymbol parentClassSymbol;
            public INamedTypeSymbol[] writeComponents;
            public INamedTypeSymbol[] readComponents;
            public INamedTypeSymbol[] excludeComponents;
        }

        //Helper class to generate the code for the Query class
        class ComponentQueryBuilder
        {
            private string _className = String.Empty;
            private string _namespaceName = String.Empty;
            private string _componentQueryName = String.Empty;
            private string _fullComponentQueryName = String.Empty;
            private string _classAccessor = String.Empty;
            private string _queryAccessor = String.Empty;

            private string[] _readComponents = Array.Empty<string>();
            private string[] _readComponentBaseFieldNames = Array.Empty<string>();
            private string[] _readComponentClassNames = Array.Empty<string>();

            private string[] _writeComponents = Array.Empty<string>();
            private string[] _writeComponentBaseFieldNames = Array.Empty<string>();
            private string[] _writeComponentClassNames = Array.Empty<string>();

            private string[] _excludeComponents = Array.Empty<string>();

            public string BuildComponentQuery()
            {
                StringBuilder sb = new();

                ComposeFullComponentQueryName();
                CreateComponentNames();

                WriteUsingsNamespaceAndClassHead(sb);
                WriteSetupMethod(sb);
                WriteIEnumerableImplementation(sb);
                WriteEnumerator(sb);
                WriteCurrentEntity(sb);
                WriteClassFoot(sb);

                var compileUnit = SyntaxFactory.ParseCompilationUnit(sb.ToString());

                return compileUnit.NormalizeWhitespace().ToFullString();
            }

            private void ComposeFullComponentQueryName()
            {
                if (_excludeComponents.Length != 0)
                {
                    _fullComponentQueryName =
                        $"{_componentQueryName}<WriteComponents, ReadComponents, ExcludeComponents>";
                    return;
                }

                if (_readComponents.Length != 0)
                {
                    _fullComponentQueryName = $"{_componentQueryName}<WriteComponents, ReadComponents>";
                    return;
                }

                _fullComponentQueryName = $"{_componentQueryName}<WriteComponents>";
            }

            private void CreateComponentNames()
            {
                _readComponentClassNames = new string[_readComponents.Length];
                for (int i = 0; i < _readComponents.Length; i++)
                {
                    var componentNameStartIndex = _readComponents[i].LastIndexOf('.');
                    _readComponentClassNames[i] = _readComponents[i].Substring(componentNameStartIndex + 1);
                }

                _writeComponentClassNames = new string[_writeComponents.Length];
                for (int i = 0; i < _writeComponents.Length; i++)
                {
                    var componentNameStartIndex = _writeComponents[i].LastIndexOf('.');
                    _writeComponentClassNames[i] = _writeComponents[i].Substring(componentNameStartIndex + 1);
                }

                _readComponentBaseFieldNames = new string[_readComponents.Length];
                for (int i = 0; i < _readComponents.Length; i++)
                {
                    _readComponentBaseFieldNames[i] = _readComponents[i].Replace('.', '_');
                }

                _writeComponentBaseFieldNames = new string[_writeComponents.Length];
                for (int i = 0; i < _writeComponents.Length; i++)
                {
                    _writeComponentBaseFieldNames[i] = _writeComponents[i].Replace('.', '_');
                }
            }

            private void WriteUsingsNamespaceAndClassHead(StringBuilder sb)
            {
                sb.AppendLine(@$"
using MintyCore.ECS;
using MintyCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace {_namespaceName};

{_classAccessor} partial class {_className} {{
    {_queryAccessor} unsafe class {_fullComponentQueryName} :  IEnumerable<{_fullComponentQueryName}.CurrentEntity> {{
        readonly Dictionary<Identification, ArchetypeStorage> _archetypeStorages = new();
");
            }

            private void WriteSetupMethod(StringBuilder sb)
            {
                sb.AppendLine($@"
        public void Setup(ASystem system)
        {{
            Logger.AssertAndThrow(system.World is not null, ""The system world cant be null"", ""ECS"");

            var archetypeMap = ArchetypeManager.GetArchetypes();
            foreach (KeyValuePair<Identification, ArchetypeContainer> entry in archetypeMap)
            {{
                var id = entry.Key;
                var archetype = entry.Value;
                bool containsAllComponents = true;
");

                foreach (var writeComponent in _writeComponents)
                {
                    sb.AppendLine(
                        $"containsAllComponents &= archetype.ArchetypeComponents.Contains(default({writeComponent}).Identification);");
                }

                sb.AppendLine();
                foreach (var readComponent in _readComponents)
                {
                    sb.AppendLine(
                        $"containsAllComponents &= archetype.ArchetypeComponents.Contains(default({readComponent}).Identification);");
                }

                sb.AppendLine(@"
                if(!containsAllComponents) continue;

                bool containsNoExcludeComponents = true; 
");
                sb.AppendLine();
                foreach (var excludeComponent in _excludeComponents)
                {
                    sb.AppendLine(
                        $"containsNoExcludeComponents &= !archetype.ArchetypeComponents.Contains(default({excludeComponent}).Identification);");
                }

                sb.AppendLine(@"
                if(!containsNoExcludeComponents) continue;
                
                _archetypeStorages.Add(id, system.World.EntityManager.GetArchetypeStorage(id));
            }

            HashSet<Identification> readComponentIDs = new();
            HashSet<Identification> writeComponentIDs = new();
");
                foreach (var readComponent in _readComponents)
                {
                    sb.AppendLine($"readComponentIDs.Add(default({readComponent}).Identification);");
                }

                sb.AppendLine();

                foreach (var writeComponent in _writeComponents)
                {
                    sb.AppendLine($"writeComponentIDs.Add(default({writeComponent}).Identification);");
                }

                sb.AppendLine(@"
            SystemManager.SetReadComponents(system.Identification, readComponentIDs);
            SystemManager.SetWriteComponents(system.Identification, writeComponentIDs);
        }
");
            }

            private void WriteIEnumerableImplementation(StringBuilder sb)
            {
                sb.AppendLine(@"
        public IEnumerator<CurrentEntity> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
");
            }

            private void WriteEnumerator(StringBuilder sb)
            {
                WriteEnumeratorHead(sb);
                WriteEnumeratorConstructor(sb);
                WriteEnumeratorNextArchetype(sb);
                WriteEnumeratorUtilityAndFood(sb);
            }

            private void WriteEnumeratorHead(StringBuilder sb)
            {
                sb.AppendLine($@"
        public struct Enumerator : IEnumerator<CurrentEntity>
        {{
            {_fullComponentQueryName} _parent;
            CurrentEntity _current;
            Dictionary<Identification, ArchetypeStorage>.Enumerator _archetypeEnumerator;
            Entity[] _entityIndexes;
            int _entityIndex;
            readonly Identification _archetypeStart;
            readonly int _archetypeCountToProcess;
            int _archetypeCountProcessed;
            bool _lastArchetype;
            int _entityCount;
");
                for (var index = 0; index < _writeComponents.Length; index++)
                {
                    var writeComponent = _writeComponents[index];
                    var writeComponentBaseFieldName = _writeComponentBaseFieldNames[index];

                    sb.AppendLine($@"
            readonly Identification {writeComponentBaseFieldName}_Id;
            private IntPtr {writeComponentBaseFieldName}_Pointer;
            private IntPtr {writeComponentBaseFieldName}_BasePointer;
            private readonly int {writeComponentBaseFieldName}_Size;
");
                }

                for (var index = 0; index < _readComponents.Length; index++)
                {
                    var readComponent = _readComponents[index];
                    var readComponentBaseFieldName = _readComponentBaseFieldNames[index];

                    sb.AppendLine($@"
            readonly Identification {readComponentBaseFieldName}_Id;
            private IntPtr {readComponentBaseFieldName}_Pointer;
            private IntPtr {readComponentBaseFieldName}_BasePointer;
            private readonly int {readComponentBaseFieldName}_Size;
");
                }

                sb.AppendLine($@"
            public CurrentEntity Current => _current;
            object IEnumerator.Current => Current;
");
            }

            private void WriteEnumeratorConstructor(StringBuilder sb)
            {
                sb.AppendLine($@"
            public Enumerator({_fullComponentQueryName} parent)
            {{
                _parent = parent;
                _current = default;
                _entityIndex = -1;
                _entityIndexes = Array.Empty<Entity>();
                _archetypeEnumerator = _parent._archetypeStorages.GetEnumerator();
                _entityCount = -1;
                _archetypeStart = default;
                _archetypeCountToProcess = _parent._archetypeStorages.Count;
                _archetypeCountProcessed = 0;
                _lastArchetype = _parent._archetypeStorages.Count == 0;
");
                for (var index = 0; index < _writeComponents.Length; index++)
                {
                    var writeComponent = _writeComponents[index];
                    var writeComponentBaseFieldName = _writeComponentBaseFieldNames[index];

                    sb.AppendLine($@"
                {writeComponentBaseFieldName}_Id = default ({writeComponent}).Identification;
                {writeComponentBaseFieldName}_Pointer = IntPtr.Zero;
                {writeComponentBaseFieldName}_BasePointer = IntPtr.Zero;
                {writeComponentBaseFieldName}_Size = ComponentManager.GetComponentSize({writeComponentBaseFieldName}_Id);
");
                }

                for (var index = 0; index < _readComponents.Length; index++)
                {
                    var readComponent = _readComponents[index];
                    var readComponentBaseFieldName = _readComponentBaseFieldNames[index];

                    sb.AppendLine($@"
                {readComponentBaseFieldName}_Id = default ({readComponent}).Identification;
                {readComponentBaseFieldName}_Pointer = IntPtr.Zero;
                {readComponentBaseFieldName}_BasePointer = IntPtr.Zero;
                {readComponentBaseFieldName}_Size = ComponentManager.GetComponentSize({readComponentBaseFieldName}_Id);
");
                }

                sb.AppendLine("}");
            }

            private void WriteEnumeratorNextArchetype(StringBuilder sb)
            {
                sb.AppendLine($@"
            bool NextArchetype()
            {{
                if (_lastArchetype) return false;

                if (_archetypeCountProcessed == 0 && _archetypeStart != default)
                {{
                    while (_archetypeEnumerator.Current.Key != _archetypeStart)
                    {{
                        if (!_archetypeEnumerator.MoveNext())
                        {{
                            return false;
                        }}
                    }}
                }}
                else if (!_archetypeEnumerator.MoveNext())
                {{
                    return false;
                }}
                
                _entityIndexes = _archetypeEnumerator.Current.Value.IndexEntity;
                _entityIndex = -1;
                _archetypeCountProcessed++;
                _lastArchetype = _archetypeCountProcessed == _archetypeCountToProcess;

                _entityCount = _archetypeEnumerator.Current.Value.EntityIndex.Count;
");
                for (var index = 0; index < _writeComponents.Length; index++)
                {
                    var writeComponent = _writeComponents[index];
                    var writeComponentBaseFieldName = _writeComponentBaseFieldNames[index];
                    sb.AppendLine(
                        $"{writeComponentBaseFieldName}_BasePointer = _archetypeEnumerator.Current.Value.GetComponentPtr(0, {writeComponentBaseFieldName}_Id);");
                }

                for (var index = 0; index < _readComponents.Length; index++)
                {
                    var readComponent = _readComponents[index];
                    var readComponentBaseFieldName = _readComponentBaseFieldNames[index];
                    sb.AppendLine(
                        $"{readComponentBaseFieldName}_BasePointer = _archetypeEnumerator.Current.Value.GetComponentPtr(0, {readComponentBaseFieldName}_Id);");
                }

                sb.AppendLine(@"
                return true;
            }");
            }

            private void WriteEnumeratorUtilityAndFood(StringBuilder sb)
            {
                sb.Append($@"
                public bool MoveNext()
                {{
                    do
                    {{
                        if (!NextEntity() && !NextArchetype())
                        {{
                            return false;
                        }}
                    }} while (!CurrentValid());
    
                    ApplyEntity();
                    return true;
                }}
    
                public void Dispose()
                {{
                    _archetypeEnumerator.Dispose();
                }}
    
                public void Reset()
                {{
                    throw new NotSupportedException();
                }}
    
                void ApplyEntity()
                {{
                    _current = new CurrentEntity(_entityIndexes[_entityIndex]");

                foreach (var writeComponentBaseFieldName in _writeComponentBaseFieldNames)
                {
                    sb.Append($", {writeComponentBaseFieldName}_Pointer");
                }

                foreach (var readComponentBaseFieldName in _readComponentBaseFieldNames)
                {
                    sb.Append($", {readComponentBaseFieldName}_Pointer");
                }

                sb.AppendLine($@");
                }}
                
                bool NextEntity()
                {{
                    _entityIndex++;

                    if (!EntityIndexValid()) return false;
");
                foreach (var writeComponentBaseFieldName in _writeComponentBaseFieldNames)
                {
                    sb.AppendLine($"{writeComponentBaseFieldName}_Pointer = {writeComponentBaseFieldName}_BasePointer + ({writeComponentBaseFieldName}_Size * _entityIndex);");
                }

                foreach (var readComponentBaseFieldName in _readComponentBaseFieldNames)
                {
                    sb.AppendLine($"{readComponentBaseFieldName}_Pointer = {readComponentBaseFieldName}_BasePointer + ({readComponentBaseFieldName}_Size * _entityIndex);");
                }

                sb.AppendLine($@"
                    return true;
                }}
                

                [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
                bool EntityIndexValid()
                {{
                    return _entityIndex >= 0 && _entityIndex < _entityCount;
                }}
    
                bool CurrentValid()
                {{
                    return EntityIndexValid() && _entityIndexes[_entityIndex] != default;
                }}
            }}
");
            }

            private void WriteCurrentEntity(StringBuilder sb)
            {
                sb.Append($@"
        public readonly unsafe struct CurrentEntity
        {{
            public readonly Entity Entity;


            public CurrentEntity(Entity entity");

                foreach (var writeComponentBaseFieldName in _writeComponentBaseFieldNames)
                {
                    sb.Append($", IntPtr {writeComponentBaseFieldName}_Pointer");
                }

                foreach (var readComponentBaseFieldName in _readComponentBaseFieldNames)
                {
                    sb.Append($", IntPtr {readComponentBaseFieldName}_Pointer");
                }

                sb.AppendLine(@")
            {
                Entity = entity;
");
                foreach (var writeComponentBaseFieldName in _writeComponentBaseFieldNames)
                {
                    sb.AppendLine(
                        $"this.{writeComponentBaseFieldName}_Pointer = {writeComponentBaseFieldName}_Pointer;");
                }

                foreach (var readComponentBaseFieldName in _readComponentBaseFieldNames)
                {
                    sb.AppendLine($"this.{readComponentBaseFieldName}_Pointer = {readComponentBaseFieldName}_Pointer;");
                }

                sb.AppendLine("}");

                for (var index = 0; index < _writeComponents.Length; index++)
                {
                    var writeComponent = _writeComponents[index];
                    var writeComponentName = _writeComponentClassNames[index];
                    var writeComponentBaseFieldName = _writeComponentBaseFieldNames[index];

                    sb.AppendLine($@"
                private readonly IntPtr {writeComponentBaseFieldName}_Pointer;
                
                [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
                public ref {writeComponent} Get{writeComponentName}()
                {{
                    return ref *({writeComponent} *) {writeComponentBaseFieldName}_Pointer;
                }}
");
                }

                for (var index = 0; index < _readComponents.Length; index++)
                {
                    var readComponent = _readComponents[index];
                    var readComponentName = _readComponentClassNames[index];
                    var readComponentBaseFieldName = _readComponentBaseFieldNames[index];

                    sb.AppendLine($@"
                private readonly IntPtr {readComponentBaseFieldName}_Pointer;
                
                [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
                public {readComponent} Get{readComponentName}()
                {{
                    return *({readComponent} *) {readComponentBaseFieldName}_Pointer;
                }}
");
                }

                sb.AppendLine(@"
        }");
            }

            private void WriteClassFoot(StringBuilder sb)
            {
                sb.AppendLine(@"
    }
}
");
            }


            public ComponentQueryBuilder SetClassName(string className)
            {
                _className = className;
                return this;
            }

            public ComponentQueryBuilder SetNamespaceName(string namespaceName)
            {
                _namespaceName = namespaceName;
                return this;
            }

            public ComponentQueryBuilder SetComponentQueryName(string componentQueryName)
            {
                _componentQueryName = componentQueryName;
                return this;
            }

            public ComponentQueryBuilder SetClassAccessor(string accessor)
            {
                _classAccessor = accessor;
                return this;
            }

            public ComponentQueryBuilder SetWriteComponents(string[] writeComponents)
            {
                _writeComponents = writeComponents;
                return this;
            }

            public ComponentQueryBuilder SetReadComponents(string[] readComponents)
            {
                _readComponents = readComponents;
                return this;
            }

            public ComponentQueryBuilder SetExcludeComponents(string[] excludeComponents)
            {
                _excludeComponents = excludeComponents;
                return this;
            }

            internal ComponentQueryBuilder SetComponentQueryAccessor(string queryAccessor)
            {
                _queryAccessor = queryAccessor;
                return this;
            }
        }
    }
}