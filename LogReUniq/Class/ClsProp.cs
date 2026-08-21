using CmnClsLib.Class;
using CmnClsLib.Module;

// 2026/08/15 Gemini 3.6 Flash (High) Review & Modified

namespace LogReUniq.Class
{
    /// <summary>
    /// アプリケーションのプロパティおよび設定情報を保持・管理するクラスです。
    /// </summary>
    /// <example>
    /// <code>
    /// var prop = new ClsProp();
    /// prop.SetValue("SrcPath", @"C:\logs");
    /// string src = prop.GetValue("SrcPath", "");
    /// </code>
    /// </example>
    public class ClsProp
    {
        /// <summary>デフォルト値：詳細表示レベル</summary>
        public static readonly int DEFAULT_VERBOSE = 0;

        /// <summary>デフォルト値：文字コード</summary>
        public static readonly string DEFAULT_ENCODING = "MS932";

        /// <summary>デフォルト値：ソースパス</summary>
        public static readonly string DEFAULT_SRC_PATH = "";

        /// <summary>デフォルト値：設定ファイルパス</summary>
        public static readonly string DEFAULT_CONF_PATH = "";

        /// <summary>デフォルト値：ログ絞込ルール</summary>
        public static readonly string DEFAULT_REGEX_RULE = "^([^,]+),([^,]+),([^,]+)$";

        /// <summary>デフォルト値：抽出順序</summary>
        public static readonly string DEFAULT_ORDER = "1,2,3";

        /// <summary>キー名：詳細表示レベル</summary>
        public static readonly string VERBOSE = "Verbose";

        /// <summary>キー名：ソースパス</summary>
        public static readonly string SRC_PATH = "SrcPath";

        /// <summary>キー名：出力先</summary>
        public static readonly string TO_PATH = "ToPath";

        /// <summary>キー名：設定ファイルパス</summary>
        public static readonly string CONF_PATH = "ConfPath";

        /// <summary>キー名：ファイル名の絞込文字列リスト</summary>
        public static readonly string INC_FILES = "IncFiles";

        /// <summary>キー名：ファイル名の除外文字列リスト</summary>
        public static readonly string EXC_FILES = "ExcFiles";

        /// <summary>キー名：サブフォルダ名の絞込文字列リスト</summary>
        public static readonly string INC_DIRS = "IncDirs";

        /// <summary>キー名：サブフォルダ名の除外文字列リスト</summary>
        public static readonly string EXC_DIRS = "ExcDirs";

        /// <summary>キー名：文字コード</summary>
        public static readonly string ENCODING = "Encoding";

        /// <summary>キー名：抽出順序</summary>
        public static readonly string ORDER = "Order";

        /// <summary>キー名：書式</summary>
        public static readonly string FORMAT = "Format";

        /// <summary>キー名：正規表現にヒットしない行の表示フラグ</summary>
        public static readonly string SHOW_NOMATCH_LINE = "ShowNoMatchLine";

        /// <summary>キー名：CASE_INSENSITIVEフラグ</summary>
        public static readonly string IS_CASE_INSENSITIVE = "IgnoreCase";

        /// <summary>キー名：パイプ入力フラグ</summary>
        public static readonly string IS_PIPE_IN = "PipeIn";

        /// <summary>キー名：再帰処理フラグ</summary>
        public static readonly string IS_RECURSIVE = "Recursive";

        /// <summary>キー名：書式指定変換フラグ</summary>
        public static readonly string IS_FORMAT_CONV = "FormatSpecifierConv";

        /// <summary>キー名：書式指定変換基準日時指定子</summary>
        public static readonly string BASE_DATE_INDICATOR = "BaseTimeIndicator";

        /// <summary>キー名：絞込正規表現</summary>
        public static readonly string INCLUDES_REGEX = "Include";

        /// <summary>キー名：除外正規表現</summary>
        public static readonly string EXCLUDES_REGEX = "Exclude";

        /// <summary>互換性確保：ログ集約ルール</summary>
        public static readonly string REGEX_RULE = "RegexRule";

        /// <summary>互換性確保：集約抽出前除外判定正規表現</summary>
        public static readonly string PRE_EXC_REGEX = "PreExcRegex";

        /// <summary>互換性確保：集約抽出後除外判定正規表現</summary>
        public static readonly string POST_EXC_REGEX = "PostExcRegex";

        /// <summary>キー名：StackTrace表示フラグ</summary>
        public static readonly string IS_STACKTRACE = "StackTrace";

