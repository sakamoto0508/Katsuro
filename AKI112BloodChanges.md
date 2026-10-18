# AKI-112：追撃Bloodの主筋・輪郭・時間差の調整

2026-10-18。開始HEAD `a90c37a`。作業開始時のGit作業ツリーはクリーンでした。現行Gitの実装とAssetを基準に調整し、Play Modeの起動・操作・撮影は行っていません。実Game Viewでの視認性は未検証です。

## 1. 確認できた構成と弱さの原因候補

確認済み：

- 既存CFXは `Crimson Threads`、`Dark Blood Drops`、`Late Fine Drops` の3層。各層はWorld Simulation Space、useUnscaledTime、非ループです。専用CFXのPoolは段階ごとに4個で、命中時は既存インスタンスを再利用しています。
- CFXのルートは実HitPoint、各EmitterのlocalPositionとShape位置は0です。発生位置に明確なずれは確認できず、今回は変更していません。
- 主筋はFirst 5本 / Second 7本、初速6.4–7.6 / 8.2–9.6m/s。命中時の遅延は0です。Velocity / Force / Noise / Limit Velocity over Lifetimeは無効で、発生後は既存の初速・重力で動きます。
- 主筋のSize over Lifetimeは発生直後から縮小し、寿命50%で0.675倍、終了時0.35倍でした。Start Size自体もFirst 0.070–0.095、Second 0.100–0.135です。Shaderの内側マスクがさらに描画幅を絞ります。
- 全層が同じShaderマスクを使い、従来は先細りの楕円形に近い形状でした。主筋固有の不規則な太さや、太さを保ってから細く消える時間変化は弱い構成でした。
- Slashは既に刀先の実測方向を算出しますが、BloodはDamageInfo.SlashDirection、つまり従来のHitbox Sweep方向を使っていました。刀先の動きとBloodの軸が一致する保証がありませんでした。
- Blood MaterialはGain 0.4、既存の深紅Tint、非加算の `Blend SrcAlpha OneMinusSrcAlpha`、Queue 3005、ZWrite Off、ZTest 8（Always）です。深度遮蔽によって血が欠落する設定ではありません。これは今回追加したAlwaysではなく、開始時からの設定です。
- Shaderに明示的なExposure補正はありません。Bloomで白く発光させる処理もありません。今回、露出・色・Gainは変更していません。

通常サイズの画面では、細い主筋がすぐ縮むこと、投影方向、背景との低い色差、Playerとの重なりが弱さにつながると考えられます。ただし、各要因の実Game Viewへの寄与は未計測です。カメラ方向へ飛ぶ場合、World Spaceの速度が大きくても画面上の移動は小さくなります。

## 2. Bloodだけへ刀先方向を再利用

`SwordTrail.NotifyCounterHit` が既に算出している方向を、戻り値として返すようにしました。刀先速度から所有者の平行移動を除き、信頼できる瞬間速度→直近履歴→Sweepの順で選ぶ既存方式です。

`CombatFeedback` はその値をBlood CFXへ渡します。方向が得られなければ従来のSlashDirectionを維持します。別のサンプラーや履歴は追加していません。

返すのは**Slashのカメラ投影補正より前の実測方向**です。Slash自体に渡す方向・Mesh・Material・時間・強さは変更していません。既存CFXの旧Cut Quadは両Prefabとも無効設定で、Contact Flashの位置・法線も従来通りです。

既存のBlood Outward Bias（First 0.22 / Second 0.30）は維持しています。ここで使用するHitNormalは現行PlayerAttackerでは「攻撃者からHitPointへ向かう方向」であり、Collider表面の実法線ではありません。新たな表面法線を推測して足す処理や、カメラ方向への無条件補正は行っていません。HitPoint / DamageInfo / Hitbox / 物理判定方向は変更していません。

## 3. 主筋・血滴・遅延小滴

粒子数・初速・寿命・基本色・Alphaの寿命カーブは維持しました。主筋の幅とサイズカーブを優先し、Stretch倍率だけを大きくする方法は採用していません。太さが増えた分、Length Scaleは少し下げ、長い赤い棒になることを抑えています。

| 項目 | First 変更前 → 後 | Second 変更前 → 後 |
|---|---:|---:|
| 主筋 Start Size | 0.070–0.095 → 0.105–0.145 | 0.100–0.135 → 0.145–0.195 |
| 主筋 Shape Angle | 10 → 13度 | 14 → 18度 |
| 主筋 Renderer Length Scale | 7.5 → 7 | 9 → 8 |
| 主筋 Velocity Scale | 0.045（維持） | 0.055（維持） |
| 主筋 Start Speed | 6.4–7.6（維持） | 8.2–9.6（維持） |
| 主筋 Start Lifetime | 0.23–0.30秒（維持） | 0.30–0.38秒（維持） |
| 主筋 Burst Count | 5（維持） | 7（維持） |
| 暗赤滴 Start Size | 0.060–0.090 → 0.075–0.105 | 0.075–0.120 → 0.090–0.140 |
| 暗赤滴 Start Delay | 0 → 0.018–0.032秒 | 固定0.012 → 0.035–0.055秒 |
| 遅延小滴 Start Size | 0.022–0.040 → 0.026–0.045 | 0.024–0.045 → 0.032–0.055 |
| 遅延小滴 Start Delay | 0.035–0.050 → 0.055–0.080秒 | 0.065–0.090 → 0.095–0.135秒 |

