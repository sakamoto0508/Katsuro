# Katsuro 敵の攻撃選択 第1段階 — 実装・調査報告

2026-10-18。Unity 6000.2.7f2 / HDRP。作業開始時の `git status --short` と `git diff` は空。
Play Modeは一度も起動していない。実プレイ・操作感・回避可能性・ゲーム画面の見た目は未検証。

## 1. 変更したクラスとアセット

| 対象 | 変更 |
|---|---|
| EnemyDecisionConfig | ActionWeightへAttack参照、RepeatPenaltyPerAttackを追加。既存フィールドは維持。 |
| EnemyDecisionMaker / 同ファイルのEnemyDecision | 行動と選択した攻撃データを一緒に返す。距離帯・重み付き抽選を維持。旧Decide APIも維持。 |
| EnemyAI | 選択データをControllerへ渡す。Slash2/HeavySlash2もBusy対象。失敗時は待機後に再抽選。WaitもControllerへ通知。 |
| EnemyController | 予約に攻撃参照を保持しAttackerへ渡す。失敗時は移動停止を解除。終了Event欠落時の復帰を追加。修練・攻撃Index Eventも同じデータを使用。 |
| EnemyAttacker | PerformAttack(data)を追加し、強弱・威力・武器グループをデータから取得。Triggerや武器が不正ならfalse。既存bool付き呼び出しは互換用に残すが強弱はデータ優先。 |
| EnemyAttackData | Strength（Light=0 / Heavy=1）、IsHeavyを追加。既存ActionTypeなどは維持。 |
| EnemyActionType | 並びを変えず既存値0～9を明記。 |
| EnemyAnimationController | 指定パラメータが有効なAnimatorのTriggerかを検証するHasTriggerのみ追加。 |
| EnemyWeapon | ColliderとRelayが存在するかを確認するHasValidHitboxを追加。既存ON/OFF実装は維持。 |
| EnemyDecisionConfig.asset | 近距離Slashの50を4攻撃へ12.5ずつ分割。後退10、遠距離Approach30、中距離Approach30/WaitWalk5を維持。 |
| EnemyAttackData.asset / 1 / 2 / 3 | StrengthをLight / Light / Heavy / Heavyへ設定。GUID、ActionType=Slash、Damageなどは維持。 |

ゲーム用の追加C#ファイルは不要。検証コードは `Artifacts/EnemyAttackSelection` 内に置いた。
Enemy.prefab、Enemy.controller、GameScene、Animation Clip、プレイヤーやVFX・UIのファイルは編集していない。

## 2. 処理フローと接続

`GameScene → Enemy.prefab → EnemyController → EnemyAI → EnemyDecisionMaker → EnemyDecision(Action, AttackData) → EnemyController → EnemyAttacker → Animator / EnemyWeapon`。

Enemy.prefabは既存DecisionConfigと4つのAttackDataを参照し、GameSceneにはそれを上書きする攻撃配列・DecisionConfig設定は無い。AnimatorもPrefabから既存Enemy.controllerへ接続している。
距離判定は従来どおり `distance > 12` が遠距離、`3 < distance <= 12` が中距離、`distance <= 3` が近距離。
攻撃データ付き候補はAttackerの二次ランダム抽選を通らず、そのデータをそのまま実行する。
Attack=Noneの旧攻撃候補だけは既存ActionType対応データからランダムに選ぶ互換経路を残した。新しい4候補ではNoneにしない。
移動・待機候補はAttackを持たず従来のMover処理へ進む。

既存AnimatorはLightAttack/LightAttack2とHeavyAttack/HeavyAttack2のTriggerで遷移する。HeavyはStart→本体の構成。
既存ClipのAnimEvent_EnableWeaponHitbox / DisableWeaponHitboxで5つの刀Colliderを一つの武器グループとしてON/OFFし、AnimEvent_OnAttackFinishedでAIが次を選ぶ。StepBackは専用終了Eventで復帰する。
Heavy用Start Clip、NormalAttackタグ、AttackRootMotionStateによる移動所有権も維持した。

## 3. Inspectorの設定方法

`Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset` を選ぶ。
Near / Mid / Far Candidatesの各行はAction、Attack、Weightを持つ。4攻撃の現在の対応は次のとおり。

