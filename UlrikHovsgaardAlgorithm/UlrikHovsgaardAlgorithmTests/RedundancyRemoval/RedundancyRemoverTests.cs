﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithmTests.RedundancyRemoval
{
    [TestClass()]
    public class RedundancyRemoverTests
    {
        [TestMethod()]
        //Include: Hvis B kun kan køres efter A(enten via inclusion eller condition) og både A og B har en include relation til C, kan “B->+C” altid slettes.
        public void TestAlwaysIncludeIncludedRedundant()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = false };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);
            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityC.Id);
            var newDcr = new RedundancyRemover().RemoveRedundancy(dcrGraph);    


            //we should now have removed include b -> c. so we are asserting that B no longer has a include relation
            Assert.IsFalse(newDcr.GetIncludeOrExcludeRelation(activityB, true).Contains(activityC));
        }
        
        [TestMethod()]
        //Include: condition A->B er redundant med include A->B, hvis B ikke har andre indgående includes og A ikke kan blive ekskluderet. 
        public void TestIncludeConditionRedundancy()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            //Now the redundant condition relation from A to B should be removed:
            Assert.IsFalse(newGraph.InRelation(activityB, newGraph.Conditions));
            
        }
        
        [TestMethod()]
        //Include: Response og Exclude fra samme source til samme target er i udgangspunkt redundante, hvis target ikke har nogle indgående include relationer.
        public void TestResponseAndExcludeRedundantIfNoTargetInclude()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            //Now the redundant response relation should be removed:

            Assert.IsFalse(newGraph.InRelation(activityB, newGraph.Responses));
        }

        [TestMethod()]
        //Hvis en aktivitet er included og ikke har nogle indgående exclude-relationer, er indgående include-relationer redundante. 
        public void TestIncludesToIncludedRedundant()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            
            //Now the redundant include relation should be removed:");
            
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);
            Assert.IsFalse(newGraph.InRelation(activityA, newGraph.IncludeExcludes));
        }

        [TestMethod()]
        //Condition og milestone er redundante med hinanden fra A til B, hvis A er excluded og altid bliver sat som pending, når den bliver inkluderet. 
        public void TestConditionMilestoneRedundancy()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddMileStone(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddResponse(activityC.Id, activityA.Id);

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            //Now either the redundant Condition or Milestone relation should be removed:");
            Assert.IsFalse(newGraph.InRelation(activityA, newGraph.Conditions) && newGraph.InRelation(activityA, newGraph.Milestones));
        }

        [TestMethod()]
        //Ikke-included og ingen include relationer med den som target, gør det er redundant at den (og alle dens udadgående relationer) er medtaget i grafen. 
        public void TestActivityAlwaysExcludedRedundant()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddMileStone(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityC.Id);
            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddResponse(activityB.Id, activityA.Id);

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            //Now all the relations from or to A should be removed:");
            Assert.IsTrue(!newGraph.InRelation(activityA, newGraph.Conditions)
                && !newGraph.InRelation(activityA, newGraph.IncludeExcludes)
                && !newGraph.InRelation(activityA, newGraph.Responses)
                && !newGraph.InRelation(activityA, newGraph.Milestones));
        }

        [TestMethod()]
        //Hvis A er pending og excluded, og der er en response og include relation fra B til A, så er pending og response redundante med hinanden, hvis B ikke kan køres efter A. 
        public void RedundancyTestCase7()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            //Now either the redundant response relation or A's initial pending state should be removed:");
            Assert.IsFalse(newGraph.InRelation(activityA, newGraph.Responses) && newGraph.GetActivity(activityA.Id).Pending);
        }

        
        [TestMethod()]
        public void TestNonRedundantIncludeIsNotRemoved()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Assert.IsTrue(newGraph.InRelation(activityC,newGraph.IncludeExcludes));
        }

        [TestMethod()]
        public void TestNonRedundantExcludeIsNotRemoved()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Assert.IsTrue(newGraph.InRelation(activityC, newGraph.IncludeExcludes));
        }

        [TestMethod()]
        public void TestNonRedundantResponseIsNotRemoved()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Assert.IsTrue(newGraph.InRelation(activityB, newGraph.Responses));
        }

        [TestMethod()]
        public void TestNonRedundantResponseIsNotRemoved2()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);

            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);

            dcrGraph.AddIncludeExclude(false, activityA.Id, activityA.Id);
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Assert.IsTrue(newGraph.InRelation(activityA, newGraph.Responses));
        }

        [TestMethod()]
        public void TestNonRedundantConditionIsNotRemoved()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddCondition(activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Assert.IsTrue(newGraph.InRelation(activityB, newGraph.Conditions));
        }

        [TestMethod()]
        public void TestNonRedundantMilestoneIsNotRemoved()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityC.Id);
            dcrGraph.AddMileStone(activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Assert.IsTrue(newGraph.InRelation(activityC, newGraph.Milestones));
        }

        [TestMethod()]
        public void TestRedundantActivityIsRemoved()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Assert.IsNull(newGraph.GetActivity(activityA.Id));
        }

        [TestMethod()]
        public void RedundantRemoverWithNested()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = true };
            var activityF = new Activity("F", "somename6") { Included = true };


            dcrGraph.AddActivities(activityA, activityB, activityC, activityD, activityE, activityF);

            dcrGraph.AddIncludeExclude(true, activityC.Id,activityD.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityE.Id); //Redundant include
            dcrGraph.AddCondition(activityE.Id,activityF.Id); //outgoing relation
            //ingoing relation
            dcrGraph.AddCondition(activityA.Id,activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() {activityC, activityD, activityE});

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);
            
            //we check that the Nested graph has had the redundant relation removed.
            Assert.IsFalse(newGraph.InRelation(activityE,newGraph.IncludeExcludes));
        }

        [TestMethod()]
        public void RedundantRemoverWithNestedCondition()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = true };
            var activityF = new Activity("F", "somename6") { Included = true };


            dcrGraph.AddActivities(activityA, activityB, activityC, activityD, activityE, activityF);

            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityE.Id); //Redundant include
            dcrGraph.AddCondition(activityE.Id, activityF.Id); //outgoing relation
            //ingoing relation
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            Activity nestedActivity = newGraph.Activities.First(a => a.IsNestedGraph);

            //we check that the Nested graph has had the redundant relation removed.
            Assert.IsFalse(newGraph.InRelation(nestedActivity, newGraph.Conditions));
        }

        [TestMethod()]
        public void RedundantRemoverWithNestedRedundantIncludes()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            
            dcrGraph.AddActivities(activityA, activityB, activityC, activityD);

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityD.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityB, activityC, activityD });

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id); // redundant include

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            //we check that the (redundant) include to an activity within an included nested graph was removed
            Assert.IsFalse(newGraph.InRelation(activityB, newGraph.IncludeExcludes));
        }

        [TestMethod()]
        public void RedundantRemoverWithNestedAndNoRemoval()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = false };
            var activityF = new Activity("F", "somename6") { Included = true };
            

            dcrGraph.AddActivities(activityA,activityB,activityC,activityD,activityE,activityF);

            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityE.Id); //non-Redundant include
            dcrGraph.AddCondition(activityE.Id, activityF.Id); //outgoing relation
            //ingoing relation
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });

            var newGraph = new RedundancyRemover().RemoveRedundancy(dcrGraph);

            //we check that the Nested graph has not removed a relation.
            Assert.IsTrue(newGraph.InRelation(activityE, newGraph.IncludeExcludes));

        }
    }
}