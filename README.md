# Catcher Man

**ステージ上のアイテムを「掴む・投げる・振る・置く」ことでステージのギミックや敵を攻略する、2Dキャラクターと3Dステージを組み合わせた横スクロールアクションゲームです。**

取得したアイテムを武器として使うか、足場や仕掛けのために残すかを選びながら、アイテムを戦闘とステージ攻略の両方で役割を持たせることを目指しています。

> **開発中の作品です（2026年10月30日時点）。**  
> 本リポジトリではUnityプロジェクトと実装コードを公開しています。完成版ではなく、ステージ制作・演出・当たり判定などを継続して調整しています。

## 制作概要

| 項目 | 内容 |
| --- | --- |
| ジャンル | 横スクロールアクション |
| プレイ人数 | 1人 |
| 制作形態 | 個人制作 |
| 主な制作範囲 | ゲームの企画、C#によるゲームロジック、ギミック・UIの実装、ステージ構成 |
| 制作期間 | 2026年8月〜開発継続中 |
| 開発環境 | Unity 6000.0.73f1 / C# |
| 使用技術 | Universal Render Pipeline、Input System、Cinemachine、ScriptableObject、3D物理演算 |
| 操作 | キーボード・マウス／ゲームパッド用の入力設定あり |

グラフィックやエフェクトには外部アセットも使用しています。

## ゲームの特徴

### アイテムを使い分けるアクション

- **掴む**：接触したアイテムを取得し、4枠のスロットで管理します。同じ種類のアイテムは個数をまとめて保持します。
- **投げる**：選択中のアイテムを前方へ投げ、敵への攻撃などに利用します。
- **振る**：装備したアイテムを振って攻撃します。鉄球など、一部のアイテムには専用の動作を用意しています。
- **置く**：取得したアイテムをステージに再配置します。
- **探す**：周囲の取得可能なアイテムを強調表示します。

### 戦闘とステージ攻略の連動

はしご、ボタン連動の扉、エレベーター、歯車、ベルトコンベアなどの仕組みを実装しています。アイテムを持ち運び、仕掛けに利用する遊びを組み合わせています。

ボスには距離や状態に応じた移動・近接攻撃・射撃・特殊攻撃を実装しています。投げた歯車の命中回数によって特殊攻撃を封じ、専用アイテムをドロップする処理も用意しています。

## 操作方法

現在プロジェクトに指定されている[PlayerAction.inputactions](Catcher_Man/Assets/InputSystem/PlayerAction.inputactions)の設定です。

| 操作 | キーボード・マウス | Switch Proコントローラー向け設定 |
| --- | --- | --- |
| 左右移動 | A / D | 左スティック左右 |
| ジャンプ | Space | B／左スティック上 |
| はしごを登る | はしごに触れてSpaceを押し続ける | はしごに触れてジャンプ入力を維持 |
| しゃがむ | 左Shiftを押し続ける | 左スティック下 |
| アイテムを掴む | E | A |
| アイテムを探す | Qを押し続ける | 右スティック押し込み |
| アイテムを置く | マウス右ボタン | ZR |
| スロット切り替え | Tab | R |
| アイテムを投げる | F | L |
| アイテムを振る／専用アクション | X | X |

ゲームパッドは機種・接続方式によって認識が異なるため、すべての機器での動作を保証するものではありません。

## 実装で工夫した点

### 1. アイテム情報とシーン上の実体を分けて管理

[ItemDataBase.cs](Catcher_Man/Assets/Scripts/ItemDataBase.cs)をScriptableObjectとして作成し、ID・種類・アイコン・Prefab・攻撃値・投げる速度・装備時の大きさ・Collider設定などをまとめています。

所持スロットにはGameObjectそのものではなく**アイテムID**を保存し、個数は[SlotController.cs](Catcher_Man/Assets/Scripts/SlotController.cs)で管理します。取得時にステージ上のアイテムを破棄し、装備・投擲・設置時にはIDに対応するPrefabからインスタンスを生成します。

この構成により、非表示にした元オブジェクトを持ち続ける方式から、所持情報と表示・物理挙動を分けた管理へ変更しました。共通のパラメータを一か所で調整し、装備時とステージ配置時で異なる見た目・当たり判定を設定できるようにしています。

**関連コード：** [ActionController.cs](Catcher_Man/Assets/Scripts/ActionController.cs) / [ItemGimmick.cs](Catcher_Man/Assets/Scripts/ItemGimmick.cs)

### 2. 移動入力・物理処理・見た目の役割を分離

