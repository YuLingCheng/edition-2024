﻿using Microsoft.Diagnostics.Runtime;

namespace ListenerGui.Models;

public class SubHeap
{
    public SubHeap()
    {

    }

    public SubHeap(ClrSubHeap subHeap)
    {
        Segments = subHeap.Segments.Select(s => new Segment(s)).ToList();
        Index = subHeap.Index;
    }

    public int Index { get; set; }

    public IReadOnlyList<Segment> Segments { get; set; }
}