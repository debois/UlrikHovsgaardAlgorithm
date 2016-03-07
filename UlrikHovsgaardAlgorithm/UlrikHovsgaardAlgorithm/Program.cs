﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var tester = new TestClassForCSharpStuff();
            //tester.TestCopyMethod();
            //tester.TestUniqueTracesMethod();
            //tester.TestDictionaryAccessAndAddition();
            //tester.TestAreTracesEqualSingle();
            //tester.TestAreUniqueTracesEqual();
            //tester.TestCompareTracesWithSupplied();
            //tester.TestRedundancyRemover();
            //tester.TestRedundancyRemoverLimited();
            //tester.TestRedundancyRemoverExcludes();
            //tester.TestUniqueTracesMethodExcludes();
            tester.ExhaustiveTest();
            //tester.TestUnhealthyInput();



            // TODO: Read from log
            // TODO: Build Processes, LogTraces and LogEvents

            // TODO: Run main algorithm
        }
    }
}