| Attack参照 | Animator Trigger | Strength | 現在のWeight | Damage |
|---|---|---|---:|---:|
| EnemyAttackData.asset | LightAttack | Light | 12.5 | 10 |
| EnemyAttackData 1.asset | LightAttack2 | Light | 12.5 | 10 |
| EnemyAttackData 2.asset | HeavyAttack | Heavy | 12.5 | 20 |
| EnemyAttackData 3.asset | HeavyAttack2 | Heavy | 12.5 | 20 |

現在の4行のActionはすべてSlashでよい。攻撃の個体はAttack参照で区別する。
例の30:20:25:25比率にする場合、従来の攻撃頻度を保ちたいなら15 / 10 / 12.5 / 12.5にする。攻撃合計50、StepBack10を保てる。
30 / 20 / 25 / 25をそのまま設定すると合計100になるため、後退に対する攻撃頻度も上がる。
Weight=0は候補の無効化。負数・NaN・Infinityは抽選から除外。有効候補なしはWaitへ戻る。

RepeatPenalty=0.25を維持。RepeatPenaltyPerAttackは初期OFFなので、直前がSlashなら4行すべてへ補正され、従来の攻撃/後退傾向を保つ。
ONなら同じAttackDataだけに補正する。他の攻撃が連続で出やすくなるため、攻撃頻度も変わる。移動・待機はどちらでもAction単位。
候補Weightは相対値で、距離帯とRepeatPenaltyを含む条件付き確率となる。

調整可能な既存項目: FarDistance / NearDistance、ObserveSeconds、ReconsiderInterval、各距離帯Candidates、RepeatPenalty。
攻撃データ側: Strength、Animator Trigger、Damage、Hitbox Index。Hitbox IndexはCollider配列のインデックスではなく武器グループの番号で、現行Prefabは0を使う。
RangeとHitbox Enable/Disable Delayは既存の参考設定のままで、選択距離制限やEventの代替としては使わない。
攻撃別Cooldown・条件・コンボ列・バック回避後専用抽選は未実装。必要になった時点で候補または攻撃定義へ追加できる。

## 4. Light / Heavyと失敗時の扱い

強弱はEnemyAttackData.Strengthだけを参照する。Trigger名の文字列解析は行わない。
IsHeavyがSwordTrailのスタイルとDamageInfo.IsHeavyへ届き、既存のHitStopや被弾側フィードバックがその情報を受け取る。威力は各データの10/20をそのまま使う。
複数のON/OFF区間でも、OFF時にWeaponが消去する威力を次のON時に選択データから再設定する。
武器のHitbox ON/OFFは既存Animation Eventのまま。HitboxIndexで選ばれた武器グループだけを有効化する。現行は1グループなので刀Collider5つの構成は同じ。

不正なTrigger、未設定データ、範囲外武器Index、Collider/Relay不在では攻撃を開始せずfalseを返す。
Controllerは移動停止を解除し、AIはObserveSeconds（最小0.1秒）待機後に再抽選する。同フレームで再帰的に抽選しない。
有効なTriggerに対応する遷移や終了Eventが欠けた場合に備え、攻撃開始からAnimator速度で8秒を超え、Root Motion所有中でもNormalAttackタグ中でも遷移中でもない時だけ復帰する。
通常の攻撃は終了Eventですぐ再抽選する。タイムアウトで正常な攻撃Stateへ割り込まない。
破損したControllerで攻撃State自体が永久ループするケースはこの復帰条件の対象外で、Animator修正が必要。

## 5. 互換性・検証

Enum値はApproach=0、StepBack=1、WaitWalk=2、Slash=3、Slash2=4、HeavySlash=5、HeavySlash2=6、Thrust=7、WarpAttack=8、Wait=9のまま。
SOの既存フィールド名、全GUID、Prefab/Scene参照を維持。既存4攻撃の抽選前の比率25%ずつとカテゴリ合計を保持した。
プレイヤー攻撃・コンボ、追撃、HitReactionの選択方針、HitStop実装、VFX、カメラ、バック回避Root Motion、死亡、勝敗・Retry、UI・チュートリアルの内容は変更していない。
敵Heavyから受ける既存フィードバックは、正しいIsHeavyが届くため従来と変わる可能性がある。これは今回の修正対象。

