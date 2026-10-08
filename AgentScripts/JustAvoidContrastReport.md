# Just Avoid モノクロ化・BGM Ducking

既存の `CombatFeedback.PlayJustAvoidFeedback(DamageInfo)` の成功通知に追加。成功判定・Slow Motion・HitStop・Counter・Camera FOV・残像・Shockwave・画面歪みは変更していません。

## 設定

| 演出 | 移行 | 保持 | 復帰 | 最大効果 |
|---|---:|---:|---:|---|
| 世界のモノクロ | 0.02秒 | 0.08秒 | 0.20秒 | Saturation -100 |
| BGM Duck | 0.03秒 | 0.10秒 | 0.18秒 | 各チャンネルの基準音量の25% |

時間は unscaledTime。CameraManager の JustAvoidContrast、AudioManager の Just Avoid BGM Duck でInspector調整できます。最大彩度は専用Profileの ColorAdjustments.saturation で調整できます。

## HDRP / HUD

CameraManager Prefabに専用Global Volumeを追加。Default layer、Priority 100、通常Weight 0。GameScene Main CameraのVolume MaskはDefaultを含み、既存VolumeのPriorityは0。専用ProfileはSaturationだけOverrideし、既存Global Volume Profileは変更していません。ランタイムProfile生成や既存Profileへの書き込みは不要です。

GameSceneのHUD CanvasはScreen Space OverlayなのでPost Process対象外。既存歪みはAfterPostProcessのまま、モノクロ化後の画面を歪めます。世界内の残像・接触フラッシュもモノクロ対象になります。シアン残像だけをカラーで描く変更は行っていません。描画結果の目視確認は未実施です。

## Audio / 中断

AudioManager内部でAudioSourceごとの基準音量とDucking倍率を分離。PlayBGM / SetBGMVolumeを倍率に対応させ、音量変更後の基準値へ復帰。チャンネルごとの設定と停止状態を維持します。独立した環境音管理やクロスフェード処理は現在のAudioManagerにないため、対象はBGMのみ。SEプール・AudioListener.volume・LowPassは変更していません。

現在のAudioConfig.JustAvoidSoundは空欄です。既存成功SE呼び出しを維持しており、登録済みSE名を設定すると従来どおり通常音量で再生されます。今回、音素材の追加・差し替えはしていません。

連続成功は現在値から再開し累積しません。カウンターなしでも自動復帰。Victory / Defeat / Title、シーン切替・Unload、所有オブジェクト無効化・破棄で解除。FinalBlow開始時に即解除し、FinalBlow再生中は新しいモノクロ/Duck開始を拒否します。復帰処理は音量だけを戻し、停止済みBGMを再生しません。

## 今回の変更ファイル

- Assets/Mock/Scripts/Feedback/JustAvoidContrast.cs（新規）
- Assets/Mock/Scripts/Feedback/JustAvoidEnvelope.cs（新規）
- Assets/Mock/Scripts/Feedback/CombatFeedback.cs（成功通知連動と無効化時解除）
- Assets/Mock/Scripts/Managers/AudioManager.cs（基準音量・Duck・中断処理）
- Assets/Mock/Scripts/Managers/FinalBlowManager.cs（優先解除と再生状態公開）
- Assets/Mock/Prefabs/Manager/CameraManager.prefab
- Assets/Mock/Prefabs/Manager/AudioManager.prefab
- Assets/Mock/ScriptableObjects/JustAvoidMonochrome.asset（新規）
- 新規Asset/Scriptの.meta、AgentScriptsの調査・設定・検証ファイル

## 検証

Unity CLI経由で現状取得・設定・保存・Prefab/Profile再取得を実施。GameSceneはPreview Sceneで確認し、現在のTitleSceneを切り替えていません。

コンパイル completed / failed=false / errors=[]。Unity C#によるEdit Mode検証に合格：タイミング、復帰中の連続発動、1000回再開始の非累積、3チャンネルの基準音量、Duck中のPlayBGM/SetBGMVolume、正確な復帰、停止BGM維持、SE/Listener非変更、中断ハンドラ、Victory時の再開始拒否、GameSceneのPrefab継承、保存値の再取得。非ExecuteAlwaysコンポーネントの無効化ハンドラは反射で明示呼び出しして検証しました。

Play Modeテスト・実描画/聴感確認は未実施です。
