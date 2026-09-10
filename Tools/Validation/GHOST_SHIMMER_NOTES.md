# 幽体化の透明感・陽炎表現（2026-09-10）

## 変更
- CombatGlow.shaderの幽体化分岐を変更。青い発光・明滅を抑え、薄い灰青色の輪郭と透けた胴体を描く。
- 頂点を小さく横方向に揺らし、上へ流れる波で透明度を変化させる。背景を屈折させる方式ではなく、輪郭と濃淡による陽炎風の表現。
- CombatFeedbackからunscaledTimeを渡すため、ジャスト回避のスロー中も揺らぎを維持。
- 剣の軌跡と被弾フラッシュのシェーダー分岐は維持。
- 追加テクスチャ、画面コピー、毎フレームのメッシュ生成は行わない。

## Inspector調整
プレイヤーのCombatFeedbackコンポーネントの「Ghost shimmer」：
- Ghost Tint：色と透明度。Aの初期値0.35。下げるほど薄くなる。
- Ghost Sway：揺れ幅。初期値0.009（モデルのローカル座標単位）。
- Ghost Flow Speed：流れる速さ。初期値1.2。

未配置の場合はPlayerControllerが起動時に自動追加する。再生前に保存したい場合は、PlayerControllerと同じオブジェクトにCombatFeedbackを追加して調整する。既に付いている場合は自動追加されない。

## 検証
- 本体C#コンパイル成功。
- HDRPシェーダー検証成功（SHADER_RESULT: passed; supported=True）。Tools/Validation/Run-ShaderValidation.ps1で実行。ログはTemp/shader-validation.log。
- 実際のプレイヤーモデルと戦闘画面での見え方は未確認。
