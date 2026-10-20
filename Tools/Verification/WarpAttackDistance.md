# WarpAttackの距離設定と検証（2026-10-20）

## 設定場所

`Assets/Mock/ScriptableObjects/Enemy/SpecialAttacks/WarpAttack.asset` をInspectorで選択する。

- `WarpAttack only > Teleport Distance`：Player背後の出現目標までの距離（m）。攻撃射程・発動距離ではない。
- `Teleport Angle Offset`：Playerの水平な背後方向を0度とした出現角度（度）。
- `Selection / travel > Maximum Travel Distance`：出現後の攻撃Root Motionの累計移動上限（m）。テレポート自体の距離は含まず、0は上限なし。

設定元の重複は追加していない。`EnemyWarpDestination.TryFind` が既存の `pattern.TeleportDistance` と `TeleportAngleOffset` を参照する。
コードの既定値は2.5mを維持。作業開始時のAssetも2.5mだったが、作業中にInspector側で1.5mへ変更されたため、そのユーザー調整は上書きしていない。
`WarpAttackData.Range` は現行の攻撃実行経路では未使用であり、ワープ距離には流用していない。Tooltipに設定先を明記した。

## AIの距離条件

GameSceneのEnemyが実際に参照している設定は `Assets/Mock/ScriptableObjects/Enemy/EnemyDecisionConfig.asset`。
確認時の値は `NearDistance = 3`、`FarDistance = 12`。

Midは **`NearDistance < Playerとの3D距離 <= FarDistance`**。
`MidCandidates` のWarpAttackはこの距離帯を優先し、Patternの `MinAttackDistance / MaxAttackDistance` による二重の除外を行わない。
抽選処理とControllerの適格性確認・攻撃開始の両方が、同じ `IsInSelectionRange` を使う。

既存のMid Warp Weight 20、Far Warp Weight 2、他候補のWeight、RepeatPenalty、Cooldownは変更していない。
Farに登録済みのWarpを削除したりMid専用に強制したりせず、Farでは従来のPattern Min/Maxを維持する。
Nearの候補と距離条件、Rush・通常コンボなど他の攻撃のMin/Maxも維持する。
無効なPattern、Animator・AttackData・武器、死亡・行動制限、安全でない出現先を許可する変更はしていない。

## 今回変更した実行コード

| ファイル（Assets/Mock/Scripts/ 以下） | 変更 |
| --- | --- |
| Enemy/Config/EnemyAttackPattern.cs | 共有の距離条件を追加。既存3項目とMin/MaxのTooltipを明確化 |
| Enemy/EnemyConfig/EnemyDecisionConfig.cs | 既存境界に一致する `IsMidDistance` を追加 |
| Enemy/EnemyAI/EnemyDecisionMaker.cs | 抽選の距離制限に共有条件を使用 |
| Enemy/EnemyController.Pattern.cs | 適格性確認・開始時にも共有条件を使用 |
| Enemy/Config/EnemyAttackData.cs | 未使用Rangeとワープ距離の違いを説明 |

`EnemyController.Warp.cs`、`EnemyWarpDestination.cs`、移動・Hitbox処理、Animator、Clip、VFX、Prefabには今回の実装変更を加えていない。
シリアライズするフィールドの追加・改名はなく、既存GUIDを維持した。Asset保存・Scene保存も行っていない。
開始前のgit status / diffは空。作業中に生じたWarpAttack.asset、WarpAttackData.asset、GatotsuRushVFXConfig.csの別途の調整は保護した。

## 検証

- **Edit Mode：30項目成功**。接続中Editor内で実行する検証スクリプトであり、NUnit Test Runnerの結果ではない。
  - 2.0 / 2.5 / 3.0mで、安全なNavMesh上の出現目標がSO値に追従。
  - Mid境界、AI閾値変更、狭いPattern Min/Max、Far・Nearの既存制限。
  - Weight比率、RepeatPenalty、Controllerによる拒否、無効なPattern。
  - Rush・通常コンボのMin/Max、Backstepの抽選。
  - 出現角度、未Bakeの壁、Playerとの重なり、NavMesh外、占有された地点の拒否。
- **GameScene Play Mode：15項目成功、238フレーム進行**。
  - 一時SOコピーでPattern Min/Maxを25〜26mにしても、実Controllerを通してMidのWarpを抽選・開始。
  - Cooldownによる再選択拒否。
  - 黒霧、非表示、実座標移動、出現VFX、Animation EventによるHitboxと一閃、移動・表示の復帰。
  - 出現後Root Motionの累計は約1.199mで、既存の2m上限以内。
  - 無効地点で開始を拒否。消失中に出現先を無効化した場合も安全にキャンセルして復帰。
- 最終状態：Play Mode停止、runInBackgroundを元のfalseへ復元、Scene未変更、コンパイルエラーなし、現在のConsoleエラー0。
- 設定変更には一時SOコピーと一時NavMeshを使用し、元のAssetへ書き戻していない。

再実行：Edit Modeで `WarpAttackDistanceChecks.cs` の先頭にあるCLIコマンドを実行する。
Play Mode確認はGameSceneを開始して `WarpAttackPlayChecks.cs` の先頭のコマンドを実行し、確認後にPlay Modeを停止する。
結果は同じディレクトリの `Results/WarpAttackDistance.EditMode.json` と `Results/WarpAttackDistance.PlayMode.json` に保存される。

## 残る確認・設計上の注意

- FarのWarp登録も引き続き有効。Midだけに完全限定したい場合は、別途Far候補を編集する必要がある。今回そのWeightや登録は変更していない。
- Min/Maxが逆転するなど、Pattern自体が不正な場合はMidでも無効。距離条件の免除で不正なPatternを実行可能にはしない。
- TeleportDistanceは目標値。NavMeshやColliderの制約により、安全な地点が見つからなければ中止する。
- 追跡中のPlayerが距離帯を越えれば、実際の開始時点の条件で再確認する。Midで一度選ばれたことを永久の発動許可にはしない。
- Play Mode検証では候補を一時コピーで限定した。自然な戦闘での長時間の発動頻度、全通常攻撃の連続プレイ、見た目の品質の再評価は行っていない。
- コミット・Pushは行っていない。
