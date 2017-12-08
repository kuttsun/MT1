rem ソリューションルートで実行すること

rem OpenCover のインストール先
set OPENCOVER=%1

rem テストコマンド
set TARGET="C:\Program Files\dotnet\dotnet.exe"

rem テストコマンド引数
set TARGET_TEST="test MT1.Tests\MT1.Tests.csproj"

rem OpenCover の出力ファイル
set OUTPUT=%2

rem カバレッジ計測対象の指定
set FILTERS="+[*]*"

rem OpenCover の実行
%OPENCOVER% -register:user -target:%TARGET% -targetargs:%TARGET_TEST% -filter:%FILTERS% -oldstyle -output:%OUTPUT%