追記（2026-10-16）：この文書は前回のEdit Mode調査時点の記録です。その後、実Play Modeで検証し追加修正しました。最新の結果・比較画像・制約は [PlayModeCFXValidation.md](PlayModeCFXValidation.md) を参照してください。

Cut Line／Blood VFXの視認性改修（2026-10-16）

今回の変更は専用CFXの描画・粒子・Materialに限定した。HitReaction、HitStop、段階識別、通常Light／Heavy、SwordTrail本体、ダメージ、入力、Animation Event、Hitbox、Root Motion、AI、UI、Scene進行、Final Blowのコードと設定は今回の開始時から維持している。

弱く見えていた原因：

- Cut Lineは実SweepDirectionを軸とする3D平面で、奥行き方向の斬りほど投影長が短くなり、敵の表面と交差して隠れる部分も多かった。
- コアはワールド寸法だけで幅を決めていたため、離れたカメラでは細くなりすぎた。線の両端へ向かう減衰も強く、名目上の長さより鮮明な区間が短かった。
- 接触光は発生直後から減衰しており、30fps相当の次フレームではピークがかなり落ちていた。
- 血の主筋は細く短く、従来Shaderには赤草から輪郭を分離する暗い縁がなかった。背景と近い赤の上では色差だけに依存していた。
- 調査時のSceneのBloomはintensity 0.007、threshold 0.71。今回の主因はBloomの過剰な白飛びより、投影・線幅・粒子形状・輪郭と判断した。Bloomや露出の設定は変更していない。

| 項目 | 1撃目 | 2撃目 |
| --- | --- | --- |
| Cut | 先細りを残し、白銀コアの鮮明な区間を拡張 | より長いコアと強い中心光、決定打の保持時間 |
| Cut用Quadの長さ上限 | 1.25→1.55m | 1.7→1.95m |
| 投影長の下限 | 上限の70% | 上限の75% |
| コアのピクセル半径下限 | 1.25px | 1.6px |
| コア保持／消失 | 0.08秒／0.185秒 | 0.155秒／0.25秒 |
| 接触光の保持／消失 | 0.033秒／0.095秒 | 0.05秒／0.12秒 |
| 薄い青白い残光の終端 | 0.215秒 | 0.3秒 |
| 深紅の主筋のサイズ | 0.045～0.065m | 0.055～0.08m |
| 深紅の主筋の速度 | 3.8～5.0m/s | 4.8～6.6m/s |
| 主筋の寿命 | 0.2～0.27秒 | 0.24～0.35秒 |
| 暗赤の補助滴 | 0.045～0.075m | 0.06～0.095m |
| 小滴の遅延 | 0.025秒 | 0.055秒 |
| 粒子数 | 主筋10／暗赤滴6／小滴7を維持 | 主筋15／暗赤滴9／小滴15を維持 |

CutはHitPointを中心とする接触演出として、SweepDirectionを視線に垂直な面へ投影した向きに描く。画面上での向きを維持しつつ、奥行き方向でも短くなりすぎない範囲を設けた。完全に視線と平行な斬りでは画面上の方向が定まらないため、法線、次いでカメラ右方向へフォールバックする。血の飛ぶ方向とSwordTrailは元の3D SweepDirectionのまま。Cutだけ視線方向へ0.025mずらして接触面との重なりを避ける。壁や身体を透過する描画にはしていない。

Cut Shaderはピクセル微分から距離に応じた最低幅を確保し、細いコアと暗い輪郭を維持する。長さだけを増やすのではなく、先細りの両端と鮮明な中央区間を作り直した。Contact Flashには短い保持区間と細い交差光を追加し、Cutの中心に接触点を残す。2撃目はHitStop 0.155秒の終わりまでコアを保持し、その後に血と残光が残る。通常のTrailやカメラ構図は変えていない。

Bloodは追撃専用のJustAvoidCounterBlood.shaderを新設し、既存の専用Materialだけを接続した。通常攻撃が使うHitBlood.shader／HitBlood.matは変更していない。形状は細い尾と湿った先端を持つ非対称の滴。明るい深紅の主筋に、赤黒い縁と控えめな深紅の反射筋を加えた。暗赤の滴と小滴も同じShaderを共有する。透明合成でRGBを0～1へ制限し、加算発光・赤い霧・煙にはしていない。粒子数は維持し、主筋の幅・伸び・寿命を優先して改修した。

ParticleSystemは従来どおりWorld Space、Cone、非Loop、useUnscaledTime、固定プール。速度はStartSpeedとCone方向から与え、Velocity over Lifetimeは従来どおり無効。1撃目の主筋は約0.012秒、2撃目は約0.018秒だけ遅らせ、接触光から血へつなぐ。HitStopそのものは0.045／0.155秒を維持した。

今回の変更ファイル：