主筋のSize over Lifetimeは、両段階とも以下へ変更しています。横軸は各粒子の寿命比率で、経過秒ではありません。

| 寿命比率 | 変更前 | 変更後 |
|---|---:|---:|
| 0% | 1.000 | 1.000 |
| 15% | 0.9025 | 1.120 |
| 48% | 0.688 | 1.000 |
| 80% | 0.480 | 0.480 |
| 100% | 0.350 | 0.060 |

初期に小さく膨らみ、前半は幅を保ち、後半で細く消える曲線です。曲線の補間はClamped Autoで、負の値や1.12を超える膨張がないことを数値確認しました。Alphaは既存通り寿命の後半で0まで減衰します。

Firstは主筋が先行し、18–32ms後に暗赤滴、55–80ms後に小滴が続きます。Secondは主筋→35–55ms後の暗赤滴→95–135ms後の小滴という順で、155msの既存HitStop解除後にも粒子が残る寿命設定を維持しています。

総数はFirst 18粒 / Second 31粒のままです。速度・重力・World Spaceを維持しているため、見せる時間を延ばすために空中で停止させる処理はありません。最も遅い小滴もPoolの既存返却時間（0.43 / 0.60秒）より前に寿命を終える設定です。

## 4. Shader / Material

- 両端が細く閉じる幅の曲線と、途中に細いくびれがある輪郭へ変更。
- 中心線にも小さな揺らぎを加え、均一な赤い棒を避ける形状に調整。
- 根元側と一部の太い部分を暗くし、深紅の筋と暗赤の縁の差を残しました。既存の色値、Gain、Vein Strength、Rim設定は維持しています。
- UVの上下端と左右端でAlphaを0にし、Quad境界で不透明な四角い面が残らない計算にしました。
- RGBはstraight alphaの色、Alpha側に輪郭Coverageを適用。RGBへCoverageを二重に掛けるPremultipliedの混在はありません。形状の内部にある暗赤色は意図した血の色であり、矩形全体を黒く覆う描画ではありません。
- `_Opacity`（1）と `_ContourVariation`（0.65）をMaterialへ追加。前者は透明度、後者は輪郭の凹凸を調整します。C#の調整フィールドは追加していません。

このMaterialは両段階・3層で共有しているため、輪郭変更と新しいMaterial設定は全Blood層へ作用します。段階ごとの幅・長さ・量・速度・時間は引き続き独立したPrefabで調整します。GUID参照の検索では、このBlood Materialを参照するPrefabは今回の2個のみでした。

## 5. 変更ファイルと保護した範囲

製品側の変更は6ファイルです。

- `Assets/Mock/Effects/JustAvoidCounterBlood.shader`：先細り、輪郭の凹凸、濃淡、境界Alpha。
- `Assets/Mock/Effects/JustAvoidCounterBlood.mat`：Opacity / Contour Variationのみ追加。
- `Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab`：Firstの幅・サイズカーブ・広がり・滴の遅延。
- `Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab`：Secondの対応設定。
- `Assets/Mock/Scripts/Feedback/SwordTrail.cs`：既存の実測方向を返すだけで、Slash表示の計算は維持。
- `Assets/Mock/Scripts/Feedback/CombatFeedback.cs`：追撃専用CFXへ実測方向を渡す。

`JustAvoidCounterCutVFX.cs`、Config、DamageInfo、PlayerWeapon / PlayerAttacker、敵HitReaction / AI / Animator / Animation Event、Slash Shader / Materialは変更していません。HitStopはFirst 0.045秒 / Second 0.155秒のままです。

新しいParticleSystem・Renderer・サンプラーは追加していません。既存Pool / ParticleSystem / Material参照を維持し、毎命中のInstantiateや毎フレームのManaged Allocを増やしていません。Shaderの計算量と描画面積は増えるため、GPUコストが完全に同じとは限りません。Profiler測定は未実施です。

開始時のGit状態・ソース / Assetコピー・SHAは `Artifacts/AKI112Blood/Baseline/`、変更前の詳細設定は `Before.json`、変更後は `After.json`、今回の差分は `TaskOnly.patch`、変更範囲の検査は `DiffVerification.json` に保存しました。記録は同じ `Artifacts/AKI112Blood/` 内です。開始時の未コミット作業はありませんでした。以前の調整記録は変更していません。

