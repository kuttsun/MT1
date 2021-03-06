﻿rem ソリューションルートで実行すること

set PROJECT=%1

rem OpenCover のインストール先
set OPENCOVER="%USERPROFILE%\.nuget\packages\opencover\4.6.519\tools\OpenCover.Console.exe"

rem テストコマンド
set TARGET=dotnet.exe

rem テストコマンド引数
set TARGET_TEST="test %PROJECT%\%PROJECT%.csproj"

rem OpenCover の出力ファイル
set OUTPUT=coverage.xml

rem カバレッジ計測対象の指定
set FILTERS="+[*]*"

rem OpenCover の実行
%OPENCOVER% -register:user -target:%TARGET% -targetargs:%TARGET_TEST% -filter:%FILTERS% -oldstyle -output:%OUTPUT%