package tool;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.Calendar;
import java.util.Collections;
import java.util.Comparator;
import java.util.Date;
import java.util.List;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertNotNull;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertTrue;

/**
 * LogReUniq の単体テストクラスです。
 */
public final class LogReUniqTest {

	/** テスト用の一時作業ディレクトリ */
	private Path tempDir;

	/**
	 * 各テストケースの事前準備処理です。
	 *
	 * @throws IOException ディレクトリ作成時に発生する例外
	 */
	@Before
	public void setUp() throws IOException {
		// 注意事項に準拠した作業ディレクトリの作成
		tempDir = Paths.get(System.getProperty("java.io.tmpdir"), "UnitTest", "LogReUniq", "LogReUniq");
		if (!Files.exists(tempDir)) {
			Files.createDirectories(tempDir);
		}
	}

	/**
	 * 各テストケースの事後クリーンアップ処理です。
	 *
	 * @throws IOException ディレクトリ削除時に発生する例外
	 */
	@After
	public void tearDown() throws IOException {
		// 作業ディレクトリ内のクリーンアップ
		if (Files.exists(tempDir)) {
			Files.walk(tempDir)
					.sorted(Comparator.reverseOrder())
					.map(Path::toFile)
					.forEach(File::delete);
		}
	}

	/**
	 * 作業ディレクトリの作成先および命名規約準拠を検証します。
	 */
	@Test
	public void testTempDirectoryCompliance() {
		assertTrue("作業ディレクトリが存在すること", Files.exists(tempDir));
		assertTrue("java.io.tmpdir 配下の作業ディレクトリであること", tempDir.startsWith(Paths.get(System.getProperty("java.io.tmpdir"))));
		assertTrue("パスにプロジェクト名とクラス名が含まれていること", tempDir.endsWith(Paths.get("LogReUniq", "LogReUniq")));
	}

	/**
	 * 文字列から整数への変換処理を検証します。
	 */
	@Test
	public void testParseInt() {
		LogReUniq tool = new LogReUniq();
		assertEquals(123, tool.parseInt("123", 0));
		assertEquals(0, tool.parseInt("0", 100));
		assertEquals(-456, tool.parseInt("-456", 0));
		assertEquals(999, tool.parseInt("invalid", 999));
		assertEquals(999, tool.parseInt("", 999));
		assertEquals(999, tool.parseInt(null, 999));
	}

	/**
	 * 数値判定メソッドの動作を検証します。
	 */
	@Test
	public void testIsNumeric() {
		LogReUniq tool = new LogReUniq();
		assertTrue(tool.isNumeric("123"));
		assertTrue(tool.isNumeric("0"));
		assertTrue(tool.isNumeric("-10"));
		assertFalse(tool.isNumeric("abc"));
		assertFalse(tool.isNumeric(""));
		assertFalse(tool.isNumeric(null));
	}

	/**
	 * CSV文字列からリストへの変換処理を検証します。
	 */
	@Test
	public void testCsvToList() {
		LogReUniq tool = new LogReUniq();
		List<String> list = tool.csvToList("apple,banana,orange", ",");
		assertEquals(3, list.size());
		assertEquals("apple", list.get(0));
		assertEquals("banana", list.get(1));
		assertEquals("orange", list.get(2));

		List<String> emptyList = tool.csvToList("", ",");
		assertTrue(emptyList.isEmpty());

		List<String> nullList = tool.csvToList(null, ",");
		assertTrue(nullList.isEmpty());

		List<String> blankItemList = tool.csvToList("a,,b,", ",");
		assertEquals(2, blankItemList.size());
		assertEquals("a", blankItemList.get(0));
		assertEquals("b", blankItemList.get(1));
	}

	/**
	 * リストおよび配列から区切り文字結合文字列の生成処理を検証します。
	 */
	@Test
	public void testJoinList() {
		LogReUniq tool = new LogReUniq();

		// List<String>
		assertEquals("a,b,c", tool.joinList(Arrays.asList("a", "b", "c"), ","));
		assertEquals("", tool.joinList(Collections.emptyList(), ","));
		assertEquals("", tool.joinList((List<String>) null, ","));

		// String[]
		assertEquals("x:y:z", tool.joinList(new String[]{"x", "y", "z"}, ":"));
		assertEquals("", tool.joinList(new String[]{}, ":"));
		assertEquals("", tool.joinList((String[]) null, ":"));

		// int[]
		assertEquals("1-2-3", tool.joinList(new int[]{1, 2, 3}, "-"));
		assertEquals("", tool.joinList(new int[]{}, "-"));
		assertEquals("", tool.joinList((int[]) null, "-"));
	}

