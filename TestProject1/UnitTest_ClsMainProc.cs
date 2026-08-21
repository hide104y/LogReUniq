using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using LogReUniq.Class;
using CmnClsLib.Class;
using CmnClsLib.Module;

namespace TestProject1
{
    public class UnitTest_ClsMainProc : IDisposable
    {
        private readonly string _tempDir;
        private readonly ClsLogger _logger;

        public UnitTest_ClsMainProc()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "UnitTest", "LogReUniq", "ClsMainProc");
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore deletion error
                }
            }
            Directory.CreateDirectory(_tempDir);
            _logger = new ClsLogger();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void Constructor_And_Property_ShouldInitializeCorrectly()
        {
            var mainProc = new ClsMainProc(_logger);
            Assert.NotNull(mainProc.Prop);

            var newProp = new ClsProp();
            mainProc.Prop = newProp;
            Assert.Same(newProp, mainProc.Prop);
        }

        [Fact]
        public void ProcessTextFile_ShouldExtractAndDeduplicateLines()
        {
            // Arrange
            string logFilePath = Path.Combine(_tempDir, "test.log");
            string outputFilePath = Path.Combine(_tempDir, "output.log");
            File.WriteAllLines(logFilePath, new[]
            {
                "2026-08-13 [INFO] User login: user1",
                "2026-08-13 [ERROR] Out of memory error",
                "2026-08-13 [INFO] User login: user1", // Duplicate
                "2026-08-13 [WARNING] Disk space low",
                "2026-08-13 [ERROR] Out of memory error" // Duplicate
            });

            var mainProc = new ClsMainProc(_logger);
            mainProc.Prop.SetValue(ClsProp.SRC_PATH, logFilePath);
            mainProc.Prop.SetValue(ClsProp.TO_PATH, outputFilePath);
            mainProc.Prop.IncludeRegexes.Add(@"\[ERROR\] (.*)");
            mainProc.Prop.OrdersList.Add(new[] { 0, 1 });

            // Act
            int exitCode = mainProc.Execute();

            // Assert
            Assert.Equal(MdlConst.LVL_I, exitCode);
            Assert.True(File.Exists(outputFilePath));

            string[] outputLines = File.ReadAllLines(outputFilePath);
            Assert.Single(outputLines);
            Assert.Contains("Out of memory error", outputLines[0]);
        }

        [Fact]
        public void ProcessDirectory_Recursive_ShouldSearchSubdirectories()
        {
            // Arrange
            string subDir = Path.Combine(_tempDir, "SubDir");
            Directory.CreateDirectory(subDir);

            string file1 = Path.Combine(_tempDir, "file1.log");
            string file2 = Path.Combine(subDir, "file2.log");

            File.WriteAllText(file1, "2026-08-13 [MATCH] Data A\n");
            File.WriteAllText(file2, "2026-08-13 [MATCH] Data B\n");

            var mainProc = new ClsMainProc(_logger);
            mainProc.Prop.SetValue(ClsProp.SRC_PATH, _tempDir);
            mainProc.Prop.SetValue(ClsProp.IS_RECURSIVE, "true");
            mainProc.Prop.IncludeRegexes.Add(@"\[MATCH\] (.*)");
            mainProc.Prop.OrdersList.Add(new[] { 0, 1 });

            // Act
            bool result = mainProc.ProcessDirectory(_tempDir, 0, 1, true, false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShowStackTrace_ShouldNotThrowException()
        {
            var mainProc = new ClsMainProc(_logger);
            mainProc.Prop.SetValue(ClsProp.IS_STACKTRACE, "true");

            var exception = new InvalidOperationException("Test exception for stacktrace");
            var recordEx = Record.Exception(() => mainProc.ShowStackTrace(exception));

            Assert.Null(recordEx);
        }
    }
}
