using System;

namespace SrinokanDreams
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (GameStart game = new GameStart())
            {
                game.Run();
            }
        }
    }
#endif
}

