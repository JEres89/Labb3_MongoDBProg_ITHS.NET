﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb2_CsProg_ITHS.NET.Game;
internal class Dice
{
    private static Random _random = new();

    internal static int Roll(int sides, int num)
    {
        int rolls = 0;
        for (int i = 0; i < num; i++)
        {
            rolls += _random.Next(1, sides + 1);
        }
        return rolls;
    }
}
