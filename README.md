# LogReUniq

## 事前作業
1. .NET SDKがインストールされていない場合はインストール：winget install -e --id Microsoft.DotNet.SDK.10
1. Github CLIがインストールされていない場合はインストール：winget install -e --id GitHub.cli
1. Powershellプロンプトを開く

## 変数設定
```shell
$base_dir = "D:\Github\Projects"
$branch = "dotnet10"
$solution = "LogReUniq"
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

## プロジェクトの作成
```shell
# コンソールアプリ：.net 10.0
cd ${base_dir}\${solution}
dotnet new console --framework net10.0 -o ${solution}
```

## ソリューションファイルの作成
.\LogReUniq\LogReUniq.slnx
```xml
<Solution>
  <Project Path="LogReUniq/LogReUniq.csproj" />
  <Project Path="TestProject1/TestProject1.csproj" />
</Solution>
```

## プロジェクトファイルの修正
.\LogReUniq\LogReUniq\LogReUniq.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <InvariantGlobalization>false</InvariantGlobalization>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="CmnClsLib">
      <HintPath>..\..\CmnClsLib\CmnClsLib\bin\Release\net10.0\CmnClsLib.dll</HintPath>
    </Reference>
  </ItemGroup>

</Project>
```

## 依存パッケージ
```shell
# CD
cd ${base_dir}
# 依存プロジェクト参照の追加
dotnet add .\LogReUniq\LogReUniq\LogReUniq.csproj reference .\CmnClsLib\CmnClsLib\CmnClsLib.csproj
# 依存パッケージのインストール
(なし)
```

## コーディング
(省略)

## AIレビュー
```shell
# CD
cd ${base_dir}
agy
/clear
「.\LogReUniq\LogReUniq」配下のソースに対して、スキル「source-review」を実行して
/exit
```

## ビルド
```shell
# CD
cd ${base_dir}
# パッケージの最新化
dotnet package update --file .\LogReUniq\LogReUniq.slnx
# ビルド
dotnet build .\LogReUniq\LogReUniq.slnx -c Release -p:InvariantGlobalization=false
dotnet build .\LogReUniq\TestProject1\TestProject1.csproj
# 単体テスト
dotnet test .\LogReUniq\TestProject1\TestProject1.csproj
# Usage
LogReUniq\LogReUniq\bin\Release\net10.0\LogReUniq.exe -h
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

## デプロイ
```shell
cd ${base_dir}
dotnet publish .\LogReUniq\LogReUniq\LogReUniq.csproj -c Release -o D:\Github\bin.n10 -r win-x64 --self-contained=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:PublishTrimmed=false -p:PublishAot=false -p:InvariantGlobalization=false
```

## リモートリポジトリの確認
- https://github.com/hide104y/LogReUniq/tree/dotnet10
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