        private Dictionary<string, string> _prop = [];
        private List<string> _keyList = [];
        private List<string> _includeRegexes = [];
        private List<string> _excludeRegexes = [];
        private List<string> _orderCsvs = [];
        private List<string> _formats = [];
        private List<string> _includeFiles = [];
        private List<string> _excludeFiles = [];
        private List<string> _includeDirectories = [];
        private List<string> _excludeDirectories = [];
        private List<int[]> _ordersList = [];
        private string _exeDir = "";
        private string _exeBaseName = "";
        private string _machineName = "";
        private int _pid;
        private DateTime _baseDate = DateTime.Now;
        private int _keyLength;

        /// <summary>
        /// ClsProp クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <example>
        /// <code>
        /// ClsProp prop = new ClsProp();
        /// </code>
        /// </example>
        public ClsProp()
        {
            Initialize();
        }

        /// <summary>プロパティ辞書を取得または設定します。</summary>
        public Dictionary<string, string> Properties { get => _prop; set => _prop = value; }

        /// <summary>引数項目名リストを取得または設定します。</summary>
        public List<string> KeyList { get => _keyList; set => _keyList = value; }

        /// <summary>絞込正規表現リストを取得または設定します。</summary>
        public List<string> IncludeRegexes { get => _includeRegexes; set => _includeRegexes = value; }

        /// <summary>除外正規表現リストを取得または設定します。</summary>
        public List<string> ExcludeRegexes { get => _excludeRegexes; set => _excludeRegexes = value; }

        /// <summary>書式リストを取得または設定します。</summary>
        public List<string> Formats { get => _formats; set => _formats = value; }

        /// <summary>絞込ファイル名リストを取得または設定します。</summary>
        public List<string> IncludeFiles { get => _includeFiles; set => _includeFiles = value; }

        /// <summary>除外ファイル名リストを取得または設定します。</summary>
        public List<string> ExcludeFiles { get => _excludeFiles; set => _excludeFiles = value; }

        /// <summary>絞込サブフォルダ名リストを取得または設定します。</summary>
        public List<string> IncludeDirectories { get => _includeDirectories; set => _includeDirectories = value; }

        /// <summary>除外サブフォルダ名リストを取得または設定します。</summary>
        public List<string> ExcludeDirectories { get => _excludeDirectories; set => _excludeDirectories = value; }

        /// <summary>抽出順序リストを取得または設定します。</summary>
        public List<int[]> OrdersList { get => _ordersList; set => _ordersList = value; }

        /// <summary>実行ディレクトリを取得または設定します。</summary>
        public string ExeDir { get => _exeDir; set => _exeDir = value; }

        /// <summary>実行ファイルのベース名を取得または設定します。</summary>
        public string ExeBaseName { get => _exeBaseName; set => _exeBaseName = value; }

        /// <summary>コンピュータ名を取得または設定します。</summary>
        public string MachineName { get => _machineName; set => _machineName = value; }

        /// <summary>プロセスIDを取得または設定します。</summary>
        public int Pid { get => _pid; set => _pid = value; }

        /// <summary>基準日付を取得または設定します。</summary>
        public DateTime BaseDate { get => _baseDate; set => _baseDate = value; }

        /// <summary>キーの最大長を取得または設定します。</summary>
        public int KeyLength { get => _keyLength; set => _keyLength = value; }

        /// <summary>
        /// プロパティの設定および内部リストを初期化します。
        /// </summary>
        /// <example>
        /// <code>
        /// prop.Initialize();
        /// </code>
        /// </example>
        public void Initialize()
        {
            _keyList.Clear();
            _keyList.Add(VERBOSE);
            _keyList.Add(SRC_PATH);
            _keyList.Add(TO_PATH);
            _keyList.Add(CONF_PATH);
            _keyList.Add(INC_FILES);
            _keyList.Add(EXC_FILES);
            _keyList.Add(INC_DIRS);
            _keyList.Add(EXC_DIRS);
            _keyList.Add(ENCODING);
            _keyList.Add(INCLUDES_REGEX);
            _keyList.Add(EXCLUDES_REGEX);
            _keyList.Add(ORDER);
            _keyList.Add(FORMAT);
            _keyList.Add(SHOW_NOMATCH_LINE);
            _keyList.Add(IS_CASE_INSENSITIVE);
            _keyList.Add(IS_PIPE_IN);
            _keyList.Add(IS_RECURSIVE);
            _keyList.Add(IS_FORMAT_CONV);
            _keyList.Add(BASE_DATE_INDICATOR);
            _keyList.Add(REGEX_RULE);
            _keyList.Add(PRE_EXC_REGEX);
            _keyList.Add(POST_EXC_REGEX);
            _keyList.Add(IS_STACKTRACE);

            _keyLength = 0;
            foreach (var key in _keyList)
            {
                if (key.Length > _keyLength)
                {
                    _keyLength = key.Length;
                }
            }
        }

