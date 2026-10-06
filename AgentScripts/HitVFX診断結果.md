# HitVFXが見えない問題の診断

## 確認できたこと

- Unity Editor接続でGameScene.unityとMock.unityを読み取り用Preview Sceneとして取得。
- 両シーンともGameManagerにPlayer/Enemy/Camera参照あり。
- 両シーンのPlayerはVFXConfig.assetを参照し、そのHitVFXはSwordHitVFX.prefab。
- Player/EnemyのCombatFeedbackは存在し、有効。
- CameraのCulling Mask=-1でLayer 0のHitVFXを描画対象に含む。
- Prefab Root/子Particleはactive。各Rendererはenabled。
- Emission Burst probability=1。必要なPosition/Normal/Color/UV頂点ストリームあり。
- Edit Modeで固定乱数42、t=0.05秒までシミュレートし、Main 4本、Droplets 9粒、Mist 2粒が実際に生成。
- HDRP StandardRequestで隔離Preview Sceneから描画成功。Shaderによる完全な非表示ではない。

## 実描画で分かった弱点

同じt=0.075秒の血飛沫を、768x512解像度・縦5mの正投影画角・暗い単色背景で描画すると、赤い描画領域は187px、エフェクト全体の高さは9pxだった。

Mainは狭いConeで同じ方向へ4本出るため重なり、引いた画角では細い1本に見える。色差と短い時間も重なって、ゲーム中の動きの中では読み取りづらい構成と判断する。単純な設定漏れより、画面上の面積と形状の問題を示す結果。

画像はRenderTextureのLinear色をsRGBへ変換してPNG化。
- HitVFX-diagnostic.png: 縦1.1mの近接プレビュー。
- HitVFX-game-scale.png: 縦5mの引いたプレビュー。

## 未確認

Play Modeは未実施。ゲーム内の命中時にCombatFeedback.Hit/PlayHitVFXが実際に呼ばれたか、キャラクターによる遮蔽、実カメラ・Volume・動く背景を含む見え方は未確認。上の5m画角は診断用であり、実ゲーム画面のキャプチャではない。

今回の診断は参照・生成・描画を切り分けたもの。実際のゲーム中で再生されていると断言するものではない。
