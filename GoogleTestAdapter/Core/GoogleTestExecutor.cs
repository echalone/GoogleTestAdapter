﻿using System.Linq;
using System.Collections.Generic;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Runners;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter
{

    public class GoogleTestExecutor
    {

        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly SchedulingAnalyzer _schedulingAnalyzer;

        private ITestRunner _runner;
        private bool _canceled;

        public GoogleTestExecutor(ILogger logger, SettingsWrapper settings)
        {
            _logger = logger;
            _settings = settings;
            _schedulingAnalyzer = new SchedulingAnalyzer(logger);
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, ITestFrameworkReporter reporter, IDebuggedProcessLauncher launcher, bool isBeingDebugged, IProcessExecutor executor)
        {
            TestCase[] testCasesToRunAsArray = testCasesToRun as TestCase[] ?? testCasesToRun.ToArray();
            _logger.LogInfo("Running " + testCasesToRunAsArray.Length + " tests...");

            lock (this)
            {
                if (_canceled)
                {
                    return;
                }
                ComputeTestRunner(reporter, isBeingDebugged);
            }

            _runner.RunTests(testCasesToRunAsArray, isBeingDebugged, launcher, executor);

            if (_settings.ParallelTestExecution)
                _schedulingAnalyzer.PrintStatisticsToDebugOutput();
        }

        public void Cancel()
        {
            lock (this)
            {
                _canceled = true;
                _runner?.Cancel();
            }
        }

        private void ComputeTestRunner(ITestFrameworkReporter reporter, bool isBeingDebugged)
        {
            if (_settings.ParallelTestExecution && !isBeingDebugged)
            {
                _runner = new ParallelTestRunner(reporter, _logger, _settings, _schedulingAnalyzer);
            }
            else
            {
                _runner = new PreparingTestRunner(reporter, _logger, _settings, _schedulingAnalyzer);
                if (_settings.ParallelTestExecution && isBeingDebugged)
                {
                    _logger.DebugInfo(
                        "Parallel execution is selected in options, but tests are executed sequentially because debugger is attached.");
                }
            }
        }

    }

}