# AKI-112：Bloodの発生位置候補・2撃目の幅・通常攻撃のバランス

2026-10-18。現在の未コミット実装を基準に作業しました。CodexはPlay Modeを起動・操作・撮影していません。ユーザー提供録画の連続フレームは確認しましたが、今回の変更後の見た目は未検証です。

## 録画とコードから分かったこと

録画「画面録画 2026-10-18 024011.mp4」の8.0–8.5秒付近では、深紅の血が明確に認識できます。一方、長い筋がPlayerの腰の手前へ重なり、太い赤い帯のように見えるフレームがあります。血の存在感は出ているので、さらに量や発光を増やす方針にはしていません。

コード上では次のようになっています。

- PlayerWeapon.GetContactPointは、Enemy Colliderへ最も近い有効刀Colliderを選び、Enemy Collider.ClosestPointをHitPointにしています。SkinnedMeshRendererの可視表面との一致は保証していません。刀Collider中心がEnemy Collider内に入っている場合、ClosestPointの結果が必ず可視表面になるとは限りません。
- EnemyのCapsuleColliderはcenter y=0.91、radius=0.38、height=1.63です。この静的寸法だけでは、アニメーション中の胴体・鎧と実HitPointのずれを確定できません。
- Counter BloodのCFXルートはHitPoint、各EmitterとShapeのオフセットは0。World空間で、刀先実測方向と既存Outward Biasを使って飛散します。Particleの実速度と、その長いStretch表示は別の要素です。
- Counterの主筋はSecondでStart Size 0.145–0.195、Length Scale 8、寿命前半のサイズ倍率最大1.12。長い筋はParticle位置だけでなく、Stretchされた描画面積を持ちます。
- Counter Blood MaterialはZTest Alwaysです。Player等の前に血が重なって見える原因候補ですが、録画だけでは当該粒子の奥行きが確定できません。

**録画に見える重なりだけを理由に、HitPointの誤りや必要な上方移動量は断定していません。** 今回、初期の発生位置は動かさず、帯の幅を抑える修正と、Blood専用の小さな位置補正を調整できる構成を採用しました。発生位置そのものの最終調整は残課題です。

## Blood専用の位置・方向設定

Counter CFXに `Blood Spawn Offset`、通常攻撃のBlood設定に `Spawn Offset` を用意しました。初期値は全て **(0,0,0)** です。

- X：攻撃者から離れる水平軸に直交する接線。斬撃方向の横成分へ符号を合わせます。
- Y：上方向。
- Z：攻撃者→HitPointの水平成分に沿う方向。
- オフセット全体を最大0.10mに制限します。水平の基準方向が取れないときは位置補正しません。

Worldの固定X/Z方向やカメラ方向へ押し出す方式ではありません。ただしZはColliderの実表面法線ではなく、攻撃者から離れる軸です。正のZが必ずEnemyの可視表面の外側になるとは限らないため、0から数cmずつ比較してください。

位置補正が作用するのはBloodの表示Transformだけです。Counterの旧Cut Rendererは従来のHitPointへ配置し、Contact Flash・Slash Stroke・判定用HitPointは動かしません。通常用もBloodStreaks / BloodDropletsだけへ補正を適用します。

方向については、Counterの既存実測方向とOutward Bias（First 0.22 / Second 0.30）を維持しました。通常用は既存SlashDirectionを基準に、同じ既存方向補正関数を利用してLight 0.15 / Heavy 0.20の小さな外向き成分を追加します。無関係な水平線やカメラの横方向へ飛ばす変更はしていません。

## Counter Second：幅75%と長さの補償

| Second Crimson Threads | 変更前 | 変更後 |
|---|---:|---:|
| Start Size | 0.145–0.195 | 0.10875–0.14625（75%） |
| Renderer Length Scale | 8 | 10.666667 |
| Start Speed | 8.2–9.6 | 維持 |
| Start Lifetime | 0.30–0.38秒 | 維持 |
| Burst Count | 7 | 維持 |
| Size over Lifetime | 既存曲線 | 維持 |
| 暗赤滴・遅延小滴 | 既存設定 | 維持 |

StretchのLength Scaleは幅に対する長さ比です。Start Sizeを75%にするだけだと長さも縮むため、Length Scaleを8÷0.75へ補償しました。`size × lengthScale` の成分は変更前と一致し、速度によるStretch成分も維持します。速度・長さ・寿命をまとめて弱める変更ではありません。