        /// <summary>
        /// 指定されたキーに対応する文字列の値を取得します。キーが存在しない場合は既定値を返します。
        /// </summary>
        /// <param name="key">取得する値のキー</param>
        /// <param name="defaultValue">キーが存在しない場合に返す既定の文字列値</param>
        /// <returns>キーに対応する文字列の値。キーが見つからない場合は既定値</returns>
        /// <example>
        /// <code>
        /// string val = prop.GetValue("Encoding", "MS932");
        /// </code>
        /// </example>
        public string GetValue(string key, string defaultValue)
        {
            string value = defaultValue ?? "";
            if (!string.IsNullOrEmpty(key) && _prop.TryGetValue(key, out var propValue))
            {
                value = propValue;
            }

            if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            {
                value = "";
            }

            return value;
        }

        /// <summary>
        /// 指定されたキーに対応する値を整数として取得します。変換に失敗した場合は既定値を返します。
        /// </summary>
        /// <param name="key">取得する値のキー</param>
        /// <param name="defaultValue">変換に失敗した場合に返す既定の整数値</param>
        /// <returns>キーに対応する整数値。変換に失敗した場合は既定値</returns>
        /// <example>
        /// <code>
        /// int verbose = prop.GetValue("Verbose", 0);
        /// </code>
        /// </example>
        public int GetValue(string key, int defaultValue)
        {
            if (int.TryParse(GetValue(key, defaultValue.ToString()), out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 指定されたキーと値を用いてプロパティを設定します。
        /// </summary>
        /// <param name="key">設定対象のプロパティ名</param>
        /// <param name="value">設定するプロパティの値</param>
        /// <example>
        /// <code>
        /// prop.SetValue("Verbose", "1");
        /// </code>
        /// </example>
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                return;
            }

            string? matchedKey = _keyList.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            if (matchedKey is null)
            {
                return;
            }

            if (string.Equals(matchedKey, INCLUDES_REGEX, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(matchedKey, REGEX_RULE, StringComparison.OrdinalIgnoreCase))
            {
                _includeRegexes.Add(value);
            }
            else if (string.Equals(matchedKey, ORDER, StringComparison.OrdinalIgnoreCase))
            {
                _orderCsvs.Add(value);
            }
            else if (string.Equals(matchedKey, FORMAT, StringComparison.OrdinalIgnoreCase))
            {
                _formats.Add(value);
            }
            else if (string.Equals(matchedKey, EXCLUDES_REGEX, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(matchedKey, PRE_EXC_REGEX, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(matchedKey, POST_EXC_REGEX, StringComparison.OrdinalIgnoreCase))
            {
                _excludeRegexes.Add(value);
            }
            else
            {
                _prop[matchedKey] = value;
            }
        }

        /// <summary>
        /// デバッグ用に現在設定されているルール情報を標準出力に表示します。
        /// </summary>
        /// <example>
        /// <code>
        /// prop.ShowRulesForDebug();
        /// </code>
        /// </example>
        public void ShowRulesForDebug()
        {
            for (int i = 0; i < _includeRegexes.Count; i++)
            {
                Console.Out.WriteLine($"{INCLUDES_REGEX.PadLeft(_keyLength, ' ')} : {i} : {_includeRegexes[i]}");
            }
            for (int i = 0; i < _ordersList.Count; i++)
            {
                Console.Out.WriteLine($"{ORDER.PadLeft(_keyLength, ' ')} : {i} : {MdlUtil.Join(_ordersList[i], ",")}");
            }
            for (int i = 0; i < _formats.Count; i++)
            {
                Console.Out.WriteLine($"{FORMAT.PadLeft(_keyLength, ' ')} : {i} : {_formats[i]}");
            }
            for (int i = 0; i < _excludeRegexes.Count; i++)
            {
                Console.Out.WriteLine($"{EXCLUDES_REGEX.PadLeft(_keyLength, ' ')} : {i} : {_excludeRegexes[i]}");
            }
        }
    }
}

