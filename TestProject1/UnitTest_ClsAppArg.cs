using System;
using System.IO;
using CmnClsLib.Class;
using LogReUniq.Class;
using Xunit;

namespace TestProject1
{
    public class UnitTest_ClsAppArg : IDisposable
    {
        private readonly string _tempDir;

        public UnitTest_ClsAppArg()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), @"UnitTest", "LogReUniq", "ClsAppArg");
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
            Directory.CreateDirectory(_tempDir);
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
                    // テストクリーンアップ時の例外は無視
                }
            }
        }

        [Fact]
        public void TestConstructor_InitializesPropertiesCorrectly()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);

            Assert.NotNull(appArg.Prop);
            Assert.Equal(ClsAppArg.USAGE_NONE, appArg.UsageFlag);
        }

        [Fact]
        public void TestParse_WithSourceDirectory_ReturnsTrue()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string[] args = ["-d", _tempDir, "-o", Path.Combine(_tempDir, "out.csv")];

            bool result = appArg.Parse(args);

            Assert.True(result);
            Assert.Equal(_tempDir, appArg.Prop.GetValue(ClsProp.SRC_PATH, ""));
            Assert.Equal(Path.Combine(_tempDir, "out.csv"), appArg.Prop.GetValue(ClsProp.TO_PATH, ""));
        }

        [Fact]
        public void TestParse_WithoutSourcePath_ReturnsFalse()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string[] args = ["-v", "1"];

            bool result = appArg.Parse(args);

            Assert.False(result);
        }

        [Fact]
        public void TestParse_WithHelpFlag_SetsUsageFlag()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string[] args = ["-h"];

            bool result = appArg.Parse(args);

            Assert.Equal(ClsAppArg.USAGE_USAGE, appArg.UsageFlag);
        }

        [Fact]
        public void TestParse_WithSampleConfigFlag_SetsUsageFlag()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string[] args = ["--show-sample-config"];

            bool result = appArg.Parse(args);

            Assert.Equal(ClsAppArg.USAGE_SHOW_SAMPLE_CONFIG, appArg.UsageFlag);
        }

        [Fact]
        public void TestParse_AdvancedOptions_ParsesFlagsCorrectly()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string[] args = ["-d", _tempDir, "-R", "-g", "-P", "-e", "UTF-8"];

            bool result = appArg.Parse(args);

            Assert.True(result);
            Assert.Equal("true", appArg.Prop.GetValue(ClsProp.IS_RECURSIVE, "false"));
            Assert.Equal("true", appArg.Prop.GetValue(ClsProp.IS_CASE_INSENSITIVE, "false"));
            Assert.Equal("true", appArg.Prop.GetValue(ClsProp.IS_PIPE_IN, "false"));
            Assert.Equal("UTF-8", appArg.Prop.GetValue(ClsProp.ENCODING, ""));
        }

        [Fact]
        public void TestParse_RegexAndOrders_MatchesCounts()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string[] args = ["-d", _tempDir, "-i", "^(.*)$", "-O", "1"];

            bool result = appArg.Parse(args);

            Assert.True(result);
            Assert.Single(appArg.Prop.IncludeRegexes);
            Assert.Single(appArg.Prop.OrdersList);
        }

        [Fact]
        public void TestParse_RegexAndOrdersMismatchedCount_ReturnsFalse()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string[] args = ["-d", _tempDir, "-i", "^(.*)$", "-i", "^[0-9]+$", "-O", "1"];

            bool result = appArg.Parse(args);

            Assert.False(result);
        }

        [Fact]
        public void TestParseBaseDate_ValidIndicators_CalculatesDateCorrectly()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);

            DateTime today = appArg.ParseBaseDate("today");
            DateTime yesterday = appArg.ParseBaseDate("yesterday");
            DateTime tomorrow = appArg.ParseBaseDate("tomorrow");
            DateTime fotm = appArg.ParseBaseDate("fotm");
            DateTime relativeMinusTwo = appArg.ParseBaseDate("-2");

            Assert.Equal(DateTime.Today, today);
            Assert.Equal(DateTime.Today.AddDays(-1), yesterday);
            Assert.Equal(DateTime.Today.AddDays(1), tomorrow);
            Assert.Equal(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), fotm);
            Assert.Equal(DateTime.Today.AddDays(-2), relativeMinusTwo);
        }

        [Fact]
        public void TestReplacePlaceholders_ReplacesComputerNameAndPid()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);
            string testString = "Host:_COMPUTERNAME_ PID:%pid";

            string replaced = appArg.ReplacePlaceholders(testString);

            Assert.DoesNotContain("_COMPUTERNAME_", replaced);
            Assert.DoesNotContain("%pid", replaced);
            Assert.Contains(appArg.Prop.MachineName, replaced);
            Assert.Contains(appArg.Prop.Pid.ToString(), replaced);
        }

        [Fact]
        public void TestShowUsageAndSampleConfig_ExecutesWithoutExceptions()
        {
            var logger = new ClsLogger();
            var appArg = new ClsAppArg(logger);

            var exception1 = Record.Exception(() => appArg.ShowUsage());
            var exception2 = Record.Exception(() => appArg.ShowSampleConfig());

            Assert.Null(exception1);
            Assert.Null(exception2);
        }
    }
}
