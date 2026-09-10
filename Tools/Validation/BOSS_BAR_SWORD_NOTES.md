# ボスHP画像・剣の軌跡変更（2026-09-10）

- BattleHUD.prefabのEnemyStatusに指定の金縁・中央紋のボスバーを適用。赤い現在HP、黒い遅延HP、空ゲージの3層を維持。
- EnemyHealthFill / EnemyDamageTrail / EnemyHealthTrackのImageのSource Image・Colorで調整可能。
- EnemyHealthFrameは外枠全体。BossBarSilhouetteが市松模様の描き込み部分を輪郭メッシュで除外する。別形状の透過画像へ交換した場合はこのコンポーネントを無効化する。Outlineを変えることも可能。
- 元画像をEnemyBarReference.pngとして保持。空ゲージ画像はEnemyBarEmpty.png。テクスチャ自体には市松模様が残るため、外枠は輪郭コンポーネントとセットで使う。
- SwordTrail：幅0.22→0.045、残存時間0.14→0.09秒、青→薄い灰白（不透明度0.3）、末端を細く透明に変更。別の攻撃の開始時に古い軌跡を消去。敵・プレイヤー双方に適用。
- 剣のColliderがあるオブジェクトへSwordTrailをあらかじめ付ければ、Duration / Width / Color / TipをInspectorで設定できる。未配置の場合は既存の自動追加を継続。
- uGUIレンダーでHPバー表示を確認。ゲージ・選択の15項目が成功。本体C#コンパイル成功。実戦中の太刀筋の見え方・HDRP画面での最終見え方は未確認。

## 素材生成記録
imagegenスキル、組み込みimage_genで添付画像を編集（CLI未使用）。採用画像はEnemyBarEmpty.png、元画像はEnemyBarReference.pngに保存。
採用プロンプト：
"Edit this exact Japanese dark fantasy boss HP bar into a production game sprite. Preserve its exact long thin ornate black and aged gold frame and central circular mitsudomoe crest, silhouette and proportions. Remove ALL checkerboard and watermark-like background outside the frame, replace exterior with genuine transparent alpha. Remove the red filled HP inside: make the entire interior uniformly the same subtle dark charcoal textured empty track as the rightmost empty part. No red remains. Center the bar with very tight transparent padding. No text, no additional elements. Output wide transparent PNG."
透明化は生成結果に反映されなかったため、再試行後も背景が残ることを確認し、採用画像は初回の形状を保ったものとし、uGUI輪郭メッシュで外側を非表示にした。

プレビュー：Tools/Validation/Previews/boss-bar-artwork.png。
