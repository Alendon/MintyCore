using System;
using FreeTypeSharp;
using FreeTypeSharp.Native;
using MintyCore.Render;
using static FreeTypeSharp.Native.FT;

namespace MintyCore.UI;

public readonly unsafe struct Font
{
    private readonly IntPtr facePtr;
    public FT_FaceRec* Face => (FT_FaceRec*)facePtr;
        
    internal Font(IntPtr face)
    {
        facePtr = face;
    }

    public void Draw(string text)
    {
            
    }
}