﻿using System;
using System.Diagnostics;
using System.Text;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.ProcessExecution.Contracts;

namespace GoogleTestAdapter.ProcessExecution
{

    public class DotNetProcessExecutor : IProcessExecutor
    {
        private readonly bool _printTestOutput;
        private readonly ILogger _logger;
        
        private Process _process;

        public DotNetProcessExecutor(bool printTestOutput, ILogger logger)
        {
            _printTestOutput = printTestOutput;
            _logger = logger;
        }

        public int ExecuteCommandBlocking(string command, string parameters, string workingDir, string pathExtension,
            Action<string> reportOutputLine)
        {
            var processStartInfo = new ProcessStartInfo(command, parameters)
            {
                StandardOutputEncoding = Encoding.Default,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            if (!string.IsNullOrEmpty(pathExtension))
                processStartInfo.EnvironmentVariables["PATH"] = Utils.GetExtendedPath(pathExtension);

            _process = Process.Start(processStartInfo);
            try
            {
                var waiter = new ProcessWaiter(_process);
                if (_printTestOutput)
                {
                    _logger.LogInfo(
                        ">>>>>>>>>>>>>>> Output of command '" + command + " " + parameters + "'");
                }
                ReadTheStream(_process, reportOutputLine);
                if (_printTestOutput)
                {
                    _logger.LogInfo("<<<<<<<<<<<<<<< End of Output");
                }
                return waiter.WaitForExit();
            }
            finally
            {
                _process?.Dispose();
            }
        }

        public void Cancel()
        {
            if (_process != null)
            {
                ProcessUtils.KillProcess(_process.Id, _logger);
            }
        }


        // ReSharper disable once UnusedParameter.Local
        private void ReadTheStream(Process process, Action<string> reportOutputLine)
        {
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                reportOutputLine(line);
                if (_printTestOutput)
                {
                    _logger.LogInfo(line);
                }
            }
        }

    }

}