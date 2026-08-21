using System;
using CmnClsLib.Class;
using CmnClsLib.Module;
using LogReUniq.Class;

// 2026/08/15 Gemini 3.6 Flash (High) Review & Modified

namespace LogReUniq;

internal static class Program
{
    /// <summary>
    /// アプリケーションのエントリーポイントです。
    /// コマンドライン引数を解析し、メイン処理の実行または使用方法／サンプル設定の表示を行います。
    /// </summary>
    /// <param name="args">コマンドライン引数の配列。</param>
    /// <returns>実行結果の終了コード（正常終了: MdlConst.LVL_I、警告: MdlConst.LVL_W、エラー: MdlConst.LVL_E）。</returns>
    /// <example>
    /// <code>
    /// string[] args = ["-c", "config.ini"];
    /// int exitCode = Program.Main(args);
    /// </code>
    /// </example>
    static int Main(string[] args)
    {
        int result = MdlConst.LVL_I;
        ClsLogger logger = new();
        ClsAppArg appArg = new(logger);
        ClsMainProc mainProc = new(logger);
        DateTime startTime = DateTime.Now;

        bool isOk = appArg.Parse(args);
        ClsProp prop = appArg.Prop;

        if (prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE) > 0)
        {
            logger.WriteLine(MdlConst.LVL_NONE, $"===<<< [{prop.ExeBaseName}] START : {MdlDate.GetFormattedDate(startTime, "yyyy/MM/dd HH:mm:ss")}>>>===");
        }

        if (isOk && appArg.UsageFlag == ClsAppArg.USAGE_NONE)
        {
            mainProc.Prop = appArg.Prop;
            result = mainProc.Execute();
        }
        else
        {
            result = appArg.UsageFlag switch
            {
                ClsAppArg.USAGE_USAGE => ShowUsage(appArg),
                ClsAppArg.USAGE_SHOW_SAMPLE_CONFIG => ShowSampleConfig(appArg),
                _ => MdlConst.LVL_E
            };
        }

        if (prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE) > 0)
        {
            DateTime endTime = DateTime.Now;
            double elapsedSeconds = (endTime - startTime).TotalSeconds;
            logger.WriteLine(MdlConst.LVL_NONE, $"===<<< [{prop.ExeBaseName}] EXIT ({result}) : {MdlDate.GetFormattedDate(endTime, "yyyy/MM/dd HH:mm:ss")} : {elapsedSeconds:F3} sec>>>===");
        }

        return result;
    }

    /// <summary>
    /// 使用方法（Usage）のメッセージを表示し、警告レベルの終了コードを返します。
    /// </summary>
    /// <param name="appArg">コマンドライン引数を保持する ClsAppArg インスタンス。</param>
    /// <returns>警告レベルの終了コード（MdlConst.LVL_W）。</returns>
    /// <example>
    /// <code>
    /// int code = ShowUsage(appArg);
    /// </code>
    /// </example>
    private static int ShowUsage(ClsAppArg appArg)
    {
        appArg.ShowUsage();
        return MdlConst.LVL_W;
    }

    /// <summary>
    /// 設定ファイルのサンプル設定を表示し、警告レベルの終了コードを返します。
    /// </summary>
    /// <param name="appArg">コマンドライン引数を保持する ClsAppArg インスタンス。</param>
    /// <returns>警告レベルの終了コード（MdlConst.LVL_W）。</returns>
    /// <example>
    /// <code>
    /// int code = ShowSampleConfig(appArg);
    /// </code>
    /// </example>
    private static int ShowSampleConfig(ClsAppArg appArg)
    {
        appArg.ShowSampleConfig();
        return MdlConst.LVL_W;
    }
}

