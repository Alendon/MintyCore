using System;
using System.IO;
using Veldrid;
using Veldrid.SPIRV;

namespace ShaderCompiler
{
	public class Program
	{
		static void Main( string[] args )
		{
			if ( args.Length != 2 )
			{
				throw new ArgumentException( "invalid argument length" );
			}

			string sourceDir = args[0];
			string compileDir = args[1];

			DirectoryInfo sourceShaderDir = new DirectoryInfo( sourceDir );
			DirectoryInfo compileShaderDir = new DirectoryInfo(compileDir);
			
			CompileShaders(sourceShaderDir, compileShaderDir);
			
			Console.WriteLine( "Compilation completed" );
		}

		public static void CompileShaders(DirectoryInfo sourceDir, DirectoryInfo compileDir)
		{
			foreach ( var shaderFile in sourceDir.GetFiles( "*", SearchOption.AllDirectories ))
			{
				string fileExtension = shaderFile.Extension.Substring(1);
				ShaderStages shaderStage =
					fileExtension switch
					{
						"vert" => ShaderStages.Vertex,
						"frag" => ShaderStages.Fragment,
						"comp" => ShaderStages.Compute,
						"geom" => ShaderStages.Geometry,
						_ => throw new InvalidOperationException( $"Invalid shader extension: {shaderFile.FullName}" )
					};

				string shaderContent = File.ReadAllText( shaderFile.FullName );
				var compileResult = SpirvCompilation.CompileGlslToSpirv( shaderContent, "", shaderStage, new GlslCompileOptions() );

				var subDir = shaderFile.DirectoryName.Length + 1 == sourceDir.FullName.Length ? string.Empty : shaderFile.DirectoryName.Substring(sourceDir.FullName.Length);

				var compiledShaderFolder = $@"{compileDir}\{subDir}\";
				var compiledShaderName = $"{compiledShaderFolder}{Path.GetFileNameWithoutExtension( shaderFile.Name )}_{fileExtension}.spv";

				CreateFolder( new DirectoryInfo( compiledShaderFolder ) );
				File.WriteAllBytes( compiledShaderName, compileResult.SpirvBytes );
			}
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
