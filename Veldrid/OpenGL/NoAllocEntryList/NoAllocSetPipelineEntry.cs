﻿namespace MintyVeldrid.OpenGL.NoAllocEntryList
{
    internal struct NoAllocSetPipelineEntry
    {
        public readonly Tracked<Pipeline> Pipeline;

        public NoAllocSetPipelineEntry(Tracked<Pipeline> pipeline)
        {
            Pipeline = pipeline;
        }
    }
}