- Script更新：Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs
- Shader更新：Assets/Mock/Resources/JustAvoidCounterCut.shader
- Shader追加：Assets/Mock/Effects/JustAvoidCounterBlood.shader（meta追加）
- Prefab更新：Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab
- Prefab更新：Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab
- Material更新：Assets/Mock/Effects/JustAvoidCounterFirstCut.mat
- Material更新：Assets/Mock/Effects/JustAvoidCounterCut.mat
- Material更新：Assets/Mock/Effects/JustAvoidCounterBlood.mat
- 比較資料：Artifacts/JustAvoidCounter/Visibility/
- 本記録：CutBloodVisibilityChanges.md

Config変更はなし。既存JustAvoidCounterConfig.assetの両Prefab参照と、VFXConfigからの接続を再利用している。Git statusには過去の未コミット実装も残っているため、今回の変更一覧とは一致しない。

Inspector調整先：

- 両CFXのJustAvoidCounterCutVFX：_cutSize、_minimumProjectedLength、_surfaceOffset、_duration。
- 両Cut Material：_MinCorePixels、_CoreGain、_CoreWidth、_SweepDuration、_LineHold、_LineDuration、_FlashHold、_FlashDuration、_FlashStrength、_GlowDuration、_EdgeContrast。
- Blood Material：_RimColor、_RimWidth、_VeinColor、_VeinStrength。深紅と暗い輪郭を通常攻撃と独立して調整できる。
- 各PrefabのCrimson Threads／Dark Blood Drops／Late Fine Drops：サイズ、速度、寿命、色、Stretch、遅延、Cone角度。

確認した内容：

- Unity 6000.2.7f2でCompile完了。実際の描画後も両専用Shaderのエラーなし。
- Save→同期Import→再取得により、両Prefab／Material／Shaderの接続、粒子数、World Spaceと演出時計を確認。Sceneは変更保存していない。
- 既存の段階識別からDamageInfo、CombatFeedback、対応する専用CFXへ届く経路をEdit Modeのコールバック検証で再確認。Physicsを実行した命中確認ではない。
- Cutの投影方向、Bloodの実3D方向、奥行き方向での長さ下限、同じオブジェクトの再利用と粒子消去を確認。
- 変更前／後それぞれ432枚を描画。明・暗・赤の背景色×近／遠×30／60fps相当のサンプル時刻×Cutのみ／Bloodのみ／全CFXを比較。
- 実GameScene背景でも、既存ロックオン構図、キャラクターのサンプル姿勢、SwordTrail履歴を使ってBefore／After各12枚を描画。加えてCutのみ12枚、Bloodのみ12枚を描画した。
- 今回開始時のハッシュと照合し、CFX表示Script以外の既存Script、Config、攻撃Clip、Animator、Player/Enemy Prefab、通常の血・Trail資産が変わっていないことを確認。

Before／Afterで読み取れた差：1撃目は以前見えにくかった実GameSceneの33msサンプルでも白銀の線を確認でき、遠いカメラでもコアの輪郭が残る。2撃目は0.2秒サンプルでもコアが残り、太めの血の筋へ視線がつながる。深紅の血は暗い輪郭によって赤い背景から分離し、0.2秒付近の余韻も増えた。

補助的な画素集計では、赤い背景・遠距離・100msの1撃目で明るいCut画素が52→208、2撃目200msで0→229だった。血の明るい画素はすべての瞬間に一律増加したわけではなく、0.2秒では1撃目38→107、2撃目236→614だった。これは特定の比較条件の集計であり、実戦の見やすさや品質の評価値ではない。全結果はPixelComparison.jsonに記録した。

[CFX Before／After](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Visibility/BeforeAfter.png>)

[実GameScene Before／After](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Visibility/SceneBeforeAfter.jpg>)

[実GameSceneのCut／Blood／全演出の分離](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Visibility/SceneSeparated.jpg>)

[Cutのみ](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Visibility/CutOnly.png>)・[Bloodのみ](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Visibility/BloodOnly.png>)・[30／60fps相当](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Visibility/FrameSamples.png>)

[保存後の参照確認](</C:/Unity game/Katsuro/Artifacts/JustAvoidCounter/Visibility/SavedReferenceCheck.txt>)

未検証：Play Modeでの連続操作・実戦録画、実フレームレートでの視認性、カメラ追従中や大きな移動を伴う全方向の遮蔽、各地面・照明条件を実際に移動して行う通し確認。色背景の比較は草や地面のテクスチャを再現したものではない。実GameScene描画もEditorの姿勢サンプルであり、「ゲーム中で確認済み」とは扱っていない。最終確認では1撃目の最初の1～3フレーム、2撃目のHitStop中、赤草上の主筋と、終了後に長い白い帯が残っていないことを確認してほしい。

