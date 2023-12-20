using System;

namespace ProjectZ.InGame.Controls
{
    [Flags]
    public enum CButtons
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
        A = 16,
        B = 32,
        X = 64,
        Y = 128,
        Select = 256,
        Start = 512,
        L = 1024,
        R = 2048
    }
}