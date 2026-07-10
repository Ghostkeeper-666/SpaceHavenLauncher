using System;
using System.Collections.Generic;

namespace SH.RectPack;

public sealed class SpritePacker
{
    public List<SpriteRectangle> Rectangles { get; private set; } = [];
    public SpriteRectangle Bounds { get; private set; }
    public int MaxRectangles => OverallBest.Length;
    public int Shrink { get; }
    public double MaxOccupancy { get; }
    public double Occupancy { get; private set;}

    private readonly int MaxWidth;
    private readonly int MaxHeight;

    private readonly List<SpriteRectangle> Slots;
    private SpriteRectangle[] OverallBest;
    private SpriteRectangle[] LocalBest;
    private SpriteRectangle[] Buffer;

    public SpritePacker(int width, int height, int maxRectangles, int shrink, double maxOccupancy)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "must be greater than 0");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "must be greater than 0");
        if (maxRectangles <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRectangles), maxRectangles, "must be greater than 0");
        if (shrink <= 0)
            throw new ArgumentOutOfRangeException(nameof(shrink), shrink, "must be greater than 0");
        if (maxOccupancy <= 0 || maxOccupancy > 1.0)
            throw new ArgumentOutOfRangeException(nameof(maxOccupancy), maxOccupancy, "must be > 0 and <= 1");

        MaxWidth = width;
        MaxHeight = height;
        Shrink = shrink;
        MaxOccupancy = maxOccupancy;

        OverallBest = new SpriteRectangle[maxRectangles];
        LocalBest = new SpriteRectangle[maxRectangles];
        Buffer = new SpriteRectangle[maxRectangles];

        Slots = new List<SpriteRectangle>(2 * maxRectangles);
    }

    public bool TryPack(List<SpriteRectangle> rectangles)
    {
        Rectangles = rectangles ?? throw new ArgumentNullException(nameof(rectangles));
        if (Rectangles.Count == 0)
        {
            Occupancy = 0;
            return true;
        }

        if (Rectangles.Count > OverallBest.Length)
            throw new ArgumentException("Too many rectangles.", nameof(rectangles));

        int rectanglesAreaSum = 0;
        for (int i = 0; i < Rectangles.Count; ++i)
            rectanglesAreaSum += rectangles[i].Area;

        rectangles.CopyTo(OverallBest, 0);
        for (int i = 0; i < Rectangles.Count; ++i)
            OverallBest[i].SortKey = OverallBest[i].Area;
        Array.Sort(OverallBest, 0, Rectangles.Count, Comparer<SpriteRectangle>.Create(static (a, b) => b.SortKey.CompareTo(a.SortKey)));
        OverallBest.AsSpan(0, rectangles.Count).CopyTo(LocalBest.AsSpan(0, rectangles.Count));

        SpriteRectangle bounds = default;
        int width = MaxWidth;
        int height = MaxHeight;
        int maxArea = ComputeMaxArea(rectanglesAreaSum);
        do
        {
            if (!TryPackRectangles(width, height, out int boundsWidth, out int boundsHeight))
                break;

            bounds.Width = boundsWidth;
            bounds.Height = boundsHeight;

            // Swap working buffers
            (Buffer, LocalBest) = (LocalBest, Buffer);
            width = bounds.Width <= Shrink ? 1 : bounds.Width - Shrink;
            height = bounds.Height <= Shrink ? 1 : bounds.Height - Shrink;
        }
        while (bounds.Area > maxArea);

        // Unable to fit all:
        if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.Width > MaxWidth || bounds.Height > MaxHeight)
            return false;

        // Done.
        Bounds = bounds;
        (OverallBest, LocalBest) = (LocalBest, OverallBest);
        int rectsArea = 0;
        for (int i = 0; i < rectangles.Count; ++i)
        {
            rectangles[i] = OverallBest[i];
            rectsArea += rectangles[i].Area;
        }
        Occupancy = rectsArea / (double)(MaxWidth * MaxHeight);
        return true;
    }


    private void SortByArea(SpriteRectangle[] buffer)
    {
    }


    private int ComputeMaxArea(int rectanglesAreaSum)
    {
        double value = Math.Ceiling(rectanglesAreaSum / MaxOccupancy);
        if (value <= 0)
            return rectanglesAreaSum;
        if (double.IsPositiveInfinity(value))
            return int.MaxValue;
        return (int)value;
    }


    private bool TryPackRectangles(int binWidth, int binHeight, out int boundsWidth, out int boundsHeight)
    {
        boundsWidth = 0;
        boundsHeight = 0;

        Slots.Clear();
        Slots.Add(new SpriteRectangle(0, 0, binWidth, binHeight));

        for (int r = 0; r < Rectangles.Count; ++r)
        {
            ref readonly SpriteRectangle source = ref LocalBest[r];

            if (!TryFindSlot(source, out int slotIndex))
                return false;

            SpriteRectangle slot = Slots[slotIndex];
            SpriteRectangle packed = source;
            packed.X = slot.X;
            packed.Y = slot.Y;
            Buffer[r] = packed;

            int right = packed.Right;
            if (right > boundsWidth)
                boundsWidth = right;

            int bottom = packed.Bottom;
            if (bottom > boundsHeight)
                boundsHeight = bottom;

            int remainingWidth = slot.Width - packed.Width;
            int remainingHeight = slot.Height - packed.Height;

            // Split slot into two remaining slots
            if (remainingWidth > 0 && remainingHeight > 0)
            {
                Slots.RemoveAt(slotIndex);
                if (remainingWidth > remainingHeight)
                {
                    AddSlot(new SpriteRectangle(packed.Right, slot.Y, remainingWidth, slot.Height));
                    AddSlot(new SpriteRectangle(slot.X, packed.Bottom, packed.Width, remainingHeight));
                }
                else
                {
                    AddSlot(new SpriteRectangle(slot.X, packed.Bottom, slot.Width, remainingHeight));
                    AddSlot(new SpriteRectangle(packed.Right, slot.Y, remainingWidth, packed.Height));
                }
            }

            // Slot filled vertically
            else if (remainingWidth <= 0)
            {
                slot.Y += packed.Height;
                slot.Height = remainingHeight;

                Slots[slotIndex] = slot;
                SortSlots(slotIndex);
            }

            // Slot filled horizontally
            else if (remainingHeight <= 0)
            {
                slot.X += packed.Width;
                slot.Width = remainingWidth;

                Slots[slotIndex] = slot;
                SortSlots(slotIndex);
            }

            // Slot completely consumed
            else Slots.RemoveAt(slotIndex);
        }

        //done.
        return true;
    }


    private bool TryFindSlot(in SpriteRectangle rectangle, out int index)
    {
        for (int i = 0; i < Slots.Count; ++i)
        {
            SpriteRectangle slot = Slots[i];
            if (rectangle.Width <= slot.Width && rectangle.Height <= slot.Height)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }


    private void AddSlot(SpriteRectangle rectangle)
    {
        rectangle.SortKey = Math.Max(rectangle.X, rectangle.Y);
        int min = 0;
        int max = Slots.Count - 1;
        while (min <= max)
        {
            int middle = (min + max) >> 1;
            if (rectangle.SortKey < Slots[middle].SortKey)
                max = middle - 1;
            else
                min = middle + 1;
        }
        Slots.Insert(min, rectangle);
    }


    private void SortSlots(int slotIndex)
    {
        SpriteRectangle rectangle = Slots[slotIndex];

        int newSortKey = Math.Max(rectangle.X, rectangle.Y);

        if (newSortKey == rectangle.SortKey)
            return;

        rectangle.SortKey = newSortKey;
        int index = slotIndex;
        while (index + 1 < Slots.Count && newSortKey > Slots[index + 1].SortKey)
        {
            Slots[index] = Slots[index + 1];
            ++index;
        }
        Slots[index] = rectangle;
    }
}