	/**
	 * プロパティの取得および設定処理を検証します。
	 */
	@Test
	public void testGetAndSetValue() {
		LogReUniq tool = new LogReUniq();

		// デフォルト値
		assertEquals("defVal", tool.getValue("nonExistingKey", "defVal"));
		assertEquals(100, tool.getValue("nonExistingKey", 100));

		// 値の設定（大文字小文字無視キーマッピング）
		tool.setValue(LogReUniq.ENCODING, "UTF-8");
		assertEquals("UTF-8", tool.getValue(LogReUniq.ENCODING, "MS932"));

		tool.setValue("encoding", "MS932");
		assertEquals("MS932", tool.getValue(LogReUniq.ENCODING, "UTF-8"));

		tool.setValue(LogReUniq.VERBOSE, "3");
		assertEquals(3, tool.getValue(LogReUniq.VERBOSE, 0));

		// "null" 文字列が null として返却されることの検証
		tool.setValue(LogReUniq.SRC_PATH, "null");
		assertNull(tool.getValue(LogReUniq.SRC_PATH, "default"));
	}

	/**
	 * 基準日時指定子からの Date オブジェクト取得処理を検証します。
	 */
	@Test
	public void testGetDate() {
		LogReUniq tool = new LogReUniq();

		Date now = tool.getDate("");
		assertNotNull(now);

		Date today = tool.getDate("today");
		assertNotNull(today);
		Calendar calToday = Calendar.getInstance();
		calToday.setTime(today);
		assertEquals(0, calToday.get(Calendar.HOUR_OF_DAY));
		assertEquals(0, calToday.get(Calendar.MINUTE));
		assertEquals(0, calToday.get(Calendar.SECOND));

		Date yesterday = tool.getDate("yesterday");
		assertNotNull(yesterday);
		assertTrue(yesterday.before(new Date()));

		Date tomorrow = tool.getDate("tomorrow");
		assertNotNull(tomorrow);
		assertTrue(tomorrow.after(new Date()));

		Date fotm = tool.getDate("fotm");
		Calendar calFotm = Calendar.getInstance();
		calFotm.setTime(fotm);
		assertEquals(1, calFotm.get(Calendar.DAY_OF_MONTH));

		Date relDate = tool.getDate("-2");
		assertNotNull(relDate);
		assertTrue(relDate.before(new Date()));
	}

	/**
	 * 日付フォーマット処理を検証します。
	 */
	@Test
	public void testFormatDate() {
		LogReUniq tool = new LogReUniq();
		Calendar cal = Calendar.getInstance();
		cal.set(2026, Calendar.AUGUST, 16, 12, 34, 56);
		Date testDate = cal.getTime();

		assertEquals("2026/08/16 12:34:56", tool.formatDate(testDate, "yyyy/MM/dd HH:mm:ss"));
		assertEquals("2026-08-16", tool.formatDate(testDate, "yyyy-MM-dd"));
	}

	/**
	 * 日付書式指定子タグの置換処理を検証します。
	 */
	@Test
	public void testFormatDateTags() {
		LogReUniq tool = new LogReUniq();
		Calendar cal = Calendar.getInstance();
		cal.set(2026, Calendar.JANUARY, 1, 10, 20, 30);
		Date testDate = cal.getTime();

		String template = "log_%Y%m%d_%H%M%S.txt";
		String result = tool.formatDateTags(testDate, template);
		assertEquals("log_20260101_102030.txt", result);
	}

	/**
	 * ホスト名指定子タグの置換処理を検証します。
	 */
	@Test
	public void testFormatHostTags() {
		LogReUniq tool = new LogReUniq();
		String host = tool.getHostName();
		assertNotNull(host);
		assertFalse(host.isEmpty());

		String template = "log__COMPUTERNAME_.txt";
		String result = tool.formatHostTags(template);
		assertEquals("log_" + host + ".txt", result);
	}

