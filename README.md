# LogReUniq

## 事前作業
1. JDKがインストールされていない場合はインストール：winget install -e --id Amazon.Corretto.8.JDK
1. Github CLIがインストールされていない場合はインストール：winget install -e --id GitHub.cli
1. Powershellプロンプトを開く

## 変数設定
```shell
$base_dir = "D:\Github\workspace.jre8"
$branch = "java08"
$solution = "LogReUniq"
$groupid="tool"
```

## リポジトリ作成（未作成の場合）
```shell
# サインイン状態の確認
gh auth status
# 初回サインインしていない場合はサインイン
gh auth login
# 削除権限付与
gh auth refresh -h github.com -s delete_repo
# リポジトリの削除
gh repo delete hide104y/${solution} --yes
# リポジトリの作成
gh repo create ${solution} --private
# 確認
gh repo list | Select-String ${solution}
```

## リモートリポジトリ（mainブランチ）の取得
```shell
# CD
cd ${base_dir}
# フォルダが存在する場合は削除
if (Test-Path -Path ".\${solution}"){rmdir ".\${solution}"}
# クローン実行
git clone https://github.com/hide104y/${solution}.git
```

## リモートリポジトリ（mainブランチ）にREADME.mdが存在しない場合
```shell
# CD
cd ${base_dir}\${solution}
# ファイル作成
$enc = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText("${base_dir}\${solution}\README.md", "# ${solution}", $enc)
cat "${base_dir}\${solution}\README.md"
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
git checkout -b ${branch}
# 作成したブランチをリモートにプッシュ
git push -u origin ${branch}
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
-DgroupId=${groupid} `
-DartifactId=${solution}
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
cd ${base_dir}
agy
「.\LogReUniq\src」配下のソースに対して、スキル「source-review」を実行して
/exit
```

## ビルド
```shell
# CD
cd ${base_dir}\${solution}
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
cd ${base_dir}\${solution}
# ブランチ切り替え
git switch ${branch}
# 修正ファイルの追加
git add .
git ls-files
# コミット
git commit -m "★修正コメントを記載★"
# 状態確認
git status
# リモートの変更を取得し、ローカルのコミットをその上に再配置
# git pull --rebase origin ${branch}
# リモートプッシュ
git push -u origin ${branch}
# chromeでリモートブランチへ接続
Invoke-Expression "C:\Progra~1\Google\Chrome\Application\chrome.exe https://github.com/hide104y/${solution}/tree/${branch}"
```

## リモートリポジトリを確認
- https://github.com/hide104y/LogReUniq/tree/java08
<br>※GitHubの画面で「Compare & pull request」が表示されるが放置

## リモートリポジトリ（指定ブランチ）の取得
```shell
# CD
cd ${base_dir}
# フォルダが存在する場合は削除
if (Test-Path -Path ".\${solution}"){rmdir ".\${solution}"}
# クローン実行
git clone -b ${branch} https://github.com/hide104y/${solution}.git
```

## License
- These codes are licensed under CC0.
- http://creativecommons.org/publicdomain/zero/1.0/deed.ja
