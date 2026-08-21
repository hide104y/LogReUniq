using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CmnClsLib.Class;
using CmnClsLib.Module;

// 2026/08/15 Gemini 3.6 Flash (High) Review & Modified

namespace LogReUniq.Class
{
    /// <summary>
    /// アプリケーションのコマンドライン引数の解析および設定情報の保持を行うクラスです。
    /// </summary>
    public class ClsAppArg
    {
        /// <summary>使用方法非表示フラグ値</summary>
        public const int USAGE_NONE = 0;
        /// <summary>使用方法表示フラグ値</summary>
        public const int USAGE_USAGE = 1;
        /// <summary>サンプル設定表示フラグ値</summary>
        public const int USAGE_SHOW_SAMPLE_CONFIG = 2;

        private ClsLogger _logger;
        private ClsProp _prop = new();
        private ClsCmmnArgs _cmmnArgs;
        private List<string> _orderCsvs = [];        // 抽出順序リスト
        private int _usageFlag = 0;

        /// <summary>
        /// <see cref="ClsAppArg"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="logger">ログ出力を行う <see cref="ClsLogger"/> オブジェクト</param>
        /// <example>
        /// <code>
        /// var logger = new ClsLogger();
        /// var appArg = new ClsAppArg(logger);
        /// </code>
        /// </example>
        public ClsAppArg(ClsLogger logger)
        {
            _logger = logger;
            _cmmnArgs = new ClsCmmnArgs(_logger);
            _cmmnArgs.GetModuleInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
            _prop.ExeDir = _cmmnArgs.ExeDir;
            _prop.ExeBaseName = _cmmnArgs.ExeBaseName;
            _prop.Pid = _cmmnArgs.Pid;
        }

        /// <summary>
        /// アプリケーションの設定プロパティを取得または設定します。
        /// </summary>
        /// <example>
        /// <code>
        /// var prop = appArg.Prop;
        /// </code>
        /// </example>
        public ClsProp Prop { get => _prop; set => _prop = value; }

        /// <summary>
        /// ヘルプやサンプル表示などの動作フラグを取得します。
        /// </summary>
        /// <example>
        /// <code>
        /// if (appArg.UsageFlag == ClsAppArg.USAGE_USAGE) {
        ///     appArg.ShowUsage();
        /// }
        /// </code>
        /// </example>
        public int UsageFlag => _usageFlag;

        /// <summary>
        /// コマンドライン引数を解析してアプリケーション設定プロパティに適用します。
        /// </summary>
        /// <param name="args">コマンドライン引数の配列</param>
        /// <returns>引数の解析および各オプションの整合性チェックが成功した場合は <c>true</c>。それ以外は <c>false</c>。</returns>
        /// <example>
        /// <code>
        /// string[] args = new[] { "-d", @"C:\logs", "-o", @"C:\output.csv" };
        /// bool isValid = appArg.Parse(args);
        /// </code>
        /// </example>
        public bool Parse(string[] args)
        {
            // 変数宣言・初期化
            Dictionary<string, string> argProperties = []; // 引数
            List<string> includeRegexes = [];     // 絞込正規表現リスト
            List<string> excludeRegexes = [];     // 除外正規表現リスト
            List<string> orderCsvs = [];          // 抽出順序リスト
            List<string> formats = [];            // 書式リスト
            string tempValue = "";
            bool isValid = true;

            // -----------------------------------------------------------------
            // ClsCmmnParams処理
            // -----------------------------------------------------------------
            Dictionary<string, string> namedArgs = MdlArg.GetNamedArgs(args, false);
            _cmmnArgs.NamedArgs = namedArgs;
            isValid = _cmmnArgs.GetCommonArgs();
            _prop.MachineName = _cmmnArgs.MachineName;

            // -----------------------------------------------------------------
            // ClsCmmnParams引数取得：ETC
            // -----------------------------------------------------------------
            // -h|--help ：使用方法
            _usageFlag = _cmmnArgs.IsUsage ? USAGE_USAGE : USAGE_NONE;
            // -v|--verbose ：冗長モード
            argProperties[ClsProp.VERBOSE] = _cmmnArgs.Verbose.ToString();
            // -stacktrace ：例外時スタックトレース表示
            if (_cmmnArgs.IsStackTrace) argProperties[ClsProp.IS_STACKTRACE] = "true";
            // --show-sample-config：SHOW SAMPLE CONFIG
            foreach (string key in (string[])["show-sample-config"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    _usageFlag = USAGE_SHOW_SAMPLE_CONFIG;
                }
            }

            // -----------------------------------------------------------------
            // options:
            // -----------------------------------------------------------------
            // -c config path ：設定ファイルパス
            foreach (string key in (string[])["c", "conf"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.CONF_PATH] = tempValue;
                    }
                }
            }

