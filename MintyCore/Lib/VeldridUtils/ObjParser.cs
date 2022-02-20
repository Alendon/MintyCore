using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace MintyCore.Lib.VeldridUtils;

/// <summary>
///     A parser for Wavefront OBJ files.
/// </summary>
public class ObjParser
{
    private static readonly char[] _whitespaceChars = { ' ' };
    private static readonly char[] _slashChar = { '/' };

    private readonly ParseContext _pc = new();

    /// <summary>
    ///     Parses an <see cref="ObjFile" /> from the given raw text lines.
    /// </summary>
    /// <param name="lines">The text lines of the OBJ file.</param>
    /// <returns>A new <see cref="ObjFile" />.</returns>
    public ObjFile Parse(string[] lines)
    {
        foreach (var line in lines) _pc.Process(line);
        _pc.EndOfFileReached();

        return _pc.FinalizeFile();
    }

    /// <summary>
    ///     Parses an <see cref="ObjFile" /> from the given text stream.
    /// </summary>
    /// <param name="s">The <see cref="Stream" /> to read from.</param>
    /// <returns>A new <see cref="ObjFile" />.</returns>
    public ObjFile Parse(Stream s)
    {
        string text;
        using (var sr = new StreamReader(s))
        {
            text = sr.ReadToEnd();
        }

        var lineStart = 0;
        var lineEnd = -1;
        while ((lineEnd = text.IndexOf('\n', lineStart)) != -1)
        {
            string line;

            if (lineEnd != 0 && text[lineEnd - 1] == '\r')
                line = text.Substring(lineStart, lineEnd - lineStart - 1);
            else
                line = text.Substring(lineStart, lineEnd - lineStart);

            _pc.Process(line);
            lineStart = lineEnd + 1;
        }

        _pc.EndOfFileReached();
        return _pc.FinalizeFile();
    }

    private class ParseContext
    {
        private readonly List<ObjFile.Face> _currentGroupFaces = new();

        private readonly List<ObjFile.MeshGroup> _groups = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector3> _positions = new();
        private readonly List<Vector2> _texCoords = new();

        private string _currentGroupName = string.Empty;

        private int _currentLine;
        private string _currentLineText = string.Empty;
        private string _currentMaterial = string.Empty;
        private int _currentSmoothingGroup;

        private string _materialLibName = string.Empty;

        public void Process(string line)
        {
            _currentLine++;
            _currentLineText = line;

            var pieces = line.Split(_whitespaceChars, StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length == 0 || pieces[0].StartsWith("#")) return;
            switch (pieces[0])
            {
                case "v":
                    ExpectExactly(pieces, 3, "v");
                    DiscoverPosition(ParseVector3(pieces[1], pieces[2], pieces[3], "position data"));
                    break;
                case "vn":
                    ExpectExactly(pieces, 3, "vn");
                    DiscoverNormal(ParseVector3(pieces[1], pieces[2], pieces[3], "normal data"));
                    break;
                case "vt":
                    ExpectAtLeast(pieces, 1, "vt");
                    var texCoord = ParseVector2(pieces[1], pieces[2], "texture coordinate data");
                    // Flip v coordinate
                    texCoord.Y = 1f - texCoord.Y;
                    DiscoverTexCoord(texCoord);
                    break;
                case "g":
                    ExpectAtLeast(pieces, 1, "g");
                    FinalizeGroup();
                    _currentGroupName = line.Substring(1, line.Length - 1).Trim();
                    break;
                case "usemtl":
                    ExpectExactly(pieces, 1, "usematl");
                    if (!string.IsNullOrEmpty(_currentMaterial))
                    {
                        var nextGroupName = _currentGroupName + "_Next";
                        FinalizeGroup();
                        _currentGroupName = nextGroupName;
                    }

                    _currentMaterial = pieces[1];
                    break;
                case "s":
                    ExpectExactly(pieces, 1, "s");
                    if (pieces[1] == "off")
                        _currentSmoothingGroup = 0;
                    else
                        _currentSmoothingGroup = ParseInt(pieces[1], "smoothing group");
                    break;
                case "f":
                    ExpectAtLeast(pieces, 3, "f");
                    ProcessFaceLine(pieces);
                    break;
                case "mtllib":
                    ExpectExactly(pieces, 1, "mtllib");
                    DiscoverMaterialLib(pieces[1]);
                    break;
                default:
                    throw new ObjParseException(
                        string.Format("An unsupported line-type specifier, '{0}', was used on line {1}, \"{2}\"",
                            pieces[0],
                            _currentLine,
                            _currentLineText));
            }
        }