実施済み:
- Unity同梱RoslynでAssembly-CSharpをコンパイル成功。
- 起動中のUnity Editorでrecompile_status: completed / failed=false / errors=[]を確認。
- Edit Modeで実アセットを使用し12万回の抽選を検証。4攻撃は25033 / 24853 / 24912 / 25396回、後退19806回。固定した直前Waitに対する基礎重み検証であり、実プレイの最終出現率ではない。
- RepeatPenaltyのカテゴリ方式・攻撃個体方式、移動待機4種、無効重み、空候補、遠中距離、旧形式を検証。
- Prefabの4攻撃参照、5武器ColliderとRelay、4Animator Triggerの型と名前を照合。
- 一時Preview Scene内でEnemyAIを評価し、選択したHeavyデータの受け渡し、Busy中の再抽選抑止、終了後再抽選、実行失敗後復帰を確認。
- AnimationClipの実名・長さ・Rootカーブと設定をUnity APIで読み取り。UAL2のRM骨移動はPreview SceneのSampleAnimationで計測。
- git diff --checkを実施。

未実施: Play Mode、実際の命中・HitStop・描画、実操作、Animatorの8秒フォールバックを実時間で発火させるテスト。
詳細証跡はEditModeValidation.txt / UnityClips.tsv / UnityRootCurves.tsv / AnimationDetails.txt。

## 6. アニメーション候補

調査範囲はARPG Pack、Universal Animation Library 2 Standardの両FBX、および既存Animフォルダ、別途実在したKevin Iglesiasの2H/CombatDamage。
UAL2のREADMEはQuaterniusを作者としている。Kevin IglesiasとARPG Pack、UAL2を同一パッケージとして扱っていない。
Animフォルダの元パッケージは実ファイルだけで確認できなかったため「出自未確認」とした。

以下のClip名はUnityのLoadAllAssetsAtPathで確認した正確な名前。FBX内の同名Clipはパスで区別する。
長さは元Clipの秒数。動作の印象やゲーム性は未プレイであり、用途は構成・曲線・長さからの採用候補。斬撃数、刀の軌跡、1撃後の間、予備動作、回避可能性は目視・実プレイで確定する必要がある。

