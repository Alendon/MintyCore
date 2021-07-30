using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MintyCoreGenerator
{
    [Generator]
    class ComponentQueryGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
        }
        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var queryFields = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Where(fields => fields.AttributeLists.Any(attribute => attribute.ToString().StartsWith("[ComponentQuery")));
                foreach (var queryField in queryFields)
                {
                    GenericNameSyntax genericQueryFieldName = queryField.Declaration.Type is GenericNameSyntax ? queryField.Declaration.Type as GenericNameSyntax : null;
                    if (genericQueryFieldName is null) continue;

                    var queryName = genericQueryFieldName.Identifier;
                    var queryComponents = genericQueryFieldName.TypeArgumentList;
                    var parentClass = queryField.Parent as ClassDeclarationSyntax;
                    var parentNamespace = parentClass.Parent as NamespaceDeclarationSyntax;
                    var compilationUnit = parentNamespace.Parent as CompilationUnitSyntax;
                    if (!parentClass.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        continue;
                    }

                    var writeComponents = GetComponents(queryComponents, ComponentType.Write);
                    var readComponents = GetComponents(queryComponents, ComponentType.Read);
                    var excludeComponents = GetComponents(queryComponents, ComponentType.Exclude);

                    string genericQueryClassName = $"{ queryName }<{ "WriteComponents"}{ (queryComponents.Arguments.Count >= 2 ? ", ReadComponents" : "")}{ (queryComponents.Arguments.Count >= 3 ? ", ExcludeComponents" : "")}>";

                    List<MethodDeclarationSyntax> queryMethods = new List<MethodDeclarationSyntax>();
                    queryMethods.Add(GetSetupMethod(writeComponents, readComponents, excludeComponents));
                    queryMethods.Add(GetObjectEnumeratorMethod(queryName.Text));
                    queryMethods.Add(GetEntityEnumeratorMethod());

                    List<StructDeclarationSyntax> childStructs = new List<StructDeclarationSyntax>();
                    var combinedComponents = writeComponents.ToList();
                    combinedComponents.AddRange(readComponents);
                    childStructs.Add(GetEnumeratorStruct(genericQueryClassName, combinedComponents));
                    childStructs.Add(GetCurrentEntityStruct(writeComponents, readComponents));

                    List<FieldDeclarationSyntax> queryMemberFields = new List<FieldDeclarationSyntax>();
                    queryMemberFields.Add(GetArchetypeStorageDictionaryField());


                    ClassDeclarationSyntax generatedQueryClass = GetQueryClass(queryName, queryComponents.Arguments.Count, genericQueryClassName, queryMethods, childStructs, queryMemberFields);
                    ClassDeclarationSyntax generatedParentClass = GetParentClass(parentClass, generatedQueryClass);
                    NamespaceDeclarationSyntax generatedNamespace = GetNamespace(parentNamespace.Name, generatedParentClass);
                    CompilationUnitSyntax generatedCompilationUnit = GetCompilationUnit(compilationUnit.Usings.ToArray(), generatedNamespace);

                    var sourceCode = generatedCompilationUnit.NormalizeWhitespace().GetText(Encoding.UTF8);
                    context.AddSource($"{parentNamespace.Name}.{parentClass.Identifier}.{queryName}.cs", sourceCode);

                }
            }
        }

        private CompilationUnitSyntax GetCompilationUnit(UsingDirectiveSyntax[] usingDirectiveSyntaxes, NamespaceDeclarationSyntax generatedNamespace)
        {
            CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit();
            compilationUnit = compilationUnit.AddUsings(usingDirectiveSyntaxes);
            compilationUnit = compilationUnit.AddMembers(generatedNamespace);

            List<UsingDirectiveSyntax> additionalUsings = new List<UsingDirectiveSyntax>();
            additionalUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
            additionalUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections")));
            additionalUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")));
            additionalUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            additionalUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("MintyCore.Utils")));
            compilationUnit = compilationUnit.AddUsings(additionalUsings.ToArray());

            return compilationUnit;
        }

        private NamespaceDeclarationSyntax GetNamespace(NameSyntax name, ClassDeclarationSyntax generatedParentClass)
        {
            var namespaceName = name.WithoutTrivia();
            NamespaceDeclarationSyntax namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(namespaceName).NormalizeWhitespace();
            namespaceDeclaration = namespaceDeclaration.AddMembers(generatedParentClass);

            return namespaceDeclaration;
        }

        private ClassDeclarationSyntax GetParentClass(ClassDeclarationSyntax parentClass, ClassDeclarationSyntax generatedQueryClass)
        {
            var className = parentClass.Identifier.WithoutTrivia();
            ClassDeclarationSyntax classDeclaration = SyntaxFactory.ClassDeclaration(className);
            classDeclaration = classDeclaration.AddMembers(generatedQueryClass);
            classDeclaration = classDeclaration.WithModifiers(parentClass.Modifiers);
            classDeclaration = classDeclaration.WithTypeParameterList(parentClass.TypeParameterList);
            return classDeclaration;
        }

        private ClassDeclarationSyntax GetQueryClass(SyntaxToken queryName, int count, string genericQueryName, List<MethodDeclarationSyntax> queryMethods, List<StructDeclarationSyntax> childStructs, List<FieldDeclarationSyntax> queryMemberFields)
        {
            var className = queryName.WithoutTrivia();
            ClassDeclarationSyntax classDeclaration = SyntaxFactory.ClassDeclaration(className);
            classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"IEnumerable<{genericQueryName}.CurrentEntity>")));
            classDeclaration = classDeclaration.AddMembers(queryMemberFields.ToArray());
            classDeclaration = classDeclaration.AddMembers(queryMethods.ToArray());
            classDeclaration = classDeclaration.AddMembers(childStructs.ToArray());
            classDeclaration = classDeclaration.WithModifiers(GetPrivateModifier());
            SeparatedSyntaxList<TypeParameterSyntax> typeParameters = new SeparatedSyntaxList<TypeParameterSyntax>();
            for (int i = 0; i < count; i++)
            {
                typeParameters = typeParameters.Add(SyntaxFactory.TypeParameter($"{((ComponentType)i)}Components"));
            }
            classDeclaration = classDeclaration.WithTypeParameterList(SyntaxFactory.TypeParameterList(typeParameters));
            return classDeclaration;
        }

        private StructDeclarationSyntax GetEnumeratorStruct(string genericQueryName, List<TypeSyntax> usedComponents)
        {
            StructDeclarationSyntax structDeclaration = SyntaxFactory.StructDeclaration("Enumerator");
            structDeclaration = structDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IEnumerator<CurrentEntity>")));
            structDeclaration = structDeclaration.WithModifiers(GetPublicModifier());

            #region fields
            List<FieldDeclarationSyntax> structFields = new List<FieldDeclarationSyntax>();

            VariableDeclarationSyntax parentDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(genericQueryName));
            VariableDeclaratorSyntax parentDeclarator = SyntaxFactory.VariableDeclarator("_parent");
            parentDeclaration = parentDeclaration.AddVariables(parentDeclarator);
            structFields.Add(SyntaxFactory.FieldDeclaration(parentDeclaration));

            VariableDeclarationSyntax archetypeSizeDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int"));
            VariableDeclaratorSyntax archetypeSizeDeclarator = SyntaxFactory.VariableDeclarator("_archetypeSize");
            archetypeSizeDeclaration = archetypeSizeDeclaration.AddVariables(archetypeSizeDeclarator);
            structFields.Add(SyntaxFactory.FieldDeclaration(archetypeSizeDeclaration));

            VariableDeclarationSyntax archetypePtrDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("IntPtr"));
            VariableDeclaratorSyntax archetypePtrDeclarator = SyntaxFactory.VariableDeclarator("_archetypePtr");
            archetypePtrDeclaration = archetypePtrDeclaration.AddVariables(archetypePtrDeclarator);
            structFields.Add(SyntaxFactory.FieldDeclaration(archetypePtrDeclaration));

            VariableDeclarationSyntax currentDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("CurrentEntity"));
            VariableDeclaratorSyntax currentDeclarator = SyntaxFactory.VariableDeclarator("_current");
            currentDeclaration = currentDeclaration.AddVariables(currentDeclarator);
            structFields.Add(SyntaxFactory.FieldDeclaration(currentDeclaration));

            VariableDeclarationSyntax archetypeEnumeratorDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("Dictionary<Identification, ArchetypeStorage>.Enumerator"));
            VariableDeclaratorSyntax archetypeEnumeratorDeclarator = SyntaxFactory.VariableDeclarator("_archetypeEnumerator");
            archetypeEnumeratorDeclaration = archetypeEnumeratorDeclaration.AddVariables(archetypeEnumeratorDeclarator);
            structFields.Add(SyntaxFactory.FieldDeclaration(archetypeEnumeratorDeclaration));

            VariableDeclarationSyntax entityIndexesDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("Entity[]"));
            VariableDeclaratorSyntax entityIndexesDeclarator = SyntaxFactory.VariableDeclarator("_entityIndexes");
            entityIndexesDeclaration = entityIndexesDeclaration.AddVariables(entityIndexesDeclarator);
            structFields.Add(SyntaxFactory.FieldDeclaration(entityIndexesDeclaration));

            VariableDeclarationSyntax entityIndexDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int"));
            VariableDeclaratorSyntax entityIndexDeclarator = SyntaxFactory.VariableDeclarator("_entityIndex");
            entityIndexDeclaration = entityIndexDeclaration.AddVariables(entityIndexDeclarator);
            structFields.Add(SyntaxFactory.FieldDeclaration(entityIndexDeclaration));

            foreach (var component in usedComponents)
            {
                VariableDeclarationSyntax componentIDDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("Identification"));
                VariableDeclaratorSyntax componentIDDeclarator = SyntaxFactory.VariableDeclarator($"_component{component}ID");
                componentIDDeclaration = componentIDDeclaration.AddVariables(componentIDDeclarator);
                structFields.Add(SyntaxFactory.FieldDeclaration(componentIDDeclaration));

                VariableDeclarationSyntax componentOffsetDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int"));
                VariableDeclaratorSyntax componentOffsetDeclarator = SyntaxFactory.VariableDeclarator($"_component{component}Offset");
                componentOffsetDeclaration = componentOffsetDeclaration.AddVariables(componentOffsetDeclarator);
                structFields.Add(SyntaxFactory.FieldDeclaration(componentOffsetDeclaration));
            }

            structDeclaration = structDeclaration.AddMembers(structFields.ToArray());
            #endregion
            #region methods
            var constructor = SyntaxFactory.ConstructorDeclaration("Enumerator");
            constructor = constructor.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            var constructorBlock = SyntaxFactory.Block();
            constructorBlock = constructorBlock.AddStatements(
                SyntaxFactory.ParseStatement("_parent = parent;"),
                SyntaxFactory.ParseStatement("_current = default;"),
                SyntaxFactory.ParseStatement("_entityIndex = -1;"),
                SyntaxFactory.ParseStatement("_entityIndexes = Array.Empty<Entity>();"),
                SyntaxFactory.ParseStatement("_archetypeSize = 0;"),
                SyntaxFactory.ParseStatement("_archetypePtr = IntPtr.Zero;"),
                SyntaxFactory.ParseStatement("_archetypeEnumerator = _parent._archetypeStorages.GetEnumerator();"));

            foreach (var component in usedComponents)
            {
                constructorBlock = constructorBlock.AddStatements(
                    SyntaxFactory.ParseStatement($"_component{component}ID = (new {component}()).Identification;"),
                    SyntaxFactory.ParseStatement($"_component{component}Offset = 0;")
                    );
            }

            constructor = constructor.WithBody(constructorBlock);
            var constructorParentParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("parent"));
            constructorParentParameter = constructorParentParameter.WithType(SyntaxFactory.IdentifierName(genericQueryName));
            constructor = constructor.AddParameterListParameters(constructorParentParameter);
            structDeclaration = structDeclaration.AddMembers(constructor);

            var disposeMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Dispose");
            disposeMethod = disposeMethod.WithModifiers(GetPublicModifier());
            disposeMethod = disposeMethod.AddBodyStatements(SyntaxFactory.ParseStatement("_archetypeEnumerator.Dispose();"));
            structDeclaration = structDeclaration.AddMembers(disposeMethod);

            var resetMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Reset");
            resetMethod = resetMethod.WithModifiers(GetPublicModifier());
            resetMethod = resetMethod.AddBodyStatements(SyntaxFactory.ParseStatement("throw new NotSupportedException();"));
            structDeclaration = structDeclaration.AddMembers(resetMethod);

            var applyEntityMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "ApplyEntity");
            StringBuilder applyEntitySyntaxBuilder = new StringBuilder();
            applyEntitySyntaxBuilder.Append("_current = new CurrentEntity(_entityIndexes[_entityIndex], _archetypePtr + (_entityIndex * _archetypeSize)");

            foreach (var component in usedComponents)
            {
                applyEntitySyntaxBuilder.Append($", _component{component}Offset");
            }

            applyEntitySyntaxBuilder.Append(");");

            applyEntityMethod = applyEntityMethod.AddBodyStatements(SyntaxFactory.ParseStatement(applyEntitySyntaxBuilder.ToString()));
            structDeclaration = structDeclaration.AddMembers(applyEntityMethod);

            var moveNextMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), "MoveNext");
            moveNextMethod = moveNextMethod.WithModifiers(GetPublicModifier());
            var moveNextSyntax = SyntaxFactory.ParseSyntaxTree("do{if (!NextEntity() && !NextArchetype()){return false;}} while (!CurrentValid()); ApplyEntity(); return true; ").GetRoot() as CompilationUnitSyntax;
            foreach (var item in moveNextSyntax.Members)
            {
                moveNextMethod = moveNextMethod.AddBodyStatements((item as GlobalStatementSyntax).Statement);
            }
            structDeclaration = structDeclaration.AddMembers(moveNextMethod);

            var nextEntityMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), "NextEntity");
            nextEntityMethod = nextEntityMethod.AddBodyStatements(
                SyntaxFactory.ParseStatement("_entityIndex++;"),
                SyntaxFactory.ParseStatement("return EntityIndexValid();"));
            structDeclaration = structDeclaration.AddMembers(nextEntityMethod);

            var entityIndexValidMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), "EntityIndexValid");
            entityIndexValidMethod = entityIndexValidMethod.AddBodyStatements(
                SyntaxFactory.ParseStatement("return _entityIndex >= 0 && _entityIndex < _entityIndexes.Length;"));
            structDeclaration = structDeclaration.AddMembers(entityIndexValidMethod);

            var currentValidMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), "CurrentValid");
            currentValidMethod = currentValidMethod.AddBodyStatements(
                SyntaxFactory.ParseStatement("return EntityIndexValid() && _entityIndexes[_entityIndex] != default;"));
            structDeclaration = structDeclaration.AddMembers(currentValidMethod);

            var nextArchetypeMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), "NextArchetype");
            var nextArchetypeBuilder = new StringBuilder();
            nextArchetypeBuilder.Append("if (!_archetypeEnumerator.MoveNext()){return false;}");
            nextArchetypeBuilder.Append("_entityIndexes = _archetypeEnumerator.Current.Value._indexEntity;");
            nextArchetypeBuilder.Append("_entityIndex = -1;");
            nextArchetypeBuilder.Append("_archetypePtr = _archetypeEnumerator.Current.Value._data;");
            nextArchetypeBuilder.Append("_archetypeSize = _archetypeEnumerator.Current.Value._archetypeSize;");

            foreach (var component in usedComponents)
            {
                nextArchetypeBuilder.Append($"_component{component}Offset = _archetypeEnumerator.Current.Value._componentOffsets[_component{component}ID];");
            }
            nextArchetypeBuilder.Append("return true;");

            var nextArchetypeSyntaxTree = SyntaxFactory.ParseSyntaxTree(nextArchetypeBuilder.ToString()).GetRoot() as CompilationUnitSyntax;
            foreach (var item in nextArchetypeSyntaxTree.Members)
            {
                nextArchetypeMethod = nextArchetypeMethod.AddBodyStatements((item as GlobalStatementSyntax).Statement);
            }
            structDeclaration = structDeclaration.AddMembers(nextArchetypeMethod);

            #endregion
            #region properties
            List<PropertyDeclarationSyntax> properties = new List<PropertyDeclarationSyntax>();

            PropertyDeclarationSyntax _currentEntity = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("CurrentEntity"), "Current");
            _currentEntity = _currentEntity.WithModifiers(GetPublicModifier());
            _currentEntity = _currentEntity.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.IdentifierName("_current")));
            _currentEntity = _currentEntity.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            properties.Add(_currentEntity);

            PropertyDeclarationSyntax _enumeratorCurrent = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("object"), "IEnumerator.Current");
            _enumeratorCurrent = _enumeratorCurrent.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.IdentifierName("Current")));
            _enumeratorCurrent = _enumeratorCurrent.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            properties.Add(_enumeratorCurrent);

            structDeclaration = structDeclaration.AddMembers(properties.ToArray());
            #endregion


            return structDeclaration;
        }

        private StructDeclarationSyntax GetCurrentEntityStruct(TypeSyntax[] writeComponents, TypeSyntax[] readComponents)
        {
            var structDeclaration = SyntaxFactory.StructDeclaration("CurrentEntity");
            structDeclaration = structDeclaration.WithModifiers(GetPublicModifier());
            structDeclaration = structDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));

            var entityField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("Entity")));
            entityField = entityField.AddDeclarationVariables(SyntaxFactory.VariableDeclarator("Entity"));
            entityField = entityField.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            structDeclaration = structDeclaration.AddMembers(entityField);

            var entityPointerField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("IntPtr")));
            entityPointerField = entityPointerField.AddDeclarationVariables(SyntaxFactory.VariableDeclarator("_entityPtr"));
            entityPointerField = entityPointerField.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
            structDeclaration = structDeclaration.AddMembers(entityPointerField);

            foreach (var component in writeComponents)
            {
                var componentOffsetField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int")));
                string componentOffsetName = $"_component{component}Offset";
                componentOffsetField = componentOffsetField.AddDeclarationVariables(SyntaxFactory.VariableDeclarator(componentOffsetName));
                componentOffsetField = componentOffsetField.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                structDeclaration = structDeclaration.AddMembers(componentOffsetField);

                var getComponentMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName($"ref {component}"), $"Get{component}");
                var getComponentStatement = SyntaxFactory.ParseStatement($"return ref *({component}*)(_entityPtr + {componentOffsetName});");
                getComponentMethod = getComponentMethod.AddBodyStatements(getComponentStatement);
                getComponentMethod = getComponentMethod.WithModifiers(GetPublicModifier());
                structDeclaration = structDeclaration.AddMembers(getComponentMethod);
            }

            foreach (var component in readComponents)
            {
                var componentOffsetField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int")));
                string componentOffsetName = $"_component{component}Offset";
                componentOffsetField = componentOffsetField.AddDeclarationVariables(SyntaxFactory.VariableDeclarator(componentOffsetName));
                componentOffsetField = componentOffsetField.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                structDeclaration = structDeclaration.AddMembers(componentOffsetField);

                var getComponentMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName($"{component}"), $"Get{component}");
                var getComponentStatement = SyntaxFactory.ParseStatement($"return *({component}*)(_entityPtr + {componentOffsetName});");
                getComponentMethod = getComponentMethod.AddBodyStatements(getComponentStatement);
                getComponentMethod = getComponentMethod.WithModifiers(GetPublicModifier());
                structDeclaration = structDeclaration.AddMembers(getComponentMethod);
            }

            var constructorDeclaration = SyntaxFactory.ConstructorDeclaration("CurrentEntity");
            constructorDeclaration = constructorDeclaration.WithModifiers(GetPublicModifier());

            var entityParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("entity"));
            entityParameter = entityParameter.WithType(SyntaxFactory.ParseTypeName("Entity"));
            constructorDeclaration = constructorDeclaration.AddParameterListParameters(entityParameter);

            var entityPtrParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("entityPtr"));
            entityPtrParameter = entityPtrParameter.WithType(SyntaxFactory.ParseTypeName("IntPtr"));
            constructorDeclaration = constructorDeclaration.AddParameterListParameters(entityPtrParameter);

            foreach (var component in writeComponents)
            {
                var componentParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier($"component{component}Offset"));
                componentParameter = componentParameter.WithType(SyntaxFactory.ParseTypeName("int"));

                constructorDeclaration = constructorDeclaration.AddParameterListParameters(componentParameter);
            }
            foreach (var component in readComponents)
            {
                var componentParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier($"component{component}Offset"));
                componentParameter = componentParameter.WithType(SyntaxFactory.ParseTypeName("int"));

                constructorDeclaration = constructorDeclaration.AddParameterListParameters(componentParameter);
            }

            constructorDeclaration = constructorDeclaration.AddBodyStatements(
                SyntaxFactory.ParseStatement("Entity = entity;"),
                SyntaxFactory.ParseStatement("_entityPtr = entityPtr;"));

            foreach (var component in writeComponents)
            {
                constructorDeclaration = constructorDeclaration.AddBodyStatements(SyntaxFactory.ParseStatement($"_component{component}Offset = component{component}Offset;"));
            }
            foreach (var component in readComponents)
            {
                constructorDeclaration = constructorDeclaration.AddBodyStatements(SyntaxFactory.ParseStatement($"_component{component}Offset = component{component}Offset;"));
            }

            structDeclaration = structDeclaration.AddMembers(constructorDeclaration);

            return structDeclaration;
        }

        private FieldDeclarationSyntax GetArchetypeStorageDictionaryField()
        {
            VariableDeclarationSyntax variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("Dictionary<Identification, ArchetypeStorage>"));
            VariableDeclaratorSyntax variableDeclarator = SyntaxFactory.VariableDeclarator("_archetypeStorages");
            variableDeclarator = variableDeclarator.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ImplicitObjectCreationExpression()));
            variableDeclaration = variableDeclaration.AddVariables(variableDeclarator);

            FieldDeclarationSyntax fieldDeclaration = SyntaxFactory.FieldDeclaration(variableDeclaration);
            return fieldDeclaration;
        }

        private MethodDeclarationSyntax GetEntityEnumeratorMethod()
        {
            MethodDeclarationSyntax method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("IEnumerator"), "IEnumerable.GetEnumerator");
            method = method.WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("return GetEnumerator();")));
            return method;
        }

        private MethodDeclarationSyntax GetObjectEnumeratorMethod(string className)
        {
            MethodDeclarationSyntax method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName($"IEnumerator<CurrentEntity>"), "GetEnumerator");
            method = method.WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("return new Enumerator(this);")));
            method = method.WithModifiers(GetPublicModifier());
            return method;
        }

        private MethodDeclarationSyntax GetSetupMethod(TypeSyntax[] writeComponents, TypeSyntax[] readComponents, TypeSyntax[] excludeComponents)
        {
            MethodDeclarationSyntax method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Setup");
            method = method.WithModifiers(GetPublicModifier());

            ParameterListSyntax parameterList = SyntaxFactory.ParameterList();
            ParameterSyntax systemParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("system"));
            systemParameter = systemParameter.WithType(SyntaxFactory.ParseTypeName("ASystem"));
            parameterList = parameterList.AddParameters(systemParameter);
            method = method.WithParameterList(parameterList);

            BlockSyntax methodBody = SyntaxFactory.Block();
            methodBody = methodBody.AddStatements(SyntaxFactory.ParseStatement("var archetypeMap = ArchetypeManager.GetArchetypes();"));

            #region archtypeLoopBody
            BlockSyntax archetypeLoopBody = SyntaxFactory.Block();
            archetypeLoopBody = archetypeLoopBody.AddStatements(
                SyntaxFactory.ParseStatement("var id = entry.Key;"),
                SyntaxFactory.ParseStatement("var archetype = entry.Value;"),
                SyntaxFactory.ParseStatement("bool containsAllComponents = true;"));

            foreach (var component in writeComponents)
            {
                archetypeLoopBody = archetypeLoopBody.AddStatements(
                SyntaxFactory.ParseStatement($"containsAllComponents = containsAllComponents && archetype.ArchetypeComponents.Contains((new {component}()).Identification);"));
            }
            foreach (var component in readComponents)
            {
                archetypeLoopBody = archetypeLoopBody.AddStatements(
                SyntaxFactory.ParseStatement($"containsAllComponents = containsAllComponents && archetype.ArchetypeComponents.Contains((new {component}()).Identification);"));
            }

            BlockSyntax allComponentCheckBody = SyntaxFactory.Block(SyntaxFactory.ContinueStatement());
            ExpressionSyntax allComponentCheckSyntax = SyntaxFactory.ParseExpression("!containsAllComponents");
            IfStatementSyntax allComponentCheck = SyntaxFactory.IfStatement(allComponentCheckSyntax, allComponentCheckBody);
            archetypeLoopBody = archetypeLoopBody.AddStatements(allComponentCheck);

            archetypeLoopBody = archetypeLoopBody.AddStatements(SyntaxFactory.ParseStatement("bool containsNoExcludeComponents = true;"));
            foreach (var component in excludeComponents)
            {
                archetypeLoopBody = archetypeLoopBody.AddStatements(
                SyntaxFactory.ParseStatement($"containsAllComponents = containsAllComponents && !archetype.ArchetypeComponents.Contains((new {component}).Identification);"));
            }

            BlockSyntax noExcludeComponentCheckBody = SyntaxFactory.Block(SyntaxFactory.ContinueStatement());
            ExpressionSyntax noExcludeComponentCheckSyntax = SyntaxFactory.ParseExpression("!containsNoExcludeComponents");
            IfStatementSyntax noExcludeComponentCheck = SyntaxFactory.IfStatement(noExcludeComponentCheckSyntax, noExcludeComponentCheckBody);
            archetypeLoopBody = archetypeLoopBody.AddStatements(noExcludeComponentCheck);

            archetypeLoopBody = archetypeLoopBody.AddStatements(SyntaxFactory.ParseStatement("_archetypeStorages.Add(id, system.World.EntityManager.GetArchetypeStorage(id));"));
            #endregion

            ForEachStatementSyntax archetypeLoop = SyntaxFactory.ForEachStatement(SyntaxFactory.ParseTypeName("KeyValuePair<Identification, ArchetypeContainer>"), "entry", SyntaxFactory.IdentifierName("archetypeMap"), archetypeLoopBody);
            methodBody = methodBody.AddStatements(archetypeLoop);

            methodBody = methodBody.AddStatements(
                SyntaxFactory.ParseStatement("HashSet<Identification> readComponentIDs = new();"),
                SyntaxFactory.ParseStatement("HashSet<Identification> writeComponentIDs = new();"));
            foreach (var component in writeComponents)
            {
                methodBody = methodBody.AddStatements(
                SyntaxFactory.ParseStatement($"readComponentIDs.Add((new {component}()).Identification);"));
            }
            foreach (var component in readComponents)
            {
                methodBody = methodBody.AddStatements(
                SyntaxFactory.ParseStatement($"writeComponentIDs.Add((new {component}()).Identification);"));
            }
            methodBody = methodBody.AddStatements(
                SyntaxFactory.ParseStatement("SystemManager.SetReadComponents(system.Identification, readComponentIDs);"),
                SyntaxFactory.ParseStatement("SystemManager.SetWriteComponents(system.Identification, writeComponentIDs);"));

            method = method.WithBody(methodBody);
            return method;
        }

        private TypeSyntax[] GetComponents(TypeArgumentListSyntax queryComponents, ComponentType componentType)
        {
            if (queryComponents.Arguments.Count <= (int)componentType)
            {
                return Array.Empty<TypeSyntax>();
            }
            var componentCollection = queryComponents.Arguments[(int)componentType];
            if (componentCollection is TupleTypeSyntax componentTuple)
            {
                List<TypeSyntax> value = new List<TypeSyntax>();
                foreach (var item in componentTuple.Elements)
                {
                    value.Add(item.Type);
                }
                return value.ToArray();
            }
            if (componentCollection is PredefinedTypeSyntax
                || componentCollection is ArrayTypeSyntax
                || componentCollection is FunctionPointerTypeSyntax
                || componentCollection is PointerTypeSyntax
                || componentCollection is RefTypeSyntax)
            {
                return Array.Empty<TypeSyntax>();
            }
            return new TypeSyntax[] { componentCollection };
        }

        private SyntaxTokenList GetPublicModifier()
        {
            return new SyntaxTokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }
        private SyntaxTokenList GetPrivateModifier()
        {
            return new SyntaxTokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        }
        private SyntaxTokenList GetInternalModifier()
        {
            return new SyntaxTokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
        }

        enum ComponentType
        {
            Write = 0,
            Read = 1,
            Exclude = 2
        }
    }
}
