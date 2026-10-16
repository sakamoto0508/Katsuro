ジャスト回避追撃2連撃：斬撃表現の再設計（2026-10-16）

今回の差分は、既存の1撃目／2撃目識別を維持し、1撃目にも専用CFXを設け、2撃目の接触・血・停止姿勢・解除後の崩れを時間で分けたこと。下記の画像はEditorで実際に描画して確認したもの。Play Modeでの実戦の見た目を保証するものではない。

現行実装で不足していた点：

- 1撃目は通常Light相当の短いSlashLineへ戻る分岐になっており、専用の斬撃光がなかった。
- 従来の血は暗赤の2系統のみで、接触直後の深紅と遅れて広がる滴の区別が弱かった。
- 2撃目はHitReactionの0.08秒の遷移を要求してAnimator.Update(0)を呼んでいた。実Prefabの検証では遷移中のまま、停止直前の胸の姿勢差が0度だった。
- 既存の白い線は、先端の通過と残光が一つの表示になっていた。

| 表現 | 1撃目 | 2撃目 |
| --- | --- | --- |
| 専用Prefab | JustAvoidCounterFirstCutCFX（新規） | JustAvoidCounterFinisherCFX（再制作） |
| Cut Line | 接触から0.04秒で走る、先細りの白銀コア | 0.065秒で走り、先端光と長めのコア保持で斬り抜けを示す |
| 切断線の最大長 | 1.25mのQuad内に先細り形状 | 1.7mのQuad内に先細り形状 |
| 接触Flash | 0.085秒、細い刃状 | 0.11秒、強い局所光 |
| コア／残光の終端 | 0.18秒／0.24秒 | 0.245秒／0.34秒 |
| 血 | 深紅の筋10粒＋暗赤の大滴6粒＋小滴7粒 | 深紅の筋15粒＋暗赤の大滴9粒＋小滴15粒 |
| 遅れて広がる血滴 | 0.025秒遅延 | 0.075秒遅延、最長0.43秒の寿命 |
| Blood全体の方向 | SweepDirectionに沿う8～19度のCone | SweepDirectionに沿う10～24度のCone |
| 被弾 | 既存Light方向別Clipの衝撃位置＋胸・頭の鋭い補正 | 既存Heavy方向別Clipの衝撃位置＋胸・肩・頭・腰の連動補正 |
| 表示上の反応 | 0.18秒から減衰 | 0.4秒から減衰、解除後0.14秒付近でFollow Throughが最大 |
| HitStop | 0.045秒を維持 | 0.155秒を維持 |

両段とも専用Prefabの4個プールから再生する。実命中で使用する段階は引き続きJustAvoidCounterSpeedState._strike → Hitbox ON時の確定 → DamageInfoという経路。専用CFXがある場合は既存のContactPulse／通常HitVFXを重ねない。通常Light／Heavyは従来の経路を維持する。

白銀の線は既存JustAvoidCounterCut.shaderを改修し、1枚のMeshで先端・細いコア・薄い青白の残光・小さい接触光を描く。先端が接触位置からSweepDirectionへ進み、コアが先に消えて残光が短く続く。明るい背景では暗い縁が輪郭を支える。2撃目だけ空気の細い筋を強める。新しいShaderは増やしていない。線の軸は実SweepDirectionを保持し、血も同じ方向へ出す。奥行き方向の斬りは視線による短縮を受けるため、実戦での向き依存性は確認が必要。

血の専用Material JustAvoidCounterBlood.matを追加した。既存HitBlood.shaderを共有し、通常攻撃のMaterialは変更していない。深紅の筋はRGBおよそ(0.68～0.88, 0.018～0.045, 0.035～0.07)、大滴は(0.2～0.4, 0.007～0.014, 0.018～0.03)。縁に深紅の色差をつける透明合成で、加算発光や全方位の爆発にはしていない。

被弾姿勢は、専用Layerの既存Clipを正規化位置0.05で直接評価してからHitStopに入る。新しい被弾ClipやAnimator Stateは追加していない。追加したCounterHitPoseは、実斬撃方向と攻撃者方向から胸・肩・頭・腰の補正を作る。命中直後の反動をHitStop中に保持し、解除後は既存Reaction時計で倒れ込みへつなぎ、補正を除去して復帰する。四方向の既存Clipを利用し、Root位置・回転を動かさない。元の敵行動拘束時間（このPrefabでは0.4＋0.08秒）は両段とも維持した。

SwordTrailは実刀軌道の表示を継続する。Configから1撃目のコアを少し読み取りやすくし、2撃目の長い白い軌跡を避けるため残光の最大寿命を0.14秒へ抑えた。接触の切断線とは別の表示。通常TrailのShader／Material／設定は変更していない。追撃の振りかぶり0.78倍・斬撃1.10倍も維持し、Animation EventとHitbox区間は変更していない。今回SE・Cameraの設定値は前回実装から変更していない。

今回変更・追加したファイル（プロジェクトルートからのパス）：