| Clip名 | Unityプロジェクト内の実パス | 使用元 | 対象 | 秒 | Root Motionの実データ | 相性・用途候補 | 注意点 |
|---|---|---|---|---:|---|---|---|
| ARPG_Samurai_Attack_Combo1 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo1.anim | ARPG Pack / Samurai | コンボ構成 | 0.8166667 | RootT.z差=0.6405（Humanoid曲線値） | 独立Clipを2～3段へつなぐ候補。各段の間隔を設計しやすい。 | 各段の斬撃数・つなぎ目・刀軌跡は未確認。ClipごとのHitbox区間と入力回避猶予をPreviewで決める。 |
| ARPG_Samurai_Attack_Combo2 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo2.anim | ARPG Pack / Samurai | コンボ構成 | 0.9666667 | RootT.z差=1.0844（Humanoid曲線値） | 独立Clipを2～3段へつなぐ候補。各段の間隔を設計しやすい。 | 各段の斬撃数・つなぎ目・刀軌跡は未確認。ClipごとのHitbox区間と入力回避猶予をPreviewで決める。 |
| ARPG_Samurai_Attack_Combo3 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Combo3.anim | ARPG Pack / Samurai | コンボ構成 | 0.8666667 | RootT.z差=1.5960（Humanoid曲線値） | 独立Clipを2～3段へつなぐ候補。各段の間隔を設計しやすい。 | 各段の斬撃数・つなぎ目・刀軌跡は未確認。ClipごとのHitbox区間と入力回避猶予をPreviewで決める。 |
| Armature\|Sword_Regular_Combo | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx | Quaternius / Universal Animation Library 2 Standard | コンボ一体Clip | 3 | root骨XYZカーブは全て0 | 一体型コンボの比較候補。Heavyは長尺で大技向きと推定。 | 2連/3連の正確な斬撃数、1撃後の間、回避可能性は未確認。GenericからHumanoidへのリターゲット検証が必要。 |
| Armature\|Sword_Regular_Combo | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard_RM.fbx | Quaternius / Universal Animation Library 2 Standard | コンボ一体Clip | 3 | 骨移動あり（下記のRM詳細参照）、Animator抽出未設定 | 一体型コンボの比較候補。Heavyは長尺で大技向きと推定。 | 2連/3連の正確な斬撃数、1撃後の間、回避可能性は未確認。GenericからHumanoidへのリターゲット検証が必要。 |
| Armature\|Sword_Heavy_Combo | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx | Quaternius / Universal Animation Library 2 Standard | コンボ一体Clip | 4.333333 | root骨XYZカーブは全て0 | 一体型コンボの比較候補。Heavyは長尺で大技向きと推定。 | 2連/3連の正確な斬撃数、1撃後の間、回避可能性は未確認。GenericからHumanoidへのリターゲット検証が必要。 |
| Armature\|Sword_Heavy_Combo | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard_RM.fbx | Quaternius / Universal Animation Library 2 Standard | コンボ一体Clip | 4.333333 | 骨移動あり（下記のRM詳細参照）、Animator抽出未設定 | 一体型コンボの比較候補。Heavyは長尺で大技向きと推定。 | 2連/3連の正確な斬撃数、1撃後の間、回避可能性は未確認。GenericからHumanoidへのリターゲット検証が必要。 |
| Armature\|Sword_Regular_A | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx | Quaternius / Universal Animation Library 2 Standard | 分割コンボ構成 | 0.4333334 | root骨XYZカーブは全て0 | A/B/CとRecを組み合わせ、段間の回復時間を調整する候補。 | Recは名称に基づく回復候補。接続Pose・斬撃数の目視は未確認。Genericのまま敵Humanoidへ直接差し替えない。 |
| Armature\|Sword_Regular_A_Rec | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx | Quaternius / Universal Animation Library 2 Standard | 分割コンボ構成 | 0.9666667 | root骨XYZカーブは全て0 | A/B/CとRecを組み合わせ、段間の回復時間を調整する候補。 | Recは名称に基づく回復候補。接続Pose・斬撃数の目視は未確認。Genericのまま敵Humanoidへ直接差し替えない。 |
| Armature\|Sword_Regular_B | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx | Quaternius / Universal Animation Library 2 Standard | 分割コンボ構成 | 0.5333334 | root骨XYZカーブは全て0 | A/B/CとRecを組み合わせ、段間の回復時間を調整する候補。 | Recは名称に基づく回復候補。接続Pose・斬撃数の目視は未確認。Genericのまま敵Humanoidへ直接差し替えない。 |
| Armature\|Sword_Regular_B_Rec | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx | Quaternius / Universal Animation Library 2 Standard | 分割コンボ構成 | 1.033333 | root骨XYZカーブは全て0 | A/B/CとRecを組み合わせ、段間の回復時間を調整する候補。 | Recは名称に基づく回復候補。接続Pose・斬撃数の目視は未確認。Genericのまま敵Humanoidへ直接差し替えない。 |
| Armature\|Sword_Regular_C | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx | Quaternius / Universal Animation Library 2 Standard | 分割コンボ構成 | 2 | root骨XYZカーブは全て0 | A/B/CとRecを組み合わせ、段間の回復時間を調整する候補。 | Recは名称に基づく回復候補。接続Pose・斬撃数の目視は未確認。Genericのまま敵Humanoidへ直接差し替えない。 |
| ARPG_Samurai_Attack_Sprint | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Sprint.anim | ARPG Pack / Samurai | 突進 | 0.9833334 | RootT.z差=2.1320（Humanoid曲線値） | 既存のHumanoid系統で、正のZ移動カーブと前方averageSpeedを確認。優先調査候補。 | 予備動作・斬りの瞬間・硬直の見え方は未確認。既存倍率0.18や衝突制限を考慮する。 |
| ARPG_Samurai_Attack_Dodge_to_Combo1 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Attack_Dodge_to_Combo1.anim | ARPG Pack / Samurai | 回避後反撃／コンボ初段 | 0.8166667 | RootT.z差=0.7213（Humanoid曲線値） | 回避から攻撃へ接続する比較候補。 | 既存バック回避終端とのPose一致は未確認。RootT.z差はSprintより小さい。 |
| Armature\|Sword_Dash | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard_RM.fbx | Quaternius / Universal Animation Library 2 Standard | 突進 | 1.566667 | 骨移動あり（下記のRM詳細参照）、Animator抽出未設定 | RM版でroot骨のモデル座標Z移動-3.69224をEdit Modeで確認。 | 敵の前方向に変換されるか、Animator.deltaPositionとして抽出されるかは未確認。予備動作・硬直も目視確認が必要。 |
| Samurai_Hit14 | Assets/InportAssets/ARPGPack/ARPGSamurai/Additional_Animations/Humanoid/Samurai_Hit14.anim | ARPG Pack / Samurai | HitReaction | 0.4833333 | RootT.z差=0.0000（Humanoid曲線値） | 0.483秒でRootTの始終差0。短いLight候補として先に比較。 | 反応の大きさ・被弾方向の見え方は未確認。移動方向からの用途推定であり、方向ラベルはPreviewで決める。既存Reactionは変更していない。 |
| ARPG_Samurai_Hit4 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Hit4.anim | ARPG Pack / Samurai | HitReaction | 0.8166667 | RootT.z差=-0.0000（Humanoid曲線値） | RootTの始終差がほぼ0。通常被弾で位置を動かしたくない場合の候補。 | 反応の大きさ・被弾方向の見え方は未確認。移動方向からの用途推定であり、方向ラベルはPreviewで決める。既存Reactionは変更していない。 |
| ARPG_Samurai_Hit1 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Hit1.anim | ARPG Pack / Samurai | HitReaction | 0.8166667 | RootT.z差=-0.4575（Humanoid曲線値） | 負Zへの移動を持つ前方被弾候補。 | 反応の大きさ・被弾方向の見え方は未確認。移動方向からの用途推定であり、方向ラベルはPreviewで決める。既存Reactionは変更していない。 |
| ARPG_Samurai_Hit3 | Assets/InportAssets/ARPGPack/ARPGSamurai/Animations/Humanoid/ARPG_Samurai_Hit3.anim | ARPG Pack / Samurai | HitReaction | 0.8166667 | RootT.z差=-0.9150（Humanoid曲線値） | Hit1より大きい負Z移動を持つHeavy候補。 | 反応の大きさ・被弾方向の見え方は未確認。移動方向からの用途推定であり、方向ラベルはPreviewで決める。既存Reactionは変更していない。 |
| Samurai_Hit11 | Assets/InportAssets/ARPGPack/ARPGSamurai/Additional_Animations/Humanoid/Samurai_Hit11.anim | ARPG Pack / Samurai | HitReaction | 1.15 | RootT.z差=0.9150（Humanoid曲線値） | 正Z移動を持つ後方被弾候補。 | 反応の大きさ・被弾方向の見え方は未確認。移動方向からの用途推定であり、方向ラベルはPreviewで決める。既存Reactionは変更していない。 |
| Samurai_Hit12 | Assets/InportAssets/ARPGPack/ARPGSamurai/Additional_Animations/Humanoid/Samurai_Hit12.anim | ARPG Pack / Samurai | HitReaction | 1.15 | RootT.z差=0.0000（Humanoid曲線値） | 正X移動を持つ側面被弾候補。 | 反応の大きさ・被弾方向の見え方は未確認。移動方向からの用途推定であり、方向ラベルはPreviewで決める。既存Reactionは変更していない。 |
| Samurai_Hit13 | Assets/InportAssets/ARPGPack/ARPGSamurai/Additional_Animations/Humanoid/Samurai_Hit13.anim | ARPG Pack / Samurai | HitReaction | 1.15 | RootT.z差=-0.0000（Humanoid曲線値） | 負X移動を持つ反対側の被弾候補。 | 反応の大きさ・被弾方向の見え方は未確認。移動方向からの用途推定であり、方向ラベルはPreviewで決める。既存Reactionは変更していない。 |
| Armature\|Hit_Knockback | Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/UAL2_Standard_RM.fbx | Quaternius / Universal Animation Library 2 Standard | Heavy HitReaction | 0.8333334 | 骨移動あり（下記のRM詳細参照）、Animator抽出未設定 | RM版はモデル座標Z+3.0の大きな骨移動。強いノックバック演出の比較候補。 | Lightには移動量が過大な可能性。Genericリターゲットと移動抽出が必要。通常版にも同名Clipが存在。 |
| HumanM@Attack2H01 | Assets/InportAssets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx | Kevin Iglesias / Human Animations | コンボを構成する単発攻撃 | 1.6 | XZ Bake Into Pose=ON、averageSpeed=0。前進Root Motionとして未確認 | 両手持ちの別系統比較候補。 | 連続攻撃Clipとは断定しない。剣の持ち方・段間Pose・予備動作は未確認。 |
| HumanM@Attack2H02 | Assets/InportAssets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H02.fbx | Kevin Iglesias / Human Animations | コンボを構成する単発攻撃 | 1.566667 | XZ Bake Into Pose=ON、averageSpeed=0。前進Root Motionとして未確認 | 両手持ちの別系統比較候補。 | 連続攻撃Clipとは断定しない。剣の持ち方・段間Pose・予備動作は未確認。 |
| HumanM@CombatDamage01 | Assets/InportAssets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx | Kevin Iglesias / Human Animations | HitReaction | 1 | XZ Bake Into Pose=ON、averageSpeed=0。前進Root Motionとして未確認 | Humanoidの別系統被弾候補。XZ Bake済みで位置を保持しやすい。 | 名称だけではLight/Heavyの大小を判定できない。全身Poseと短縮再生の相性は未確認。 |
| HumanM@CombatDamage02 | Assets/InportAssets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage02.fbx | Kevin Iglesias / Human Animations | HitReaction | 1.066667 | XZ Bake Into Pose=ON、averageSpeed=0。前進Root Motionとして未確認 | Humanoidの別系統被弾候補。XZ Bake済みで位置を保持しやすい。 | 名称だけではLight/Heavyの大小を判定できない。全身Poseと短縮再生の相性は未確認。 |
| Hit_F_2_InPlace | Assets/InportAssets/Anim/Hit_F_2_InPlace.anim | 既存Animフォルダ（出自未確認） | Light HitReaction | 0.5666667 | XZ Bake Into Pose=ON、averageSpeed=0。前進Root Motionとして未確認 | 0.567秒、XZ Bake済み。短い被弾の追加比較候補。 | ファイルだけでは元パッケージを特定できないためUALと断定しない。正面被弾らしさは名称からの推定。 |
| Enemy_Attack_1_Run | Assets/InportAssets/Anim/Enemy_Attack_1_Run.anim | 既存Animフォルダ（出自未確認） | 走り攻撃の比較 | 2.5 | XZ Bake Into Pose=ON、averageSpeed=0。前進Root Motionとして未確認 | 2.5秒の走り攻撃候補が実在する。 | InPlace接尾辞が無くてもXZ Bake済み、averageSpeed=0。前進Root Motionが使えるとは報告できない。 |

