﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var activities = new HashSet<LogEvent>();

            for (char ch = 'A'; ch < 'F'; ch++)
            {
                activities.Add(new LogEvent { Id = "" + ch, Name = ""});
            }

            var exAl = new ExhaustiveApproach(activities);

            while(true)
            {
                exAl.AddEvent(Console.ReadLine());

                Console.WriteLine(exAl.Graph);
            }
            // TODO: Read from log
            // TODO: Build Processes, LogTraces and LogEvents
            
            // TODO: Run main algorithm
        }
    }
}
