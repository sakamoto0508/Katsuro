# HitVFX改修結果

## 原因（Unity Editorから取得した設定に基づく判断）

既存SwordHitVFXはBloodStreaks、BloodDroplets、FineSliversの3層。Mistがなく、FineSliversは暖色の明るい粒子だった。BloodStreaksの赤はRGB (0.65, 0.025, 0.035)、全層World Space、ConeのShape回転はY=155°。Shaderは柔らかい均一な楕円で、血の筋と小滴の輪郭を分けられていなかった。HitNormalは攻撃者から命中点への方向で、刀の横斬り・斬り上げの移動方向を表していなかった。

## 変更したAsset / Script

- 既存Prefab: Assets/Mock/Effects/SwordHitVFX.prefab（同じPrefabとGUIDを維持）
- 既存Material: Assets/Mock/Effects/HitBlood.mat
- 新規Material: Assets/Mock/Effects/HitBloodMist.mat（同名AssetがないことをEditorから確認して作成）
- 既存Shader: Assets/Mock/Effects/HitBlood.shader（HDRP用。非対称の滴、暗い中心と透ける縁、Mist用マスク、HDRPの露出倍率）
- Script: DamageInfo.cs、CombatFeedback.cs、VFXConfig.cs、PlayerAttacker.cs、PlayerWeapon.cs、WeaponHitboxRelay.cs
- ScriptableObject: Assets/Mock/ScriptableObjects/VFXConfig.asset
- 許可された接続用Package: com.unity.pipeline 0.8.0-exp.1

Enemy/Player PrefabとPlayerControllerには作業開始前から変更があり、今回これらは編集していない。EnemyController、HitReaction、SwordTrail、Damage計算、操作処理にも変更を加えていない。

## Particle構成（保存後のEditor再取得値）

| 名前 | 役割 | Light Burst | Lifetime秒 | Start Speed | Start Size | Start Color RGBAの範囲 | Shape | Simulation Space | Renderer |
|---|---|---|---|---|---|---|---|---|---|
| BloodStreaks | Main Blood Slash | 3–4 | 0.14–0.23 | 3.8–5.5 | 0.018–0.029 | (0.145,0.005,0.004,0.94) ～ (0.32,0.021,0.018,0.9) | Cone 5° / 半径0.008 | Local | Stretch / Length 3.5 / Speed Scale 0.065 |
| FineSlivers | Blood Mist（既存名維持） | 2 | 0.09–0.15 | 0.25–0.6 | 0.10–0.17 | (0.20,0.011,0.009,0.09) ～ (0.27,0.018,0.015,0.14) | Cone 14° / 半径0.025 | Local | Billboard / HitBloodMist |
| BloodDroplets | Micro Droplets | 7–10 | 0.19–0.34 | 1.5–3.0 | 0.009–0.017 | (0.28,0.015,0.012,0.9) ～ (0.60,0.063,0.063,0.8) | Cone 16° / 半径0.008 | Local | Stretch / Length 1.3 / Speed Scale 0.008 |

共通: Duration 0.4秒、Loop OFF、Play On Awake OFF、時刻0のBurstを1回のみ。Shape回転0°、Root/子Transformの回転0°、連続Emissionなし。Color over LifetimeでAlphaを消し、Size over Lifetimeで血の筋と小滴を縮小。Mistは短い時間で少しだけ広がる。Velocity over Lifetime / NoiseはOFF。小滴のGravity Modifierは0.22、他は0。Max ParticlesはMain/Mist=12、小滴=20。Sorting OrderはMain/小滴=1、Mist=0、Distanceソート。影・Light Probe・Reflection ProbeはOFF。

Textureは新規作成していない。取得対象のAssets/Mock内にblood名の既存Textureはなく、現在のShaderのUVマスクで輪郭を作った。強いEmissionは追加していない。環境はHDRP Balanced、Linear色空間。

## 方向・Flash・Light / Heavy

武器Collider中心の移動から斬撃方向を取得し、追加したDamageInfo.SlashDirectionで血だけに渡す。取得できない場合は既存HitNormal、次に受け手のforwardへフォールバックする。HitNormal自体は変更しない。血のRootの+Zを斬撃方向へ向け、Local Spaceで追従する。垂直方向もLookRotationのupと平行にならないよう処理する。

既存ContactPulseを再利用。Flash追加なし。接触Flashは最大0.05秒、サイズLight=0.12 / Heavy=0.16、淡い暖白色、強度Light=0.45 / Heavy=0.65。本体HitReactionの設定は変更していない。

HeavyはBurst数1.4倍（整数に丸める）、Rootスケール1.3倍、Start Speed1.2倍、Cone角度1.05倍。Main=4–6本、Mist=3粒、小滴=10–14粒。LightはPrefab基準値。倍率はキャッシュした基準値から毎回計算し、HeavyからLightへの再利用で累積しない。RootスケールもLightで1に戻す。4個のRootプールを維持し、毎ヒットInstantiate/Destroyには変更していない。

## VFXConfig

Editorから保存後に再取得した_hitVFXはAssets/Mock/Effects/SwordHitVFX.prefab。
GUID: 0606e9ce5afba844d8f73096112372ce。
HitVFXDuration=0.4秒（最長Lifetime 0.34秒を途中で切らない）。

