using Godot;
using System;

public static class PhysicsConstants
{
    public static uint Mask(params int[] layers)
    {
        uint mask = 0;
        foreach (int layer in layers)
        {
            if (layer < 1 || layer > 32)
                throw new ArgumentOutOfRangeException(nameof(layer), "Layer must be between 1 and 32");
            mask |= 1u << (layer - 1);
        }
        return mask;
    }

    public static readonly uint CHARACTER_COLLIDABLES_MASK = Mask(10);

}