既存の不規則な輪郭・両端先細り・濃淡Shaderは維持しています。筋ごとの太さと長さの差は既存のTwo Constantsのサイズ・速度範囲を使用します。今回、新しいShapeやShaderを増やしていません。

Counter FirstのPrefabはバイト単位で変更していません。現在の鋭さ・速さをそのまま比較基準にしてください。

## 通常Light / Heavy：既存Bloodを再利用

SwordHitVFXは既に次の4層を持っていました。

- BloodStreaks：5粒、サイズ0.035–0.055、速度2.8–4.2、寿命0.12–0.19秒。
- BloodDroplets：8粒、サイズ0.025–0.055、速度2.2–3.8、寿命0.20–0.30秒。
- BloodMist：既存の控えめな煙状粒子。
- SlashLine：既存の接触斬撃線。

全子GameObjectのactiveSelf=true、World空間、playOnAwake=false、StopAction=None、Material参照有効を確認しました。従来の再生処理も、ルートとPrefabを有効化して各ParticleSystem.Play(false)を呼びます。Counterで発生した子の有効化不足は、現行の通常Prefabにはありませんでした。実Play Modeでの再生確認は未実施です。

新しいBloodシステムは追加せず、既存4スロットPool / SwordHitVFX / HitBlood Materialを再利用します。Normal Blood用の設定をVFXConfig内にまとめ、**Enemyが受ける非CounterのLight / Heavyにだけ適用**します。Player側の被弾VFXやCounter専用設定には適用しません。

### 通常Blood設定（Prefab値への倍率）

| 項目 | Light 変更前 → 後 | Heavy 変更前 → 後 |
|---|---:|---:|
| 主筋Count | 5 → 3 | 7 → 4 |
| 血滴Count | 8 → 4 | 11 → 6 |
| Width倍率 | 1 → 1.25 | 1.12 → 1.50 |
| Speed倍率 | 1 → 1 | 1.25 → 1.15 |
| Length倍率 | 1 → 1 | 1 → 1.25 |
| Lifetime倍率 | 1 → 1 | 1.05 → 1.10 |
| Direction Bias | 専用補正なし → 0.15 | 専用補正なし → 0.20 |
| Spawn Offset | なし → 0 | なし → 0 |

上表のCountは主筋・滴それぞれの最初のBurstの実数です。Heavyの変更前Countは既存の1.4倍と丸めを含みます。BloodMistの既存設定は維持しており、上表のCountには含めていません。

通常Bloodを多数の小粒から、少数で少し読める幅の筋と滴へ寄せました。Heavyには既存VFXルートScale 1.15も残るため、表の倍率だけをWorldサイズと解釈しないでください。Light / HeavyのBlood設定は独立し、CounterのParticle値は参照しません。SlashLineには新しい倍率・個数・方向補正を適用していません。

Counterとの違いは、Firstが5本の高速な主筋と6+7粒の滴、Secondが7本のさらに長く速い主筋と9+15粒の滴です。First / Secondの速度・寿命は通常より大きいままです。通常用とCounter用のMaterialは従来通り別です。

再利用前には初期化時に記録したローカル位置・回転を復元し、サイズ・速度・寿命・Burstは既存の保存値から毎回計算します。Heavy→LightやOffset変更後に倍率や位置が累積しない構成です。追加バッファと通常Blood種別のキャッシュは初期化時に確保し、命中ごとに配列や設定オブジェクトを生成しません。

## Inspector

### 通常Light / Heavy

`Assets/Mock/ScriptableObjects/VFXConfig.asset` の **Enemy normal hit / Blood only > Light Blood / Heavy Blood** を調整します。

- Streak Count / Droplet Count：個数。
- Width：Prefabサイズ倍率。
- Speed：Prefab初速倍率。
- Length：RendererのStretch長倍率。
- Lifetime：Prefab寿命倍率。
- Direction Bias：攻撃者から離れる成分。
- Spawn Offset：Bloodだけの位置候補。初期0。

Heavyの既存全体VFX Scaleは残ります。新しいBlood設定は既存Heavy全体倍率の一部をBlood限定の値で上書きするため、Bloodを調整するときは新しいグループを使ってください。寿命はPool返却0.42秒以内に収める必要があります。現在のHeavy滴は最大0.30×1.10+遅延0.022=0.352秒で収まっています。

### Counter First / Second

各専用CFX Prefabのルートに **Blood Spawn Offset**、既存 **Blood Outward Bias**。主筋幅・速度・数・寿命は既存Crimson ThreadsのMain / Emission / Rendererで調整します。Secondの比較候補は今回の75%値です。