### Root Motionの重要な違い

ARPGのRootTはHumanoidの曲線値であり、RootT.zの差をそのまま敵の移動メートル数とは断定しない。
ARPG_Samurai_Attack_SprintはXZ Bake=false、keepOriginalXZ=false、Unity averageSpeed=(-0.09,0,1.94)を確認。前方成分がある。現行Enemyの攻撃移動倍率0.18やRigidbody/NavMesh制限によって実移動は変わる。

UAL2は現在Generic、rootMotionBoneName未指定で、RM版もhumanMotion=false、hasRootCurves=false、averageSpeed=(0,0,0)。これは骨移動が無いという意味ではない。
AnimationUtilityでArmature/rootの移動カーブを確認し、モデル複製をSampleAnimationしたモデル座標で次を計測した:

| Clip（RM版） | モデル座標のroot始終差 | ローカルカーブ |
|---|---|---|
| Armature\|Sword_Dash | Z=-3.69224 | rootのlocal Y: 0 → -3.69224 |
| Armature\|Sword_Regular_Combo | Z=-2.34253 | local Y: 0.0662249 → -2.276304 |
| Armature\|Sword_Heavy_Combo | Z=-5.32429 | local Y: 0.07584163 → -5.248451 |
| Armature\|Hit_Knockback | Z=+3.0 | local Y: 0 → +3 |

