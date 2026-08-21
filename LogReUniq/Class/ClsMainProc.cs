using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CmnClsLib.Class;
using CmnClsLib.Module;

// 2026/08/15 Gemini 3.6 Flash (High) Review & Modified

namespace LogReUniq.Class
{
    /// <summary>
    /// ログファイルの抽出・並び替え・一意化（重複除去）処理を管理・実行するメインプロセスクラスです。
    /// </summary>
    /// <example>
    /// <code>
    /// var logger = new ClsLogger();
    /// var proc = new ClsMainProc(logger);
    /// proc.Prop.SetValue(ClsProp.SRC_PATH, @"C:\logs");
    /// int exitCode = proc.Execute();
    /// </code>
    /// </example>
    public class ClsMainProc
    {
        private readonly ClsLogger _logger;
        private ClsProp _prop = new();
        private readonly List<string> _results = [];
        private readonly HashSet<string> _resultSet = new(StringComparer.Ordinal);

        /// <summary>
        /// <see cref="ClsMainProc"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">処理の進捗やエラーを記録するログオブジェクト</param>
        /// <example>
        /// <code>
        /// var logger = new ClsLogger();
        /// var mainProc = new ClsMainProc(logger);
        /// </code>
        /// </example>
        public ClsMainProc(ClsLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// プロパティ設定オブジェクトを取得または設定します。
        /// </summary>
        /// <value>プロパティ設定 (<see cref="ClsProp"/>)</value>
        /// <example>
        /// <code>
        /// mainProc.Prop.SetValue(ClsProp.VERBOSE, "3");
        /// </code>
        /// </example>
        public ClsProp Prop { get => _prop; set => _prop = value; }

        /// <summary>
        /// 設定されたプロパティに基づき、ログの読み込み、絞り込み、重複除去、ソート、出力を一連のフローとして実行します。
        /// </summary>
        /// <returns>処理の実行結果コード（成功時: <see cref="MdlConst.LVL_I"/>、エラー発生時: <see cref="MdlConst.LVL_E"/>）</returns>
        /// <example>
        /// <code>
        /// int resultCode = mainProc.Execute();
        /// if (resultCode == MdlConst.LVL_E) {
        ///     Console.WriteLine("エラーが発生しました。");
        /// }
        /// </code>
        /// </example>
        public int Execute()
        {
            int returnCode = MdlConst.LVL_I;
            bool showUnmatchedLines = string.Equals(_prop.GetValue(ClsProp.SHOW_NOMATCH_LINE, "false"), "true", StringComparison.OrdinalIgnoreCase);
            bool shouldExit = false;

            // DEBUG
            if (!shouldExit && _prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE) > 2)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                _logger.WriteLine(MdlConst.LVL_NONE, "# PROPERTIES");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                _logger.WriteLine(MdlConst.LVL_NONE, ClsProp.VERBOSE.PadLeft(_prop.KeyLength, ' ') + " : " + _prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE));
                if (string.Equals(_prop.GetValue(ClsProp.IS_PIPE_IN, "false"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, ClsProp.SRC_PATH.PadLeft(_prop.KeyLength, ' ') + " : PIPE");
                }
                else
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, ClsProp.SRC_PATH.PadLeft(_prop.KeyLength, ' ') + " : " + _prop.GetValue(ClsProp.SRC_PATH, ClsProp.DEFAULT_SRC_PATH));
                }
                _logger.WriteLine(MdlConst.LVL_NONE, ClsProp.INC_FILES.PadLeft(_prop.KeyLength, ' ') + " : " + _prop.IncludeFiles.Count + " : " + MdlUtil.Join(_prop.IncludeFiles, ", "));
                _logger.WriteLine(MdlConst.LVL_NONE, ClsProp.EXC_FILES.PadLeft(_prop.KeyLength, ' ') + " : " + _prop.ExcludeFiles.Count + " : " + MdlUtil.Join(_prop.ExcludeFiles, ", "));
                _prop.ShowRulesForDebug();
            }

            // 処理
            if (!shouldExit)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                _logger.WriteLine(MdlConst.LVL_NONE, "# READ LOGS");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                try
                {
                    string sourcePath = MdlFile.GetAbsolutePath(_prop.GetValue(ClsProp.SRC_PATH, ClsProp.DEFAULT_SRC_PATH));
                    int verbose = _prop.GetValue(ClsProp.VERBOSE, 0);
                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        if (Directory.Exists(sourcePath))
                        {
                            bool isRecursive = string.Equals(_prop.GetValue(ClsProp.IS_RECURSIVE, "false"), "true", StringComparison.OrdinalIgnoreCase);
                            if (!ProcessDirectory(sourcePath, 0, verbose, isRecursive, showUnmatchedLines)) returnCode = MdlConst.LVL_E;
                        }
                        else if (File.Exists(sourcePath))
                        {
                            if (!ProcessTextFile(sourcePath, verbose, showUnmatchedLines)) returnCode = MdlConst.LVL_E;
                        }
                    }
                    else
                    {
                        if (!ProcessPipeStream(verbose, showUnmatchedLines)) returnCode = MdlConst.LVL_E;
                    }
                }
                catch (Exception ex)
                {
                    returnCode = MdlConst.LVL_E;
                    _logger.WriteLine(MdlConst.LVL_E, ex.Message);
                    ShowStackTrace(ex);
                }
            }

            // ソート
            if (!shouldExit)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                _logger.WriteLine(MdlConst.LVL_NONE, "# SORT");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                _logger.WriteLine(MdlConst.LVL_NONE, "---> " + _results.Count + " LINES");
                _results.Sort();
            }

            // 出力
            if (!shouldExit)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                _logger.WriteLine(MdlConst.LVL_NONE, "# OUTPUT");
                _logger.WriteLine(MdlConst.LVL_NONE, "############################################################");
                string outputPath = _prop.GetValue(ClsProp.TO_PATH, "");
                if (string.IsNullOrEmpty(outputPath))
                {
                    foreach (string res in _results)
                    {
                        _logger.WriteLine(MdlConst.LVL_NONE, res);
                    }
                }
                else
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "---> " + outputPath);
                    try
                    {
                        MdlFile.WriteFile(outputPath, _results, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    }
                    catch (Exception ex)
                    {
                        returnCode = MdlConst.LVL_E;
                        _logger.WriteLine(MdlConst.LVL_E, "EXCEPTION : " + outputPath + "：" + ex.Message);
                        ShowStackTrace(ex);
                    }
                }
            }

            // DEBUG
            if (_prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE) > 5)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _logger.WriteLine(MdlConst.LVL_NONE, "EXIT CODE = " + returnCode);
            }

            // END
            return returnCode;
        }

        /// <summary>
        /// 指定されたディレクトリパスを対象に、配下のファイルおよびサブディレクトリの抽出処理を再帰的に行います。
        /// </summary>
        /// <param name="directoryPath">検索対象のディレクトリパス</param>
        /// <param name="hierarchy">現在の階層レベル（ルートが 0）</param>
        /// <param name="verbose">詳細ログ出力のレベル</param>
        /// <param name="isRecursive">サブディレクトリを再帰的に検索する場合は true</param>
        /// <param name="showUnmatchedLines">抽出条件にマッチしなかった行を出力・表示する場合は true</param>
        /// <returns>正常に完了した場合は <c>true</c>。エラーが発生した場合は <c>false</c></returns>
        /// <example>
        /// <code>
        /// bool success = mainProc.ProcessDirectory(@"C:\Logs", 0, 1, true, false);
        /// </code>
        /// </example>
        public bool ProcessDirectory(string directoryPath, long hierarchy, int verbose, bool isRecursive, bool showUnmatchedLines)
        {
            bool isSuccess = true;

            try
            {
                // 現在のディレクトリのファイルを処理
                if (isRecursive && hierarchy == 0 && _prop.ExcludeDirectories.Count > 0 && _prop.ExcludeDirectories.Contains(@"^\.$"))
                {
                    if (verbose > 2) _logger.WriteLine(MdlConst.LVL_NONE, "HIT : " + ClsProp.EXC_DIRS + " : .");
                }
                else
                {
                    if (!ProcessFilesInDirectory(directoryPath, verbose, showUnmatchedLines)) isSuccess = false;
                }

                // 現在のディレクトリのサブディレクトリを処理
                if (isRecursive)
                {
                    if (!ProcessSubdirectories(directoryPath, hierarchy, verbose, isRecursive, showUnmatchedLines)) isSuccess = false;
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _logger.WriteLine(MdlConst.LVL_E, "EXCEPTION : " + directoryPath + "：" + ex.Message);
                ShowStackTrace(ex);
            }
            return isSuccess;
        }

        /// <summary>
        /// 指定されたディレクトリ内のサブディレクトリをフィルター条件に基づいて処理し、再帰的なログ検索を実施します。
        /// </summary>
        /// <param name="directoryPath">親ディレクトリのパス</param>
        /// <param name="hierarchy">現在の階層レベル</param>
        /// <param name="verbose">詳細ログ出力のレベル</param>
        /// <param name="isRecursive">再帰検索を行うフラグ</param>
        /// <param name="showUnmatchedLines">非マッチ行を表示するフラグ</param>
        /// <returns>すべてのサブディレクトリの処理が正常に行われた場合は <c>true</c>、エラーが含まれる場合は <c>false</c></returns>
        /// <example>
        /// <code>
        /// bool success = mainProc.ProcessSubdirectories(@"C:\Logs", 0, 1, true, false);
        /// </code>
        /// </example>
        public bool ProcessSubdirectories(string directoryPath, long hierarchy, int verbose, bool isRecursive, bool showUnmatchedLines)
        {
            bool isSuccess = true;

            foreach (string subDirectoryPath in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
            {
                // 絞込／除外
                bool isEffective = MdlFile.IsPathFilterMatched(subDirectoryPath, true, true, _prop.IncludeFiles, _prop.ExcludeFiles, false, verbose);
                // サブディレクトリの処理
                if (isEffective)
                {
                    if (!ProcessDirectory(subDirectoryPath, hierarchy + 1, verbose, isRecursive, showUnmatchedLines)) isSuccess = false;
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// 指定されたディレクトリ直下のファイルを対象に、包含/除外フィルターを評価して抽出処理を呼び出します。
        /// </summary>
        /// <param name="directoryPath">検索対象のディレクトリパス</param>
        /// <param name="verbose">詳細ログ出力のレベル</param>
        /// <param name="showUnmatchedLines">非マッチ行を表示するフラグ</param>
        /// <returns>ディレクトリ内すべてのファイルの読み込み・抽出が成功した場合は <c>true</c>、失敗があった場合は <c>false</c></returns>
        /// <example>
        /// <code>
        /// bool success = mainProc.ProcessFilesInDirectory(@"C:\Logs", 1, false);
        /// </code>
        /// </example>
        public bool ProcessFilesInDirectory(string directoryPath, int verbose, bool showUnmatchedLines)
        {
            bool isSuccess = true;
            foreach (string filePath in Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly))
            {
                // 絞込／除外
                bool isEffective = MdlFile.IsPathFilterMatched(filePath, true, true, _prop.IncludeFiles, _prop.ExcludeFiles, false, verbose);
                // ファイル処理
                if (isEffective)
                {
                    if (!ProcessTextFile(filePath, verbose, showUnmatchedLines)) isSuccess = false;
                }
            }

            return isSuccess;
        }

        /// <summary>
        /// 指定されたテキストファイルを指定のエンコーディングで読み込み、各行に対してログフィルタリング処理を実行します。
        /// </summary>
        /// <param name="filePath">読み込み対象のテキストファイルパス</param>
        /// <param name="verbose">詳細ログ出力のレベル</param>
        /// <param name="showUnmatchedLines">非マッチ行を表示するフラグ</param>
        /// <returns>ファイル読込・解析が正常に終了した場合は <c>true</c>、エラー発生時は <c>false</c></returns>
        /// <example>
        /// <code>
        /// bool success = mainProc.ProcessTextFile(@"C:\Logs\app.log", 1, false);
        /// </code>
        /// </example>
        public bool ProcessTextFile(string filePath, int verbose, bool showUnmatchedLines)
        {
            bool isSuccess = true;

            // MS932等有効化
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            try
            {
                using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new(stream, MdlUtil.GetEncoding(_prop.GetValue(ClsProp.ENCODING, ClsProp.DEFAULT_ENCODING)));
                _logger.WriteLine(MdlConst.LVL_NONE, "---> " + filePath);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    ProcessLine(line, verbose, showUnmatchedLines);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _logger.WriteLine(MdlConst.LVL_E, "EXCEPTION : " + filePath + "：" + ex.Message);
                ShowStackTrace(ex);
            }

            return isSuccess;
        }

        /// <summary>
        /// 標準入力（パイプストリーム）から入力行を順次読み込み、ログフィルタリング処理を実行します。
        /// </summary>
        /// <param name="verbose">詳細ログ出力のレベル</param>
        /// <param name="showUnmatchedLines">非マッチ行を表示するフラグ</param>
        /// <returns>ストリームの読み込み処理が正常に完了した場合は <c>true</c>、エラー発生時は <c>false</c></returns>
        /// <example>
        /// <code>
        /// bool success = mainProc.ProcessPipeStream(1, false);
        /// </code>
        /// </example>
        public bool ProcessPipeStream(int verbose, bool showUnmatchedLines)
        {
            bool isSuccess = true;

            try
            {
                string? line;
                while ((line = Console.In.ReadLine()) != null)
                {
                    ProcessLine(line, verbose, showUnmatchedLines);
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                _logger.WriteLine(MdlConst.LVL_E, "EXCEPTION : " + ex.Message);
                ShowStackTrace(ex);
            }

            return isSuccess;
        }

        /// <summary>
        /// 1行のログ文字列に対して正規表現による抽出・除外パターンを評価し、フォーマット整形・一意化（重複チェック）を行って結果リストに格納します。
        /// </summary>
        /// <param name="line">評価対象のログ行文字列</param>
        /// <param name="verbose">詳細ログ出力のレベル</param>
        /// <param name="showUnmatchedLines">抽出・除外にマッチしなかった行を非マッチログとして出力する場合は true</param>
        /// <example>
        /// <code>
        /// mainProc.ProcessLine("2026-08-13 12:00:00 [ERROR] Connection failed", 1, false);
        /// </code>
        /// </example>
        public void ProcessLine(string line, int verbose, bool showUnmatchedLines)
        {
            bool isIncludeMatched = false;
            bool isExcludeMatched = false;
            bool isIgnoreCase = string.Equals(_prop.GetValue(ClsProp.IS_CASE_INSENSITIVE, "false"), "true", StringComparison.OrdinalIgnoreCase);
            RegexOptions options = isIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

            string trimmedLine = line.Trim();

            for (int i = 0; i < _prop.IncludeRegexes.Count; i++)
            {
                string incPattern = _prop.IncludeRegexes[i];
                Match incMatch = Regex.Match(trimmedLine, incPattern, options);
                if (incMatch.Success)
                {
                    isIncludeMatched = true;

                    // 除外判定
                    for (int x = 0; x < _prop.ExcludeRegexes.Count; x++)
                    {
                        string excPattern = _prop.ExcludeRegexes[x];
                        Match excMatch = Regex.Match(trimmedLine, excPattern, options);
                        if (excMatch.Success)
                        {
                            isExcludeMatched = true;
                            if (verbose > 5) _logger.WriteLine(MdlConst.LVL_NONE, "EXC-MATCH : " + x + " : " + line);
                            break;
                        }
                    }

                    // 除外対象外の場合
                    if (!isExcludeMatched)
                    {
                        int[] orders = _prop.OrdersList[i];
                        List<string> matchedGroups = new(orders.Length);
                        for (int o = 0; o < orders.Length; o++)
                        {
                            matchedGroups.Add(incMatch.Groups[orders[o]].Value);
                        }

                        string formattedLine = MdlUtil.Join(matchedGroups, ",");
                        if (_prop.Formats.Count > i)
                        {
                            formattedLine = MdlUtil.Sprintf(_prop.Formats[i], matchedGroups.ToArray());
                        }

                        if (_resultSet.Add(formattedLine))
                        {
                            _results.Add(formattedLine);
                        }

                        if (verbose > 4) _logger.WriteLine(MdlConst.LVL_NONE, "INC-MATCH : " + i + " : " + MdlUtil.Join(orders, ",") + " -> " + formattedLine);
                    }
                    break;
                }
            }

            // DEBUG
            if (!isIncludeMatched && !isExcludeMatched && showUnmatchedLines)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "NO-MATCH : " + line);
            }
        }

        /// <summary>
        /// プロパティの設定情報（<see cref="ClsProp.IS_STACKTRACE"/>）が有効な場合、キャッチした例外のスタックトレースをログに出力します。
        /// </summary>
        /// <param name="ex">スタックトレースを出力する例外オブジェクト (<see cref="Exception"/>)</param>
        /// <example>
        /// <code>
        /// try {
        ///     // 処理
        /// } catch (Exception ex) {
        ///     mainProc.ShowStackTrace(ex);
        /// }
        /// </code>
        /// </example>
        public void ShowStackTrace(Exception ex)
        {
            if (string.Equals(_prop.GetValue(ClsProp.IS_STACKTRACE, "false"), "true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _logger.WriteLine(MdlConst.LVL_NONE, ex.StackTrace ?? "");
                _logger.WriteLine(MdlConst.LVL_NONE, "");
            }
        }
    }
}