            // -d|--src dir   ：ソースパス
            foreach (string key in (string[])["d", "src"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.SRC_PATH] = tempValue;
                    }
                }
            }

            // -o|--out path  ：出力ファイルパス
            foreach (string key in (string[])["o", "out", "save"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.TO_PATH] = tempValue;
                    }
                }
            }

            // -i|-r regex    ：集約正規表現
            foreach (string key in (string[])["i", "r"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    // 重複キー対応
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (("-" + key).Equals(args[i]) || ("--" + key).Equals(args[i]))
                        {
                            if (!"-".Equals(args[i + 1].Substring(0, 1)) || MdlUtil.IsNumeric(args[i + 1]))
                            {
                                includeRegexes.Add(args[++i]);
                            }
                        }
                    }
                }
            }

            // -O|--order csv ：抽出順序
            foreach (string key in (string[])["O", "order"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    // 重複キー対応
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (("-" + key).Equals(args[i]) || ("--" + key).Equals(args[i]))
                        {
                            if (!"-".Equals(args[i + 1].Substring(0, 1)) || MdlUtil.IsNumeric(args[i + 1]))
                            {
                                orderCsvs.Add(args[++i]);
                            }
                        }
                    }
                }
            }

            // -F|--format str：書式
            foreach (string key in (string[])["F", "format"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    // 重複キー対応
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (("-" + key).Equals(args[i]) || ("--" + key).Equals(args[i]))
                        {
                            if (!"-".Equals(args[i + 1].Substring(0, 1)) || MdlUtil.IsNumeric(args[i + 1]))
                            {
                                formats.Add(args[++i]);
                            }
                        }
                    }
                }
            }

            // -x regex    ：除外判定正規表現
            foreach (string key in (string[])["x", "pre-exc", "post-exc"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    // 重複キー対応
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (("-" + key).Equals(args[i]) || ("--" + key).Equals(args[i]))
                        {
                            if (!"-".Equals(args[i + 1].Substring(0, 1)) || MdlUtil.IsNumeric(args[i + 1]))
                            {
                                excludeRegexes.Add(args[++i]);
                            }
                        }
                    }
                }
            }

            // -----------------------------------------------------------------
            // Advanced options：
            // -----------------------------------------------------------------
            // -R|--recursive ：再帰処理フラグ
            foreach (string key in (string[])["R", "recursive"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    argProperties[ClsProp.IS_RECURSIVE] = "true";
                }
            }

            // -e|--enc enc   ：UTF-8|MS932|EUC-JP
            foreach (string key in (string[])["e", "enc"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.ENCODING] = tempValue;
                    }
                }
            }

            // -g|--ic        ：大文字小文字非区別フラグ
            foreach (string key in (string[])["g", "case-insensitive", "ic", "ignore-case"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    argProperties[ClsProp.IS_CASE_INSENSITIVE] = "true";
                }
            }

            // -P|--pipe      ：パイプ入力フラグ
            foreach (string key in (string[])["P", "pipe"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    argProperties[ClsProp.IS_PIPE_IN] = "true";
                }
            }

            // -----------------------------------------------------------------
            // File Filter options：
            // -----------------------------------------------------------------
            // -if inc files ：絞込ファイル
            foreach (string key in (string[])["if", "inc"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.INC_FILES] = tempValue;
                    }
                }
            }

            // -xf exc files ：除外ファイル
            foreach (string key in (string[])["xf", "exc"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.EXC_FILES] = tempValue;
                    }
                }
            }

            // -id inc dirs ：絞込ディレクトリ
            foreach (string key in (string[])["id"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.INC_DIRS] = tempValue;
                    }
                }
            }

            // -xd exc dirs ：除外ディレクトリ
            foreach (string key in (string[])["xd"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        argProperties[ClsProp.EXC_DIRS] = tempValue;
                    }
                }
            }

            // -----------------------------------------------------------------
            // Format specifier conversion options:
            // -----------------------------------------------------------------
            // --specifier [日時]  ：書式指定変換フラグ
            foreach (string key in (string[])["specifier"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    argProperties[ClsProp.IS_FORMAT_CONV] = "true";
                    tempValue = MdlArg.GetValue(namedArgs, key) ?? "";
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        Match match = Regex.Match(tempValue, @"^m(\d+)$");
                        if (match.Success)
                        {
                            tempValue = "-" + match.Groups[1].Value;
                        }
                        argProperties[ClsProp.BASE_DATE_INDICATOR] = tempValue;
                    }
                }
            }

            // -----------------------------------------------------------------
            // Debug options：
            // -----------------------------------------------------------------
            // -show-nomatch ：非一致行表示フラグ
            foreach (string key in (string[])["show-nomatch"])
            {
                if (MdlArg.ContainsKey(namedArgs, key))
                {
                    argProperties[ClsProp.SHOW_NOMATCH_LINE] = "true";
                }
            }

            // -----------------------------------------------------------------
            // 設定ファイルの読込
            // -----------------------------------------------------------------
            if (argProperties.TryGetValue(ClsProp.CONF_PATH, out string? confPathValue))
            {
                ClsConfigFile confFile = new(_logger);
                try
                {
                    confFile.Clear();
                    confFile.ConfigDictionary = _prop.Properties;
                    confFile.DuplicateKeys.Add(ClsProp.INCLUDES_REGEX);
                    confFile.DuplicateKeys.Add(ClsProp.ORDER);
                    confFile.DuplicateKeys.Add(ClsProp.FORMAT);
                    confFile.DuplicateKeys.Add(ClsProp.EXCLUDES_REGEX);
                    confFile.LoadToDictionary(confPathValue);
                    if (confFile.ListDictionary.TryGetValue(ClsProp.INCLUDES_REGEX, out List<string>? incRegexList))
                    {
                        foreach (string value in incRegexList)
                        {
                            _prop.IncludeRegexes.Add(value);
                        }
                    }
                    if (confFile.ListDictionary.TryGetValue(ClsProp.ORDER, out List<string>? orderList))
                    {
                        foreach (string value in orderList)
                        {
                            _orderCsvs.Add(value);
                        }
                    }
                    if (confFile.ListDictionary.TryGetValue(ClsProp.FORMAT, out List<string>? formatList))
                    {
                        foreach (string value in formatList)
                        {
                            _prop.Formats.Add(value);
                        }
                    }
                    if (confFile.ListDictionary.TryGetValue(ClsProp.EXCLUDES_REGEX, out List<string>? excRegexList))
                    {
                        foreach (string value in excRegexList)
                        {
                            _prop.ExcludeRegexes.Add(value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    isValid = false;
                    _logger.WriteLine(MdlConst.LVL_E, "EXCEPTION : READ CONF : " + confPathValue + "：" + ex.Message);
                }
                confFile.ListDictionary.Clear();
                confFile.DuplicateKeys.Clear();
            }

            // -----------------------------------------------------------------
            // 引数を上書き
            // -----------------------------------------------------------------
            if (argProperties.Count > 0)
            {
                foreach (string key in argProperties.Keys)
                {
                    _prop.Properties[key] = argProperties[key];
                }
            }

            // -----------------------------------------------------------------
            // --replace
            // -----------------------------------------------------------------
            foreach (string key in (string[])[ClsProp.SRC_PATH, ClsProp.TO_PATH, ClsProp.CONF_PATH, ClsProp.INC_FILES, ClsProp.EXC_FILES, ClsProp.INC_DIRS, ClsProp.EXC_DIRS])
            {
                tempValue = _prop.GetValue(key, "");
                if (!string.IsNullOrEmpty(tempValue))
                {
                    if (_cmmnArgs.ReplaceDic.Count > 0) tempValue = _cmmnArgs.ReplaceByDictionary(tempValue);
                    _prop.Properties[key] = tempValue;
                }
            }

            // 引数を上書き：絞込正規表現リスト
            if (_prop.IncludeRegexes.Count > 0 || includeRegexes.Count > 0)
            {
                if (includeRegexes.Count > 0)
                {
                    _prop.IncludeRegexes.Clear();
                    foreach (string s in includeRegexes)
                    {
                        _prop.IncludeRegexes.Add(s);
                    }
                }
                includeRegexes.Clear();
            }
            else
            {
                _prop.IncludeRegexes.Add(ClsProp.DEFAULT_REGEX_RULE);
            }

            // 引数を上書き：除外正規表現リスト
            if (_prop.ExcludeRegexes.Count > 0 || excludeRegexes.Count > 0)
            {
                if (excludeRegexes.Count > 0)
                {
                    _prop.ExcludeRegexes.Clear();
                    foreach (string s in excludeRegexes)
                    {
                        _prop.ExcludeRegexes.Add(s);
                    }
                }
                excludeRegexes.Clear();
            }

            // 引数を上書き：抽出順序リスト
            if (_orderCsvs.Count > 0 || orderCsvs.Count > 0)
            {
                if (orderCsvs.Count > 0)
                {
                    _orderCsvs.Clear();
                    foreach (string s in orderCsvs)
                    {
                        _orderCsvs.Add(s);
                    }
                }
                orderCsvs.Clear();
            }
            else
            {
                _orderCsvs.Add(ClsProp.DEFAULT_ORDER);
            }
            for (int m = 0; m < _orderCsvs.Count; m++)
            {
                string[] ordersTemp = Regex.Split(_orderCsvs[m], ",");
                int[] orders = new int[ordersTemp.Length];
                for (int i = 0; i < ordersTemp.Length; i++)
                {
                    orders[i] = MdlUtil.ParseInt(ordersTemp[i], 1);
                }
                _prop.OrdersList.Add(orders);
            }

            // 引数を上書き：書式リスト
            if (_prop.Formats.Count > 0 || formats.Count > 0)
            {
                if (formats.Count > 0)
                {
                    _prop.Formats.Clear();
                    foreach (string s in formats)
                    {
                        _prop.Formats.Add(s);
                    }
                }
                formats.Clear();
            }
            // -----------------------------------------------------------------
            // チェック
            // -----------------------------------------------------------------
            // 集約正規表現と抽出順序の数が一致しているか
            if (_prop.OrdersList.Count != _prop.IncludeRegexes.Count)
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _logger.WriteLine(MdlConst.LVL_E, "THE NUMBERS OF REGEXRULE AND ORDER ARE DIFFERENT");
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                _prop.ShowRulesForDebug();
                _logger.WriteLine(MdlConst.LVL_NONE, "");
                isValid = false;
            }

            // SRC_PATHが指定されていない場合
            if (string.IsNullOrEmpty(_prop.GetValue(ClsProp.SRC_PATH, ClsProp.DEFAULT_SRC_PATH)))
            {
                if (_usageFlag == USAGE_NONE)
                {
                    if (!string.Equals(_prop.GetValue(ClsProp.IS_PIPE_IN, "false"), "true", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.WriteLine(MdlConst.LVL_E, "PLEASE SPECIFY THE -d OPTION");
                        isValid = false;
                    }
                }
            }
            // -----------------------------------------------------------------
            // 事前処理：書式指定変換
            // -----------------------------------------------------------------
            if (string.Equals(_prop.GetValue(ClsProp.IS_FORMAT_CONV, "false"), "true", StringComparison.OrdinalIgnoreCase))
            {
                string baseDateIndicator = _prop.GetValue(ClsProp.BASE_DATE_INDICATOR, "now").ToLower();
                // 基準日時取得
                if (!"now".Equals(baseDateIndicator))
                {
                    _prop.BaseDate = ParseBaseDate(baseDateIndicator);
                }
                // SRC_PATH
                tempValue = _prop.GetValue(ClsProp.SRC_PATH, "");
                if (!string.IsNullOrEmpty(tempValue))
                {
                    _prop.Properties[ClsProp.SRC_PATH] = ReplacePlaceholders(MdlDate.ConvertFormattedDate(ParseBaseDate(baseDateIndicator), tempValue));
                }
                // TO_PATH
                tempValue = _prop.GetValue(ClsProp.TO_PATH, "");
                if (!string.IsNullOrEmpty(tempValue))
                {
                    _prop.Properties[ClsProp.TO_PATH] = ReplacePlaceholders(MdlDate.ConvertFormattedDate(ParseBaseDate(baseDateIndicator), tempValue));
                }
                // INC_FILES
                tempValue = _prop.GetValue(ClsProp.INC_FILES, "");
                if (!string.IsNullOrEmpty(tempValue))
                {
                    _prop.Properties[ClsProp.INC_FILES] = ReplacePlaceholders(MdlDate.ConvertFormattedDate(ParseBaseDate(baseDateIndicator), tempValue));
                }
                // EXC_FILES
                tempValue = _prop.GetValue(ClsProp.EXC_FILES, "");
                if (!string.IsNullOrEmpty(tempValue))
                {
                    _prop.Properties[ClsProp.EXC_FILES] = ReplacePlaceholders(MdlDate.ConvertFormattedDate(ParseBaseDate(baseDateIndicator), tempValue));
                }
                // INC_DIRS
                tempValue = _prop.GetValue(ClsProp.INC_DIRS, "");
                if (!string.IsNullOrEmpty(tempValue))
                {
                    _prop.Properties[ClsProp.INC_DIRS] = ReplacePlaceholders(MdlDate.ConvertFormattedDate(ParseBaseDate(baseDateIndicator), tempValue));
                }
                // EXC_DIRS
                tempValue = _prop.GetValue(ClsProp.EXC_DIRS, "");
                if (!string.IsNullOrEmpty(tempValue))
                {
                    _prop.Properties[ClsProp.EXC_DIRS] = ReplacePlaceholders(MdlDate.ConvertFormattedDate(ParseBaseDate(baseDateIndicator), tempValue));
                }
            }

            // -----------------------------------------------------------------
            // CSV文字列のリスト化
            // -----------------------------------------------------------------
            _prop.IncludeFiles = MdlUtil.ParseCsvToList([], _prop.GetValue(ClsProp.INC_FILES, ""), ",", _prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE), true, true);
            _prop.ExcludeFiles = MdlUtil.ParseCsvToList([], _prop.GetValue(ClsProp.EXC_FILES, ""), ",", _prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE), true, true);
            _prop.IncludeDirectories = MdlUtil.ParseCsvToList([], _prop.GetValue(ClsProp.INC_DIRS, ""), ",", _prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE), true, true);
            _prop.ExcludeDirectories = MdlUtil.ParseCsvToList([], _prop.GetValue(ClsProp.EXC_DIRS, ""), ",", _prop.GetValue(ClsProp.VERBOSE, ClsProp.DEFAULT_VERBOSE), true, true);

            // -----------------------------------------------------------------
            // 掃除
            // -----------------------------------------------------------------
            namedArgs.Clear();
            argProperties.Clear();
            includeRegexes.Clear();
            excludeRegexes.Clear();
            orderCsvs.Clear();
            formats.Clear();
            if (_usageFlag == USAGE_NONE)
            {
                _orderCsvs.Clear();
            }

            // -----------------------------------------------------------------
            // END
            // -----------------------------------------------------------------
            return isValid;
        }

        /// <summary>
        /// アプリケーションの使用方法（ヘルプメッセージ）をログに出力します。
        /// </summary>
        /// <example>
        /// <code>
        /// appArg.ShowUsage();
        /// </code>
        /// </example>
        public void ShowUsage()
        {
            _logger.WriteLine(MdlConst.LVL_NONE, "");
            _logger.WriteLine(MdlConst.LVL_NONE, "Usage : " + _prop.ExeDir + Path.DirectorySeparatorChar + _prop.ExeBaseName + ".exe [Option] [Option]...");
            _logger.WriteLine(MdlConst.LVL_NONE, "");
            _logger.WriteLine(MdlConst.LVL_NONE, "options:");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -c config path ：設定ファイルパス              （現在値⇒" + _prop.GetValue(ClsProp.CONF_PATH, ClsProp.DEFAULT_CONF_PATH) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -d|--src dir   ：ソースパス                    （現在値⇒" + _prop.GetValue(ClsProp.SRC_PATH, ClsProp.DEFAULT_SRC_PATH) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -o|--out path  ：出力ファイルパス              （現在値⇒" + _prop.GetValue(ClsProp.TO_PATH, "") + "）");
            if (_prop.IncludeRegexes.Count > 0)
            {
                foreach (string s in _prop.IncludeRegexes)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "  -i|-r regex    ：集約正規表現                  （現在値⇒" + s + "）");
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "  -i|-r regex    ：集約正規表現                  （現在値⇒" + MdlUtil.Join(_prop.IncludeRegexes, ", ") + "）");
            }
            if (_orderCsvs.Count > 0)
            {
                foreach (string s in _orderCsvs)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "  -O|--order csv ：抽出順序                      （現在値⇒" + s + "）");
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "  -O|--order csv ：抽出順序                      （現在値⇒" + MdlUtil.Join(_orderCsvs, ", ") + "）");
            }
            if (_prop.Formats.Count > 0)
            {
                foreach (string s in _prop.Formats)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "  -F|--format str：書式                          （現在値⇒" + s + "）");
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "  -F|--format str：書式                          （現在値⇒" + MdlUtil.Join(_prop.Formats, ", ") + "）");
            }
            if (_prop.ExcludeRegexes.Count > 0)
            {
                foreach (string s in _prop.ExcludeRegexes)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "  -x regex       ：除外判定正規表現              （現在値⇒" + s + "）");
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "  -x regex       ：除外判定正規表現              （現在値⇒" + MdlUtil.Join(_prop.ExcludeRegexes, ", ") + "）");
            }
            _logger.WriteLine(MdlConst.LVL_NONE, "Advanced options：");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -R|--recursive ：再帰処理フラグ                （現在値⇒" + _prop.GetValue(ClsProp.IS_RECURSIVE, "false") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -e|--enc enc   ：UTF-8|MS932|EUC-JP            （現在値⇒" + _prop.GetValue(ClsProp.ENCODING, ClsProp.DEFAULT_ENCODING) + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -g|--ic        ：大文字小文字非区別フラグ      （現在値⇒" + _prop.GetValue(ClsProp.IS_CASE_INSENSITIVE, "false") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -P|--pipe      ：パイプ入力フラグ              （現在値⇒" + _prop.GetValue(ClsProp.IS_PIPE_IN, "false") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "File Filter options：");
            _logger.WriteLine(MdlConst.LVL_NONE, "  --id 正規表現  ：絞込ディレクトリ名(,|/区切り）（現在値⇒" + _prop.GetValue(ClsProp.INC_DIRS, "") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  --xd 正規表現  ：除外ディレクトリ名(,|/区切り）（現在値⇒" + _prop.GetValue(ClsProp.EXC_DIRS, "") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  --if 正規表現  ：絞込ファイル名(,|/区切り)     （現在値⇒" + _prop.GetValue(ClsProp.INC_FILES, "") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "  --xf 正規表現  ：除外ファイル名(,|/区切り)     （現在値⇒" + _prop.GetValue(ClsProp.EXC_FILES, "") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "Format specifier conversion options:");
            _logger.WriteLine(MdlConst.LVL_NONE, "  --specifier [日時]  ：書式指定変換フラグ       （現在値⇒" + _prop.GetValue(ClsProp.IS_FORMAT_CONV, "false") + " : " + _prop.GetValue(ClsProp.BASE_DATE_INDICATOR, "now") + " ⇒ " + MdlDate.GetFormattedDate(_prop.BaseDate, "yyyy/MM/dd HH:mm:ss") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "    ※書式指定子：%Y、%m、%d、%H、%M、%S、%w、_COMPUTERNAME_");
            _logger.WriteLine(MdlConst.LVL_NONE, "    ※基準日時  ：now|today|yesterday|nextday|FirstOfThisMonth|EndOfLastMonth|fotm|eolm");
            _logger.WriteLine(MdlConst.LVL_NONE, "Debug options：");
            _logger.WriteLine(MdlConst.LVL_NONE, "  --show-nomatch ：未マッチ行の表示フラグ        （現在値⇒" + _prop.GetValue(ClsProp.SHOW_NOMATCH_LINE, "false") + "）");
            _logger.WriteLine(MdlConst.LVL_NONE, "");
            _logger.WriteLine(MdlConst.LVL_NONE, "Help options:");
            _logger.WriteLine(MdlConst.LVL_NONE, "  -h                  ：SHOW THIS HELP MESSAGE");
            _logger.WriteLine(MdlConst.LVL_NONE, "  --show-sample-config：SHOW SAMPLE CONFIG");
            _logger.WriteLine(MdlConst.LVL_NONE, "");
            _logger.WriteLine(MdlConst.LVL_NONE, "exit code:                 正常=0 / 異常=20");
            _logger.WriteLine(MdlConst.LVL_NONE, "");
        }

        /// <summary>
        /// 設定ファイルのサンプル内容をログに出力します。
        /// </summary>
        /// <example>
        /// <code>
        /// appArg.ShowSampleConfig();
        /// </code>
        /// </example>
        public void ShowSampleConfig()
        {
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# パス設定");
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ソースパス：ディレクトリ（非再帰）|ファイル");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-d|--src path");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.SRC_PATH + " = " + _prop.GetValue(ClsProp.SRC_PATH, "/var/log/xxx"));
            _logger.WriteLine(MdlConst.LVL_NONE, "# 再帰処理フラグ");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-R|--recursive");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.IS_RECURSIVE + " = " + _prop.GetValue(ClsProp.IS_RECURSIVE, "false"));
            _logger.WriteLine(MdlConst.LVL_NONE, "# パイプ入力フラグ");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-P|--pipe");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.IS_PIPE_IN + " = " + _prop.GetValue(ClsProp.IS_PIPE_IN, "false"));
            _logger.WriteLine(MdlConst.LVL_NONE, "# 出力ファイル");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--out file path");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.TO_PATH + " = " + _prop.GetValue(ClsProp.TO_PATH, "/temp/output.csv"));
            _logger.WriteLine(MdlConst.LVL_NONE, "# 文字コード");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-e  MS932|UTF-8");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.ENCODING + " = " + _prop.GetValue(ClsProp.ENCODING, ClsProp.DEFAULT_ENCODING));
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# 集約ルール");
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# 正規表現（複数指定可能）");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-i regex");
            if (_prop.IncludeRegexes.Count > 0)
            {
                foreach (string s in _prop.IncludeRegexes)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.INCLUDES_REGEX + " = " + s);
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.INCLUDES_REGEX + " = ^.*\\(SERVICE_NAME=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*$");
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.INCLUDES_REGEX + " = ^.*\\(HOST=([^\\)]+)\\).*\\(SERVICE_NAME=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*$");
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.INCLUDES_REGEX + " = ^.*\\(HOST=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*\\(SERVICE_NAME=([^\\)]+)\\).*$");
            }
            _logger.WriteLine(MdlConst.LVL_NONE, "# 上述正規表現で指定したキャプチャグループの抽出順序（複数指定可能）");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-O|--order csv");
            if (_orderCsvs.Count > 0)
            {
                foreach (string s in _orderCsvs)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.ORDER + " = " + s);
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.ORDER + " = 2,3,1");
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.ORDER + " = 3,2,1");
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.ORDER + " = 3,1,2");
            }
            _logger.WriteLine(MdlConst.LVL_NONE, "# 上述正規表現で指定したキャプチャグループの出力書式（複数指定可能）");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-F|--format format");
            if (_prop.Formats.Count > 0)
            {
                foreach (string s in _prop.Formats)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.FORMAT + " = " + s);
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.FORMAT + " = {\"host\": \"%s\", \"ip\": \"%s\", \"service_name\": \"%s\"}");
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.FORMAT + " = {\"host\": \"%s\", \"ip\": \"%s\", \"service_name\": \"%s\"}");
                _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.FORMAT + " = {\"host\": \"%s\", \"ip\": \"%s\", \"service_name\": \"%s\"}");
            }
            _logger.WriteLine(MdlConst.LVL_NONE, "# 上述正規表現にマッチしなかった行の表示フラグ");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--show-nomatch");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.SHOW_NOMATCH_LINE + " = " + _prop.GetValue(ClsProp.SHOW_NOMATCH_LINE, "false"));
            _logger.WriteLine(MdlConst.LVL_NONE, "# 除外判定正規表現（複数指定可能）");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-x regex");
            if (_prop.ExcludeRegexes.Count > 0)
            {
                foreach (string s in _prop.ExcludeRegexes)
                {
                    _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.EXCLUDES_REGEX + " = " + s);
                }
            }
            else
            {
                _logger.WriteLine(MdlConst.LVL_NONE, "#" + ClsProp.EXCLUDES_REGEX + " = ^.*\\(USER=grid\\).*$");
                _logger.WriteLine(MdlConst.LVL_NONE, "#" + ClsProp.EXCLUDES_REGEX + " = ^.*\\(COMMAND=status\\).*$");
            }
            _logger.WriteLine(MdlConst.LVL_NONE, "# 大文字小文字非区別フラグ");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--ic");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.IS_CASE_INSENSITIVE + " = " + _prop.GetValue(ClsProp.IS_CASE_INSENSITIVE, "false"));
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ファイル名の絞込／除外");
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# 絞込");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--if csv");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.INC_FILES + " = " + _prop.GetValue(ClsProp.INC_FILES, ".*\\.log,.*\\.txt"));
            _logger.WriteLine(MdlConst.LVL_NONE, "# 除外");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--xf csv");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.EXC_FILES + " = " + _prop.GetValue(ClsProp.EXC_FILES, "^access_aaa,^access_bbb"));
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# サブフォルダ名の絞込／除外（再帰処理フラグがONの場合）");
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# 絞込");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--id csv");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.INC_DIRS + " = " + _prop.GetValue(ClsProp.INC_DIRS, "^SUB-A$,.*SUB-B.*$"));
            _logger.WriteLine(MdlConst.LVL_NONE, "# 除外");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--xd csv");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.EXC_DIRS + " = " + _prop.GetValue(ClsProp.EXC_DIRS, "^SUB-C$,.*SUB-D.*$"));
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# 書式指定変換");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ※書式指定子：%Y、%m、%d、%H、%M、%S、%w、%pid、_COMPUTERNAME_");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ※基準日時  ：now|today|yesterday|nextday|FirstOfThisMonth|EndOfLastMonth|fotm|eolm");
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# 書式指定変換フラグ");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：--specifier [日時]");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.IS_FORMAT_CONV + " = " + _prop.GetValue(ClsProp.IS_FORMAT_CONV, "false"));
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.BASE_DATE_INDICATOR + " = " + _prop.GetValue(ClsProp.BASE_DATE_INDICATOR, "now"));
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# その他");
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
            _logger.WriteLine(MdlConst.LVL_NONE, "# 冗長レベル");
            _logger.WriteLine(MdlConst.LVL_NONE, "# ---> 引数：-v|-vv|-vvv|-vv num");
            _logger.WriteLine(MdlConst.LVL_NONE, "" + ClsProp.VERBOSE + " = " + _prop.GetValue(ClsProp.VERBOSE, 0));
            _logger.WriteLine(MdlConst.LVL_NONE, "################################################################################");
        }

        /// <summary>
        /// 指定された基準日付インジケータ文字列 ("today", "yesterday", 相対数値文字列等) を解析して該当する <see cref="DateTime"/> を返します。
        /// </summary>
        /// <param name="baseDateIndicator">基準日付インジケータ文字列</param>
        /// <returns>計算された <see cref="DateTime"/> オブジェクト</returns>
        /// <example>
        /// <code>
        /// DateTime dt1 = appArg.ParseBaseDate("today");
        /// DateTime dt2 = appArg.ParseBaseDate("-3"); // 3日前
        /// </code>
        /// </example>
        public DateTime ParseBaseDate(string baseDateIndicator)
        {
            if (string.IsNullOrEmpty(baseDateIndicator) || "0".Equals(baseDateIndicator))
            {
                return DateTime.Now;
            }

            if (MdlUtil.IsNumeric(baseDateIndicator))
            {
                return DateTime.Today.AddDays(MdlUtil.ParseInt(baseDateIndicator, 0));
            }

            return baseDateIndicator.ToLower() switch
            {
                "today" => DateTime.Today,
                "yesterday" or "lastday" => DateTime.Today.AddDays(-1),
                "tomorrow" or "nextday" => DateTime.Today.AddDays(1),
                "fotm" or "firstofthismonth" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                "eolm" or "endoflastmonth" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1),
                _ => DateTime.Now
            };
        }

        /// <summary>
        /// 文字列内のコンピュータ名やプロセスIDなどのプレースホルダーを現在の設定値へ置換します。
        /// </summary>
        /// <param name="target">置換対象の文字列</param>
        /// <returns>プレースホルダーが値に置き換えられた文字列</returns>
        /// <example>
        /// <code>
        /// string path = appArg.ReplacePlaceholders(@"C:\logs\_COMPUTERNAME_\%pid.log");
        /// </code>
        /// </example>
        public string ReplacePlaceholders(string target)
        {
            target = target.Replace("_COMPUTERNAME_", _prop.MachineName);
            target = target.Replace("%pid", _prop.Pid.ToString());
            return target;
        }

    }
}