通常版の同じ4Clipのroot XYZカーブはすべて0。RM版の方向が敵の正面へ変換されること、Humanoid化後の姿勢、Animator.deltaPositionの抽出は未確認。
名前の_RM、Root Motion設定、hasRootCurvesだけを根拠に「前進突進をそのまま使える」とは判断しない。

## 7. 次段階のおすすめ

1. コンボ: まずARPG_Samurai_Attack_Combo1 → Combo2（必要ならCombo3）。既存Humanoidと合わせやすく、1撃ごとに間とHitbox区間を決められる構成を優先候補とする。
2. UAL2の比較: Armature|Sword_Regular_ComboとA / A_Rec / B / B_Rec / C。まずRetargetとPose接続を検証する。Heavy_Comboは4.333秒と長いため、採用時はテンポと回避猶予を確認する。
3. 突進: ARPG_Samurai_Attack_Sprintを先にPreview。UAL2のArmature|Sword_Dash（RM版）を次点比較。バック回避終端からの構えの連続性はどちらも未確認。
4. Light HitReaction: Samurai_Hit14、Hit_F_2_InPlaceを短さ・位置保持の候補として比較。
5. Heavy HitReaction: ARPG_Samurai_Hit3、HumanM@CombatDamage02、UAL2のHit_Knockbackを比較。姿勢の強弱は目視で確認し、Root移動と怯み時間を別々に調整する。
6. 方向別: ARPG_Samurai_Hit1とSamurai_Hit11/12/13の方向候補を先に比較。UAL2 Standardの43Clip内には方向別HitReactionセットは見つからなかった。