## 確認

- 現状取得: Unity接続からVFXConfig、Prefab内ParticleSystem、Material/Shader、Enemy/Player PrefabのCombatFeedback、Rendering環境を取得。
- Compile: Unity recompile_statusでfailed=false、errors=[]。Asset変更後の再Compile要求はup_to_date。Shaderはsupported=true、ShaderHasError=false。
- Save: PrefabUtility.SaveAsPrefabAsset成功。変更したMaterialとVFXConfigをSaveAssetIfDirtyで保存。
- 再取得: 保存後にUnity接続から取得し、さらにForceUpdate再インポートして検証PASS。Prefab GUID/参照、3層のLocal Space、Burst、Material、Shader、プールの寿命設定を確認。
- Edit Modeコード検証: 実際のScaleHitCurve関数でHeavy速度倍率とLight基準値の非累積を確認。DamageInfoの既存コンストラクターと追加SlashDirectionがHitNormalを変更しないことを確認。
- Play Mode: 未実施。ゲーム内の見た目、実戦での刀方向追従、正常動作は未確認。今回の見た目の判断はAsset設定に基づくもので、映像を見た結果ではない。

InspectHitVFX.cs / AuthorHitVFX.cs / VerifyHitVFX.csはEditor接続用の作業・検証スクリプト。Assets外に置き、ゲーム用コンポーネントとして追加していない。

## 視認性の追加調整（ユーザーの実機確認後）

「あまり出ている感じがない」とのフィードバックにより、上記の初回設定から次の値へ変更した。主因は設定上の筋の細さ（Start Size 1.8–2.9cmに対しShaderがさらに細く絞っていた）と判断。暗い背景との低い色差も影響すると推測。映像による原因確定はしていない。

| Particle | Burst Light / Heavy | Lifetime秒 | Speed | Size | Color RGBA |
|---|---|---|---|---|---|
| BloodStreaks | 4–5 / 6–7 | 0.18–0.27 | 3.2–4.8 | 0.045–0.075 | (0.25,0.01,0.008,0.98) ～ (0.48,0.032,0.018,0.96) |
| FineSlivers (Mist) | 2 / 3 | 0.10–0.17 | 0.25–0.6 | 0.14–0.22 | (0.27,0.016,0.009,0.12) ～ (0.36,0.025,0.014,0.19) |
| BloodDroplets | 7–10 / 10–14 | 0.22–0.36 | 1.5–3.0 | 0.017–0.028 | (0.36,0.02,0.012,0.94) ～ (0.60,0.055,0.03,0.9) |

MainのStretch Lengthは3.5→4.5。Shaderの滴の輪郭を広げ、暗い芯の減光を0.68→0.82に抑え、先端の縁へ深紅(0.55,0.045,0.025)を最大35%混ぜる。Mistにはこの縁の色差を適用しない。透明AlphaブレンドとHDRP露出処理を維持。加算発光は使用しない。

Main/小滴のAlphaをLifetimeの45%まで維持してから消す。HitVFXDurationとParticle Durationは0.42秒。Shape・Local Space・方向処理・4個のプール・Heavy倍率は維持。既存Prefab/Material/Shader/VFXConfigを更新し、新規描画Assetなし。Play Modeは未実施。

## 筋の重なりの改善（隔離HDRPプレビューで比較）

さらに既存SwordHitVFX.prefabのみを変更。Mainの細い重なりを減らすため、Cone angle=18°、radius=0.035、radiusThickness=0、arc=360°、arcMode=BurstSpreadに変更。Burst内の方位を均等に分け、主方向+Zを保つ。ShapeのrandomDirectionAmount/sphericalDirectionAmountは0。

最新のMain設定: Burst Light=4–5 / Heavy=6–7、Lifetime=0.20–0.28秒、Start Speed=3.8–5.8、Start Size=0.085–0.125、Stretch Length=3.2 / Speed Scale=0.045。最初のLifetime 30%はサイズを保ち、その後0.25へ縮小。色は直前の赤黒い2色範囲を維持。

小滴はStart Size=0.022–0.036、Cone angle=22° / radius=0.025に変更。Burst=7–10、Lifetime=0.22–0.36秒、Speed=1.5–3、色・Stretch設定は直前の値を維持。Mist設定は変更なし。全層Local Space。Particle数、Material、Shader、VFXConfig参照、プール・方向処理は変更なし。

同じ乱数42、時刻0.075秒、768×512、縦5mの正投影画角、同じ背景、HDRPで変更前後を実描画して比較。

| 視点 | 変更前の赤い描画領域 | 変更後 | 前の高さ | 後の高さ |
|---|---:|---:|---:|---:|
| 横から | 187px | 667px | 9px | 28px |
| 進行方向の正面から | 25px | 307px | 7px | 24px |

比較画像: HitVFX-before-spread-game-scale.png / HitVFX-after-spread-game-scale.png、正面は*-front.png、近接は*-diagnostic.png。Linear RenderTextureをsRGBへ変換したPNG。隔離プレビューの比較であり、ゲーム映像ではない。Play Modeは未実施。
