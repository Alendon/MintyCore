using System;

namespace MintyCore.ECS
{
	/// <summary>
	///     Set this attribute to a class that inherits from <see cref="AParallelSystem" /> to prevent the auto source
	///     generator to kick in
	///     This will be also applied to all classes that inherits from your implementation
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
    public class PreventDefaultParallelSystemGenerationAttribute : Attribute
    {
    }


	/// <summary>
	///     Abstract class for all parallel systems. Make sure that your implementation meets the following Conditions:
	///     1. Create the class with the "partial" keyworkd
	///     2. Have exactly ONE auto generated ComponentQuery present
	///     Also make sure you have referenced the Engine SourceGeneratorProject
	/// </summary>
	public abstract class AParallelSystem : ASystem
    {
	    /// <summary>
	    ///     <inheritdoc />
	    /// </summary>
	    protected sealed override void Execute()
        {
            //The Execute method is sealed overriden as the implementing class have to define a new Execute(ComponentQuery.CurrentEntity entity) method
        }
    }
}