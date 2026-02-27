using Godot;
using System;
using System.Collections.Generic;

public static class ListUtils
{
    private static Random _random = new Random();

    public static T RandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new InvalidOperationException("Cannot pick a random element from an empty list.");

        return list[_random.Next(list.Count)];
    }
}
