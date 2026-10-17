# Katsuro：銀青のTrailと命中時の直線ストローク

実装・Asset設定・コンパイルと静的検証を完了。**GameSceneの実行、Play Mode、撮影、見た目の評価は行っていない。** 以下は初期設定であり、最終的な色・透明感・方向はユーザーの実画面で確認する。

## 今回の役割分担

- 弧状SwordTrail：刀の動きが分かる薄い銀青の補助。従来の履歴・補間・Meshを継続使用。
- 直線ストローク：命中時だけ出る主役。短い後端から長い前方へ抜ける、先細りの刃。
- Blood：既存の斬った結果。仕様・Assetとも変更なし。
- 旧Cut CFX：前回設定のQuad非表示と局所ContactPulseを維持。今回のストロークには旧Cut Quadを使わない。

## 色と質感

従来は広い弧のコア、白いエッジ、明るい面に命中増光が重なっていた。今回は弧のCore Widthと強度を下げ、白い発光を新しい側の30〜35%へ限定した。後方と外周、面の時間減衰も早めた。

面は白ではなく銀青のTintを使用。刀身方向に明暗の段差と細いハイライトを作り、平坦な発光面に見えにくくした。残光も同じ青系で急速に弱くなる。面全体に白いピークを加える処理は抑制した。

新しい直線Shaderは、細い白芯、片側の明るい前縁、伸びる先端の白いピーク、銀青の面を別々に計算する。白芯が先に消え、その後に薄い青い面と残光が消える。先端の光は、表示を前方へ伸ばすマスクと一緒に進む。

両Shaderとも `Blend One One / ColorMask RGB / Alpha=0 / ZWrite Off / ZTest LEqual` を使用。透明感は背景を残す低輝度の加算発光で表現する。RGBがない領域に不透明なAlphaだけが残る合成は追加していない。金属風の質感は形状と明暗分布による表現で、PBRの環境反射・屈折を新設したものではない。

## 直線Meshと方向

`CounterImpactStroke` は初期化時にMesh、配列、MaterialPropertyBlock、表示オブジェクトを1回だけ確保する。刀の共有Trail一つにつき1本を再利用し、毎フレーム・毎命中のInstantiateは追加しない。

Meshは長手9断面×幅方向3頂点＝27頂点、32三角形。幅を両端で0へ絞り、短い後端と長い前方を作る。命中点は長さの35%の位置を通る。旧接触Quadの拡大ではなく、方向と段階別の寸法から命中ごとに生成する刃状Meshである。

方向は既存の `DamageInfo.SlashDirection`（WeaponHitboxRelayのワールド空間Sweep）を基本にする。既存SwordTrailの直近最大4点から得たtip進行方向が同じ向きなら、Sweep 65% / 履歴35%で平均する。古い履歴が逆方向の場合は平均しない。Sweepがなければ履歴へ戻り、どちらもなければ誤方向の線を捏造せず省略する。

幅方向は命中時のカメラ方向を読み、実際の3D斬撃方向に直交する向きとして決定。斬撃方向自体を画面水平などに置換しない。カメラ設定や制御には変更なし。

命中後の頂点位置は固定。短い表示マスクの伸長と減衰だけを行い、前方へ移動する飛び道具にはしない。同じ刀で複数対象に命中した場合は、最新の命中1本へ更新する。

時間は `FeedbackVisualTime` を利用し、Pause中は停止する。直線の白芯は命中直後に強く、その後に減衰する。旧弧のHitStop中の履歴保持は維持するが、直線の表示年齢はHitStopでも進み、白から青い余韻へ変わる。HitStop時間を変更したり、新たな時間停止を要求したりしない。

既存の表示Reset/Disableで直線も非表示にし、Destroyでリソースを解放する。これらのゲーム内遷移は未検証。

## First / Secondの初期設定

| 項目 | First | Second |
|---|---:|---:|
| Stroke Length | 1.35m | 2.1m |
| Stroke Width（上限幅） | .085m | .15m |
| Stroke Core Duration | .055秒 | .09秒 |
| Stroke Duration | .14秒 | .23秒 |
| Stroke Intensity | 1 | 1.25 |
| Stroke Material Core Intensity | 2.4 | 2.8 |
| Stroke Material Front Intensity | 1.3 | 1.8 |
| Arc Trail Brightness | .7 | .9 |
| Arc Trail Duration上限 | .18秒 | .25秒 |
| Arc White Front Span | .30 | .35 |

Firstは細く短い高速の刃、Secondは幅・長さと前縁を強めた決定打。Secondでも面全体を白く増光する方針にはしていない。

## Inspectorで調整する項目

`Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset` のFirst / Second：

- **Stroke Length / Width**：直線の長さと面の幅。大きな白帯に見えた場合、まずWidthを下げる。
- **Stroke Core Duration / Stroke Duration**：白芯と青い余韻の寿命。総寿命は芯より長くする。30fpsでも初期値の芯は約2〜3フレームあるが、実際の視認性は未確認。
- **Stroke Intensity**：段階ごとの全体強度。
- **Trail Brightness / Core Intensity / Afterglow**：補助となる弧の強度。弧が主役に戻ってしまう場合に下げる。

