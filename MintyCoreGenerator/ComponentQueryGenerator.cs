using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MintyCoreGenerator
{
	[Generator]
	class ComponentQueryGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{

		}
		public void Execute(GeneratorExecutionContext context)
		{
			foreach (var syntaxTree in context.Compilation.SyntaxTrees)
			{
				var queryFields = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Where(fields => fields.AttributeLists.Any(attribute => attribute.ToString().StartsWith("[ComponentQuery")));
				foreach (var queryField in queryFields)
				{
					GenericNameSyntax genericQueryName = queryField.Declaration.Type is GenericNameSyntax ? queryField.Declaration.Type as GenericNameSyntax : null;
					if (genericQueryName is null) continue;

					var queryName = genericQueryName.Identifier;
					var queryComponents = genericQueryName.TypeArgumentList;
					var parentClass = queryField.Parent as ClassDeclarationSyntax;
					var parentNamespace = parentClass.Parent as NamespaceDeclarationSyntax;
					var compilationUnit = parentNamespace.Parent as CompilationUnitSyntax;
					if (!parentClass.Modifiers.Any(x=>x.IsKind(SyntaxKind.PartialKeyword)))
					{
						continue;
					}

					var writeComponents = GetComponents(queryComponents, ComponentType.Write);
					var readComponents = GetComponents(queryComponents, ComponentType.Read);
					var excludeComponents = GetComponents(queryComponents, ComponentType.Exclude);

					List<MethodDeclarationSyntax> queryMethods = new List<MethodDeclarationSyntax>();
					queryMethods.Add(GetSetupMethod(writeComponents, readComponents, excludeComponents));
					queryMethods.Add(GetObjectEnumeratorMethod());
					queryMethods.Add(GetEntityEnumeratorMethod());

					List<StructDeclarationSyntax> childStructs = new List<StructDeclarationSyntax>();
					childStructs.Add(GetEnumeratorStruct());
					//childStructs.Add(GetCurrentEntityStruct());

					List<FieldDeclarationSyntax> queryMemberFields = new List<FieldDeclarationSyntax>();
					queryMemberFields.Add(GetArchetypeStorageDictionaryField());

					ClassDeclarationSyntax generatedQueryClass = GetQueryClass(queryName, queryComponents.Arguments.Count, queryMethods, childStructs, queryMemberFields);
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

		private ClassDeclarationSyntax GetQueryClass(SyntaxToken queryName, int count, List<MethodDeclarationSyntax> queryMethods, List<StructDeclarationSyntax> childStructs, List<FieldDeclarationSyntax> queryMemberFields)
		{
			var className = queryName.WithoutTrivia();
			ClassDeclarationSyntax classDeclaration = SyntaxFactory.ClassDeclaration(className);
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

		private StructDeclarationSyntax GetEnumeratorStruct()
		{
			StructDeclarationSyntax structDeclaration = SyntaxFactory.StructDeclaration("Enumerator");
			structDeclaration = structDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IEnumerator<CurrentEntity>")));
			structDeclaration = structDeclaration.WithModifiers(GetPrivateModifier());

			#region fields
			List<FieldDeclarationSyntax> structFields = new List<FieldDeclarationSyntax>();

			VariableDeclarationSyntax parentDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("ComponentQuery"));
			VariableDeclaratorSyntax parentDeclarator = SyntaxFactory.VariableDeclarator("_parent");
			parentDeclaration = parentDeclaration.AddVariables(parentDeclarator);
			structFields.Add(SyntaxFactory.FieldDeclaration(parentDeclaration));

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

			structDeclaration = structDeclaration.AddMembers(structFields.ToArray());
			#endregion
			#region properties
			List<PropertyDeclarationSyntax> properties = new List<PropertyDeclarationSyntax>();

			PropertyDeclarationSyntax _currentEntity = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("CurrentEntity"), "Current");
			_currentEntity = _currentEntity.WithModifiers(GetPublicModifier());
			_currentEntity = _currentEntity.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.IdentifierName("_current")));
			properties.Add(_currentEntity);

			PropertyDeclarationSyntax _enumeratorCurrent = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("object"), "IEnumerator.Current");
			_enumeratorCurrent = _enumeratorCurrent.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.IdentifierName("Current")));
			properties.Add(_enumeratorCurrent);

			structDeclaration = structDeclaration.AddMembers(properties.ToArray());
			#endregion

			//TODO Add Methods

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
			method = method.WithModifiers(GetPublicModifier());
			return method;
		}

		private MethodDeclarationSyntax GetObjectEnumeratorMethod()
		{
			MethodDeclarationSyntax method = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("IEnumerator<CurrentEntity>"), "GetEnumerator");
			method = method.WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("return new Enumerator(this);")));
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
				SyntaxFactory.ParseStatement($"containsAllComponents = containsAllComponents && archetype.ArchetypeComponents.Contains((new {component}).Identification);"));
			}
			foreach (var component in readComponents)
			{
				archetypeLoopBody = archetypeLoopBody.AddStatements(
				SyntaxFactory.ParseStatement($"containsAllComponents = containsAllComponents && archetype.ArchetypeComponents.Contains((new {component}).Identification);"));
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
				SyntaxFactory.ParseStatement($"readComponentIDs.Add((new {component}).Identification);"));
			}
			foreach (var component in readComponents)
			{
				methodBody = methodBody.AddStatements(
				SyntaxFactory.ParseStatement($"writeComponentIDs.Add((new {component}).Identification);"));
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