今回これらのAnimator登録、コンボ実装、突進AI、被弾差し替えは行っていない。

## 8. 未解決・範囲外の観察

- UAL2のGeneric→HumanoidのAvatar設定とRoot Motion抽出が未設定。将来採用時に対応する。
- 一体型Comboの実斬撃数・段間の隙・予備動作・後隙は未確認。候補名だけから2連/3連とは断定していない。
- 既存HitReaction LayerのLightLeft/Right、HeavyLeft/Rightは同じHalberd_Hit2を参照している。真の左右別Clipへの差し替えは今回行っていない。
- EnemyMoverはWaitのStopMove後も通常追跡の距離条件を更新する設計。遠い距離にWaitを新規登録した時の完全停止は実プレイで確認が必要。今回Moverの追跡/待機ポリシーは改修していない。
- 旧形式の攻撃候補でAttack=Noneなら二次ランダムが残る。個別制御したい候補は必ずAttackを指定する。
- 将来、別の強攻撃データを追加するときはStrength=Heavyを明示する。新規データの既定値はLight。
- 既存コードの未使用フィールドや外部アセットの非推奨API警告は今回の範囲外として維持。

## 9. Unityでの手動確認手順

1. GameSceneを開き、EnemyのControllerでDecisionConfigと4AttackDataの参照を確認する。SOの4候補のAttackがNoneでないことと、StrengthがLight/Light/Heavy/Heavyであることを確認。
2. NearCandidatesで試す攻撃だけWeight=50、残り3つ=0、StepBack=0、RepeatPenalty=1とする。4攻撃を1種類ずつユーザー側でPlayして、意図したTrigger・攻撃Motion・終了後の再攻撃を確認。SOはPlay中の変更も残るため元値をメモしておく。
3. 各攻撃で武器判定ON/OFF、Light10/Heavy20を基準とする命中、HeavyのSwordTrail・被弾側HitStop/HitReactionを比較する。RunSessionの攻撃補正があるので実表示ダメージは基準値と異なることがある。
4. 4候補を12.5ずつ、StepBack10、RepeatPenalty0.25、PerAttack OFFへ戻し、近距離の攻撃/後退、3～12のApproach/WaitWalk、12超のApproachが従来の傾向か確認。
5. PerAttack ONも比較し、同じ攻撃の連続だけが抑えられることを確認。最終的には好みの設定へ戻す。
6. Wait/Approach/WaitWalk/StepBackを各距離帯で単独候補にして、移動・待機・バック回避の終了が止まらないことを確認。
7. テスト用に複製したAttackDataでTrigger名不正・武器Index不正を試し、警告後にAIが永久Busyにならないことを確認。元アセットには不正値を残さない。
8. 既存のジャスト回避→追撃2連撃、通常被弾、死亡/Final Blow、勝敗/Retryと修練を確認。プレイヤー操作・VFX・カメラ・UIが従来どおりか確認。
9. 次段階用のClipはInspector Previewで再生。コンボの斬撃回数と段間の間、突進の予備動作/前方移動/硬直、被弾方向と反応量を記録する。UAL2はリターゲット検証後に敵で比較する。

Search式は `t:AnimationClip`。Clip名で絞り込んでよい。Searchを開くコマンドはタイムアウトしたため表示成功とは報告していない。
