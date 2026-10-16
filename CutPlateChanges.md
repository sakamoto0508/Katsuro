# 白銀の斬撃板への改修 — 2026-10-16

実Sceneテスト・見た目の最終評価はユーザー担当です。Codexは実装、Unity APIによる保存・参照確認、Shaderパスの同期コンパイル、差分確認だけを実施しました。Play Mode起動／停止、操作入力、撮影、実戦再現コードの追加は行っていません。

## 1. 弱い原因と今回の対応

前回のSilver Sheetはコアと同じ長さ・対称レンズ形状に従い、コアの減衰にも強く連動していました。Shader上で面の層が存在しても、細い芯と同じ形に重なり、残光も弱くなる設計でした。幅や発光だけを上げる方法には限界があります。

今回は同じQuadの中で、コアと独立した銀の斬撃板を生成します。

- **コア**：冷たい白銀の最も明るい芯。両端を尖らせ、前方の肩を太くし、緩い反りを付ける。
- **銀の面**：コアより長い独立した輪郭。上側を大きく張り出し、下側を短く鋭くした非対称の三日月状プレート。面の中に銀灰～青白銀の濃淡と明るい稜線を作り、GaussianのGlowと区別する。
- **接触中心**：Flashのサイズを独立制御。Core Intensityに応じた局所加算を使い、線全体の発光を上げても接触中心が強いピークになるよう調整する。色は白主体の冷色。

発生時から広い輪郭を出し、先端が短時間で伸びます。その後は後端そのものが前方へ抜け、コアが先に減衰。銀の面はCore Lifeへ直結させず、少し遅れて弱い残光へ移ります。2撃目は芯を.16秒保持し、既存HitStop .155秒を覆います。既存FeedbackVisualTimeを変更していないので、Animatorの停止中も表示時間が進み、ゲームPause時は止まります。

これはShader内の局所的な構造変更です。新しいGameObject・Mesh・ParticleSystem・描画Passは追加していません。固定Pool、MaterialPropertyBlock、HitPoint、SlashDirectionの投影、Depth Test LEqual、微小オフセットは維持しています。

## 2. 変更ファイル

- `Assets/Mock/Resources/JustAvoidCounterCut.shader`：独立した斬撃板形状、後端の抜け、接触ピーク、調整パラメータ。
- `Assets/Mock/Effects/JustAvoidCounterFirstCut.mat`：1撃目の冷色、形状、時間、強度。
- `Assets/Mock/Effects/JustAvoidCounterCut.mat`：2撃目の強めの冷色、形状、時間、強度。
- `Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab`：Cut Sizeのみ。
- `Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab`：Cut Sizeのみ。

**C#は変更していません。** BloodのShader／Material／ParticleSystem、Enemy HitReaction、HitStop設定、入力・コンボ、Animation Event、Hitbox、ダメージ、Root Motion、AI、カメラ、UI、Tutorial、Final Blow、Scene進行は開始時のままです。

## 3. 1撃目と2撃目

| 項目 | 1撃目 | 2撃目 |
|---|---:|---:|
| Prefab Cut Size X / Y | 2.15 / .76m | 3.4 / 1.2m |
| Core Width | .18 | .32 |
| Core Length | .88 | .92 |
| Core Intensity | 1.9 | 3.1 |
| Sheet Width | .32 | .64 |
| Sheet Length | .95 | .99 |
| Sheet Intensity | .65 | 1.7 |
| Sheet Softness | .24 | .12 |
| Flash Intensity | 1.7 | 2.5 |
| Flash Size X / Y | .09 / .20 | .10 / .22 |
| Flash Duration | .045秒 | .06秒 |
| Taper Strength | .95 | .70 |
| Forward Extension Amount | .24 | .45 |
| Extension Duration | .016秒 | .026秒 |
| Core Hold | .025秒 | .16秒 |
| Fade Duration | .08秒 | .095秒 |
| Residual Glow Duration | .06秒 | .11秒 |
| Strike Multiplier | 1.0 | 1.35 |

1撃目は芯を鋭く、面を狭く、抜けを高速にしています。2撃目は明確に大きいプレート、鋭い面の境界、強い局所ピーク、長めの保持と残光に振っています。寸法設定上のコア代表幅は1撃目の約2.8倍、銀の面の代表幅は約3.2倍です。実際の見かけの幅は先細り・射影・遮蔽で変わります。

白飛びを避ける保証が取れた最終値ではなく、ユーザー評価のための「やりすぎ寄り」の初期値です。敵全身を白くする構造にはしていませんが、Poseが読めるかは実Sceneで確認してください。

## 4. Inspector

