using System;

namespace FlightSim_AMS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (FlightSimulator game = new FlightSimulator())
            {
                game.Run();
            }
        }
    }
}

