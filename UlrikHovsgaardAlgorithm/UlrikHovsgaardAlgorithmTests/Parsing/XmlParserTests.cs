﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Parsing;

namespace UlrikHovsgaardAlgorithmTests.Parsing
{
    [TestClass()]
    public class XmlParserTests
    {
        [TestMethod()]
        public void ParseLogTest()
        {

            var log =
                XmlParser.ParseLog(
                                new LogStandard("http://www.xes-standard.org/", "trace",
                                    new LogStandardEntry(DataType.String, "conceptName"), "event",
                                    new LogStandardEntry(DataType.String, "conceptName"),
                                    new LogStandardEntry(DataType.String, "activityNameEN"),
                                    new LogStandardEntry(DataType.String, "")), UlrikHovsgaardAlgorithm.Properties.Resources.BPIC15_1_xes);
            Console.WriteLine("Finished parsing " + log.Traces.Count);
            foreach (var trace in log.Traces.First().Events)
            {
                Console.WriteLine("Example trace: " + log.Traces.First().Id);
                Console.Write("ID: " + trace.IdOfActivity + ", Name: " + trace.Name + "   |   ");
            }
            Console.ReadLine();

            
            Assert.IsTrue(log.Traces.Any(t => t.Events.Count > 5));

        }
    }
}