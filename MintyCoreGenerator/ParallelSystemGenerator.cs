using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;



namespace MintyCoreGenerator
{
	[Generator]
	class ParallelSystemGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			//Debugger.Launch();
		}

		public void Execute(GeneratorExecutionContext context)
		{
			List<ClassDeclarationSyntax> parallelClasses = new List<ClassDeclarationSyntax>();
			HashSet<ClassDeclarationSyntax> classesToCheck = new HashSet<ClassDeclarationSyntax>();

			SearchParallelSystems(context, parallelClasses, classesToCheck, "ParallelSystem");

			while (classesToCheck.Count > 0)
			{
				foreach (var toCheck in new HashSet<ClassDeclarationSyntax>(classesToCheck))
				{
					SearchParallelSystems(context, parallelClasses, classesToCheck, toCheck.Identifier.ValueText);
					classesToCheck.Remove(toCheck);
				}
			}

			foreach (var parallelClass in parallelClasses)
			{
				ClassDeclarationSyntax extensionClass = GenerateParallelExtensionClass(parallelClass);
				
				var parentNamespace = parallelClass.Parent as BaseNamespaceDeclarationSyntax;
				parentNamespace = parentNamespace.WithMembers(new SyntaxList<MemberDeclarationSyntax>(extensionClass));
					
				
				CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit();
				compilationUnit = compilationUnit.WithUsings((parallelClass.Parent.Parent as CompilationUnitSyntax).Usings);
				compilationUnit = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")));
				compilationUnit = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")));
				compilationUnit = compilationUnit.AddMembers(parentNamespace);
				var sourceCode = compilationUnit.NormalizeWhitespace().GetText(Encoding.UTF8);
				context.AddSource($"{parentNamespace.Name}.{extensionClass.Identifier}_parallelExtension.cs", sourceCode);
			}
		}
		
		private ClassDeclarationSyntax GenerateParallelExtensionClass(ClassDeclarationSyntax parallelClass)
		{
			ClassDeclarationSyntax extensionClass = SyntaxFactory.ClassDeclaration(parallelClass.Identifier);
			extensionClass = extensionClass.WithModifiers(parallelClass.Modifiers);

			var queryField = parallelClass.Members.Where(member => member.AttributeLists.Any(attribute => attribute.ToString().StartsWith("[ComponentQuery"))).First() as FieldDeclarationSyntax;
			string componentQueryFieldName = queryField.Declaration.Variables.First().Identifier.ToString();
			string componentQueryTypeName = queryField.Declaration.Type.ToString();

			string classContent = @"
	public override Task QueueSystem(IEnumerable<Task> dependencies)
		{
			return Task.WhenAll(dependencies).ContinueWith(_ =>
				Parallel.ForEach(
					{0}, Execute));
		}
";
			classContent = classContent.Replace("{0}", componentQueryFieldName);

			var parsedContent = SyntaxFactory.ParseSyntaxTree(classContent).GetRoot() as CompilationUnitSyntax;

			foreach (var member in parsedContent.Members)
			{
				extensionClass = extensionClass.AddMembers(member);
			}
			
			return extensionClass;
		}

		private static void SearchParallelSystems(GeneratorExecutionContext context, List<ClassDeclarationSyntax> parallelClasses, HashSet<ClassDeclarationSyntax> classesToCheck, string attributeName)
		{
			foreach (var classDeclaration in from syntaxTree in context.Compilation.SyntaxTrees
											 from classDeclaration in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
											 where classDeclaration.BaseList != null && classDeclaration.AttributeLists.Any(x => x.ToString().StartsWith($"[{attributeName}"))
											 select classDeclaration)
			{
				if (classDeclaration.Modifiers.Any(x => x.ValueText.Equals("abstract")))
				{
					classesToCheck.Add(classDeclaration);
				}
				else
				{
					parallelClasses.Add(classDeclaration);
				}
			}
		}

	}
}