`Assets/Mock/Effects/JustAvoidCounterFirstStroke.mat` / `JustAvoidCounterFinisherStroke.mat`：

- **Tip and Thin Core White / Transparent Silver Blue Face**：白と青の色を別々に変更。
- **Thin Core Width / Intensity**：細い白芯。遠距離で消えるのを防ぐMinimum Core Radius in Pixelsも用意。
- **Forward Tip and Edge Intensity**：先端と前縁だけの強さ。全体が白ければ、まずこちらと芯の強度を下げる。
- **Blue Face Intensity / Steel Facet Contrast**：青い面の存在感と金属風の明暗差。
- **Outer Transparency Falloff / Forward Reveal Duration**：外周の消え方と前方への表示伸長。

既存のFirst/Finisher Slash Materialは弧用。**White Restricted to Fresh Front**を小さくすると白い範囲を前端へ絞れる。**Cold Silver Surface / Silver Surface Intensity / Blue Steel Facet Contrast**で面の銀青を調整する。

## ユーザーによるGameScene確認

1. 追撃を空振りし、薄い弧だけが出て、直線は出ないことを確認。
2. 1撃目と2撃目を実命中させ、刀の直近の進行方向と直線が一致するか確認。
3. 先端・前縁・芯だけが白く、面と後端が青く透けるか確認。明るい背景と暗い背景で比較。
4. HitStop中に白いピークから青い余韻へ変わるか、敵のPoseとBloodが見えるか確認。
5. 奥行き方向の斬り、斜めの命中、30fpsで確認。LEqualにより敵の奥へ入る部分は隠れる。実3D方向が視線と平行な場合は直線が短く見える可能性がある。
6. 通常Light/Heavy、連続追撃、中断、Pause/Resume、Retry、Scene切り替えで残留や誤表示がないか確認。

明るい背景や発光の重なりでは加算色が白へ寄る可能性が残る。強度を上げる前に、白芯・前縁と青い面を個別に比較する。命中点が描画上の敵表面より内側なら深度で隠れる可能性もあり、実画面での確認が必要。

## 変更ファイルと影響

既存変更8ファイル：

- `Assets/Mock/Resources/CounterSwordSlash.shader`：弧の白い範囲、青い面の明暗、後方の減衰。
- `Assets/Mock/Effects/JustAvoidCounterFirstSlash.mat` / `JustAvoidCounterFinisherSlash.mat`：補助Trailの色・強度。
- `Assets/Mock/Scripts/Config/JustAvoidCounterConfig.cs` / `Assets/Mock/ScriptableObjects/JustAvoidCounterConfig.asset`：独立した直線設定と弧の調整。
- `Assets/Mock/Scripts/Feedback/SwordTrail.cs`：共有履歴から方向を取得、表示リソースの初期化・解放。
- `Assets/Mock/Scripts/Feedback/CounterSlashPresentation.cs`：既存表示ライフサイクルから直線の再生・更新・Resetへ接続。
- `Assets/Mock/Scripts/Feedback/CombatFeedback.cs`：既存SlashDirectionを表示通知へ渡す1行の変更。

新規：`CounterImpactStroke.cs`、`CounterImpactStroke.shader`、First/Finisher Stroke Materialと各meta。

作業開始時はGit差分なし（HEAD `df9bf12`）。開始時のAssets/Packages/ProjectSettings全ファイルをSHA256で比較し、上記8ファイル以外は一致した。Prefab、通常BladeTrail Shader/Material、Blood、Enemy HitReaction、HitStop、Animation Event、Hitbox、ダメージ、Root Motion、AI、カメラ、UI、Scene関連ファイルは変更なし。Configの変更も追撃VFXフィールドに限定されている。

## 完了した検証と未検証

- C#全体をUnity同梱Roslynと既存参照でコンパイル成功。既存警告9件、エラーなし。
- Unity APIによる4つのMaterialのShader Passコンパイル成功。現在のDirect3D12環境で診断メッセージ0件。
- 共有サンプラーが1つであること、Stroke Material参照、既存の旧Cut抑制設定を確認。
- Scene/GameObjectを作らないC#計算テストで、方向平均・逆向き履歴の扱い・履歴へのフォールバックと、両段階の54頂点の直線性・幅上限・先細りを確認。
- `git diff --check`、開始時Hash比較、Configの変更範囲比較は成功。

記録は `Artifacts/CounterBladeStroke/EditorVerification.json`、`DiffVerification.json`、`Compile.log`、`TaskOnly.patch`。

追加で予定した従来Trailの回帰スクリプト再実行は、**自動承認レビューの利用上限により実行されなかった**。安全性の否認ではなくレビューを完了できなかったためであり、成功扱いにはしていない。実Sceneの見た目、Pause/Retry等の実行時動作、Profiler上のGC Alloc・描画負荷はユーザー側で確認が必要。