        private void DiscoverMaterialLib(string libName)
        {
            if (!string.IsNullOrEmpty(_materialLibName))
                throw new ObjParseException(
                    $"mtllib appeared again in the file. It should only appear once. Line {_currentLine}, \"{_currentLineText}\"");

            _materialLibName = libName;
        }

        private void ProcessFaceLine(string[] pieces)
        {
            var first = pieces[1];
            var faceVertex0 = ParseFaceVertex(first);

            for (var i = 0; i < pieces.Length - 3; i++)
            {
                var second = pieces[i + 2];
                var faceVertex1 = ParseFaceVertex(second);
                var third = pieces[i + 3];
                var faceVertex2 = ParseFaceVertex(third);

                DiscoverFace(new ObjFile.Face(faceVertex0, faceVertex1, faceVertex2, _currentSmoothingGroup));
            }
        }

        private ObjFile.FaceVertex ParseFaceVertex(string faceComponents)
        {
            var slashSplit = faceComponents.Split(_slashChar, StringSplitOptions.None);
            if (slashSplit.Length != 1 && slashSplit.Length != 2 && slashSplit.Length != 3)
                throw CreateExceptionForWrongFaceCount(slashSplit.Length);

            var pos = ParseInt(slashSplit[0], "the first face position index");

            var texCoord = -1;
            if (slashSplit.Length >= 2 && !string.IsNullOrEmpty(slashSplit[1]))
                texCoord = ParseInt(slashSplit[1], "the first face texture coordinate index");

            var normal = -1;
            if (slashSplit.Length == 3) normal = ParseInt(slashSplit[2], "the first face normal index");

            return new ObjFile.FaceVertex { PositionIndex = pos, NormalIndex = normal, TexCoordIndex = texCoord };
        }

        private ObjParseException CreateExceptionForWrongFaceCount(int count)
        {
            return new ObjParseException(
                $"Expected 1, 2, or 3 face components, but got {count}, on line {_currentLine}, \"{_currentLineText}\"");
        }

        public void DiscoverPosition(Vector3 position)
        {
            _positions.Add(position);
        }

        public void DiscoverNormal(Vector3 normal)
        {
            _normals.Add(normal);
        }

        public void DiscoverTexCoord(Vector2 texCoord)
        {
            _texCoords.Add(texCoord);
        }

        public void DiscoverFace(ObjFile.Face face)
        {
            _currentGroupFaces.Add(face);
        }

        public void FinalizeGroup()
        {
            if (!string.IsNullOrEmpty(_currentGroupName))
            {
                var faces = _currentGroupFaces.ToArray();
                _groups.Add(new ObjFile.MeshGroup(_currentGroupName, _currentMaterial, faces));

                _currentGroupName = String.Empty;
                _currentMaterial = String.Empty;
                _currentSmoothingGroup = -1;
                _currentGroupFaces.Clear();
            }
        }

        public void EndOfFileReached()
        {
            _currentGroupName = !string.IsNullOrEmpty(_currentGroupName) ? _currentGroupName : "GlobalFileGroup";
            _groups.Add(new ObjFile.MeshGroup(_currentGroupName, _currentMaterial, _currentGroupFaces.ToArray()));
        }

        public ObjFile FinalizeFile()
        {
            return new ObjFile(_positions.ToArray(), _normals.ToArray(), _texCoords.ToArray(), _groups.ToArray(),
                _materialLibName);
        }

        private Vector3 ParseVector3(string xStr, string yStr, string zStr, string location)
        {
            try
            {
                var x = float.Parse(xStr, CultureInfo.InvariantCulture);
                var y = float.Parse(yStr, CultureInfo.InvariantCulture);
                var z = float.Parse(zStr, CultureInfo.InvariantCulture);

                return new Vector3(x, y, z);
            }
            catch (FormatException fe)
            {
                throw CreateParseException(location, fe);
            }
        }

        private Vector2 ParseVector2(string xStr, string yStr, string location)
        {
            try
            {
                var x = float.Parse(xStr, CultureInfo.InvariantCulture);
                var y = float.Parse(yStr, CultureInfo.InvariantCulture);

                return new Vector2(x, y);
            }
            catch (FormatException fe)
            {
                throw CreateParseException(location, fe);
            }
        }

