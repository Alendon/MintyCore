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

			SearchParallelSystems(context, parallelClasses, classesToCheck, "AParallelSystem");

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
				NamespaceDeclarationSyntax namespaceDeclaration = parallelClass.Parent as NamespaceDeclarationSyntax;
				namespaceDeclaration = namespaceDeclaration.WithMembers(new SyntaxList<MemberDeclarationSyntax>(extensionClass));
				CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit();
				compilationUnit = compilationUnit.WithUsings((parallelClass.Parent.Parent as CompilationUnitSyntax).Usings);
				compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);
				var sourceCode = compilationUnit.NormalizeWhitespace().GetText(Encoding.UTF8);
				context.AddSource($"{namespaceDeclaration.Name}.{extensionClass.Identifier}_parallelExtension.cs", sourceCode);
			}
		}

		private ClassDeclarationSyntax GenerateParallelExtensionClass(ClassDeclarationSyntax parallelClass)
		{
			ClassDeclarationSyntax extensionClass = SyntaxFactory.ClassDeclaration(parallelClass.Identifier);
			extensionClass = extensionClass.WithModifiers(parallelClass.Modifiers);

			var queryField = parallelClass.Members.Where(member => member.AttributeLists.Any(attribute => attribute.ToString().StartsWith("[ComponentQuery"))).First() as FieldDeclarationSyntax;
			string componentQueryTypeName = queryField.Declaration.Type.ToString();
			string componentQueryFieldName = queryField.Declaration.Variables.First().Identifier.ToString();
			string systemTypeName = parallelClass.Identifier.ToString();

			string classContent = @"public override JobHandleCollection QueueSystem(JobHandleCollection dependency)
		{
			JobHandleCollection jobHandles = new JobHandleCollection();
			var storages = {1}.GetArchetypeStorages();
			int entityCount = 0;
			foreach (var storage in storages)
			{
				entityCount += storage._indexEntity.Length;
			}
		
			var splittedSize = entityCount / Environment.ProcessorCount;
		
			int remainingEntities = entityCount;
			int entitiesQueried = 0;
		
			Identification currentArchetypeStart;
			int startOffset = -1;
			int archetypesToProcess = 0;
			int entityIndexToStop = -1;
		
			var archetypeStorageEnumerator = storages.GetEnumerator();
			if (!archetypeStorageEnumerator.MoveNext()) return jobHandles;
			currentArchetypeStart = archetypeStorageEnumerator.Current.ID;
			archetypesToProcess++;
		
			int batchSize;
			switch (splittedSize)
			{
				case int n when n < MinEntityBatchSize: batchSize = MinEntityBatchSize; break;
				case int n when n > MaxEntityBatchSize: batchSize = MaxEntityBatchSize; break;
				default: batchSize = splittedSize; break;
			}
		
			int lastBatchSize = entityCount % batchSize;
		
			while (remainingEntities > 0)
			{
				//Check if its the last execution
				if (remainingEntities == lastBatchSize && entitiesQueried == lastBatchSize)
				{
					var enumerator = new {0}.Enumerator({1});
					enumerator.SetParallelInformation(currentArchetypeStart, archetypesToProcess, entityIndexToStop, startOffset);
					ParallelSystemJob job = new ParallelSystemJob(this, enumerator);
					jobHandles.AddJobHandle(job.Schedule(dependency));
		
					remainingEntities -= entitiesQueried;
					break;
				}
		
				int archetypeOffset = archetypesToProcess == 1 && startOffset > 0 ? startOffset : 0;
		
				int remainingEntitiesForBatch = batchSize - entitiesQueried;
				int archetypeRemainingEntityCount = archetypeStorageEnumerator.Current._indexEntity.Length - archetypeOffset;
		
		
				if(archetypeRemainingEntityCount > remainingEntitiesForBatch)
				{
					entityIndexToStop = archetypeOffset + remainingEntitiesForBatch;
					entitiesQueried += remainingEntitiesForBatch;
					remainingEntities -= entitiesQueried;
					entitiesQueried = 0;
		
					var enumerator = new {0}.Enumerator({1});
					enumerator.SetParallelInformation(currentArchetypeStart, archetypesToProcess, entityIndexToStop, startOffset);
					ParallelSystemJob job = new ParallelSystemJob(this, enumerator);
					jobHandles.AddJobHandle(job.Schedule(dependency));
		
					startOffset = entityIndexToStop;
					archetypesToProcess = 1;
					entityIndexToStop = -1;
					currentArchetypeStart = archetypeStorageEnumerator.Current.ID;
					continue;
				}
		
				if(archetypeRemainingEntityCount == remainingEntitiesForBatch)
				{
					entitiesQueried += remainingEntitiesForBatch;
					remainingEntities -= entitiesQueried;
					entitiesQueried = 0;
		
					var enumerator = new {0}.Enumerator({1});
					enumerator.SetParallelInformation(currentArchetypeStart, archetypesToProcess, entityIndexToStop, startOffset);
					ParallelSystemJob job = new ParallelSystemJob(this, enumerator);
					jobHandles.AddJobHandle(job.Schedule(dependency));
		
					startOffset = -1;
					archetypesToProcess = 1;
					entityIndexToStop = -1;
					if (archetypeStorageEnumerator.MoveNext())
					{
						currentArchetypeStart = archetypeStorageEnumerator.Current.ID;
					}
					continue;
				}
		
				if(archetypeRemainingEntityCount < remainingEntitiesForBatch)
				{
					entitiesQueried += archetypeRemainingEntityCount;
		
					archetypesToProcess++;
					archetypeStorageEnumerator.MoveNext();
					continue;
				}
		
		
		
			}
		
			return jobHandles;
		}

		class ParallelSystemJob : AJob
		{
			private {2} _system;
			private {0}.Enumerator _enumerator;

			public ParallelSystemJob({2} system, {0}.Enumerator enumerator)
			{
				_system = system;
				_enumerator = enumerator;
			}

			public override void Execute()
			{
				while (_enumerator.MoveNext())
				{
					_system.Execute(_enumerator.Current);
				}
			}
		}";
			classContent = classContent.Replace("{0}", componentQueryTypeName).Replace("{1}", componentQueryFieldName).Replace("{2}", systemTypeName);

			var parsedContent = SyntaxFactory.ParseSyntaxTree(classContent).GetRoot() as CompilationUnitSyntax;

			foreach (var member in parsedContent.Members)
			{
				extensionClass = extensionClass.AddMembers(member);
			}
			
			return extensionClass;
		}

		private static void SearchParallelSystems(GeneratorExecutionContext context, List<ClassDeclarationSyntax> parallelClasses, HashSet<ClassDeclarationSyntax> classesToCheck, string baseName)
		{
			foreach (var classDeclaration in from syntaxTree in context.Compilation.SyntaxTrees
											 from classDeclaration in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
											 where classDeclaration.BaseList != null && classDeclaration.BaseList.Types.Any(x => x.Type.ToString().Equals(baseName))
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
