﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.GraphSimulation
{
    public class UniqueTraceFinderWithComparison
    {
        #region Fields

        private List<LogTrace> _uniqueTraces = new List<LogTrace>();
        private Dictionary<string, Dictionary<byte[], int>> _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();
        //private Dictionary<string, DcrGraph> _traceStates = new Dictionary<string, DcrGraph>();
        
        public List<LogTrace> TracesToBeComparedTo { get; }
        private bool _comparisonResult = true;

        private readonly object _lockObject = new object();
        private List<Task> _threads = new List<Task>(); 

        #endregion
        
        public UniqueTraceFinderWithComparison(DcrGraph graph)
        {
            TracesToBeComparedTo = GetUniqueTraces(graph);
        }

        #region Primary methods
        

        #region Working version with regular recursion

        // WORKING
        public List<LogTrace> GetUniqueTraces(DcrGraph inputGraph)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //TODO: remove included nodes with no edges, as they don't matter in the differences between the traces.
            //TODO: add the removed activities after finding the unique traces.

            // Start from scratch
            //_traceStates = new Dictionary<string, DcrGraph>();
            _uniqueTraces = new List<LogTrace>();
            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();

            FindUniqueTraces(new LogTrace { Events = new List<LogEvent>() }, inputGraph, false);

#if DEBUG
            Console.WriteLine("Unique Traces: " + _uniqueTraces.Count);
            Console.WriteLine("Elapsed: " + stopwatch.Elapsed);

            //Console.WriteLine("-----Start-----");
            //foreach (var logTrace in _uniqueTraces)
            //{
            //    foreach (var logEvent in logTrace.Events)
            //    {
            //        Console.Write(logEvent.EventId);
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine("------End------");
#endif

            return _uniqueTraces;
        }

        // WORKING
        public bool CompareTracesFoundWithSupplied(DcrGraph inputGraph)
        {
            if (TracesToBeComparedTo == null)
            {
                throw new Exception("You must first supply traces to be compared to.");
            }

            // Start from scratch
            //_traceStates = new Dictionary<string, DcrGraph>();
            _uniqueTraces = new List<LogTrace>();
            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();

            _comparisonResult = true;

            // Potentially discover that the found traces do not corrspond, altering _comparisonResult to false
            FindUniqueTraces(new LogTrace { Events = new List<LogEvent>() }, inputGraph, true);
            
            if (_uniqueTraces.Count != TracesToBeComparedTo.Count)
            {
                _comparisonResult = false;
            }

#if DEBUG
            Console.WriteLine("Unique Traces: " + _uniqueTraces.Count);

            //Console.WriteLine("-----Start-----");
            //foreach (var logTrace in _uniqueTraces)
            //{
            //    foreach (var logEvent in logTrace.Events)
            //    {
            //        Console.Write(logEvent.EventId);
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine("------End------");
#endif

            return _comparisonResult;
        }

        // WORKING
        private void FindUniqueTraces(LogTrace currentTrace, DcrGraph inputGraph, bool compareTraces)
        {
            var activitiesToRun = inputGraph.GetRunnableActivities();
            var iterations = new List<Tuple<LogTrace, DcrGraph>>();

            foreach (var activity in activitiesToRun)
            {
                if (activity.Executed) // Has already been executed at least once... (ABB... & BAB...)
                {
                    // TODO: Do something!
                }
                
                // Create copies
                var inputGraphCopy = inputGraph.Copy();
                var traceCopy = currentTrace.Copy();

                // Record execution
                inputGraphCopy.Running = true;
                inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
                traceCopy.Events.Add(new LogEvent { IdOfActivity = activity.Id });

                // Update collections
                //_seenStates.Add(inputGraphCopy);
                //_traceStates.Add(traceCopy.ToStringForm(), inputGraphCopy); // Always valid, as all traces are unique TODO May be wrong place to add states
                lock (_lockObject)
                {
                    AddToAllStatesForTraces(currentTrace, traceCopy, inputGraphCopy);
                }


                var currentTraceIndex = _uniqueTraces.Count;

                if (inputGraphCopy.IsFinalState())
                    // Nothing is pending and included at the same time --> Valid new trace
                {
                    _uniqueTraces.Add(traceCopy);
                    // IF
                    if (compareTraces // we should compare traces
                        && // AND
                        (currentTraceIndex >= TracesToBeComparedTo.Count
                            // (more traces found than the amount being compared to
                         || !traceCopy.Equals(TracesToBeComparedTo[currentTraceIndex]))) // OR the traces are unequal)
                    {
                        // THEN
                        // One inconsistent trace found - thus not all unique traces are equal
                        _comparisonResult = false;
                        return; // Stops all recursion TODO: If threading implemented - terminate all threads here
                    }
                }

                //var stateSeenTwiceBefore = IsStateSeenTwiceBefore(traceCopy, inputGraphCopy);

                //TODO: We should be able to do something more effective, than seen twice before.
                var stateSeenTwiceBefore = IsStateSeenTwiceBeforeInTrace(traceCopy, inputGraphCopy);
                if (!stateSeenTwiceBefore)
                {
                    // Register wish to continue
                    iterations.Add(new Tuple<LogTrace, DcrGraph>(traceCopy, inputGraphCopy));
                }
            }

            // For each case where we want to go deeper, recurse
            for (int i = 0; i < iterations.Count; i++)
            {
                FindUniqueTraces(iterations[i].Item1, iterations[i].Item2, compareTraces); // TODO: Spawn thread
            }
        }

        #region Threading attempt

        // Threading attempt
        public List<LogTrace> GetUniqueTracesThreaded(DcrGraph inputGraph)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Start from scratch
            _uniqueTraces = new List<LogTrace>();
            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();

            //FindUniqueTraces(new LogTrace { Events = new List<LogEvent>() }, inputGraph, false); // Spawn thread and record thread ID in collection with locking
            //var task = Task.Factory.StartNew((traceAndGraph) => FindUniqueTracesThreaded(new LogTrace { Events = new List<LogEvent>() }, inputGraph, false), new TraceAndGraph());
            var task = Task.Factory.StartNew(() => FindUniqueTracesThreaded(new LogTrace { Events = new List<LogEvent>() }, inputGraph, false));
            _threads.Add(task);

            _threads[0].Wait();
            
            foreach (var thread in _threads)
            {
                thread.Wait();
            }

            // TODO: Keep checking if first thread is still alive
            // TODO: Wait for all threads to finish

            Console.WriteLine("Unique Traces - Calculated with " + _threads.Count + " threads: " + _uniqueTraces.Count);
            Console.WriteLine("Elapsed: " + stopwatch.Elapsed);
            return _uniqueTraces;
        }

        // Threading attempt
        private void FindUniqueTracesThreaded(LogTrace currentTrace, DcrGraph inputGraph, bool compareTraces)
        {
            var activitiesToRun = inputGraph.GetRunnableActivities();
            var iterations = new List<Tuple<LogTrace, DcrGraph>>();

            foreach (var activity in activitiesToRun)
            {
                if (activity.Executed) // Has already been executed at least once... (ABB... & BAB...)
                {
                    // TODO: Do something!
                }

                // Create copies
                var inputGraphCopy = inputGraph.Copy();
                var traceCopy = currentTrace.Copy();

                // Record execution
                inputGraphCopy.Running = true;
                inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
                traceCopy.Events.Add(new LogEvent { IdOfActivity = activity.Id });

                // Update collections
                //_seenStates.Add(inputGraphCopy);
                //_traceStates.Add(traceCopy.ToStringForm(), inputGraphCopy); // Always valid, as all traces are unique TODO May be wrong place to add states
                lock (_lockObject)
                {
                    AddToAllStatesForTraces(currentTrace, traceCopy, inputGraphCopy);
                }


                var currentTraceIndex = _uniqueTraces.Count;

                if (inputGraphCopy.IsFinalState())
                // Nothing is pending and included at the same time --> Valid new trace
                {
                    lock (_lockObject)
                    {
                        _uniqueTraces.Add(traceCopy);
                    }
                    // IF
                    if (compareTraces // we should compare traces
                        && // AND
                        (currentTraceIndex >= TracesToBeComparedTo.Count
                         // (more traces found than the amount being compared to
                         || !traceCopy.Equals(TracesToBeComparedTo[currentTraceIndex]))) // OR the traces are unequal)
                    {
                        // THEN
                        // One inconsistent trace found - thus not all unique traces are equal
                        _comparisonResult = false;

                        return; // Stops all recursion TODO: If threading implemented - terminate all threads here
                    }
                }

                //var stateSeenTwiceBefore = IsStateSeenTwiceBefore(traceCopy, inputGraphCopy);
                
                lock (_lockObject)
                {
                    //TODO: We should be able to do something more effective, than seen twice before.
                    var stateSeenTwiceBefore = IsStateSeenTwiceBeforeInTrace(traceCopy, inputGraphCopy);
                    if (!stateSeenTwiceBefore)
                    {
                        // Register wish to continue
                        iterations.Add(new Tuple<LogTrace, DcrGraph>(traceCopy, inputGraphCopy));
                    }
                }
            }

            // For each case where we want to go deeper, recurse
            foreach (var iteration in iterations)
            {
                var task = Task.Factory.StartNew(() => FindUniqueTraces(iteration.Item1, iteration.Item2, compareTraces));
                _threads.Add(task);
                //FindUniqueTraces(iterations[i].Item1, iterations[i].Item2, compareTraces);
            }
            
            //for (int i = 0; i < iterations.Count; i++)
            //{
            //    var i1 = i;
            //    var task = Task.Factory.StartNew(() => FindUniqueTraces(iterations[i1].Item1, iterations[i1].Item2, compareTraces));
            //    _threads.Add(task);
            //    task.Start();
            //    //FindUniqueTraces(iterations[i].Item1, iterations[i].Item2, compareTraces);
            //}
        }

        #endregion

        private void AddToAllStatesForTraces(LogTrace currentTrace, LogTrace newTrace, DcrGraph newGraph)
        {
            var newDcrState = DcrGraph.HashDcrGraph(newGraph);
            Dictionary<byte[], int> prevStates;
            if (_allStatesForTraces.TryGetValue(currentTrace.ToStringForm(), out prevStates)) // Trace before execution - any states seen?
            {
                var clonedDictionary = new Dictionary<byte[], int>(prevStates, new ByteArrayComparer());

                // Search clonedDictionary for already exisiting state
                int count;
                if (clonedDictionary.TryGetValue(newDcrState, out count))
                {
                    // Increase count for this state
                    clonedDictionary[newDcrState] += 1;
                }
                else
                {
                    // Add first occurance of new state
                    clonedDictionary.Add(newDcrState, 1);
                }
                // Update outer dictionary with this trace's states
                _allStatesForTraces.Add(newTrace.ToStringForm(), clonedDictionary); // NewTrace --> PrevStates + NewState
            }
            else
            {
                // Add for current trace (post-exec)
                _allStatesForTraces.Add(newTrace.ToStringForm(), new Dictionary<byte[], int>(new ByteArrayComparer()) { { newDcrState, 1 } });
            }
        }

        // Using this method rather than accumulating a larger field (all states seen at certain trace) uses less memory but pays via search time
        //private List<byte[]> GetTracePreviousStates(LogTrace trace) // TODO: Consider using a list of states for currentTrace instead (method param) and update at each iteration
        //{
        //    var res = new List<byte[]>();
        //    var stringForm = trace.ToStringForm();
        //    for (int i = 0; i < trace.Events.Count; i++)
        //    {
        //        // A;B;A;B;A --> A;B;A;B
        //        var index = stringForm.LastIndexOf(";", StringComparison.InvariantCulture);
        //        if (index > 0)
        //        {
        //            stringForm = stringForm.Substring(0, index); // Remove last part of string
        //        }
        //        else
        //        {
        //            break; // If only one event in trace, it was added previously, can therefore break
        //        }
        //        byte[] traceState;
        //        if (_traceStates.TryGetValue(stringForm, out traceState))
        //        {
        //            res.Add(traceState);
        //        }
        //    }
        //    return res;
        //}

        //private bool IsStateSeenTwiceBefore(LogTrace trace, byte[] state) // TODO: Consider using a list of states for currentTrace instead (method param) and update at each iteration
        //{
        //    var traceString = trace.ToStringForm();
        //    var count = 0;
        //    for (int i = 0; i < trace.Events.Count; i++)
        //    {
        //        // A;B;A;B;A --> A;B;A;B
        //        var index = traceString.LastIndexOf(";", StringComparison.InvariantCulture);
        //        if (index > 0)
        //        {
        //            traceString = traceString.Substring(0, index); // Remove last part of string
        //        }
        //        else
        //        {
        //            break; // If only one event in trace, it was added previously, can therefore break
        //        }
        //        DcrGraph traceState;
        //        if (!_traceStates.TryGetValue(traceString, out traceState)) continue;
        //        if (traceState.AreInEqualState(state) && ++count == 2)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        private bool IsStateSeenTwiceBeforeInTrace(LogTrace trace, DcrGraph graph)
        {
            Dictionary<byte[], int> traceStates;
            if (_allStatesForTraces.TryGetValue(trace.ToStringForm(), out traceStates))
            {
                int count;
                return traceStates.TryGetValue(DcrGraph.HashDcrGraph(graph), out count) && count > 1;
            }
            throw new Exception("Whoops! Seems you didn't correctly add the states for your current trace! :&");
        }

        // WORKING (old)
        //private void FindUniqueTraces2(LogTrace currentTrace, DcrGraph inputGraph, bool compareTraces)
        //{
        //    var activitiesToRun = inputGraph.GetRunnableActivities();
        //    var iterations = new List<Tuple<LogTrace, DcrGraph>>();

        //    _seenStates.Add(inputGraph);

        //    foreach (var activity in activitiesToRun)
        //    {
        //        // Spawn new work
        //        var inputGraphCopy = inputGraph.Copy();
        //        var traceCopy = currentTrace.Copy();
        //        inputGraphCopy.Running = true;
        //        inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
        //        traceCopy.Events.Add(new LogEvent { IdOfActivity = activity.Id });

        //        if (inputGraphCopy.IsFinalState())
        //        // Nothing is pending and included at the same time --> Valid new trace
        //        {
        //            var currentTraceIndex = _uniqueTraces.Count;
        //            _uniqueTraces.Add(traceCopy);

        //            // Perform comparison of this trace with same-index trace of compared trace list
        //            if (compareTraces // Whether to compare traces or not
        //                &&
        //                // TODO: Consider if less traces found than in _tracesToBeComparedTo
        //                (currentTraceIndex >= TracesToBeComparedTo.Count // More traces found than the amount being compared to
        //                    || (currentTraceIndex < TracesToBeComparedTo.Count
        //                        && !traceCopy.Equals(TracesToBeComparedTo[currentTraceIndex])))) // The traces are unequal
        //            {
        //                // One inconsistent trace found - thus not all unique traces are equal
        //                _comparisonResult = false;
        //                return; // Stops all recursion TODO: If threading implemented - terminate all threads here
        //            }
        //        }

        //        // If state seen before, do not explore further
        //        var stateSeen = _seenStates.Any(seenState => seenState.AreInEqualState(inputGraphCopy));
        //        if (!stateSeen)
        //        {
        //            // Register wish to continue
        //            iterations.Add(new Tuple<LogTrace, DcrGraph>(traceCopy, inputGraphCopy));
        //        }
        //    }

        //    // For each case where we want to go deeper, recurse
        //    for (int i = 0; i < iterations.Count; i++)
        //    {
        //        // One of these calls may lead to the call below, ending all execution...
        //        var i1 = i;
        //        var t = new Thread(() => FindUniqueTraces(iterations[i1].Item1, iterations[i1].Item2, compareTraces));
        //        t.Start();
        //        // TODO: Add thread to list of threads that need to be waited for eventually - potentially async usage?
        //    }
        //}

        #endregion
            
        #endregion

        #region Helper methods

        // TODO: Can be improved if both inputlists (of traces) are sorted by event-ID (or ASSUMPTION)
        public static bool AreUniqueTracesEqual(List<LogTrace> traces1, List<LogTrace> traces2)
        {
            if (traces1.Count != traces2.Count)
            {
                return false;
            }

            #region Conversion and sorting of trace lists

            // Convert to List<string> ["Thread safe"]
            //var stringList1 = traces1.Select(logTrace => logTrace.ToStringForm()).ToList();
            //var stringList2 = traces2.Select(logTrace => logTrace.ToStringForm()).ToList();
            //stringList1.Sort();
            //stringList2.Sort();
            //return !stringList1.Where((t, i) => t != stringList2[i]).Any();

            #endregion

            #region WITHOUT sorting assumption

            //foreach (var trace1 in traces1)
            //{
            //    bool matchingTraceFound = false;
            //    foreach (var trace2 in traces2)
            //    {
            //        if (AreTracesEqualSingle(trace1, trace2))
            //        {
            //            matchingTraceFound = true;
            //            break;
            //        }
            //    }
            //    if (!matchingTraceFound)
            //    {
            //        return false;
            //    }
            //}

            #endregion

            #region WITH sorting assumption

            // (That two equal sets of unique traces are not discovered in different order, due to using the same discovery method)
            return !traces1.Where((t, i) => !(t.Equals(traces2[i]))).Any();

            #endregion
        }

        #endregion
    }
}