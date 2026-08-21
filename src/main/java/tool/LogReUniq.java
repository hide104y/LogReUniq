package tool;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Calendar;
import java.util.Collections;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Scanner;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Collectors;

/**
 * 正規表現を用いてログファイルを検索・抽出・集約・整形するユーティリティクラスです。
 * <p>
 * ディレクトリ再帰検索や標準入力（パイプ）からのログ読み込みに対応し、
 * 正規表現にマッチした文字列の抽出・並べ替え・フォーマット整形・重複排除を行って出力します。
 * </p>
 */
public final class LogReUniq {

	/** OSがWindowsであるかどうかのフラグ */
	public static final boolean IS_WINDOWS = System.getProperty("os.name").toLowerCase().startsWith("win");
	/** デフォルトの冗長レベル */
	public static final int DEFAULT_VERBOSE = 0;
	/** デフォルトの文字エンコーディング */
	public static final String DEFAULT_ENCODING = IS_WINDOWS ? "MS932" : "UTF-8";
	/** デフォルトのソースパス */
	public static final String DEFAULT_SRC_PATH = "";
	/** デフォルトの設定ファイルパス */
	public static final String DEFAULT_CONF_PATH = "";
	/** デフォルトの正規表現ルール */
	public static final String DEFAULT_REGEX_RULE = "^([^,]+),([^,]+),([^,]+)$";
	/** デフォルトの抽出順序 */
	public static final String DEFAULT_ORDER = "1,2,3";
	/** プロパティキー: 冗長レベル */
	public static final String VERBOSE = "Verbose";
	/** プロパティキー: ソースパス */
	public static final String SRC_PATH = "SrcPath";
	/** プロパティキー: 出力ファイルパス */
	public static final String TO_PATH = "ToPath";
	/** プロパティキー: 設定ファイルパス */
	public static final String CONF_PATH = "ConfPath";
	/** プロパティキー: 対象ファイル名条件 */
	public static final String INC_FILES = "IncFiles";
	/** プロパティキー: 除外ファイル名条件 */
	public static final String EXC_FILES = "ExcFiles";
	/** プロパティキー: 対象ディレクトリ名条件 */
	public static final String INC_DIRS = "IncDirs";
	/** プロパティキー: 除外ディレクトリ名条件 */
	public static final String EXC_DIRS = "ExcDirs";
	/** プロパティキー: 文字エンコーディング */
	public static final String ENCODING = "Encoding";
	/** プロパティキー: 抽出順序 */
	public static final String ORDER = "Order";
	/** プロパティキー: 出力書式 */
	public static final String FORMAT = "Format";
	/** プロパティキー: 未マッチ行表示フラグ */
	public static final String SHOW_NOMATCH_LINE = "ShowNoMatchLine";
	/** プロパティキー: 大文字小文字無視フラグ */
	public static final String IS_CASE_INSENSITIVE = "IgnoreCase";
	/** プロパティキー: パイプ入力フラグ */
	public static final String IS_PIPE_IN = "PipeIn";
	/** プロパティキー: 再帰検索フラグ */
	public static final String IS_RECURSIVE = "Recursive";
	/** プロパティキー: 書式指定子変換フラグ */
	public static final String IS_FORMAT_CONV = "FormatSpecifierConv";
	/** プロパティキー: 基準日時指定子 */
	public static final String BASE_DATE_INDICATOR = "BaseTimeIndicator";
	/** プロパティキー: 抽出正規表現 */
	public static final String INCLUDES_REGEX = "Include";
	/** プロパティキー: 除外正規表現 */
	public static final String EXCLUDES_REGEX = "Exclude";
	/** プロパティキー: 正規表現ルール（エイリアス） */
	public static final String REGEX_RULE = "RegexRule";
	/** プロパティキー: 事前除外正規表現（エイリアス） */
	public static final String PRE_EXC_REGEX = "PreExcRegex";
	/** プロパティキー: 事後除外正規表現（エイリアス） */
	public static final String POST_EXC_REGEX = "PostExcRegex";

	private static final Pattern COMMENT_PATTERN = Pattern.compile("^\\s*#.*");
	private static final Pattern KEY_VAL_PATTERN = Pattern.compile("^\\s*([\\w_\\-]+)\\s*=\\s*(.+)\\s*$");
	private static final Pattern SPECIFIER_PATTERN = Pattern.compile("^m(\\d+)$");

	/** 終了時に System.exit を呼び出すかどうかのフラグ */
	private boolean isExit = false;
	/** プロパティ設定マップ */
	private final Map<String, String> props = new LinkedHashMap<>();
	/** 引数・設定キーのリスト */
	private final List<String> keyList = new ArrayList<>();
	/** 抽出・集約結果リスト */
	private final List<String> resultList = new ArrayList<>();
	/** 抽出正規表現リスト */
	private final List<String> incRegexList = new ArrayList<>();
	/** 除外正規表現リスト */
	private final List<String> excRegexList = new ArrayList<>();
	/** 抽出順序CSV文字列リスト */
	private final List<String> orderCsvList = new ArrayList<>();
	/** 出力書式文字列リスト */
	private final List<String> formatList = new ArrayList<>();
	/** 対象ファイル名パターンリスト */
	private List<String> incFileList = new ArrayList<>();
	/** 除外ファイル名パターンリスト */
	private List<String> excFileList = new ArrayList<>();
	/** 対象ディレクトリ名パターンリスト */
	private List<String> incDirList = new ArrayList<>();
	/** 除外ディレクトリ名パターンリスト */
	private List<String> excDirList = new ArrayList<>();
	/** 抽出順序インデックス配列リスト */
	private final List<int[]> ordersList = new ArrayList<>();
	/** 書式指定変換で使用する基準日時 */
	private Date baseDate = getDate("");

	/**
	 * アプリケーションのメインエントリポイントです。
	 * <p>
	 * コマンドライン引数を解析してログ集約処理を実行し、処理終了時に System.exit を呼び出します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq.main(new String[]{"-d", "/var/log", "-i", "^(\\S+) (\\S+)", "-O", "1,2"});
	 * }</pre>
	 * </p>
	 *
	 * @param args コマンドライン引数の配列
	 */
	public static void main(final String[] args) {
		new LogReUniq(args, true);
	}

	/**
	 * 外部呼び出し用のタスク実行メソッドです。
	 * <p>
	 * System.exit を呼び出さずにログ集約処理を実行します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq.doTask(new String[]{"-d", "C:/logs", "-i", "^(.*)$", "-o", "C:/out.txt"});
	 * }</pre>
	 * </p>
	 *
	 * @param args コマンドライン引数の配列
	 */
	public static void doTask(final String[] args) {
		new LogReUniq(args, false);
	}

	/**
	 * デフォルトコンストラクタです。
	 * <p>
	 * 各種ユーティリティメソッドの単体呼び出しや個別インスタンス生成時に使用します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * int val = tool.parseInt("123", 0);
	 * }</pre>
	 * </p>
	 */
	public LogReUniq() {
		initKeyList();
	}