Firstは `JustAvoidCounterFirstCut.mat`、Secondは `JustAvoidCounterCut.mat`。各Materialで同じ項目を独立に調整できます。

- **Core Width / Length / Intensity**：芯の太さ、使用する長さ、明るさ。
- **Sheet Width / Length / Intensity / Softness**：銀の斬撃面の広さ、長さ、密度、縁の柔らかさ。今回の主調整です。
- **Flash Intensity / Size / Duration**：接触中心の明るさ、局所的な大きさ、消える時刻。SizeのXはCut長さ、Yは面の高さに対する比率です。Z/Wは使いません。
- **Taper Strength**：高いほど端を絞る。高すぎると細い線へ戻ります。
- **Forward Extension Amount**：発生時から先端がさらに走る量。高いほど前方への伸びが強くなります。Extension Durationで速度を調整します。
- **Fade Duration**：Core Hold後に芯が消えるまでの秒数。
- **Residual Glow Duration**：芯が消えた後の銀の面／残光の追加時間。
- **Strike Multiplier**：そのMaterialの全Cut発光倍率。Firstの値がStrike1 Multiplier、Secondの値がStrike2 Multiplierに相当します。重複する2種類のグローバル倍率は追加していません。

Width／LengthはQuadの寸法に対する比率で、両端の先細りもあります。最大の物理寸法は各PrefabルートのCut Size X/Yです。CoreとSheetの長さを分けたので、面だけ少し先まで抜けさせることができます。

**Prefab寸法を反映するため、ユーザー側でPlayを再開始してから確認してください。** すでに生成されたPoolインスタンスには、PrefabのCut Size変更だけでは新寸法が反映されない場合があります。Materialの変更だけを見て比較すると、古い小さな描画面で評価してしまう可能性があります。

## 5. さらに強める場合

2撃目で次の順に試してください。

1. **Sheet Intensity 1.7→2.0**、**Sheet Width .64→.70**。Bloomより先に銀の面を増やす。
2. **Core Width .32→.36**、**Core Intensity 3.1→3.6**。芯の存在感を追加。
3. 長さが不足するならPrefab Cut Size Xを**3.4→3.7m**。Sheet Lengthはすでに.99なので、1を超えて拡張する用途には使わない。
4. 全体の明るさだけを押す場合はSecondのStrike Multiplier **1.35→1.5**。形状を決めた後に調整する。

## 6. やりすぎた場合

- 白Flashに見える：**Flash Intensity 2.5→1.8**、Flash Sizeを**.10/.22→.08/.18**。銀の面を先に消さない。
- 白い帯でPoseが隠れる：**Sheet Width .64→.50**、**Sheet Intensity 1.7→1.2**。
- 全体が白飛びする：**Strike Multiplier 1.35→1.1**、次にCore Intensity。カメラ全体のBloomは変更しない。
- 長く白く残る：**Fade Duration .095→.07秒**、**Residual Glow Duration .11→.07秒**。2撃目のCore Holdは.16秒を保ち、既存HitStopとの関係を維持する。
- 面がぼやける：Sheet Softnessを下げて輪郭を立てる。単に明るさを上げ続けない。

## 7. 確認状況とユーザー側テスト

Unity APIで専用Assetだけを個別保存しました。今回は `SaveAssetIfDirty` を使用し、他の編集済みAssetを一括保存していません。両Materialを再取得し、Shaderパスを同期コンパイル：`compiled=true`、エラー0件。HitStopは.045／.155秒のままです。

開始時ハッシュとの比較で意図的な変更は上記5ファイルだけ。対象外40ファイルは同一で、C#もすべて不変。PrefabはCut Size以外の内容・参照・Bloodが同一でした。`git diff --check`も通過しています。

実Sceneの迫力、白飛び、Poseの見え方、Bloodとの主従、30／60fpsの見え方、Playerによる遮蔽は未検証です。GameSceneでPlayを再開始し、両段を明るい地面・暗い地面・赤草背景、通常距離・近距離で比較してください。特に2撃目は命中直後／HitStop中／停止明けを見て、接触光→銀の板→短い残光の順で読めるか確認してください。通常Light／Heavyに専用Cutが混ざらないことも確認対象です。

今回もLEqualを維持しているため、Playerが接触点を覆う構図ではCutが隠れます。そうした場合は「弱い発光」と「遮蔽で見えていない」を分けてフィードバックしてください。HitPointやカメラをVFXのために動かす変更は加えていません。

今回分だけの差分は `Artifacts/CutPlate/ThisTask.patch`、開始時コピーは `Baseline/`、Shaderと保存値は `EditorVerification.json`、保護対象の比較は `StaticVerification.json` にあります。撮影画像や実戦診断コードは追加していません。
