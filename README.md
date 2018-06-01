# CopyComponentsByRegex

## 概要

これは正規表現でマッチする、構造が同じ場所にあるコンポーネントを一括でコピーするUnityエディタ拡張です。

## インストール

[このリポジトリのzipファイル](https://github.com/Taremin/CopyComponentsByRegex/archive/master.zip)をダウンロードして、解凍したものをアセットの `Plugins` フォルダにコピーします。


## 使い方

1. ヒエラルキーでコピー元のオブジェクトを選択
2. ヒエラルキーで右クリックしてコンテキストメニューから `Copy Components By Regex` をクリック
3. `Copy Components By Regex` ウィンドウが開くので `正規表現` にコピーしたいコンポーネントとマッチする正規表現を書く
   (例: `Dynamic Bone` と `Dynamic Bone Collider` をコピーしたいなら `Dynamic` など)
4. `Copy Components By Regex` ウィンドウの `Copy` ボタンを押す
5. ヒエラルキーでコピー先のオブジェクトを選択
6. `Copy Components By Regex` ウィンドウの `Paste` ボタンを押す


## 注意

コピーするオブジェクトとコンポーネント内で完結しているオブジェクト参照(Dynamic Bone の `root` など)は自動的にコピー先のオブジェクトやコンポーネントに差し替えます。
構造の同一性はオブジェクトの名前で判断しているため、同じ親を持つ同名の子オブジェクトがある場合などで動作がおかしくなる可能性があります。
また、完全に構造が同一でなくても子の名前が同じならできるだけ辿ろうとするため、ボーンの増加などの場合もそのままコピーできます。

## ライセンス

[MIT](./LICENSE)