Opacityは既存Material設定を維持しています。色・Shaderは共通変更せず、今回の細さと個数の比較を先に行ってください。

## 変更ファイルとGit

今回の製品側変更は5ファイルです。

- `Assets/Mock/Scripts/Feedback/JustAvoidCounterCutVFX.cs`：Blood限定位置候補と共通座標計算。
- `Assets/Mock/Scripts/Feedback/CombatFeedback.cs`：既存通常Bloodへの設定適用、初期ローカルPoseの保存・復元、種別キャッシュ。
- `Assets/Mock/Scripts/Config/VFXConfig.cs`：Light / Heavyの独立設定。
- `Assets/Mock/ScriptableObjects/VFXConfig.asset`：初期設定値。
- `Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab`：Second主筋の幅・Stretch補償、Offset初期0。

Shader / Material、SwordHitVFX.prefab、Counter First Prefab、既存VFXConfig項目は変更していません。SwordTrail / Slash Stroke / HitReaction / HitStop / Hitbox / DamageInfo / PlayerWeapon / PlayerAttacker / AI / Root Motion / Camera / UI / Scene / Animation Eventも変更していません。前回までの未コミット変更は保持しました。

開始時コピー・ハッシュと差分は `Artifacts/BloodPlacementBalance/Baseline/`、今回だけの差分は `TaskOnly.patch`、確認結果は `Before.json / After.json / DiffVerification.json` に記録しました。録画からの抽出フレームは `Artifacts/BloodVideoReview20261018/` で、Unityの自動撮影ではありません。

## 確認結果と限界

- Editor / Development / Release相当のC#コンパイル：全てエラー0、既存警告9。
- 通常 / Counter / Slashの計8 Material：Shader Passコンパイル成功、メッセージ0。
- 通常PrefabのActive / World空間 / Material参照、独立設定を確認。
- Second主筋の幅75%、サイズ依存の長さ成分の維持、速度・寿命・個数不変を確認。
- 位置計算8条件：Offset 0でHitPoint維持、水平回転に追従、最大10cm、方向不明時の無補正を確認。
- ハッシュ比較：今回の5ファイル以外は開始時と同一。全Shader / Material、First Prefab、通常Prefabを保護。

新しいRenderer / ParticleSystem / 命中ごとのInstantiateは追加していません。実ゲームの負荷・実粒子放出・通常攻撃の再生・Pool再利用・見た目はPlay Mode未検証です。提供録画は変更前の評価材料で、今回の変更後の品質を検証したものではありません。

位置の適切な補正値、実Colliderと可視表面の差、ZTest AlwaysによるPlayer手前への重なり、カメラ方向による短縮は未解決の検証項目です。Widthを抑えると帯の重なりは減る可能性がありますが、発生位置の根本原因を解消したとは断定していません。

## ユーザーのGameSceneテスト

1. Playを再開始し、同じ敵・距離・カメラ・背景でLight / Heavy / Counter First / Counter Secondを録画。通常Bloodは控えめ、Firstは高速、Secondは長く強いが帯ではない、という差を比較します。
2. CounterはBloodOnlyでも比較し、Secondの細さが適切か、Firstの迫力が維持されたかを確認。速度・寿命・色は同じなので、まず主筋幅だけを評価してください。
3. 発生位置はOffset 0を基準に、ユーザー自身で命中フレームをPauseし、ConsoleのRequestのHitPointとCFXのRoot位置、敵Colliderと見た目の胴体を比較します。筋の中心や先端を発生原点と取り違えないようにしてください。
4. 必要ならSpawn OffsetをY +0.02m、あるいはX / Z ±0.02mのように**1軸ずつ**比較してください。これらは調査候補で、推奨確定値ではありません。Zは実表面法線ではないため、正負両方向を少量で比較し、不要なら0へ戻します。
5. 位置は合っているのにPlayerへ広がるなら、Offsetで隠さずDirection Bias / Shape Angle / Stretchの重なりを比較します。正面・側面・斜めのカメラでも確認してください。
6. 明るい草地・赤草・暗い地面、通常速度・スロー再生で、赤い板感、切先の細さ、敵のPoseを隠していないかを評価します。
7. Heavy→Light→Counter→Light、Pause / 再開、連続命中、Retryで倍率・位置が残らないか確認。通常Slash、HitReaction、敵攻撃、HitStopが以前と同じかも確認します。

実画面で位置が依然不自然なら、実HitPoint、CFX位置、カメラ角度、該当フレームを次の調査材料とし、Colliderやダメージ判定を動かさずにBlood表示だけを詰めます。
