﻿namespace OpenTK_Learning
{
    class Program
    {
        static void Main()
        {
            using Main game = new Main(1920, 1080, "OpenGL - GA");
            game.Run();
        }
    }
}