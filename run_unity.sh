#!/bin/bash
# Unity テスト実行スクリプト
# 使用方法:
#   sh run_unity.sh        → フォアグラウンド実行（AI用、完了まで待機）
#   sh run_unity.sh --bg   → バックグラウンド実行（手動用）

# Unityプロジェクトのルートディレクトリを自動検出
# ProjectSettings/ProjectVersion.txt が存在するディレクトリを探す
find_unity_project_root() {
    local current_dir="$(pwd)"
    
    while [ "$current_dir" != "/" ]; do
        if [ -f "$current_dir/ProjectSettings/ProjectVersion.txt" ]; then
            echo "$current_dir"
            return 0
        fi
        current_dir="$(dirname "$current_dir")"
    done
    
    # 見つからなかった場合
    return 1
}

PROJECT_ROOT=$(find_unity_project_root)
if [ -z "$PROJECT_ROOT" ]; then
    echo "エラー: Unityプロジェクトのルートディレクトリが見つかりませんでした"
    echo "ProjectSettings/ProjectVersion.txt を含むディレクトリを探しています"
    exit 1
fi

echo "PROJECT_ROOT: $PROJECT_ROOT"

# バックグラウンドモードの判定
BG_MODE=false
if [ "$1" = "--bg" ]; then
    BG_MODE=true
    echo "バックグラウンドモードで実行します"
fi

# Launch a unity project with the right editor
HUB_PATH="/c/Program Files/Unity Hub/Unity Hub.exe"

# キャッシュファイルのパス（スクリプトと同じディレクトリに保存）
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CACHE_FILE="$SCRIPT_DIR/.unity_path_cache"

# Extract into variable via sed
PROJECT_VERSION=$(sed -nE 's/m_EditorVersion: (.+)/\1/p' "$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt")

# Display
echo "PROJECT_VERSION: $PROJECT_VERSION"

# キャッシュからPROJECT_PATHを読み込む、または新規取得
if [ -f "$CACHE_FILE" ]; then
    # キャッシュファイルが存在する場合、バージョンが一致するか確認
    CACHED_VERSION=$(sed -n '1p' "$CACHE_FILE")
    if [ "$CACHED_VERSION" = "$PROJECT_VERSION" ]; then
        echo "キャッシュからPROJECT_PATHを読み込みます"
        PROJECT_PATH=$(sed -n '2p' "$CACHE_FILE")
    else
        echo "バージョンが変更されました。Unity Hubから再取得します"
        PROJECT_PATH=""
    fi
fi

# キャッシュにない場合はUnity Hubから取得
if [ -z "$PROJECT_PATH" ]; then
    echo "Unity Hubからインストール済みエディタを取得中..."
    
    PROJECT_VERSIONS=$("$HUB_PATH" -- --headless editors -i)
    echo "PROJECT_VERSIONS: $PROJECT_VERSIONS"

    PROJECT_PATH=$(echo "$PROJECT_VERSIONS" | grep "$PROJECT_VERSION" | cut -d" " -f4-)
    
    # キャッシュに保存
    if [ -n "$PROJECT_PATH" ]; then
        echo "PROJECT_PATHをキャッシュに保存します"
        echo "$PROJECT_VERSION" > "$CACHE_FILE"
        echo "$PROJECT_PATH" >> "$CACHE_FILE"
    fi
fi

# Display
echo "PROJECT_PATH: $PROJECT_PATH"

# Launch Unity
PROJECT_PATH_MSYS=$(echo "$PROJECT_PATH" | sed -r "s/^([A-Z]):/\/\1/" | sed -r "s/\\\\/\//g")
echo "PROJECT_PATH_MSYS: $PROJECT_PATH_MSYS"
RESULT_PATH="$SCRIPT_DIR/results.xml"
echo "RESULT_PATH: $RESULT_PATH"
LOG_PATH="$SCRIPT_DIR/unity.log"
echo "LOG_PATH: $LOG_PATH"

# テスト実行
if [ "$BG_MODE" = true ]; then
    # バックグラウンド実行
    nohup "$PROJECT_PATH_MSYS" -runTests -batchmode -projectPath "$PROJECT_ROOT" -testPlatform EditMode -testResults "$RESULT_PATH" -logFile "$LOG_PATH" >/dev/null 2>&1 &
    echo "Unityをバックグラウンドで起動しました"
else
    # フォアグラウンド実行（完了まで待機）
    echo "テストを実行中..."
    "$PROJECT_PATH_MSYS" -runTests -batchmode -projectPath "$PROJECT_ROOT" -testPlatform EditMode -testResults "$RESULT_PATH" -logFile "$LOG_PATH"
    echo "テスト完了"
fi