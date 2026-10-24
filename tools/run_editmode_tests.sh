#!/usr/bin/env bash
# Core の EditMode テストをバッチモードで実行する。
# 使い方: bash tools/run_editmode_tests.sh   （Unity エディタでプロジェクトを開いていないときに実行）
# 結果: TestResults-EditMode.xml、ログ: Logs/editmode-tests.log
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Unity.exe}"
PROJECT="$ROOT"
"$UNITY" -batchmode -nographics -projectPath "$PROJECT" \
  -runTests -testPlatform EditMode \
  -testResults "$PROJECT/TestResults-EditMode.xml" \
  -logFile "$PROJECT/Logs/editmode-tests.log"
code=$?
case $code in
  0) echo "テスト: すべて成功" ;;
  2) echo "テスト: 失敗あり（$PROJECT/TestResults-EditMode.xml を見る）" ;;
  *) echo "Unity が異常終了しました（code=$code）。ログ: $PROJECT/Logs/editmode-tests.log" ;;
esac
exit $code
