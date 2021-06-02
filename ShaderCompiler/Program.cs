using System;
using System.IO;
using Veldrid;
using Veldrid.SPIRV;

namespace ShaderCompiler
{
	class Program
	{
		static void Main( string[] args )
		{
			if ( args.Length != 1 )
			{
				throw new ArgumentException( "invalid argument length" );
			}

			string SourceDir = args[0];

			DirectoryInfo sourceShaderDir = new DirectoryInfo( SourceDir + @"Render\Shaders\" );

			foreach ( var shaderFile in sourceShaderDir.GetFiles( "*", SearchOption.AllDirectories ))
			{
				string fileExtention = shaderFile.Extension.Substring(1);
				ShaderStages shaderStage =
				fileExtention switch
				{
					"vert" => ShaderStages.Vertex,
					"frag" => ShaderStages.Fragment,
					"comp" => ShaderStages.Compute,
					"geom" => ShaderStages.Geometry,
					_ => throw new InvalidOperationException( $"Invalid shader extension: {shaderFile.FullName}" )
				};

				string shaderContent = File.ReadAllText( shaderFile.FullName );
				var compileResult = SpirvCompilation.CompileGlslToSpirv( shaderContent, "", shaderStage, new GlslCompileOptions() );

				var subdir = shaderFile.DirectoryName.Length + 1 == sourceShaderDir.FullName.Length ? string.Empty : shaderFile.DirectoryName.Substring(sourceShaderDir.FullName.Length);

				var compiledShaderFoler = $@"{SourceDir}Resources\shaders\{subdir}\";
				var compiledShaderName = $"{compiledShaderFoler}{Path.GetFileNameWithoutExtension( shaderFile.Name )}_{fileExtention}.spv";

				CreateFolder( new DirectoryInfo( compiledShaderFoler ) );
				File.WriteAllBytes( compiledShaderName, compileResult.SpirvBytes );
			}

			Console.WriteLine( "Compilation completed" );
		}

		static void CreateFolder(DirectoryInfo folder )
		{
			if ( !folder.Parent.Exists )
			{
				CreateFolder( folder.Parent );
			}
			if ( !folder.Exists )
			{
				Directory.CreateDirectory( folder.FullName );
			}
		}
	}
}
