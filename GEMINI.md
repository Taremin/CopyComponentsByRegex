# CopyComponentsByRegex

## 言語設定
1. **言語設定**: すべての思考、コメント、チャット応答、およびTask UI（TaskName, TaskStatus, TaskSummary）は日本語で行うこと。
2. **アーティファクト**: 生成するドキュメント（task.md, walkthrough.md, implementation_plan.mdなど）の本文はすべて日本語で記述すること。ただし、ファイル名やコードブロック内の変数名などは英語のままでよい。
3. **コードコメント**: コード内のコメントも、技術的な慣習に反しない限り日本語で記述すること。

## 概要
Unityエディタ拡張機能。
正規表現（Regex）を使用して、GameObjectの階層構造から特定のコンポーネントを抽出し、別のGameObject階層にコピーするツールです。

## 主な機能

### 1. 正規表現によるコンポーネントコピー
- 指定した正規表現パターンに一致する型名のコンポーネントのみをコピー対象とします。
- `Transform` コンポーネントの値をコピーするかどうかのオプションがあります。

### 2. 階層構造のサポート
- 対象のGameObjectだけでなく、その子オブジェクトも含めて再帰的に処理を行います（Walkdown）。
- コピー先に同名のオブジェクトが存在しない場合、オブジェクトごとコピーするオプションがあります（`isObjectCopy`）。

### 3. コンポーネントの削除
- コピー先に既に同じ型のコンポーネントが存在する場合、コピー前に削除するオプションがあります（`isRemoveBeforeCopy`）。

### 4. Clothコンポーネントの特別対応 (`isClothNNS`)
- `Cloth` コンポーネントのコピー時に、頂点数が異なるメッシュ間でも設定を転送できる機能があります。
- **KD-Tree** アルゴリズムを使用して、コピー元の頂点に最も近いコピー先の頂点を探索し、`coefficients`（collisionSphereDistance, maxDistance）を適用します。

### 5. 参照の解決 (`UpdateProperties`)
- コピーされたコンポーネント内の `ObjectReference`（TransformやComponentへの参照）を、コピー先の階層内の対応するオブジェクトに自動的に繋ぎ直します。

### 6. 置換リスト機能
- コピー元とコピー先でオブジェクト名が異なる場合（例：VRoid Studioの `J_Bip_C_Head` と標準的な `Head`）、名前の違いを吸収できます。
- **正規表現ルール**: 任意の正規表現パターンで名前を変換
- **HumanoidBoneルール**: Unity Humanoidボーン名のエイリアスを使用してマッチング

## ソースコード構成

### `CopyComponentsByRegex.cs`
- **継承**: `EditorWindow`
- **役割**: メインのUI描画とコピーロジックの実装。
- **主要メソッド**:
    - `CopyWalkdown`: コピー元の階層を走査し、条件に合うコンポーネントを収集して `TreeItem` 構造体に格納。
    - `MergeWalkdown`: 収集した情報を元に、コピー先の階層にコンポーネントを追加・値の適用。
    - `RemoveWalkdown`: コンポーネントの削除処理。
    - `UpdateProperties`: コピー後の参照関係の修復。
    - `OnGUI`: エディタウィンドウのUI定義。設定値は `EditorUserSettings` に保存されます。

### `KDTree.cs`
- **役割**: 3次元空間における近傍探索（Nearest Neighbor Search）を行うためのKD木の実装。
- **用途**: `CopyComponentsByRegex` において、Clothコンポーネントの頂点データのマッピングに使用されます。
- **出典**: A Stark (2009) のコードを Taremin (2018) が改変したもの。

### `NameMatcher.cs`
- **役割**: 名前変換ロジックを提供する静的クラス。
- **主要メソッド**:
    - `TransformName`: 置換ルールを適用して名前を変換
    - `NamesMatch`: 2つの名前が置換ルールを考慮してマッチするか判定
    - `TryFindMatchingName`: 辞書から置換ルールを考慮してマッチする子を検索
    - `GetBoneMapping`: AnimatorからHumanoidボーンマッピングを動的に取得
    - `IsHumanoid`: GameObjectがHumanoidリグかチェック

### `ReplacementRule.cs`
- **役割**: 置換ルールのデータ構造を定義
- **RuleType**: `Regex`（正規表現）または `HumanoidBone`（ボーンマッピング）
- **HumanoidBoneGroup**: `All`、`Head`、`LeftArm` など部位別のグループ定義

## 使用方法（コードからの推測）
1. Unityメニュー `GameObject/Copy Components By Regex` からウィンドウを開く。
2. コピー元のGameObjectを選択し、「Copy」ボタンを押して構造をメモリに保持。
3. コピー先のGameObjectを選択。
4. 必要に応じてオプション（正規表現、Transformコピー、既存削除、オブジェクトコピー、Cloth NNSなど）を設定。
5. 「Paste」ボタンを押して適用。
