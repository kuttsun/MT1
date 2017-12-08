rem ソリューションルートで実行すること

rem Codecov のインストール先
set CODECOV="%USERPROFILE%\.nuget\packages\codecov\1.0.3\tools\codecov.exe"

rem Codecov の実行
%CODECOV% -f "coverage.xml"