	/**
	 * コマンドライン引数を指定してログ集約処理を実行するコンストラクタです。
	 * <p>
	 * 設定ファイルや引数を解析し、対象ログの走査・正規表現抽出・ソート・ファイル出力または標準出力を行います。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * String[] args = {"-d", "logs", "-i", "^(\\d+),(.*)$", "-O", "2,1"};
	 * LogReUniq app = new LogReUniq(args, false);
	 * }</pre>
	 * </p>
	 *
	 * @param args コマンドライン引数の配列
	 * @param isExit 処理完了時に System.exit を実行するかどうかのフラグ（true: 終了する, false: 終了しない）
	 */
	public LogReUniq(final String[] args, final boolean isExit) {
		boolean isOk = true;
		boolean isUsage = false;
		boolean showSampleConf = false;
		Map<String, String> argsMap = new LinkedHashMap<>();
		List<String> incRegexArgs = new ArrayList<>();
		List<String> excRegexArgs = new ArrayList<>();
		List<String> orderCsvArgs = new ArrayList<>();
		List<String> formatArgs = new ArrayList<>();
		this.isExit = isExit;

		initKeyList();

		int keyLength = keyList.stream().mapToInt(String::length).max().orElse(0);
		String keyFormat = "%" + keyLength + "s";

		for (int i = 0; i < args.length; ++i) {
			if ("-h".equals(args[i]) || "-?".equals(args[i]) || "--help".equals(args[i])) {
				isUsage = true;
			} else if ("--show-sample-config".equals(args[i]) || "-show-sample-config".equals(args[i])) {
				showSampleConf = true;
			} else if ("-v".equals(args[i])) {
				argsMap.put(VERBOSE, "1");
			} else if ("-vv".equals(args[i]) || "--vv".equals(args[i])) {
				argsMap.put(VERBOSE, "2");
				if (i + 1 < args.length && !args[i + 1].isEmpty()) {
					if (!args[i + 1].startsWith("-")) {
						if (0 < parseInt(args[i + 1], 0)) {
							argsMap.put(VERBOSE, args[++i]);
						}
					}
				}
			} else if ("-vvv".equals(args[i]) || "--vvv".equals(args[i])) {
				argsMap.put(VERBOSE, "3");
			} else if ("--specifier".equals(args[i])) {
				argsMap.put(IS_FORMAT_CONV, "true");
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					String specifierVal = args[++i];
					Matcher matcher = SPECIFIER_PATTERN.matcher(specifierVal);
					if (matcher.find()) {
						specifierVal = "-" + matcher.group(1);
					}
					argsMap.put(BASE_DATE_INDICATOR, specifierVal);
				}
			} else if ("-e".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(ENCODING, args[++i].toUpperCase());
				}
			} else if ("-c".equals(args[i]) || "--conf".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(CONF_PATH, args[++i]);
				}
			} else if ("-o".equals(args[i]) || "--out".equals(args[i]) || "--save".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(TO_PATH, args[++i]);
				}
			} else if ("-i".equals(args[i]) || "-r".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					incRegexArgs.add(args[++i]);
				}
			} else if ("-O".equals(args[i]) || "--order".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					orderCsvArgs.add(args[++i]);
				}
			} else if ("-F".equals(args[i]) || "--format".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					formatArgs.add(args[++i]);
				}
			} else if ("-x".equals(args[i]) || "--pre-exc".equals(args[i]) || "--post-exc".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					excRegexArgs.add(args[++i]);
				}
			} else if ("--if".equals(args[i]) || "--inc".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(INC_FILES, args[++i]);
				}
			} else if ("--xf".equals(args[i]) || "--exc".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(EXC_FILES, args[++i]);
				}
			} else if ("--id".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(INC_DIRS, args[++i]);
				}
			} else if ("--xd".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(EXC_DIRS, args[++i]);
				}
			} else if ("-P".equals(args[i]) || "--pipe".equals(args[i])) {
				argsMap.put(IS_PIPE_IN, "true");
			} else if ("-R".equals(args[i]) || "--recursive".equals(args[i])) {
				argsMap.put(IS_RECURSIVE, "true");
			} else if ("--show-nomatch".equals(args[i])) {
				argsMap.put(SHOW_NOMATCH_LINE, "true");
			} else if ("-g".equals(args[i]) || "--case-insensitive".equals(args[i]) || "--ic".equals(args[i]) || "--ignore-case".equals(args[i])) {
				argsMap.put(IS_CASE_INSENSITIVE, "true");
			} else if ("-d".equals(args[i]) || "--src".equals(args[i])) {
				if (i + 1 < args.length && !args[i + 1].startsWith("-")) {
					argsMap.put(SRC_PATH, args[++i]);
				}
			}
		}

		if (argsMap.containsKey(CONF_PATH)) {
			readConfig(argsMap.get(CONF_PATH));
		}

		if (!argsMap.isEmpty()) {
			props.putAll(argsMap);
			argsMap.clear();
		}

		boolean showNoMatch = "true".equalsIgnoreCase(getValue(SHOW_NOMATCH_LINE, "false"));

		if (!incRegexArgs.isEmpty()) {
			incRegexList.clear();
			incRegexList.addAll(incRegexArgs);
			incRegexArgs.clear();
		} else if (incRegexList.isEmpty()) {
			incRegexList.add(DEFAULT_REGEX_RULE);
		}

		if (!excRegexArgs.isEmpty()) {
			excRegexList.clear();
			excRegexList.addAll(excRegexArgs);
			excRegexArgs.clear();
		}

		if (!orderCsvArgs.isEmpty()) {
			orderCsvList.clear();
			orderCsvList.addAll(orderCsvArgs);
			orderCsvArgs.clear();
		} else if (orderCsvList.isEmpty()) {
			orderCsvList.add(DEFAULT_ORDER);
		}

		for (final String s : orderCsvList) {
			int[] orders = Arrays.stream(s.split(","))
					.mapToInt(val -> parseInt(val.trim(), 1))
					.toArray();
			ordersList.add(orders);
		}

		if (!formatArgs.isEmpty()) {
			formatList.clear();
			formatList.addAll(formatArgs);
			formatArgs.clear();
		}

		if (ordersList.size() != incRegexList.size()) {
			System.err.println("");
			System.err.println("ERROR : The numbers of regexrule and order are different");
			System.err.println("");
			debugRules(keyFormat);
			System.err.println("");
			if (isUsage) {
				showUsage(20);
			}
		}

		if (getValue(SRC_PATH, DEFAULT_SRC_PATH) == null || getValue(SRC_PATH, DEFAULT_SRC_PATH).isEmpty()) {
			if (!showSampleConf && !isUsage) {
				if (!"true".equalsIgnoreCase(getValue(IS_PIPE_IN, "false"))) {
					System.err.println("");
					System.err.println("ERROR : Argument is not specified : -d src path");
					showUsage(20);
				}
			}
		}

		if ("true".equalsIgnoreCase(getValue(IS_FORMAT_CONV, "false"))) {
			String baseDateSpec = getValue(BASE_DATE_INDICATOR, "now").toLowerCase();
			if (!"now".equals(baseDateSpec)) {
				baseDate = getDate(baseDateSpec);
			}
			String src = getValue(SRC_PATH, "");
			if (src != null && !src.isEmpty()) {
				props.put(SRC_PATH, formatHostTags(formatDateTags(getDate(baseDateSpec), src)));
			}
			String to = getValue(TO_PATH, "");
			if (to != null && !to.isEmpty()) {
				props.put(TO_PATH, formatHostTags(formatDateTags(getDate(baseDateSpec), to)));
			}
			String incFiles = getValue(INC_FILES, "");
			if (incFiles != null && !incFiles.isEmpty()) {
				props.put(INC_FILES, formatHostTags(formatDateTags(getDate(baseDateSpec), incFiles)));
			}
			String excFiles = getValue(EXC_FILES, "");
			if (excFiles != null && !excFiles.isEmpty()) {
				props.put(EXC_FILES, formatHostTags(formatDateTags(getDate(baseDateSpec), excFiles)));
			}
			String incDirs = getValue(INC_DIRS, "");
			if (incDirs != null && !incDirs.isEmpty()) {
				props.put(INC_DIRS, formatHostTags(formatDateTags(getDate(baseDateSpec), incDirs)));
			}
			String excDirs = getValue(EXC_DIRS, "");
			if (excDirs != null && !excDirs.isEmpty()) {
				props.put(EXC_DIRS, formatHostTags(formatDateTags(getDate(baseDateSpec), excDirs)));
			}
		}

		incFileList = csvToList(getValue(INC_FILES, ""), ",");
		excFileList = csvToList(getValue(EXC_FILES, ""), ",");
		incDirList = csvToList(getValue(INC_DIRS, ""), ",");
		excDirList = csvToList(getValue(EXC_DIRS, ""), ",");

		if (showSampleConf) {
			showSampleConf();
		}
		if (isUsage) {
			showUsage(0);
		}
		if (!isOk) {
			showUsage(20);
		}

		if (2 < getValue(VERBOSE, DEFAULT_VERBOSE)) {
			System.out.println("");
			System.out.println("############################################################");
			System.out.println("# PROPERTIES");
			System.out.println("############################################################");
			System.out.println(String.format(keyFormat, VERBOSE) + " : " + getValue(VERBOSE, DEFAULT_VERBOSE));
			if ("true".equalsIgnoreCase(getValue(IS_PIPE_IN, "false"))) {
				System.out.println(String.format(keyFormat, SRC_PATH) + " : PIPE");
			} else {
				System.out.println(String.format(keyFormat, SRC_PATH) + " : " + getValue(SRC_PATH, DEFAULT_SRC_PATH));
			}
			System.out.println(String.format(keyFormat, INC_FILES) + " : " + incFileList.size() + " : " + joinList(incFileList, ", "));
			System.out.println(String.format(keyFormat, EXC_FILES) + " : " + excFileList.size() + " : " + joinList(excFileList, ", "));
			debugRules(keyFormat);
		}

		System.out.println("");
		System.out.println("############################################################");
		System.out.println("# READ LOGS");
		System.out.println("############################################################");
		try {
			String srcPath = getValue(SRC_PATH, DEFAULT_SRC_PATH);
			int verbose = getValue(VERBOSE, 0);
			boolean isEffectiveDir = incDirList.isEmpty();
			if (!srcPath.isEmpty()) {
				Path checkPath = Paths.get(srcPath);
				if (Files.isDirectory(checkPath)) {
					boolean recursive = "true".equalsIgnoreCase(getValue(IS_RECURSIVE, "false"));
					if (!grepDir(srcPath, 0, verbose, recursive, isEffectiveDir, showNoMatch)) {
						isOk = false;
					}
				} else {
					File curFile = new File(srcPath);
					if (!grepTextFile(curFile, verbose, showNoMatch)) {
						isOk = false;
					}
				}
			} else {
				if (!grepPipe(verbose, showNoMatch)) {
					isOk = false;
				}
			}
		} catch (Exception exception) {
			isOk = false;
			exception.printStackTrace();
		}

		System.out.println("");
		System.out.println("############################################################");
		System.out.println("# SORT");
		System.out.println("############################################################");
		System.out.println("---> " + resultList.size() + " LINES");
		Collections.sort(resultList);

		System.out.println("");
		System.out.println("############################################################");
		System.out.println("# OUTPUT");
		System.out.println("############################################################");
		if (getValue(TO_PATH, "") == null || getValue(TO_PATH, "").isEmpty()) {
			for (final String line : resultList) {
				System.out.println(line);
			}
		} else {
			System.out.println("---> " + getValue(TO_PATH, ""));
			if (!writeFile()) {
				isOk = false;
			}
		}

		if (isOk) {
			terminate(0);
		} else {
			terminate(20);
		}
	}

	/**
	 * 指定されたパスの設定ファイルを読み込み、プロパティに設定します。
	 * <p>
	 * key=value 形式の行を解析して内部設定を更新します。# で始まる行はコメントとして無視されます。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * boolean success = tool.readConfig("conf/setting.conf");
	 * }</pre>
	 * </p>
	 *
	 * @param path 読み込む設定ファイルのパス
	 * @return 読み込みに成功した場合は true、失敗した場合は false
	 */
	public boolean readConfig(final String path) {
		initKeyList();
		boolean isOk = true;
		try (BufferedReader br = Files.newBufferedReader(Paths.get(path), StandardCharsets.UTF_8)) {
			String line;
			while ((line = br.readLine()) != null) {
				if (!COMMENT_PATTERN.matcher(line).matches()) {
					Matcher matcher = KEY_VAL_PATTERN.matcher(line);
					if (matcher.find()) {
						String key = matcher.group(1).trim();
						String val = matcher.group(2).trim();
						setValue(key, val);
					}
				}
			}
		} catch (IOException ex) {
			isOk = false;
			System.err.println("EXCEPTION : " + path + "：" + ex.getMessage());
		}
		return isOk;
	}

	/**
	 * 集約・抽出された結果リストを指定の出力ファイルへ書き出します。
	 * <p>
	 * 設定されたエンコーディングで出力先に各行を書き込みます。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * tool.setValue(LogReUniq.TO_PATH, "output.txt");
	 * boolean success = tool.writeFile();
	 * }</pre>
	 * </p>
	 *
	 * @return 書き込みに成功した場合は true、失敗した場合は false
	 */
	public boolean writeFile() {
		boolean isOk = true;
		Path outPath = Paths.get(getValue(TO_PATH, ""));
		Charset charset = Charset.forName(getValue(ENCODING, DEFAULT_ENCODING));
		try (BufferedWriter bw = Files.newBufferedWriter(outPath, charset)) {
			for (final String line : resultList) {
				bw.write(line);
				bw.newLine();
			}
		} catch (IOException ex) {
			isOk = false;
			System.err.println("EXCEPTION : " + getValue(TO_PATH, "") + "：" + ex.getMessage());
		}
		return isOk;
	}

	/**
	 * 指定された文字列が整数値として解釈可能かどうかを判定します。
	 * <p>
	 * Integer.parseInt でパース可能であれば true、そうでなければ false を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * boolean isNum = tool.isNumeric("123"); // true
	 * boolean isNotNum = tool.isNumeric("abc"); // false
	 * }</pre>
	 * </p>
	 *
	 * @param value 判定対象の文字列
	 * @return 数値に変換可能な場合は true、それ以外は false
	 */
	public boolean isNumeric(final String value) {
		boolean ret = true;
		try {
			Integer.parseInt(value);
		} catch (NumberFormatException ex) {
			// ignore
			ret = false;
		}
		return ret;
	}

	/**
	 * 文字列を整数値に変換します。変換に失敗した場合は指定のデフォルト値を返します。
	 * <p>
	 * null や数値以外の文字列が渡された場合でも例外をスローせず安全に処理します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * int port = tool.parseInt("8080", 80); // 8080
	 * int fallback = tool.parseInt("invalid", 80); // 80
	 * }</pre>
	 * </p>
	 *
	 * @param value 変換対象の文字列
	 * @param defaultValue 変換失敗時に返却するデフォルト整数値
	 * @return 変換後の整数値、失敗時は defaultValue
	 */
	public int parseInt(final String value, final int defaultValue) {
		int ret = defaultValue;
		try {
			ret = Integer.parseInt(value);
		} catch (NumberFormatException ex) {
			// ignore
			ret = defaultValue;
		}
		return ret;
	}

	/**
	 * プロパティマップから指定されたキーの値を取得します。
	 * <p>
	 * キーが存在しない場合やキーが空の場合はデフォルト値を返します。"null" 文字列が設定されている場合は null を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String enc = tool.getValue(LogReUniq.ENCODING, "UTF-8");
	 * }</pre>
	 * </p>
	 *
	 * @param key プロパティのキー名
	 * @param defaultValue キーが存在しない場合のデフォルト値
	 * @return 取得した文字列値
	 */
	public String getValue(final String key, final String defaultValue) {
		String value = defaultValue;
		if (key != null && !key.isEmpty() && props.containsKey(key)) {
			value = props.get(key);
		}
		if ("null".equalsIgnoreCase(value)) {
			value = null;
		}
		return value;
	}

	/**
	 * プロパティマップから指定されたキーの整数値を取得します。
	 * <p>
	 * キーが存在しない場合や整数値として解釈できない場合はデフォルト値を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * int verbose = tool.getValue(LogReUniq.VERBOSE, 0);
	 * }</pre>
	 * </p>
	 *
	 * @param key プロパティのキー名
	 * @param defaultValue キーが存在しないまたはパース失敗時のデフォルト整数値
	 * @return 取得した整数値
	 */
	public int getValue(final String key, final int defaultValue) {
		String valStr = getValue(key, String.valueOf(defaultValue));
		return parseInt(valStr, defaultValue);
	}

	/**
	 * プロパティや抽出・除外ルールにキーと値を設定します。
	 * <p>
	 * キー名は大文字小文字を区別せずに正規キーにマッピングされ、正規表現リストや抽出順序リスト等へ適切に格納されます。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * tool.setValue(LogReUniq.SRC_PATH, "C:/logs");
	 * tool.setValue("encoding", "UTF-8");
	 * }</pre>
	 * </p>
	 *
	 * @param key 設定するキー名
	 * @param value 設定する値
	 */
	public void setValue(final String key, final String value) {
		initKeyList();
		String keyName = "";
		boolean isHit = false;
		if (keyList.contains(key)) {
			keyName = key;
			isHit = true;
		} else {
			for (final String argKey : keyList) {
				if (argKey.equalsIgnoreCase(key)) {
					keyName = argKey;
					isHit = true;
					break;
				}
			}
		}
		if (isHit && !"".equals(value)) {
			if (keyName.equals(INCLUDES_REGEX)) {
				incRegexList.add(value);
			} else if (keyName.equals(ORDER)) {
				orderCsvList.add(value);
			} else if (keyName.equals(FORMAT)) {
				formatList.add(value);
			} else if (keyName.equals(EXCLUDES_REGEX)) {
				excRegexList.add(value);
			} else if (keyName.equals(REGEX_RULE)) {
				incRegexList.add(value);
			} else if (keyName.equals(PRE_EXC_REGEX)) {
				excRegexList.add(value);
			} else if (keyName.equals(POST_EXC_REGEX)) {
				excRegexList.add(value);
			} else {
				props.put(keyName, value);
			}
		}
	}

	/**
	 * 区切り文字で区切られた文字列を分割し、空要素を除去した文字列リストとして取得します。
	 * <p>
	 * 各トークンをトリムおよび空文字除外してリストを構築します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * List<String> list = tool.csvToList("apple,banana,orange", ",");
	 * }</pre>
	 * </p>
	 *
	 * @param csv 分割対象の文字列
	 * @param delimiter 区切り文字列
	 * @return 分割された文字列のリスト
	 */
	public List<String> csvToList(final String csv, final String delimiter) {
		if (csv == null || csv.isEmpty()) {
			return new ArrayList<>();
		}
		return Arrays.stream(csv.split(Pattern.quote(delimiter)))
				.filter(s -> s != null && !s.isEmpty())
				.collect(Collectors.toList());
	}

	/**
	 * 文字列リストの各要素を指定した区切り文字で結合した文字列を生成します。
	 * <p>
	 * 空リストまたは null の場合は空文字を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String joined = tool.joinList(Arrays.asList("a", "b", "c"), ", "); // "a, b, c"
	 * }</pre>
	 * </p>
	 *
	 * @param list 結合対象の文字列リスト
	 * @param delimiter 区切り文字列
	 * @return 結合された文字列
	 */
	public String joinList(final List<String> list, final String delimiter) {
		if (list == null || list.isEmpty()) {
			return "";
		}
		return String.join(delimiter, list);
	}

	/**
	 * 文字列配列の各要素を指定した区切り文字で結合した文字列を生成します。
	 * <p>
	 * 配列が null または長さ 0 の場合は空文字を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String joined = tool.joinList(new String[]{"1", "2", "3"}, "-"); // "1-2-3"
	 * }</pre>
	 * </p>
	 *
	 * @param list 結合対象の文字列配列
	 * @param delimiter 区切り文字列
	 * @return 結合された文字列
	 */
	public String joinList(final String[] list, final String delimiter) {
		if (list == null || list.length == 0) {
			return "";
		}
		return String.join(delimiter, list);
	}

	/**
	 * 整数配列の各要素を指定した区切り文字で結合した文字列を生成します。
	 * <p>
	 * 配列が null または長さ 0 の場合は空文字を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String joined = tool.joinList(new int[]{1, 2, 3}, ","); // "1,2,3"
	 * }</pre>
	 * </p>
	 *
	 * @param list 結合対象の整数配列
	 * @param delimiter 区切り文字列
	 * @return 結合された文字列
	 */
	public String joinList(final int[] list, final String delimiter) {
		if (list == null || list.length == 0) {
			return "";
		}
		return Arrays.stream(list)
				.mapToObj(String::valueOf)
				.collect(Collectors.joining(delimiter));
	}

	/**
	 * 基準日時指定子に基づいて対応する Date オブジェクトを取得します。
	 * <p>
	 * "today", "yesterday", "tomorrow", "FirstOfThisMonth", "EndOfLastMonth", 相対日数（正負の整数値）などの指定子を解釈します。
	 * 空文字列または null の場合は現在日時を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * Date today = tool.getDate("today");
	 * Date yesterday = tool.getDate("yesterday");
	 * Date threeDaysAgo = tool.getDate("-3");
	 * }</pre>
	 * </p>
	 *
	 * @param baseDateSpec 基準日時指定子文字列（"today", "yesterday", "fotm", "eolm", "-1" 等）
	 * @return 計算された {@link Date} オブジェクト
	 */
	public Date getDate(final String baseDateSpec) {
		Calendar cal = Calendar.getInstance();
		if (baseDateSpec == null || baseDateSpec.isEmpty()) {
			// return current time
		} else if ("today".equals(baseDateSpec) || "t".equals(baseDateSpec)) {
			cal.set(Calendar.HOUR_OF_DAY, 0);
			cal.set(Calendar.MINUTE, 0);
			cal.set(Calendar.SECOND, 0);
			cal.set(Calendar.MILLISECOND, 0);
		} else if ("yesterday".equalsIgnoreCase(baseDateSpec) || "lastday".equalsIgnoreCase(baseDateSpec)) {
			cal.add(Calendar.DAY_OF_MONTH, -1);
		} else if ("tomorrow".equalsIgnoreCase(baseDateSpec) || "nextday".equalsIgnoreCase(baseDateSpec)) {
			cal.add(Calendar.DAY_OF_MONTH, 1);
		} else if ("FirstOfThisMonth".equalsIgnoreCase(baseDateSpec) || "fotm".equalsIgnoreCase(baseDateSpec)) {
			cal.set(Calendar.DAY_OF_MONTH, 1);
		} else if ("EndOfLastMonth".equalsIgnoreCase(baseDateSpec) || "eolm".equalsIgnoreCase(baseDateSpec)) {
			cal.add(Calendar.MONTH, -1);
			cal.set(Calendar.DAY_OF_MONTH, cal.getActualMaximum(Calendar.DAY_OF_MONTH));
		} else if (isNumeric(baseDateSpec) && !"0".equals(baseDateSpec)) {
			cal.add(Calendar.DAY_OF_MONTH, parseInt(baseDateSpec, 0));
		}
		return cal.getTime();
	}

	/**
	 * OS のコマンドを実行し、その標準出力結果を文字列として取得します。
	 * <p>
	 * コマンド実行時の標準出力をすべて読み取り、文字列として返します。例外発生時はスタックトレースを出力し空文字を返します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String hostname = tool.execCmd("hostname");
	 * }</pre>
	 * </p>
	 *
	 * @param cmd 実行する OS コマンド文字列
	 * @return コマンドの標準出力結果文字列
	 */
	public String execCmd(final String cmd) {
		String retVal = "";
		try {
			Process process = Runtime.getRuntime().exec(cmd);
			try (Scanner s = new Scanner(process.getInputStream()).useDelimiter("\\A")) {
				retVal = s.hasNext() ? s.next() : "";
			}
			process.waitFor();
		} catch (IOException | InterruptedException e) {
			e.printStackTrace();
		}
		return retVal;
	}

	/**
	 * 実行環境のローカルホスト名を取得します。
	 * <p>
	 * "hostname" コマンドを実行して取得し、失敗した場合は InetAddress 経由で取得します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String host = tool.getHostName();
	 * }</pre>
	 * </p>
	 *
	 * @return ローカルホスト名文字列（ドメイン部分を除去したホスト名）
	 */
	public String getHostName() {
		String hostName = execCmd("hostname");
		if (null == hostName || hostName.isEmpty()) {
			try {
				hostName = InetAddress.getLocalHost().getHostName();
			} catch (UnknownHostException e) {
				// ignore
				hostName = "localhost";
			}
		}
		return hostName.trim().split("[\\s\\.]")[0];
	}

	/**
	 * 指定された Date オブジェクトを指定フォーマットの文字列に変換します。
	 * <p>
	 * SimpleDateFormat を用いて日時のフォーマット処理を行います。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String str = tool.formatDate(new Date(), "yyyy/MM/dd HH:mm:ss");
	 * }</pre>
	 * </p>
	 *
	 * @param date フォーマット対象の Date オブジェクト
	 * @param format SimpleDateFormat 形式の日付書式文字列
	 * @return フォーマット後の日付文字列
	 */
	public String formatDate(final Date date, final String format) {
		SimpleDateFormat sdf = new SimpleDateFormat(format);
		return sdf.format(date);
	}

	/**
	 * 文字列内に含まれる日付書式指定子（%Y, %m, %d, %H, %M, %S, %w）を指定日時で置換します。
	 * <p>
	 * 年 (%Y), 月 (%m), 日 (%d), 時 (%H), 分 (%M), 秒 (%S), 曜日番号 (%w: 日曜=0〜土曜=6) を置換します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String path = tool.formatDateTags(new Date(), "app_%Y%m%d.log");
	 * }</pre>
	 * </p>
	 *
	 * @param date 置換に使用する基準 Date オブジェクト
	 * @param target 書式指定子を含む置換対象文字列
	 * @return 日付書式指定子が置換された文字列
	 */
	public String formatDateTags(final Date date, final String target) {
		String result = target;
		String weekNoStr = formatDate(date, "u");
		result = result.replace("%Y", formatDate(date, "yyyy"));
		result = result.replace("%m", formatDate(date, "MM"));
		result = result.replace("%d", formatDate(date, "dd"));
		result = result.replace("%H", formatDate(date, "HH"));
		result = result.replace("%M", formatDate(date, "mm"));
		result = result.replace("%S", formatDate(date, "ss"));
		result = result.replace("%w", "7".equals(weekNoStr) ? "0" : weekNoStr);
		return result;
	}

	/**
	 * 文字列内に含まれるホスト名指定子（_COMPUTERNAME_）をローカルホスト名で置換します。
	 * <p>
	 * 実行端末のホスト名で文字列中の _COMPUTERNAME_ を置き換えます。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * LogReUniq tool = new LogReUniq();
	 * String path = tool.formatHostTags("log__COMPUTERNAME_.txt");
	 * }</pre>
	 * </p>
	 *
	 * @param target ホスト名指定子を含む置換対象文字列
	 * @return ホスト名が置換された文字列
	 */
	public String formatHostTags(final String target) {
		return target.replace("_COMPUTERNAME_", getHostName());
	}

	/**
	 * サポートする引数キーリストを初期化します。
	 */
	private void initKeyList() {
		if (keyList.isEmpty()) {
			keyList.add(VERBOSE);
			keyList.add(SRC_PATH);
			keyList.add(TO_PATH);
			keyList.add(CONF_PATH);
			keyList.add(INC_FILES);
			keyList.add(EXC_FILES);
			keyList.add(INC_DIRS);
			keyList.add(EXC_DIRS);
			keyList.add(ENCODING);
			keyList.add(INCLUDES_REGEX);
			keyList.add(EXCLUDES_REGEX);
			keyList.add(ORDER);
			keyList.add(FORMAT);
			keyList.add(SHOW_NOMATCH_LINE);
			keyList.add(IS_CASE_INSENSITIVE);
			keyList.add(IS_PIPE_IN);
			keyList.add(IS_RECURSIVE);
			keyList.add(IS_FORMAT_CONV);
			keyList.add(BASE_DATE_INDICATOR);
			keyList.add(REGEX_RULE);
			keyList.add(PRE_EXC_REGEX);
			keyList.add(POST_EXC_REGEX);
		}
	}

	/**
	 * アプリケーションの終了処理を実行します。
	 * <p>
	 * isExit フラグが true の場合、指定された終了コードで System.exit を呼び出します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * terminate(0); // 正常終了
	 * terminate(20); // 異常終了
	 * }</pre>
	 * </p>
	 *
	 * @param exitCode 終了ステータスコード（0: 正常, 20: 異常）
	 */
	private void terminate(final int exitCode) {
		if (isExit) {
			if (0 < getValue(VERBOSE, DEFAULT_VERBOSE)) {
				System.out.println("");
				System.out.println("EXIT CODE = " + exitCode);
			}
			System.exit(exitCode);
		}
	}

	/**
	 * コマンドラインヘルプ（Usage）を表示し、指定の終了コードで終了処理を行います。
	 * <p>
	 * 利用可能なコマンドラインオプションの一覧および現在設定されているデフォルト値を標準出力に表示します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * showUsage(0);
	 * }</pre>
	 * </p>
	 *
	 * @param exitCode 終了ステータスコード
	 */
	private void showUsage(final int exitCode) {
		System.out.println("");
		System.out.println("Usage:   java -jar LogReUniq [option...]");
		System.out.println("");
		System.out.println("options:");
		System.out.println("  -c config path ：設定ファイルパス          （現在値⇒" + getValue(CONF_PATH, DEFAULT_CONF_PATH) + "）");
		System.out.println("  -d|--src dir   ：ソースパス                （現在値⇒" + getValue(SRC_PATH, DEFAULT_SRC_PATH) + "）");
		System.out.println("  -o|--out path  ：出力ファイルパス          （現在値⇒" + getValue(TO_PATH, "") + "）");
		if (!incRegexList.isEmpty()) {
			for (final String s : incRegexList) {
				System.out.println("  -i|-r regex    ：集約正規表現              （現在値⇒" + s + "）");
			}
		} else {
			System.out.println("  -i|-r regex    ：集約正規表現              （現在値⇒" + joinList(incRegexList, ", ") + "）");
		}
		if (!orderCsvList.isEmpty()) {
			for (final String s : orderCsvList) {
				System.out.println("  -O|--order csv ：抽出順序                  （現在値⇒" + s + "）");
			}
		} else {
			System.out.println("  -O|--order csv ：抽出順序                  （現在値⇒" + joinList(orderCsvList, ", ") + "）");
		}
		if (!formatList.isEmpty()) {
			for (final String s : formatList) {
				System.out.println("  -F|--format str：書式                      （現在値⇒" + s + "）");
			}
		} else {
			System.out.println("  -F|--format str：書式                      （現在値⇒" + joinList(formatList, ", ") + "）");
		}
		if (!excRegexList.isEmpty()) {
			for (final String s : excRegexList) {
				System.out.println("  -x regex       ：除外判定正規表現          （現在値⇒" + s + "）");
			}
		} else {
			System.out.println("  -x regex       ：除外判定正規表現          （現在値⇒" + joinList(excRegexList, ", ") + "）");
		}
		System.out.println("Advanced options：");
		System.out.println("  -R|--recursive ：再帰処理フラグ            （現在値⇒" + getValue(IS_RECURSIVE, "false") + "）");
		System.out.println("  -e enc         ：文字コード                （現在値⇒" + getValue(ENCODING, DEFAULT_ENCODING) + "）");
		System.out.println("  -g|--ic        ：大文字小文字非区別フラグ  （現在値⇒" + getValue(IS_CASE_INSENSITIVE, "false") + "）");
		System.out.println("  -P|--pipe      ：パイプ入力フラグ          （現在値⇒" + getValue(IS_PIPE_IN, "false") + "）");
		System.out.println("File Filter options：");
		System.out.println("  --id csv       ：サブフォルダ名絞込        （現在値⇒" + getValue(INC_DIRS, "") + "）");
		System.out.println("  --xd csv       ：サブフォルダ名除外        （現在値⇒" + getValue(EXC_DIRS, "") + "）");
		System.out.println("  --if csv       ：ファイル名絞込            （現在値⇒" + getValue(INC_FILES, "") + "）");
		System.out.println("  --xf csv       ：ファイル名除外            （現在値⇒" + getValue(EXC_FILES, "") + "）");
		System.out.println("Format specifier conversion options:");
		System.out.println("  --specifier [日時]  ：書式指定変換フラグ   （現在値⇒" + getValue(IS_FORMAT_CONV, "false") + " : " + getValue(BASE_DATE_INDICATOR, "now") + " ⇒ " + formatDate(baseDate, "yyyy/MM/dd HH:mm:ss") + "）");
		System.out.println("    ※書式指定子：%Y、%m、%d、%H、%M、%S、%w、_COMPUTERNAME_");
		System.out.println("    ※基準日時  ：now|today|yesterday|nextday|FirstOfThisMonth|EndOfLastMonth|fotm|eolm");
		System.out.println("Debug options：");
		System.out.println("  --show-nomatch ：未マッチ行の表示フラグ    （現在値⇒" + getValue(SHOW_NOMATCH_LINE, "false") + "）");
		System.out.println("");
		System.out.println("Help options:");
		System.out.println("  -h                  ：SHOW THIS HELP MESSAGE");
		System.out.println("  --show-sample-config：SHOW SAMPLE CONFIG");
		System.out.println("");
		System.out.println("exit code:                 正常=0 / 異常=20");
		System.out.println("");
		terminate(exitCode);
	}

	/**
	 * 設定ファイルのサンプル定義を標準出力に表示します。
	 * <p>
	 * 各種パラメータ（パス設定、集約ルール、除外条件、書式指定子等）の設定例を表示し、正常終了します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * showSampleConf();
	 * }</pre>
	 * </p>
	 */
	private void showSampleConf() {
		System.out.println("################################################################################");
		System.out.println("# パス設定");
		System.out.println("################################################################################");
		System.out.println("# ソースパス：ディレクトリ（非再帰）|ファイル");
		System.out.println("# ---> 引数：-d|--src path");
		System.out.println("" + SRC_PATH + " = " + getValue(SRC_PATH, "/var/log/xxx"));
		System.out.println("# 再帰処理フラグ");
		System.out.println("# ---> 引数：-R|--recursive");
		System.out.println("" + IS_RECURSIVE + " = " + getValue(IS_RECURSIVE, "false"));
		System.out.println("# パイプ入力フラグ");
		System.out.println("# ---> 引数：-P|--pipe");
		System.out.println("" + IS_PIPE_IN + " = " + getValue(IS_PIPE_IN, "false"));
		System.out.println("# 出力ファイル");
		System.out.println("# ---> 引数：--out file path");
		System.out.println("" + TO_PATH + " = " + getValue(TO_PATH, "/temp/output.csv"));
		System.out.println("# 文字コード");
		System.out.println("# ---> 引数：-e  MS932|UTF-8");
		System.out.println("" + ENCODING + " = " + getValue(ENCODING, DEFAULT_ENCODING));
		System.out.println("################################################################################");
		System.out.println("# 集約ルール");
		System.out.println("################################################################################");
		System.out.println("# 正規表現（複数指定可能）");
		System.out.println("# ---> 引数：-i regex");
		if (!incRegexList.isEmpty()) {
			for (final String s : incRegexList) {
				System.out.println("" + INCLUDES_REGEX + " = " + s);
			}
		} else {
			System.out.println("" + INCLUDES_REGEX + " = ^.*\\(SERVICE_NAME=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*$");
			System.out.println("" + INCLUDES_REGEX + " = ^.*\\(HOST=([^\\)]+)\\).*\\(SERVICE_NAME=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*$");
			System.out.println("" + INCLUDES_REGEX + " = ^.*\\(HOST=([^\\)]+)\\).*\\(HOST=([^\\)]+)\\).*\\(SERVICE_NAME=([^\\)]+)\\).*$");
		}
		System.out.println("# 上述正規表現で指定したキャプチャグループの抽出順序（複数指定可能）");
		System.out.println("# ---> 引数：-O|--order csv");
		if (!orderCsvList.isEmpty()) {
			for (final String s : orderCsvList) {
				System.out.println("" + ORDER + " = " + s);
			}
		} else {
			System.out.println("" + ORDER + " = 2,3,1");
			System.out.println("" + ORDER + " = 3,2,1");
			System.out.println("" + ORDER + " = 3,1,2");
		}
		System.out.println("# 上述正規表現で指定したキャプチャグループの出力書式（複数指定可能）");
		System.out.println("# ---> 引数：-F|--format format");
		if (!formatList.isEmpty()) {
			for (final String s : formatList) {
				System.out.println("" + FORMAT + " = " + s);
			}
		} else {
			System.out.println("" + FORMAT + " = {\"host\": \"%s\", \"ip\": \"%s\", \"service_name\": \"%s\"}");
			System.out.println("" + FORMAT + " = {\"host\": \"%s\", \"ip\": \"%s\", \"service_name\": \"%s\"}");
			System.out.println("" + FORMAT + " = {\"host\": \"%s\", \"ip\": \"%s\", \"service_name\": \"%s\"}");
		}
		System.out.println("# 上述正規表現にマッチしなかった行の表示フラグ");
		System.out.println("# ---> 引数：--show-nomatch");
		System.out.println("" + SHOW_NOMATCH_LINE + " = " + getValue(SHOW_NOMATCH_LINE, "false"));
		System.out.println("# 除外判定正規表現（複数指定可能）");
		System.out.println("# ---> 引数：-x regex");
		if (!excRegexList.isEmpty()) {
			for (final String s : excRegexList) {
				System.out.println("" + EXCLUDES_REGEX + " = " + s);
			}
		} else {
			System.out.println("#" + EXCLUDES_REGEX + " = ^.*\\(USER=grid\\).*$");
			System.out.println("#" + EXCLUDES_REGEX + " = ^.*\\(COMMAND=status\\).*$");
		}
		System.out.println("# 大文字小文字非区別フラグ");
		System.out.println("# ---> 引数：--ic");
		System.out.println("" + IS_CASE_INSENSITIVE + " = " + getValue(IS_CASE_INSENSITIVE, "false"));
		System.out.println("################################################################################");
		System.out.println("# ファイル名の絞込／除外");
		System.out.println("################################################################################");
		System.out.println("# 絞込");
		System.out.println("# ---> 引数：--if csv");
		System.out.println("" + INC_FILES + " = " + getValue(INC_FILES, ".*\\.log,.*\\.txt"));
		System.out.println("# 除外");
		System.out.println("# ---> 引数：--xf csv");
		System.out.println("" + EXC_FILES + " = " + getValue(EXC_FILES, "^access_aaa,^access_bbb"));
		System.out.println("################################################################################");
		System.out.println("# サブフォルダ名の絞込／除外（再帰処理フラグがONの場合）");
		System.out.println("################################################################################");
		System.out.println("# 絞込");
		System.out.println("# ---> 引数：--id csv");
		System.out.println("" + INC_DIRS + " = " + getValue(INC_DIRS, "^SUB-A$,.*SUB-B.*$"));
		System.out.println("# 除外");
		System.out.println("# ---> 引数：--xd csv");
		System.out.println("" + EXC_DIRS + " = " + getValue(EXC_DIRS, "^SUB-C$,.*SUB-D.*$"));
		System.out.println("################################################################################");
		System.out.println("# 書式指定変換");
		System.out.println("# ※書式指定子：%Y、%m、%d、%H、%M、%S、%w、%pid、_COMPUTERNAME_");
		System.out.println("# ※基準日時  ：now|today|yesterday|nextday|FirstOfThisMonth|EndOfLastMonth|fotm|eolm");
		System.out.println("################################################################################");
		System.out.println("# 書式指定変換フラグ");
		System.out.println("# ---> 引数：--specifier [日時]");
		System.out.println("" + IS_FORMAT_CONV + " = " + getValue(IS_FORMAT_CONV, "false"));
		System.out.println("" + BASE_DATE_INDICATOR + " = " + getValue(BASE_DATE_INDICATOR, "now"));
		System.out.println("################################################################################");
		System.out.println("# その他");
		System.out.println("################################################################################");
		System.out.println("# 冗長レベル");
		System.out.println("# ---> 引数：-v|-vv|-vvv|-vv num");
		System.out.println("" + VERBOSE + " = " + getValue(VERBOSE, 0));
		System.out.println("################################################################################");
		terminate(0);
	}

	/**
	 * 設定されている抽出正規表現、抽出順序、書式、除外正規表現のデバッグ情報を標準出力に表示します。
	 * <p>
	 * 各ルールのインデックスと内容を整形して出力します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * debugRules("%20s");
	 * }</pre>
	 * </p>
	 *
	 * @param keyFormat キー名の表示用フォーマット文字列（例: "%20s"）
	 */
	private void debugRules(final String keyFormat) {
		for (int i = 0; i < incRegexList.size(); i++) {
			System.out.println(String.format(keyFormat, INCLUDES_REGEX) + " : " + i + " : " + incRegexList.get(i));
		}
		for (int i = 0; i < ordersList.size(); i++) {
			System.out.println(String.format(keyFormat, ORDER) + " : " + i + " : " + joinList(ordersList.get(i), ","));
		}
		for (int i = 0; i < formatList.size(); i++) {
			System.out.println(String.format(keyFormat, FORMAT) + " : " + i + " : " + formatList.get(i));
		}
		for (int i = 0; i < excRegexList.size(); i++) {
			System.out.println(String.format(keyFormat, EXCLUDES_REGEX) + " : " + i + " : " + excRegexList.get(i));
		}
	}

	/**
	 * 指定されたディレクトリ配下のファイルを再帰的または非再帰的に走査し、ログ行の抽出・除外処理を実行します。
	 * <p>
	 * ファイル名・サブディレクトリ名の絞込および除外条件に従って走査対象をフィルタリングします。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * boolean ok = grepDir("logs", 0, 1, true, true, false);
	 * }</pre>
	 * </p>
	 *
	 * @param srcPath 走査対象ディレクトリのパス
	 * @param hierarchy 探索の階層レベル（ルート階層は 0）
	 * @param verbose 詳細ログ出力レベル
	 * @param isRecursive サブディレクトリを再帰的に走査するかどうか
	 * @param isEffectiveDir 対象ディレクトリが有効と判定されているかどうか
	 * @param showNoMatch マッチしなかった行を表示するかどうか
	 * @return 走査・処理がすべて正常に完了した場合は true、エラーが発生した場合は false
	 */
	private boolean grepDir(final String srcPath, final long hierarchy, final int verbose, final boolean isRecursive, final boolean isEffectiveDir, final boolean showNoMatch) {
		File srcFile = new File(srcPath);
		File[] files = srcFile.listFiles();
		if (files == null) {
			return true;
		}
		boolean isOk = true;
		boolean isNoticed = false;
		boolean effectiveDirFlag = isEffectiveDir;
		for (final File curFile : files) {
			try {
				boolean hitFile = true;
				if (curFile.isFile()) {
					if (isRecursive && hierarchy == 0 && !excDirList.isEmpty() && excDirList.contains("^\\.$")) {
						if (!isNoticed && 2 < verbose) {
							System.out.println("HIT : " + EXC_DIRS + " : .");
						}
						isNoticed = true;
					} else if (isRecursive && 0 < hierarchy && !effectiveDirFlag) {
						continue;
					} else {
						if (!incFileList.isEmpty()) {
							hitFile = false;
							for (final String incFile : incFileList) {
								if (curFile.getName().matches(incFile)) {
									hitFile = true;
									break;
								}
							}
							if (!hitFile) {
								if (2 < verbose) {
									System.out.println("NO HIT : " + INC_FILES + " : " + curFile.getAbsolutePath());
								}
								continue;
							}
						}
						if (!excFileList.isEmpty()) {
							for (final String excFile : excFileList) {
								if (curFile.getName().matches(excFile)) {
									hitFile = false;
									break;
								}
							}
							if (!hitFile) {
								if (2 < verbose) {
									System.out.println("HIT : " + EXC_FILES + " : " + curFile.getAbsolutePath());
								}
								continue;
							}
						}
						if (hitFile) {
							if (!grepTextFile(curFile, verbose, showNoMatch)) {
								isOk = false;
							}
						}
					}
				} else {
					if (isRecursive && curFile.isDirectory()) {
						boolean hitExcDir = false;
						if (!effectiveDirFlag && !incDirList.isEmpty()) {
							for (final String incDir : incDirList) {
								if (curFile.getName().matches(incDir)) {
									effectiveDirFlag = true;
									break;
								}
							}
						}
						if (!excDirList.isEmpty()) {
							for (final String excDir : excDirList) {
								if (curFile.getName().matches(excDir)) {
									hitExcDir = true;
									break;
								}
							}
							if (hitExcDir) {
								if (2 < verbose) {
									System.out.println("HIT : " + EXC_DIRS + " : " + curFile.getAbsolutePath());
								}
								continue;
							}
						}
						if (!hitExcDir) {
							if (!grepDir(curFile.getAbsolutePath(), hierarchy + 1, verbose, isRecursive, effectiveDirFlag, showNoMatch)) {
								isOk = false;
							}
						}
					}
				}
			} catch (Exception ex) {
				isOk = false;
				System.err.println("EXCEPTION : " + srcPath + "：" + ex.getMessage());
			}
		}
		return isOk;
	}

	/**
	 * 単一のテキストファイルを読み込み、行ごとの抽出・除外処理を実行します。
	 * <p>
	 * 設定されたエンコーディングでファイルを 1 行ずつ読み込み、grepLine を呼び出します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * boolean ok = grepTextFile(new File("app.log"), 1, false);
	 * }</pre>
	 * </p>
	 *
	 * @param file 読み込み対象のファイルオブジェクト
	 * @param verbose 詳細ログ出力レベル
	 * @param showNoMatch マッチしなかった行を表示するかどうか
	 * @return 正常に読み込み完了した場合は true、例外が発生した場合は false
	 */
	private boolean grepTextFile(final File file, final int verbose, final boolean showNoMatch) {
		boolean isOk = true;
		try {
			System.out.println("---> " + file.getAbsolutePath());
			Charset charset = Charset.forName(getValue(ENCODING, DEFAULT_ENCODING));
			try (BufferedReader br = Files.newBufferedReader(file.toPath(), charset)) {
				String line;
				while ((line = br.readLine()) != null) {
					grepLine(line, verbose, showNoMatch);
				}
			}
		} catch (IOException ex) {
			isOk = false;
			System.err.println("EXCEPTION : " + file.getAbsolutePath() + "：" + ex.getMessage());
		}
		return isOk;
	}

	/**
	 * 標準入力（パイプストリーム）から行を読み込み、抽出・除外処理を実行します。
	 * <p>
	 * パイプ入力されたログデータを 1 行ずつ読み込み、grepLine を呼び出します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * boolean ok = grepPipe(1, false);
	 * }</pre>
	 * </p>
	 *
	 * @param verbose 詳細ログ出力レベル
	 * @param showNoMatch マッチしなかった行を表示するかどうか
	 * @return 正常に処理完了した場合は true、例外が発生した場合は false
	 */
	private boolean grepPipe(final int verbose, final boolean showNoMatch) {
		boolean isOk = true;
		try {
			System.out.println("---> PIPE");
			Charset charset = Charset.forName(getValue(ENCODING, DEFAULT_ENCODING));
			try (BufferedReader br = new BufferedReader(new InputStreamReader(System.in, charset))) {
				String line;
				while ((line = br.readLine()) != null) {
					grepLine(line, verbose, showNoMatch);
				}
			}
		} catch (IOException ex) {
			isOk = false;
			System.err.println("EXCEPTION : " + ex.getMessage());
		}
		return isOk;
	}

	/**
	 * 1 行の文字列に対して抽出正規表現および除外正規表現のマッチングを行い、結果リストに追加します。
	 * <p>
	 * 抽出条件にマッチし、かつ除外条件にマッチしなかった行について、指定されたグループ順序およびフォーマットに従って
	 * 重複を排除しながら結果リストに登録します。
	 * </p>
	 *
	 * <p>使用例:
	 * <pre>{@code
	 * grepLine("2026-08-16 12:00:00 INFO [Server] Started", 1, false);
	 * }</pre>
	 * </p>
	 *
	 * @param line 判定対象のログ行文字列
	 * @param verbose 詳細ログ出力レベル
	 * @param showNoMatch 抽出・除外のいずれにもマッチしなかった行を表示するかどうか
	 */
	private void grepLine(final String line, final int verbose, final boolean showNoMatch) {
		boolean isIncMatch = false;
		boolean isExcMatch = false;
		boolean ignoreCase = "true".equalsIgnoreCase(getValue(IS_CASE_INSENSITIVE, "false"));

		for (int i = 0; i < incRegexList.size(); i++) {
			Pattern incPattern = ignoreCase
					? Pattern.compile(incRegexList.get(i), Pattern.CASE_INSENSITIVE)
					: Pattern.compile(incRegexList.get(i));
			Matcher incMatcher = incPattern.matcher(line);
			if (incMatcher.find()) {
				isIncMatch = true;
				for (int x = 0; x < excRegexList.size(); x++) {
					Pattern excPattern = ignoreCase
							? Pattern.compile(excRegexList.get(x), Pattern.CASE_INSENSITIVE)
							: Pattern.compile(excRegexList.get(x));
					Matcher excMatcher = excPattern.matcher(line);
					if (excMatcher.find()) {
						isExcMatch = true;
						if (5 < verbose) {
							System.out.println("EXC-MATCH : " + x + " : " + line);
						}
						break;
					}
				}
				if (!isExcMatch) {
					int[] orders = ordersList.get(i);
					List<String> tempList = Arrays.stream(orders)
							.mapToObj(incMatcher::group)
							.collect(Collectors.toList());
					String lineBuf = String.join(",", tempList);
					if (i < formatList.size()) {
						lineBuf = String.format(formatList.get(i), (Object[]) tempList.toArray());
					}
					if (!resultList.contains(lineBuf)) {
						resultList.add(lineBuf);
					}
					if (4 < verbose) {
						System.out.println("INC-MATCH : " + i + " : " + joinList(orders, ",") + " -> " + lineBuf);
					}
				}
				break;
			}
		}
		if (!isIncMatch && !isExcMatch && showNoMatch) {
			System.out.println("NO-MATCH : " + line);
		}
	}

}