Input Systemのイベントから移動・ジャンプなどの入力を受け取り、Rigidbodyの速度をFixedUpdateで更新しています。接地・はしご・コンベアへの接触はHashSetで管理し、複数のColliderへの接触を扱っています。

しゃがみでは足元の高さを保ちながら本体Colliderを縮め、立ち上がるときに頭上の障害物を確認します。スプライトの選択は別クラスに分け、左右・移動・ジャンプ・しゃがみの状態に応じて切り替えます。しゃがみジャンプ中はしゃがみ姿を維持します。

**関連コード：** [MoveController.cs](Catcher_Man/Assets/Scripts/MoveController.cs) / [PlayerSpriteController.cs](Catcher_Man/Assets/Scripts/PlayerSpriteController.cs)

### 3. 状態と接触情報を使った敵・ギミックの制御

ボスの行動を状態ごとに分け、攻撃中の判定やクールダウン、特殊攻撃の解除、撃破処理を制御しています。[IEnemyProvider](Catcher_Man/Assets/Scripts/IEnemyProvider.cs)を通して、敵の状態やプレイヤーへの方向を取得する構成も使用しています。

ボタン連動の扉には「一定時間開く」「押している間だけ往復する」「一度だけ移動する」のモードを用意し、共通の処理を設定で使い分けています。歯車の設置判定では、複数の接触相手を保持することで、一つの歯車が離れただけで状態が解除されないようにしています。

**関連コード：** [BossController.cs](Catcher_Man/Assets/Scripts/BossController.cs) / [ButtonDrivenDoor.cs](Catcher_Man/Assets/Scripts/ButtonDrivenDoor.cs) / [GearSet.cs](Catcher_Man/Assets/Scripts/GearSet.cs)

## 現在の開発状況

コード上では、基本移動・ジャンプ・しゃがみ・はしご移動、アイテムの取得／ストック／装備／投擲／設置、通常敵・ボスの行動、各種ギミック、スプライト切り替え、足音・ジャンプ音の処理を実装しています。

現在は主に、以下の点を調整しています。

- アイテム取得範囲、設置、攻撃時の当たり判定の整合性
- コンベアなどのギミックとプレイヤーの物理挙動
- ステージの配置、背景装飾、演出、難易度
- 各機能を組み合わせた通しプレイでの不具合修正

※ 実装済みの処理にも調整中の箇所があり、すべての組み合わせで動作確認が完了しているわけではありません。

## プロジェクトの開き方

1. このリポジトリを取得します。
2. Unity Hubで、リポジトリ直下ではなく **`Catcher_Man`フォルダー**をプロジェクトとして追加します。
3. Unity **6000.0.73f1**で開き、パッケージの読み込みを待ちます。
4. [`Assets/Scenes/ProtoType.unity`](Catcher_Man/Assets/Scenes/ProtoType.unity)を開き、Playを実行します。

現在のBuild Settingsにも上記のシーンが登録されています。初回起動時のインポートには時間がかかる場合があります。

## コードを見る際の入口

| 確認したい内容 | ファイル |
| --- | --- |
| アイテムを使う一連の処理 | [ActionController.cs](Catcher_Man/Assets/Scripts/ActionController.cs) |
| アイテムのデータ定義・取得 | [ItemDataBase.cs](Catcher_Man/Assets/Scripts/ItemDataBase.cs) |
| 移動・接地・ジャンプ・しゃがみ | [MoveController.cs](Catcher_Man/Assets/Scripts/MoveController.cs) |
| ストック数とスロットUI | [SlotController.cs](Catcher_Man/Assets/Scripts/SlotController.cs) |
| ボスの行動・特殊攻撃解除・撃破 | [BossController.cs](Catcher_Man/Assets/Scripts/BossController.cs) |
| ボタンと扉の連動 | [ButtonPressGimmick.cs](Catcher_Man/Assets/Scripts/ButtonPressGimmick.cs) / [ButtonDrivenDoor.cs](Catcher_Man/Assets/Scripts/ButtonDrivenDoor.cs) |
| 足音とエリアによる音の切り替え | [PlayerMovementAudio.cs](Catcher_Man/Assets/Scripts/PlayerMovementAudio.cs) / [FootstepSoundZone.cs](Catcher_Man/Assets/Scripts/FootstepSoundZone.cs) |

## 素材について

外部アセットの権利は各制作者に帰属します。掲載されている素材の利用条件は、それぞれの配布元・ライセンスに従ってください。
