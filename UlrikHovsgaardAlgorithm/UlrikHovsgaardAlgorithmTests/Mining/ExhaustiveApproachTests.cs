﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithmTests.Mining
{
    [TestClass()]
    public class ExhaustiveApproachTests
    {
        //Test that the inner graph contains the included event A.
        [TestMethod()]
        public void AddEventTest()
        {
            var a = new Activity("A", "nameA");

            var exhaust = new ContradictionApproach(new HashSet<Activity>() {a,new Activity("B","nameB")});

            exhaust.AddEvent("A", "1000");

            Assert.IsTrue(exhaust.Graph.GetIncludedActivities().Contains(a));
        }

        //Test that the Graph has a condition
        [TestMethod()]
        public void ConditionTest()
        {
            var a = new Activity("A", "nameA");

            var b = new Activity("B", "nameB");


            var exhaust = new ContradictionApproach(new HashSet<Activity>() { a, b });

            exhaust.AddEvent("A", "1000");
            exhaust.AddEvent("B", "1000");
            exhaust.Stop();

            Dictionary<Activity,Confidence> con;

            exhaust.Graph.Conditions.TryGetValue(a, out con);

            Assert.IsTrue(con.ContainsKey(b));
        }

        //Test that the Graph does not have a condition to B
        [TestMethod()]
        public void ConditionNegativeTest()
        {
            var a = new Activity("A", "nameA");

            var b = new Activity("B", "nameB");


            var exhaust = new ContradictionApproach(new HashSet<Activity>() { a, b });
            
            exhaust.AddEvent("B", "1000");
            exhaust.Stop();

            Dictionary<Activity, Confidence> con;

            exhaust.Graph.Conditions.TryGetValue(a, out con);

            Assert.IsFalse(con[b].Get <= Threshold.Value );
        }

        //log of size 100.000 traces with 8 random events in each
        [TestMethod()]
        public void ExhaustiveWithBigDataLog()
        {
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'Z'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }

            var rnd = new Random();
            var inputLog = new Log();
            var traceId = 1000;
            var currentTrace = new LogTrace() {Id = traceId.ToString()};
            while (inputLog.Traces.Count < 100000)
            {
                currentTrace.Add(new LogEvent(activities.ElementAt(rnd.Next(activities.Count)).Id, ""));

                if (currentTrace.Events.Count == 8)
                {
                    inputLog.AddTrace(currentTrace);
                    traceId++;
                    currentTrace = (new LogTrace() {Id = traceId.ToString()});

                }
                
            }
            
            var exAl = new ContradictionApproach(activities);

            foreach (var trace in inputLog.Traces)
            {
                exAl.AddTrace(trace);
            }

            Assert.IsTrue(true);
        }

        [TestMethod()]
        public void OriginalLogWithThreshold15()
        {

            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new Activity("" + ch, "" + ch));
            }

            var exAl = new ContradictionApproach(activities);



            var trace1 = new LogTrace { Id = "1" };
            trace1.AddEventsWithChars('A', 'B', 'E');
            var trace2 = new LogTrace { Id = "2" };
            trace2.AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F');
            var trace3 = new LogTrace { Id = "3" };
            trace3.AddEventsWithChars('A', 'C', 'E');
            var trace4 = new LogTrace { Id = "4" };
            trace4.AddEventsWithChars('A', 'D', 'F');
            var trace5 = new LogTrace { Id = "5" };
            trace5.AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E');
            var trace6 = new LogTrace { Id = "6" };
            trace6.AddEventsWithChars('A', 'C', 'F');
            var trace7 = new LogTrace { Id = "7" };
            trace7.AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E');
            var trace8 = new LogTrace { Id = "8" };
            trace8.AddEventsWithChars('A', 'B', 'B', 'B', 'F');
            var trace9 = new LogTrace { Id = "9" };
            trace9.AddEventsWithChars('A', 'B', 'B', 'E');
            var trace10 = new LogTrace { Id = "10" };
            trace10.AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E');

            Log log = new Log() { Traces = { trace1, trace2, trace3, trace4, trace5, trace6, trace7, trace8, trace9, trace10 } };

            exAl.AddLog(log);

            Threshold.Value = 0.15;

            exAl.Graph = new RedundancyRemover().RemoveRedundancy(exAl.Graph);
            
            exAl.Graph = ContradictionApproach.PostProcessing(exAl.Graph);

            
            Assert.IsFalse(exAl.Graph.Activities.Any(a => a.Id == "D"));
        }

        [TestMethod()]
        public void OriginalLogWithThreshold0()
        {

            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new Activity("" + ch, "" + ch));
            }

            var exAl = new ContradictionApproach(activities);



            var trace1 = new LogTrace { Id = "1" };
            trace1.AddEventsWithChars('A', 'B', 'E');
            var trace2 = new LogTrace { Id = "2" };
            trace2.AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F');
            var trace3 = new LogTrace { Id = "3" };
            trace3.AddEventsWithChars('A', 'C', 'E');
            var trace4 = new LogTrace { Id = "4" };
            trace4.AddEventsWithChars('A', 'D', 'F');
            var trace5 = new LogTrace { Id = "5" };
            trace5.AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E');
            var trace6 = new LogTrace { Id = "6" };
            trace6.AddEventsWithChars('A', 'C', 'F');
            var trace7 = new LogTrace { Id = "7" };
            trace7.AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E');
            var trace8 = new LogTrace { Id = "8" };
            trace8.AddEventsWithChars('A', 'B', 'B', 'B', 'F');
            var trace9 = new LogTrace { Id = "9" };
            trace9.AddEventsWithChars('A', 'B', 'B', 'E');
            var trace10 = new LogTrace { Id = "10" };
            trace10.AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E');

            Log log = new Log() { Traces = { trace1, trace2, trace3, trace4, trace5, trace6, trace7, trace8, trace9, trace10 } };

            exAl.AddLog(log);

            Threshold.Value = 0;

            exAl.Graph = new RedundancyRemover().RemoveRedundancy(exAl.Graph);

            exAl.Graph = ContradictionApproach.PostProcessing(exAl.Graph);


            Assert.IsFalse(exAl.Graph.Activities.Any(a => a.Id == "D"));
        }



        [TestMethod()]
        public void CreateNestsTest()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = true };
            var activityE = new Activity("E", "somename5") { Included = true, Pending = true };
            var activityF = new Activity("F", "somename6") { Included = true };
            var activityG = new Activity("G", "somename7") { Included = true };

            dcrGraph.AddActivities(activityA, activityB, activityC, activityD, activityE, activityF, activityG);

            dcrGraph.AddResponse(activityB.Id, activityC.Id); //inner nest condition

            //From A to all inner
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityE.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);

            dcrGraph.AddIncludeExclude(false, activityD.Id, activityF.Id); //from in to out
            dcrGraph.AddMileStone(activityF.Id, activityG.Id);

            //From G to all inner and F
            dcrGraph.AddCondition(activityG.Id, activityB.Id);
            dcrGraph.AddCondition(activityG.Id, activityC.Id);
            dcrGraph.AddCondition(activityG.Id, activityD.Id);
            dcrGraph.AddCondition(activityG.Id, activityE.Id);
            dcrGraph.AddCondition(activityG.Id, activityF.Id);

            var exhaust = new ContradictionApproach(dcrGraph.Activities) { Graph = dcrGraph };

            exhaust.Graph = ContradictionApproach.CreateNests(exhaust.Graph);
            
            Assert.IsTrue(exhaust.Graph.Activities.Any(a => a.IsNestedGraph));
        }
        
    }
}