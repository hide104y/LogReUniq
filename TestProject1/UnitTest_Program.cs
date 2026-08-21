using System;
using System.IO;
using CmnClsLib.Class;
using CmnClsLib.Module;
using LogReUniq;
using Xunit;

namespace TestProject1
{
    public class UnitTest_Program : IDisposable
    {
        private readonly string _tempDir;

        public UnitTest_Program()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), @"UnitTest", "LogReUniq", "Program");
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // 無視
                }
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
        public void TestMain_WithHelpOption_ReturnsWarningExitCode()
        {
            string[] args = ["-h"];
            int result = Program_Host.InvokeMain(args);

            Assert.Equal(MdlConst.LVL_W, result);
        }

        [Fact]
        public void TestMain_WithShowSampleConfigOption_ReturnsWarningExitCode()
        {
            string[] args = ["--show-sample-config"];
            int result = Program_Host.InvokeMain(args);

            Assert.Equal(MdlConst.LVL_W, result);
        }

        [Fact]
        public void TestMain_WithInvalidOption_ReturnsErrorExitCode()
        {
            string[] args = ["-invalidOptionSpecialUnrecognized"];
            int result = Program_Host.InvokeMain(args);

            Assert.Equal(MdlConst.LVL_E, result);
        }
    }

    /// <summary>
    /// internal クラスの Program.Main メソッド呼び出しをテストから行うためのヘルパークラス
    /// </summary>
    internal static class Program_Host
    {
        public static int InvokeMain(string[] args)
        {
            // Reflection を用いて internal static class Program の Main メソッドを呼び出す
            var programType = typeof(LogReUniq.Class.ClsMainProc).Assembly.GetType("LogReUniq.Program");
            if (programType == null)
            {
                throw new InvalidOperationException("LogReUniq.Program 型が見つかりません。");
            }

            var mainMethod = programType.GetMethod("Main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (mainMethod == null)
            {
                throw new InvalidOperationException("Program.Main メソッドが見つかりません。");
            }

            object? result = mainMethod.Invoke(null, [args]);
            return result is int code ? code : MdlConst.LVL_E;
        }
    }
}
