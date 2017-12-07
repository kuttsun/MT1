rem ソリューションルートで実行すること

rem ReportGenerator のインストール先
set REPORTGEN="%USERPROFILE%\.nuget\packages\ReportGenerator\3.0.2\tools\ReportGenerator.exe"

rem OpenCover の出力ファイル
set OUTPUT="coverage.xml"

rem ReportGenerator の HTML 出力先
set OUTPUT_DIR="Coverage"

rem レポートの生成 (xml から html へ変換)
%REPORTGEN% -reports:%OUTPUT% -targetdir:%OUTPUT_DIR%