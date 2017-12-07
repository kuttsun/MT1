rem ソリューションルートで実行すること

rem OpenCover のインストール先
set OPENCOVER="%USERPROFILE%\.nuget\packages\OpenCover\4.6.519\tools\OpenCover.Console.exe"

rem テストコマンド
set TARGET="C:\Program Files\dotnet\dotnet.exe"

rem テストコマンド引数
set TARGET_TEST="test MT1.Tests\MT1.Tests.csproj"

rem OpenCover の出力ファイル
set OUTPUT="coverage.xml"

rem カバレッジ計測対象の指定
set FILTERS="+[*]*"

rem OpenCover の実行
%OPENCOVER% -register:user -target:%TARGET% -targetargs:%TARGET_TEST% -filter:%FILTERS% -oldstyle -output:%OUTPUT%