        private int ParseInt(string intStr, string location)
        {
            try
            {
                var i = int.Parse(intStr, CultureInfo.InvariantCulture);
                return i;
            }
            catch (FormatException fe)
            {
                throw CreateParseException(location, fe);
            }
        }

        private void ExpectExactly(string[] pieces, int count, string name)
        {
            if (pieces.Length != count + 1)
            {
                var message = string.Format(
                    "Expected exactly {0} components to a line starting with {1}, on line {2}, \"{3}\".",
                    count,
                    name,
                    _currentLine,
                    _currentLineText);
                throw new ObjParseException(message);
            }
        }

        private void ExpectAtLeast(string[] pieces, int count, string name)
        {
            if (pieces.Length < count + 1)
            {
                var message = string.Format(
                    "Expected at least {0} components to a line starting with {1}, on line {2}, \"{3}\".",
                    count,
                    name,
                    _currentLine,
                    _currentLineText);
                throw new ObjParseException(message);
            }
        }

        private ObjParseException CreateParseException(string location, FormatException fe)
        {
            var message = string.Format("An error ocurred while parsing {0} on line {1}, \"{2}\"", location,
                _currentLine, _currentLineText);
            return new ObjParseException(message, fe);
        }
    }
}

/// <summary>
///     An parsing error for Wavefront OBJ files.
/// </summary>
public class ObjParseException : Exception
{
    /// <summary />
    public ObjParseException(string message) : base(message)
    {
    }

    /// <summary />
    public ObjParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
///     Represents a parset Wavefront OBJ file.
/// </summary>
public class ObjFile
{
    /// <summary />
    public ObjFile(Vector3[] positions, Vector3[] normals, Vector2[] texCoords, MeshGroup[] meshGroups,
        string materialLibName)
    {
        Positions = positions;
        Normals = normals;
        TexCoords = texCoords;
        MeshGroups = meshGroups;
        MaterialLibName = materialLibName;
    }

    /// <summary />
    public Vector3[] Positions { get; }

    /// <summary />
    public Vector3[] Normals { get; }

    /// <summary />
    public Vector2[] TexCoords { get; }

    /// <summary />
    public MeshGroup[] MeshGroups { get; }

    /// <summary />
    public string MaterialLibName { get; }

    /// <summary>
    ///     An OBJ file construct describing an individual mesh group.
    /// </summary>
    public struct MeshGroup
    {
        /// <summary>
        ///     The name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The name of the associated <see cref="MaterialDefinition" />.
        /// </summary>
        public readonly string Material;

        /// <summary>
        ///     The set of <see cref="Face" />s comprising this mesh group.
        /// </summary>
        public readonly Face[] Faces;

        /// <summary>
        ///     Constructs a new <see cref="MeshGroup" />.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="material">The name of the associated <see cref="MaterialDefinition" />.</param>
        /// <param name="faces">The faces.</param>
        public MeshGroup(string name, string material, Face[] faces)
        {
            Name = name;
            Material = material;
            Faces = faces;
        }
    }

    /// <summary>
    ///     An OBJ file construct describing the indices of vertex components.
    /// </summary>
    public struct FaceVertex
    {
        /// <summary>
        ///     The index of the position component.
        /// </summary>
        public int PositionIndex;

        /// <summary>
        ///     The index of the normal component.
        /// </summary>
        public int NormalIndex;

        /// <summary>
        ///     The index of the texture coordinate component.
        /// </summary>
        public int TexCoordIndex;


        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("Pos:{0}, Normal:{1}, TexCoord:{2}", PositionIndex, NormalIndex, TexCoordIndex);
        }
    }

    /// <summary>
    ///     An OBJ file construct describing an individual mesh face.
    /// </summary>
    public struct Face
    {
        /// <summary>
        ///     The first vertex.
        /// </summary>
        public readonly FaceVertex Vertex0;

        /// <summary>
        ///     The second vertex.
        /// </summary>
        public readonly FaceVertex Vertex1;

        /// <summary>
        ///     The third vertex.
        /// </summary>
        public readonly FaceVertex Vertex2;

        /// <summary>
        ///     The smoothing group. Describes which kind of vertex smoothing should be applied.
        /// </summary>
        public readonly int SmoothingGroup;

        /// <summary />
        public Face(FaceVertex v0, FaceVertex v1, FaceVertex v2, int smoothingGroup = -1)
        {
            Vertex0 = v0;
            Vertex1 = v1;
            Vertex2 = v2;

            SmoothingGroup = smoothingGroup;
        }
    }
}