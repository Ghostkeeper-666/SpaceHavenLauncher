using System;

namespace SH.Content.Video;

public sealed class Clip : IDisposable
{
    public Clip(string name, int frameRate, int frameCount)
    {
        Name = name;
        FrameRate = frameRate;
        Frames = new Frame[frameCount < 1 ? 1 : frameCount];
    }

    public string Name { get; }
    public int FrameRate { get; } = 60;
    public Frame[] Frames { get; }

    public void Dispose()
    {
        for(int f = 0; f < Frames.Length; ++f)
        {
            Frames[f].Dispose();
            Frames[f] = null;
        }
    }
}
