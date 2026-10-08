# Katsuro 通常攻撃HitReaction / Hit Feedback改修

敵の通常Hitを、行動を中断しないボーン演出として調整。Base Layer・AI・NavMesh・攻撃Root Motion・Hitbox・Animation Eventは通常Hitから変更していない。Play Modeは未実施。

## 調査結果と原因

変更前のEnemy CombatFeedbackはLight5°、Heavy10°、共通.18秒、Chest1ボーン。本体Hit Flashは.055秒・Tint alpha .9で元Materialと差し替えていた。Playerは5°/10°/.18秒、Flash .09秒。

既存Reactionの係数remaining×sin(remaining×π)は最大約.58。設定5°のLightはChest実効ピークが約2.9°となり、開始直後は0だった。またTime.unscaledTimeで進むため、AnimatorがHitStopで停止している間にもReactionが消化される。旧オフセットを逆回転で除去する方式は、Animatorが既に新Poseを評価したタイミングではその新Poseへ古い逆回転を掛ける可能性がある。

EnemyControllerの通常Hitはボーン演出、Just Avoid CounterのみInterruptAttackForHitReaction・全身Reaction・Movement保持へ分岐する構造を確認。この分岐を維持した。

## 通常Reaction設定

| 項目 | Light | Heavy |
|---|---:|---:|
| 全ボーンへ分配する基準角度 | 7.5° | 13° |
| 主反応時間 | .16秒 | .22秒 |
| 主ピーク時刻 | .0288秒 | .0396秒 |
| Chest単体ピーク | 5.85° | 10.14° |
| 攻撃中の角度倍率 | .55 | .55 |
| 通常本体Flash alpha | .18 | .28 |
| 本体Flash時間 | .055秒（維持） | .055秒（維持） |

時間はAnimator.speed=1時。首・頭の遅れを含む最終復帰は主反応時間＋最大.020秒。

実Humanoid Avatarに存在するSpine（Spine02）、Chest（Spine01）、UpperChest（Spine）、Neck（neck）、Headを使用。角度分配は10% / 78% / 6% / 4% / 2%。UpperChestは.006秒、Neckは.012秒、Headは.020秒遅れて追従。腰・Root・肩・腕・手へ直接オフセットを加えない。肩には胸・UpperChestから自然に伝わる。存在しないボーンは除外し、利用可能なボーン間で重みを正規化する。

Envelopeは命中時28%の小さな衝撃、反応時間の18%地点で100%、残りをSmoothStepで0へ復帰。首・頭は自身の遅れ時間を使う。HitStop中はAnimator.speed=0によりReaction時計を停止し、初期Impactを表示したまま保持。解除後にピークとRecoveryを継続する。既存HitStop時間・AnimationSpeedControllerには変更なし。

方向は攻撃元から敵へ向かう水平ベクトルを優先し、HitNormal、HitPoint、後方へフォールバック。Enemyローカル座標へ変換し、上方向との外積から傾き軸を決定。各ボーンの親座標へ変換して適用する。正面→後方、背後→前方、左→右、右→左を実Skeletonでも確認。

## 非累積と連続Hit

EnemyBoneHitReactionはAnimatorが作った各ボーンのベースPoseと適用済みPoseを記録する。次の適用前に、現在Poseが記録した適用済みPoseと一致する場合だけベースPoseへ復元。Animatorが新Poseを書いた場合は古い逆回転を引かない。

連続Hitでは前の表示角度から新しい目標へ.025秒で接続する。反応角度を加算せず、最大16°以下の目標をボーンへ分配する。終了、無効化、Counter開始、死亡時に解除。通常Hit中にCounterが開始された場合は、既存全身Reactionの処理より先に通常オフセットと通常Flashを解除する。

## Flashと既存演出

Enemyの通常本体Flashだけを薄いSkinnedMeshオーバーレイへ変更。元Materialを差し替えず、元の身体の明暗・形状を残す。既存CombatGlow Shader、同じMesh・bones・rootBoneを利用し、BlendShapeも追従する。新しいParticle VFXやShaderは追加していない。

接触Flash、Hit VFX / Blood VFX、Light/Heavy Hit SE、HitStop、Camera Shakeの呼出と設定は維持。Counter本体Flashは既存経路を保持。Playerは敵専用ヘルパー・オーバーレイを作らず、既存Chest反応とFlash経路を保持する。

## Editor検証

- 実Avatar取得：Spine / Chest / UpperChest / Neck / Head / 両Shoulderが存在。
- 4方向×4 yaw＝16ケースで、ローカル方向と実頭部移動が攻撃元から離れることを確認。
- Light / HeavyのChestピーク5.85° / 10.14°、攻撃中55%の反応量を確認。
- Light→Heavy接続でPoseジャンプなし、100フレーム停止でReaction時計とPoseが不変。
- 1000回の連続Hitで角度が無制限に増えず、終了後に元Poseへ復帰。
- Animatorが新Poseを書いた場合、そのPoseへ古い逆回転を加えないことを確認。
- Root位置・回転、Hips回転が変わらないことを確認。
- EnemyController.ApplyDamageを依存注入して検証：通常Light/Heavy後もBase LayerのState、攻撃所有Hash、Root Motionフラグ、Hitbox有効フラグ、予約行動、NavMesh設定を維持。Counterのみ既存の全身Reaction・攻撃中断を実行。死亡時に通常/全身反応を解除。
- 実Meshで通常FlashのMaterial不変、alpha .18/.28、Mesh/bones/rootBone参照、解除を確認。
- Playerの5°/10°/.18秒の既存処理を確認。
- 作業開始時と保存後の指紋比較：Player/Enemy Animator Controller、全使用Animation Clip（Eventを含む）、Player Prefab、VFXConfigが不変。前タスクの未コミット変更も今回の作業開始時の状態を維持。
- Compile Errorなし。Enemy Prefab保存成功。Unityプラグインから保存後のAnimator / Prefab / VFX / ボーン設定を再取得済み。

これらはEdit Modeの数値・手動評価・依存注入による検証。ゲームループ、実物理衝突、AI判断の実行、Event配送、レンダリングの見た目を検証したものではない。

## 変更ファイル

- Assets/Mock/Scripts/Feedback/CombatFeedback.cs
- Assets/Mock/Scripts/Feedback/EnemyBoneHitReaction.cs（新規、meta含む）
- Assets/Mock/Scripts/Enemy/EnemyController.cs（攻撃中状態の読み取り、Counter/Death時の通常演出解除のみ）
- Assets/Mock/Prefabs/Enemy.prefab（通常反応と通常Flash設定のみ）
- AgentScriptsの調査・設定・検証スクリプトと結果記録。

今回Animator Controller・Animation Clip・Animation Event・Player Prefab・Player Combo・EnemyMover・EnemyAttacker・AI・Root Motion・ダメージ・HitStop時間・Counter固有設定・VFX・SE・Camera・UIは変更していない。

## Play Modeでの確認項目

通常Light/Heavyを敵の攻撃中に連続命中させ、既存HitStop後も敵の攻撃と攻撃判定が続くこと、追跡/WaitWalkが継続すること、胸と頭の反応量および武器軌道が自然なことを確認する。Counterへの切替、死亡、無効化後に通常オフセットやFlashが残らないことを確認する。黒い背景・キャラクターに対するFlashと身体の見え方は未検証で、必要なら今回のEnemy専用値で調整する。