- 更新Script：Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs
- 更新Script：Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs
- 更新Script：Assets/Mock/Scripts/Feedback/CombatFeedback.cs
- 更新Script：Assets/Mock/Scripts/Enemy/Animation/EnemyAnimationController.cs
- 追加Script：Assets/Mock/Scripts/Feedback/CounterHitPose.cs
- 追加Prefab：Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab
- 更新Prefab：Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab
- 追加Material：Assets/Mock/Effects/JustAvoidCounterFirstCut.mat
- 追加Material：Assets/Mock/Effects/JustAvoidCounterBlood.mat
- 更新Material：Assets/Mock/Effects/JustAvoidCounterCut.mat
- 更新Shader：Assets/Mock/Resources/JustAvoidCounterCut.shader
- 更新ScriptableObject：Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset
- 改修記録：JustAvoidCounterChanges.md
- 比較画像・検証結果：Artifacts/JustAvoidCounter/

新規Unityファイルにはmetaも追加。前回からの未コミット変更は保持しているため、Git statusには上記以外の前回実装も表示される。PlayerAttacker、PlayerWeapon、DamageInfo、JustAvoidCounterAnimation、JustAvoidCounterSpeedState、SwordTrail、各Config、Player/Enemy Prefab、Animatorは今回も調査し、必要な既存識別と参照を再利用した。

検証結果：

- Unity 6000.2.7f2で最終コンパイル完了、エラーなし。Shaderは描画後もエラーなし。
- 保存後にAssetを再取得し、VFXConfig→CounterConfig→両CFX、5個のPlayer Trail、GameSceneのEnemy Reaction参照を確認。両CFXのMissing Scriptなし。
- 両追撃の既存Animation Event名・時刻列が変更前のEditor取得結果と一致。追加Eventなし。
- Edit Modeのコールバック検証で、実StateのBehaviourから既存WeaponHitboxRelay、PlayerAttacker、DamageInfo、CombatFeedbackまで通し、1／2段目が対応する専用CFXへ届くこと、SweepDirectionの維持、同一対象への重複命中抑止を確認。State callbackと接触通知は検証側から呼んでおり、実行中のPhysicsによる衝突テストではない。
- 2撃目の停止直前の胸の姿勢差は約16度。遷移待ちなし、停止中の姿勢保持、解除後の姿勢変化、Cancel時のLayer／補正解除を確認。
- 両段×四方向でRoot位置・回転を維持し、元の0.48秒の拘束時間で終了することを確認。
- CFXは同一オブジェクトを再利用。粒子停止・消去、寿命、Pause／Resumeを確認。
- 変更前／後それぞれ432枚を独立描画。明・暗・赤の背景色、近／遠、30／60fps相当のサンプル時刻、斬撃線のみ／血のみ／全CFXを比較。背景色は視認性の対照条件であり、実際の赤い草の画像ではない。
- 実GameSceneの草・照明、既存ロックオンの位置計算、実Player/Enemy Prefab、攻撃Clip、SwordTrail履歴と被弾姿勢を使った12枚のEditor描画を確認。シーンは保存変更していない。

比較資料：

- [Before / After](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/BeforeAfter.png>)
- [斬撃線のみ](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/CutOnly.png>)
- [血飛沫のみ](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/BloodOnly.png>)
- [全CFX](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Combined.png>)
- [30 / 60fps相当の表示時刻](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/FrameSamples.png>)
- [GameScene・1撃目・100ms](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Scene/Strike1-LockOn-100ms.png>)
- [GameScene・2撃目・100ms](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Scene/Strike2-LockOn-100ms.png>)
- [参照検証](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/ReferenceVerification.txt>)
- [命中経路・停止姿勢の検証](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/PipelineVerification.txt>)

未検証：Play Modeでの実操作、実戦でのカメラ追従と入力からの連続再生、実時間30／60fps録画、移動や回避を伴う全方向の遮蔽、Tutorial／Retry／Boss／致死とFinal Blowの通しプレイ。コード上では既存の死亡時CancelとFinal Blow経路を維持しているが、実戦で競合がないことまでは保証していない。最終録画では、最初の命中時から線が見えるか、赤い草の上で血が分離するか、2撃目の停止中と解除後に姿勢差があるかを優先して確認してほしい。

Inspectorの調整先：

- JustAvoidCounterConfig.asset：FirstCFX／FinisherCFX、段ごとのTrail・HitStop・ImpactPoseTime・PoseDuration・胸の衝撃とFollow Through・頭の反動・腰の補正・Follow Throughピーク。
- 両CFX Prefab：_cutSize、_duration、3層のBlood粒子数・速度・サイズ・寿命・Cone角度・遅延。
- 両Cut Material：銀色コア、青白い残光、線幅、走る時間、コア保持時間、Flash時間／強度、残光終端、空気の筋、明背景の輪郭。
- JustAvoidCounterBlood.mat：血の縁色とコントラスト。通常攻撃とは独立。