	/**
	 * 設定ファイル読み込み処理を検証します。
	 *
	 * @throws IOException ファイル入出力エラーが発生した場合
	 */
	@Test
	public void testReadConfig() throws IOException {
		Path confFile = tempDir.resolve("test.conf");
		List<String> lines = Arrays.asList(
				"# コメント行",
				"SrcPath = " + tempDir.resolve("src").toString().replace("\\", "/"),
				"ToPath = " + tempDir.resolve("out.txt").toString().replace("\\", "/"),
				"Encoding = UTF-8",
				"Verbose = 2",
				"Include = ^(\\d+),(.*)$",
				"Order = 2,1"
		);
		Files.write(confFile, lines, StandardCharsets.UTF_8);

		LogReUniq tool = new LogReUniq();
		boolean success = tool.readConfig(confFile.toString());
		assertTrue("設定ファイルの読み込みが成功すること", success);
		assertEquals("UTF-8", tool.getValue(LogReUniq.ENCODING, ""));
		assertEquals(2, tool.getValue(LogReUniq.VERBOSE, 0));
	}

	/**
	 * ログファイルの抽出・集約およびファイル書き出し処理を検証します。
	 *
	 * @throws IOException ファイル入出力エラーが発生した場合
	 */
	@Test
	public void testGrepTextFileAndWriteFile() throws IOException {
		// テスト用ログファイルの作成
		Path logFile = tempDir.resolve("sample.log");
		List<String> logLines = Arrays.asList(
				"2026-08-16 10:00:00,INFO,Server started",
				"2026-08-16 10:01:00,DEBUG,Keepalive ping",
				"2026-08-16 10:02:00,ERROR,Connection timed out",
				"2026-08-16 10:03:00,INFO,Server stopped"
		);
		Files.write(logFile, logLines, StandardCharsets.UTF_8);

		Path outFile = tempDir.resolve("output.txt");

		// LogReUniq 実行
		String[] args = {
				"-d", logFile.toString(),
				"-o", outFile.toString(),
				"-i", "^([^,]+),([^,]+),([^,]+)$",
				"-O", "2,1,3",
				"-F", "[%s] %s - %s",
				"-x", "^.*DEBUG.*$",
				"-e", "UTF-8"
		};

		LogReUniq.doTask(args);

		assertTrue("出力ファイルが生成されていること", Files.exists(outFile));
		List<String> resultLines = Files.readAllLines(outFile, StandardCharsets.UTF_8);

		// DEBUG 行は除外され、ソートされて 3 行出力されること
		assertEquals(3, resultLines.size());
		assertEquals("[ERROR] 2026-08-16 10:02:00 - Connection timed out", resultLines.get(0));
		assertEquals("[INFO] 2026-08-16 10:00:00 - Server started", resultLines.get(1));
		assertEquals("[INFO] 2026-08-16 10:03:00 - Server stopped", resultLines.get(2));
	}

	/**
	 * ディレクトリ再帰走査によるログ抽出・集約処理を検証します。
	 *
	 * @throws IOException ファイル入出力エラーが発生した場合
	 */
	@Test
	public void testRecursiveDirectoryGrep() throws IOException {
		// ディレクトリ構造の作成: tempDir/logs/sub1, tempDir/logs/sub2
		Path logsDir = tempDir.resolve("logs");
		Path sub1 = logsDir.resolve("sub1");
		Path sub2 = logsDir.resolve("sub2");
		Files.createDirectories(sub1);
		Files.createDirectories(sub2);

		Files.write(sub1.resolve("access1.log"), Arrays.asList("192.168.1.1,GET,/index.html"), StandardCharsets.UTF_8);
		Files.write(sub2.resolve("access2.log"), Arrays.asList("192.168.1.2,POST,/login"), StandardCharsets.UTF_8);
		Files.write(sub2.resolve("ignore.txt"), Arrays.asList("192.168.1.3,GET,/ignore"), StandardCharsets.UTF_8);

		Path outFile = tempDir.resolve("combined.txt");

		String[] args = {
				"-d", logsDir.toString(),
				"-o", outFile.toString(),
				"-R",
				"--if", ".*\\.log",
				"-i", "^([^,]+),([^,]+),([^,]+)$",
				"-O", "1,2,3",
				"-e", "UTF-8"
		};

		LogReUniq.doTask(args);

		assertTrue("再帰走査結果の出力ファイルが生成されていること", Files.exists(outFile));
		List<String> resultLines = Files.readAllLines(outFile, StandardCharsets.UTF_8);
		assertEquals(2, resultLines.size());
		assertTrue(resultLines.contains("192.168.1.1,GET,/index.html"));
		assertTrue(resultLines.contains("192.168.1.2,POST,/login"));
	}

}
