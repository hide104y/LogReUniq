# LogReUniq

## 事前作業
1. JDKがインストールされていない場合はインストール：winget install -e --id Amazon.Corretto.8.JDK
1. Github CLIがインストールされていない場合はインストール：winget install -e --id GitHub.cli
1. Powershellプロンプトを開く

## リポジトリ作成（未作成の場合）
```shell
# サインイン状態の確認
gh auth status
# 初回サインインしていない場合はサインイン
gh auth login
# 削除権限付与
gh auth refresh -h github.com -s delete_repo
# 作成
gh repo create LogReUniq --private
# 確認
gh repo list | Select-String LogReUniq
```

## リモートリポジトリ（mainブランチ）の取得
```shell
# CD
cd D:\Github\workspace.jre8
# フォルダが存在する場合は削除
if (-Not (Test-Path -Path .\LogReUniq)){rmdir .\LogReUniq}
# クローン実行
git clone https://github.com/hide104y/LogReUniq.git
```

## リモートリポジトリ（mainブランチ）にREADME.mdが存在しない場合
```shell
# CD
cd D:\Github\workspace.jre8\LogReUniq
# ファイル作成
ruby -e "File.write('README.md', '# LogReUniq', encoding: 'UTF-8')"
# コミット
git add README.md
git commit -m "add README.md"
# プッシュ
git push -u origin main
# ブランチの一覧表示
git branch -a
```

## ブランチの作成
```shell
# ブランチをmainに切り替え・復元
git checkout main
# ブランチ作成
git checkout -b java08
# 作成したブランチをリモートにプッシュ
git push -u origin java08
```

## Java、Mavenの切り替え
```shell
# PATHの設定
$Env:JAVA_HOME="${Env:USERPROFILE}\App\Java\jdk1.8.0_472"
$Env:MAVEN_HOME="${Env:USERPROFILE}\App\Maven\apache-maven-3.9.11"
$Env:PATH="${Env:JAVA_HOME}\bin;${Env:MAVEN_HOME}\bin;${Env:PATH}"
# 確認
java -version
mvn -version
```

## MAVENプロジェクトの作成
```shell
mvn archetype:generate `
-DarchetypeArtifactId=maven-archetype-quickstart `
-DinteractiveMode=false `
-DgroupId=tool `
-DartifactId=LogReUniq
```

## 手動ビルドが必要な依存ライブラリー
- 次をMAVENローカルリポジトリに「mvn clean install」して下さい
  - なし

## 依存ライブラリー一覧
```shell
PS D:\Github\workspace.jre8\LogReUniq> mvn dependency:tree
Picked up JAVA_TOOL_OPTIONS: -Dfile.encoding=UTF-8 -Dsun.stdout.encoding=UTF-8 -Dsun.stderr.encoding=UTF-8
[INFO] Scanning for projects...
[INFO]
[INFO] ---------------------------< tool:LogReUniq >---------------------------
[INFO] Building LogReUniq 1.0
[INFO]   from pom.xml
[INFO] --------------------------------[ jar ]---------------------------------
[INFO]
[INFO] --- dependency:3.7.0:tree (default-cli) @ LogReUniq ---
[INFO] tool:LogReUniq:jar:1.0
[INFO] \- junit:junit:jar:4.13.2:test
[INFO]    \- org.hamcrest:hamcrest-core:jar:1.3:test
[INFO] ------------------------------------------------------------------------
[INFO] BUILD SUCCESS
[INFO] ------------------------------------------------------------------------
[INFO] Total time:  0.796 s
[INFO] Finished at: 2026-08-20T00:15:48+09:00
[INFO] ------------------------------------------------------------------------
PS D:\Github\workspace.jre8\LogReUniq>
```

## コーディング
- pom.xml
- src\main\java\tool\LogReUniq.java

## テスト素材
- src\test\resources\listner.log
- src\test\resources\rule.conf
- src\test\resources\output.txt

## AIレビュー
```shell
# CD
cd D:\Github\workspace.jre8
agy
.\LogReUniq\src配下のソースに対して、スキル「source-review」を実行して
/exit
```

## ビルド
```shell
# CD
cd D:\Github\workspace.jre8\LogReUniq
# クリーン
mvn clean
# コンパイル
mvn clean compile
# 外部ライブラリの非推奨メソッド使用有無確認
mvn clean compile "-Dmaven.compiler.showDeprecation=true"
# テスト
mvn test
# jar化
mvn package "-Dmaven.test.skip=true"
# 依存ライブラリの更新確認
mvn versions:display-dependency-updates
# プロジェクトがどのような依存関係を持っているかをツリーで確認
mvn dependency:tree
# ローカルリポジトリにインストール
mvn clean install "-Dmaven.test.skip=true"
# Usage
java -jar target\LogReUniq-1.0-jre8.jar -h
# 動作確認
java -jar target\LogReUniq-1.0-jre8.jar -d src\test\resources\listner.log -c src\test\resources\rule.conf -vvv
```

## リポジトリにコミット
```shell
# CD
cd D:\Github\workspace.jre8\LogReUniq
# ブランチをjava08に切り替え
git switch java08
# コミット
git add .
git commit -m "Gemini 3.6 Flash (High) Review & Modified"
# リモートリポジトリ（java08ブランチ）にプッシュ
git push -u origin java08
```

## リモートリポジトリを確認
- https://github.com/hide104y/LogReUniq/tree/java08
<br>※GitHubの画面で「Compare & pull request」が表示されるが放置

## リモートリポジトリ（java08ブランチ）の取得
```shell
# CD
cd D:\Github\workspace.jre8
# フォルダが存在する場合は削除
if (-Not (Test-Path -Path .\LogReUniq)){rmdir .\LogReUniq}
# クローン実行
git clone -b java08 https://github.com/hide104y/LogReUniq.git
```

## License
- These codes are licensed under CC0.
- http://creativecommons.org/publicdomain/zero/1.0/deed.ja
