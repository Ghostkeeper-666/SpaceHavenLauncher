using System;
using System.Collections.Generic;

namespace SH.Modding.Build;

public sealed class SpritePacker
{
    private static readonly Comparer<SpriteRectangle> AreaDescending =
        Comparer<SpriteRectangle>.Create(static (a, b) => b.Area.CompareTo(a.Area));

    public List<SpriteRectangle> Rectangles { get; private set; } = [];
    public SpriteRectangle Bounds { get; private set; }
    public int MaxRectangles => Working.Length;
    public double Occupancy { get; private set; }

    private readonly int MaxWidth;
    private readonly int MaxHeight;

    private readonly List<SpriteRectangle> Slots;
    private readonly SpriteRectangle[] Working;

    public SpritePacker(int width, int height, int maxRectangles)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "must be greater than 0");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "must be greater than 0");
        if (maxRectangles <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRectangles), maxRectangles, "must be greater than 0");

        MaxWidth = width;
        MaxHeight = height;

        Working = new SpriteRectangle[maxRectangles];
        Slots = new List<SpriteRectangle>(2 * maxRectangles);
    }

    public bool TryPack(List<SpriteRectangle> rectangles)
    {
        Rectangles = rectangles ?? throw new ArgumentNullException(nameof(rectangles));

        int count = Rectangles.Count;
        if (count == 0)
        {
            Occupancy = 0;
            return true;
        }

        if (count > Working.Length)
            throw new ArgumentException("Too many rectangles!", nameof(rectangles));

        rectangles.CopyTo(Working, 0);
        Array.Sort(Working, 0, count, AreaDescending);

        if (!TryPackRectangles(count, MaxWidth, MaxHeight, out int boundsWidth, out int boundsHeight))
            return false;

        // Unable to fit all:
        if (boundsWidth <= 0 || boundsHeight <= 0 || boundsWidth > MaxWidth || boundsHeight > MaxHeight)
            return false;

        // Done.
        Bounds = new SpriteRectangle(0, 0, boundsWidth, boundsHeight);

        int rectsArea = 0;
        for (int i = 0; i < count; ++i)
        {
            rectangles[i] = Working[i];
            rectsArea += Working[i].Area;
        }
        Occupancy = rectsArea / (double)(MaxWidth * MaxHeight);
        return true;
    }

    private bool TryPackRectangles(int count, int binWidth, int binHeight, out int boundsWidth, out int boundsHeight)
    {
        boundsWidth = 0;
        boundsHeight = 0;

        Slots.Clear();
        Slots.Add(new SpriteRectangle(0, 0, binWidth, binHeight));

        for (int r = 0; r < count; ++r)
        {
            SpriteRectangle packed = Working[r];

            if (!TryFindSlot(packed, out int slotIndex))
                return false;

            SpriteRectangle slot = Slots[slotIndex];
            packed.X = slot.X;
            packed.Y = slot.Y;
            Working[r] = packed;

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

        // Done.
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
        int key = SlotSortKey(rectangle);
        int min = 0;
        int max = Slots.Count - 1;
        while (min <= max)
        {
            int middle = (min + max) >> 1;
            if (key < SlotSortKey(Slots[middle]))
                max = middle - 1;
            else
                min = middle + 1;
        }
        Slots.Insert(min, rectangle);
    }

    private void SortSlots(int slotIndex)
    {
        SpriteRectangle rectangle = Slots[slotIndex];
        int key = SlotSortKey(rectangle);

        int index = slotIndex;
        while (index + 1 < Slots.Count && key > SlotSortKey(Slots[index + 1]))
        {
            Slots[index] = Slots[index + 1];
            ++index;
        }
        Slots[index] = rectangle;
    }

    private static int SlotSortKey(in SpriteRectangle rectangle) =>
        Math.Max(rectangle.X, rectangle.Y);
}