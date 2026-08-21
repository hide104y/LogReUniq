using System;
using System.IO;
using LogReUniq.Class;
using Xunit;

namespace TestProject1
{
    public class UnitTest_ClsProp
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), @"UnitTest", "LogReUniq", "ClsProp");

        public UnitTest_ClsProp()
        {
            if (!Directory.Exists(_tempDir))
            {
                Directory.CreateDirectory(_tempDir);
            }
        }

        [Fact]
        public void Test_Constructor_And_Initialize()
        {
            var prop = new ClsProp();

            Assert.NotNull(prop.KeyList);
            Assert.NotEmpty(prop.KeyList);
            Assert.True(prop.KeyLength > 0);
            Assert.Equal(ClsProp.DEFAULT_VERBOSE, prop.GetValue(ClsProp.VERBOSE, 0));
        }

        [Fact]
        public void Test_GetValue_String()
        {
            var prop = new ClsProp();

            // 存在しないキー -> 既定値
            Assert.Equal("defaultVal", prop.GetValue("NonExistingKey", "defaultVal"));

            // 存在するキー設定
            prop.SetValue(ClsProp.SRC_PATH, @"C:\TestPath");
            Assert.Equal(@"C:\TestPath", prop.GetValue(ClsProp.SRC_PATH, ""));

            // "null" 文字列 -> 空文字
            prop.Properties["NullTest"] = "null";
            prop.KeyList.Add("NullTest");
            Assert.Equal("", prop.GetValue("NullTest", "defaultVal"));
        }

        [Fact]
        public void Test_GetValue_Int()
        {
            var prop = new ClsProp();

            // 既定値
            Assert.Equal(99, prop.GetValue("NonExistingKey", 99));

            // 設定値取得
            prop.SetValue(ClsProp.VERBOSE, "3");
            Assert.Equal(3, prop.GetValue(ClsProp.VERBOSE, 0));

            // 変換失敗時 -> 既定値
            prop.Properties[ClsProp.VERBOSE] = "invalid_int";
            Assert.Equal(10, prop.GetValue(ClsProp.VERBOSE, 10));
        }

        [Fact]
        public void Test_SetValue_SpecialKeys()
        {
            var prop = new ClsProp();

            // Include
            prop.SetValue(ClsProp.INCLUDES_REGEX, "pattern1");
            Assert.Single(prop.IncludeRegexes);
            Assert.Equal("pattern1", prop.IncludeRegexes[0]);

            // Exclude
            prop.SetValue(ClsProp.EXCLUDES_REGEX, "pattern2");
            Assert.Single(prop.ExcludeRegexes);
            Assert.Equal("pattern2", prop.ExcludeRegexes[0]);

            // Order
            prop.SetValue(ClsProp.ORDER, "1,2");

            // Format
            prop.SetValue(ClsProp.FORMAT, "{0} - {1}");
            Assert.Single(prop.Formats);
            Assert.Equal("{0} - {1}", prop.Formats[0]);
        }

        [Fact]
        public void Test_ShowRulesForDebug()
        {
            var prop = new ClsProp();
            prop.SetValue(ClsProp.INCLUDES_REGEX, "test_inc");
            prop.SetValue(ClsProp.EXCLUDES_REGEX, "test_exc");
            prop.SetValue(ClsProp.FORMAT, "test_fmt");

            // 例外が発生しないことを検証
            var exception = Record.Exception(() => prop.ShowRulesForDebug());
            Assert.Null(exception);
        }

        [Fact]
        public void Test_TempDir_Rule_Compliance()
        {
            // 一時ディレクトリの作成と権限確認
            Assert.True(Directory.Exists(_tempDir));

            string testFilePath = Path.Combine(_tempDir, "test.txt");
            File.WriteAllText(testFilePath, "unit test temp file");

            Assert.True(File.Exists(testFilePath));

            // 後処理
            File.Delete(testFilePath);
        }
    }
}