## 6. 検証結果と未検証事項

実施済み：

- Unity付属RoslynによるAssembly-CSharp全ソースのコンパイル：エラー0、既存警告9。
- Unity APIによるBloodと両段階Slash / Strokeの計5 MaterialのPassコンパイル：成功、Shaderメッセージ0。
- 両PrefabのEmitter参照、World Space、unscaled時間、非ループ、命中原点、粒子数、寿命とPool返却時間、サイズカーブの範囲を確認。
- 既存Material値は新しい2項目を除き開始時と同一。対象外の開始時Assets / Packages / ProjectSettingsのファイルはSHA比較で変更なし。
- Git diff --check：エラーなし。

未検証：通常サイズGame View、30fpsでのBloodの読み取り、実カメラ角度、敵シルエットからの抜け、赤い衣装との重なり、HDRP Exposure / Tonemapping / 背景による色差、Pause / Retryの実動作、実ゲームの負荷です。

ZTest Alwaysは現行値を維持しています。したがって、壁やPlayerの手前へ血が透けて見える可能性は引き続きあります。深度設定を変えると血が身体に隠れる別の問題が起こり得るため、今回は未確認の遮蔽問題を推測で変更していません。

## 7. Inspectorでの推奨調整順

1. `Crimson Threads > Main > Start Size`：まず通常サイズの画面で主筋が読める太さにする。太い赤い帯になるなら先に下げます。
2. `Size over Lifetime`：前半の山と保持、後半の細まり。前半を長くしすぎると血の棒が残ります。
3. `Renderer > Length Scale`、次に `Velocity Scale`：長さと速度による伸び。幅を決めてから調整します。今回Length Scaleが下がっていても、Start SizeとShaderの実描画幅が増えています。
4. `Shape > Angle` とCFXルートの既存 `Blood Outward Bias`：方向の束と攻撃者からの離れ方。広げすぎると主筋の向きが読みづらくなります。
5. `Start Speed / Start Lifetime`：身体の外へ伸びる距離と残る時間。今回は維持しています。
6. `Dark Blood Drops / Late Fine Drops > Main > Start Delay / Start Size`：主筋の後へ続く滴を調整。
7. 共通Materialの `Blood opacity`、`Wet contour variation`、既存 `Rim width / Rim color / Vein`：背景との分離。Opacityは初期値1なので、それ以上の強化は粒子の幅・形状から行います。GainやTintを先に明るくしないでください。

Countは各EmitterのEmission Burst Count、ColorはMain Start Colorで調整できます。段階別の粒子設定と共通Material設定を混同しないようにしてください。

## 8. ユーザーによるGameScene手動テスト

1. 同じ敵・距離・カメラで、First単体と2連撃を通常速度で録画。白銀の一閃が先に読み取れ、その後に敵から血が出たと感じるか確認します。
2. 明るい草地・赤草・暗い地面の3背景で、First / Secondを同じ条件で比較してください。通常サイズのGame Viewを判定基準とし、拡大画像だけで判断しないでください。
3. ユーザー自身でPlay Mode中の表示を切り替え、BloodOnly / Allを比較してください。BloodOnlyでは既存SwordTrailのMeshRendererと `Counter Impact Straight Blade`、Contact FlashのRendererだけを一時OFFにします。SwordTrailコンポーネントやPlayer GameObject、Enemy / Animatorは無効にしないでください。方向の算出とゲーム進行は動かしたままにします。判別しにくい場合は無理にAssetを変更せず、Allでの比較を優先してください。
4. Firstは5本の細く鋭い主筋が読めるか、Secondは7本の太い主筋が敵の輪郭を越えるかを確認。先細りやくびれが見える一方、赤い棒・帯・煙に見えないかを確認します。
5. 正面・側面・斜めのカメラで方向を比較。実刀先方向へ変更した結果が不自然でないか、奥行きへ飛んで広がりが小さい角度がないか、Playerの衣装へ重ならないか確認します。完全に視線方向へ向く場合の短縮は今回も残り得ます。
6. スロー再生で、接触点→主筋→暗赤滴→小滴→消失の順を確認。HitStop中も血が飛散し、解除後の崩れと余韻がつながるか、途中で一斉に消えないか確認してください。
7. Pause / 再開、連続命中、空振り、致死命中、Retryで残留・再生停止・Poolの不具合がないか確認。通常Light / Heavy、Slashの強さ、HitReactionが以前と同じかも比較してください。
8. 比較後は一時的なRenderer変更を元に戻し、PrefabやSceneへ検証用の表示OFFを保存しないでください。

今回の調整は見た目改善の候補を実装した段階です。実動画を受けて、幅が過剰ならStart Size、散りすぎならShape Angle、余韻が長ければLifetime、背景へ溶ける場合は輪郭と色差を順に調整する必要があります。
