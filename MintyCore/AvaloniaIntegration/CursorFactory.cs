using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Platform;

namespace MintyCore.AvaloniaIntegration;

public class CursorFactory : ICursorFactory
{
    public ICursorImpl GetCursor(StandardCursorType cursorType)
    {
        
        //TODO actually implement this
        return new CursorImpl();
    }

    public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
    {
        //TODO: Investigate Custom cursor support
        throw new NotSupportedException("Custom cursors aren't supported");
    }
    
    class CursorImpl : ICursorImpl
    {
        public void Dispose()
        {
        }
    }
}