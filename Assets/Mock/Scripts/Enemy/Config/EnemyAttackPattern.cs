using UnityEngine;
using UnityEngine.Serialization;

/// <summary>1本のコンボClipと完了後の復帰設定。斬撃区間は敵専用ClipのAnimation Eventが所有する。</summary>
[CreateAssetMenu(menuName = "Enemy/Attack Pattern", fileName = "EnemyAttackPattern")]
public sealed class EnemyAttackPattern : ScriptableObject
{
    /// <summary>Inspectorやログで識別する攻撃パターン名。</summary>
    public string PatternName = "Combo";
    /// <summary>このパターンで実行する既存の攻撃データ。</summary>
    public EnemyAttackData Attack;
    /// <summary>完全なAnimator Stateパス。遅れた別StateのEventを除外する。</summary>
    [Tooltip("完全なAnimator Stateパス。遅れた別StateのEventを除外する。")]
    public string AnimatorState;
    /// <summary>構えに入ってから突進（攻撃判定ON）が始まるまでの実時間（秒）。RushAttackでは、構えの動きは通常速度で再生し、残りの時間は突進直前の姿勢（Stance Hold Start 以降）で溜めて保持する。HitStop中は停止する。TuneDashRootMotion使用時は0.01秒以上。</summary>
    [FormerlySerializedAs("Windup"), InspectorName("構え→突進までの時間（秒） / Stance Duration"), Min(0f),
     Tooltip("構えに入ってから突進（攻撃判定ON）が始まるまでの実時間（秒）。RushAttackでは、構えの動きは通常速度で再生し、" +
             "残りの時間は突進直前の姿勢（Stance Hold Start 以降）で溜めて保持する。HitStop中は停止する。TuneDashRootMotion使用時は0.01秒以上。")]
    public float WindupDuration = .35f;
    /// <summary>予兆として遅く再生するClip冒頭の終端（Clip秒）。必ず初撃のHitbox ONより前にする。</summary>
    [Min(0f), Tooltip("予兆として遅く再生するClip冒頭の終端（Clip秒）。必ず初撃のHitbox ONより前にする。")]
    public float WindupEndTime = .15f;
    /// <summary>Clip終了後、次の行動まで追加で待つ硬直時間（秒）。</summary>
    [Min(0f)] public float RecoveryDuration = .45f;
    /// <summary>速度1での異常復帰期限。再生速度に応じて自動補正する。</summary>
    [Min(.1f), Tooltip("速度1での異常復帰期限。再生速度に応じて自動補正する。")]
    public float Timeout = 6f;
    /// <summary>コンボ本体の速度。Rush調整使用時は刺突後の保持・復帰だけに適用。Animator全体のspeedは変更しない。</summary>
    [Range(.5f, 1.5f), Tooltip("コンボ本体の速度。Rush調整使用時は刺突後の保持・復帰だけに適用。Animator全体のspeedは変更しない。")]
    public float PlaybackSpeed = 1f;
    /// <summary>このコンボだけの水平Root Motion倍率。Rush調整使用時はDashDistanceと実測値から自動算出する。</summary>
    [Min(0f), Tooltip("このコンボだけの水平Root Motion倍率。Rush調整使用時はDashDistanceと実測値から自動算出する。")]
    public float RootMotionScale = .18f;
    /// <summary>RushAttackだけで使用。WindupDurationがStanceDurationに相当。距離と速度はRoot Motionから調整する。</summary>
    [Header("RushAttack / Root Motion tuning")]
    [Tooltip("RushAttackだけで使用。WindupDurationがStanceDurationに相当。距離と速度はRoot Motionから調整する。")]
    public bool TuneDashRootMotion;
    /// <summary>障害物がない場合の目標突進距離（m）。衝突で制限された残距離を強制移動で補わない。</summary>
    [Min(.1f)] public float DashDistance = 3f;
    /// <summary>平均前進速度。瞬間速度は元Clipの加速カーブに従う。</summary>
    [Min(.1f), Tooltip("平均前進速度。瞬間速度は元Clipの加速カーブに従う。")]
    public float DashSpeed = 12f;
    /// <summary>刺突区間の終端（元Clip秒）。開始はWindupEndTime。</summary>
    [Min(0f), Tooltip("刺突区間の終端（元Clip秒）。開始はWindupEndTime。")]
    public float DashEndTime = .55f;
    /// <summary>既存Enemy Avatarで測定した刺突区間の水平前進量。Clip変更時は再測定。</summary>
    [Min(.001f), Tooltip("既存Enemy Avatarで測定した刺突区間の水平前進量。Clip変更時は再測定。")]
    public float MeasuredDashDistance = 3.145f;
    /// <summary>RushAttack：溜めて保持する姿勢の開始（元Clip秒）。ここまでは通常速度、ここからWindupEndTimeまでを「構え→突進までの時間」に合わせて引き延ばす。0なら構え全体を均等に伸縮する。</summary>
    [Min(0f), Tooltip("RushAttack：溜めて保持する姿勢の開始（元Clip秒）。ここまでは通常速度、ここからWindupEndTimeまでを" +
                      "「構え→突進までの時間」に合わせて引き延ばす。0なら構え全体を均等に伸縮する。")]
    public float StanceHoldStartTime = .42f;
    /// <summary>自然な手首で制作したRush Clip用のGrip。終了・中断時に元の装備Transformへ戻す。</summary>
    [Header("Rush pose / weapon grip")]
    [Tooltip("自然な手首で制作したRush Clip用のGrip。終了・中断時に元の装備Transformへ戻す。")]
    public bool OverrideWeaponGrip;
    /// <summary>上書きする武器Gripの親Transform基準のローカル位置（m）。</summary>
    public Vector3 WeaponGripLocalPosition;
    /// <summary>上書きする武器Gripの親Transform基準のローカルEuler角（度）。</summary>
    public Vector3 WeaponGripLocalEuler;
    /// <summary>Rush攻撃で距離・速度のRoot Motion調整を適用する条件を満たすか。</summary>
    public bool UsesDashTuning => TuneDashRootMotion && !IsWarp && Attack != null && Attack.ActionType == EnemyActionType.RushAttack;
    /// <summary>目標距離を目標平均速度で割った、理想的な突進の実時間（秒）。</summary>
    public float DashDuration => DashDistance / DashSpeed;
    /// <summary>元Clipの刺突区間をDashDuration秒で再生するための速度倍率。</summary>
    public float DashPlaybackSpeed => (DashEndTime - WindupEndTime) / DashDuration;
    /// <summary>目標距離をAvatarの実測距離で割った、刺突区間の水平Root Motion倍率。</summary>
    public float DashRootMotionScale => DashDistance / MeasuredDashDistance;

    /// <summary>
    /// 構え区間（元Clip 0〜WindupEndTime）の再生速度。保持開始までは通常速度、その後の短い区間を残り時間いっぱいに
    /// 引き延ばすので、構えから突進までの時間を長くしてもスローモーションにならず「溜め」として見える。
    /// 構え時間が保持開始より短い場合は、構え全体を均等に縮める。
    /// </summary>
    public float StanceSpeedAt(float clipTime)
    {
        float total = Mathf.Max(.01f, ActiveWindupDuration);
        float hold = Mathf.Clamp(StanceHoldStartTime, 0f, WindupEndTime - .01f);
        if (!UsesDashTuning || hold <= 0f || total <= hold + .01f) return WindupEndTime / total;
        return clipTime < hold ? 1f : (WindupEndTime - hold) / (total - hold);
    }
    /// <summary>本体再生に適用する、再生速度・突進時間を考慮した異常復帰期限（秒）。</summary>
    public float BodyTimeout => UsesDashTuning ? Timeout + DashDuration : Timeout / PlaybackSpeed;
    /// <summary>対象までの最小選択距離（m）。Mid距離帯のWarpではAIの距離帯判定を優先する。</summary>
    [Header("Selection / travel")]
    [Min(0f), Tooltip("対象までの最小選択距離（m）。WarpのMid候補ではNearDistance/FarDistanceを優先し、この制限を適用しない。Near/Farや他の攻撃では使用する。")]
    public float MinAttackDistance;
    /// <summary>対象までの最大選択距離（m）。Mid距離帯のWarpではAIの距離帯判定を優先する。</summary>
    [Min(0f), Tooltip("対象までの最大選択距離（m）。WarpのMid候補ではNearDistance/FarDistanceを優先し、この制限を適用しない。Near/Farや他の攻撃では使用する。")]
    public float MaxAttackDistance = 1000f;
    /// <summary>同じパターンを再選択できるまでの時間（秒）。</summary>
    [Min(0f)] public float Cooldown;
    /// <summary>攻撃中の水平Root Motionの累計移動上限（m）。ワープ自体の移動距離は含まない。0は制限なし。</summary>
    [Min(0f), Tooltip("攻撃中の水平Root Motionの累計移動上限（m）。Warpでは出現後の攻撃移動だけを制限し、テレポート距離は含まない。0は制限なし。")]
    public float MaximumTravelDistance;
    /// <summary>Player背後の出現目標地点までの距離（m）。攻撃射程・発動距離とは独立。</summary>
    [Header("WarpAttack only")]
    [Min(.1f), Tooltip("Playerの背後に出現する目標地点までの距離（m）。攻撃射程や発動条件ではない。NavMesh・衝突検証で安全な地点が見つからなければ中止する。")]
    public float TeleportDistance = 2.5f;
    /// <summary>ワープ先の方向を対象の背後からずらす角度（度）。</summary>
    [Range(-90f, 90f), Tooltip("Playerの水平な背後方向を0度として、ワープ先を上下軸まわりにずらす角度（度）。TeleportDistanceと組み合わせて目標地点を決める。")]
    public float TeleportAngleOffset;
    /// <summary>本体が非表示になってからワープ先へ再出現するまでの基準時間（秒）。</summary>
    [Min(0f)] public float DisappearDuration = .18f;
    /// <summary>再出現後の攻撃予兆。WarpではWindupDurationは消失前、こちらは出現後の構え時間。</summary>
    [Min(.1f), Tooltip("再出現後の攻撃予兆。WarpではWindupDurationは消失前、こちらは出現後の構え時間。")]
    public float ReappearWindupDuration = .5f;
    /// <summary>ワープ消失演出の開始時に再生するSE。</summary>
    public AudioClip DisappearSound;
    /// <summary>ワープ再出現時に再生するSE。</summary>
    public AudioClip ReappearSound;
    /// <summary>牙突風 RushAttack の構え・突進・刺突の演出設定。アニメーション・Root Motion・攻撃判定は変えない。未設定なら出さない。</summary>
    [Header("RushAttack VFX (look only)")]
    [Tooltip("牙突風 RushAttack の構え・突進・刺突の演出設定。アニメーション・Root Motion・攻撃判定は変えない。未設定なら出さない。")]
    public GatotsuRushVFXConfig RushVFX;
    /// <summary>旧ワープ演出。下のArt VFX（消失/出現）が未設定のときだけ使う。</summary>
    [Tooltip("旧ワープ演出。下のArt VFX（消失/出現）が未設定のときだけ使う。")]
    public GameObject WarpEffectPrefab;
    /// <summary>消失時の黒霧（足元基準）。</summary>
    [Tooltip("消失時の黒霧（足元基準）。")]
    public KatsuroTimedVFX WarpVanishVFX;
    /// <summary>出現時の青白い収束光・閃光・衝撃波（収束点基準）。</summary>
    [Tooltip("出現時の青白い収束光・閃光・衝撃波（収束点基準）。")]
    public KatsuroTimedVFX WarpAppearVFX;
    /// <summary>出現後の斬撃で、最初の攻撃判定が開いた瞬間に出す一閃。</summary>
    [Tooltip("出現後の斬撃で、最初の攻撃判定が開いた瞬間に出す一閃。")]
    public KatsuroTimedVFX WarpSlashVFX;

    /// <summary>黒霧を出してから本体を隠すまでの秒数。移動・判定には使用しない。</summary>
    [Min(0f), Tooltip("黒霧を出してから本体を隠すまでの秒数。移動・判定には使用しない。")]
    public float WarpVanishLead = .07f;
    /// <summary>実体出現前に収束光を始める秒数。</summary>
    [Min(0f), Tooltip("実体出現前に収束光を始める秒数。") ]
    public float WarpAppearLead = .06f;
    /// <summary>収束点・一閃の高さ（足元からm）。</summary>
    [Min(0f), Tooltip("収束点・一閃の高さ（足元からm）。")]
    public float WarpEffectHeight = 1.1f;
    /// <summary>一閃を敵の前方へ出す距離（m）。</summary>
    [Min(0f), Tooltip("一閃を敵の前方へ出す距離（m）。")]
    public float WarpSlashForward = .8f;
    /// <summary>一閃の向き。敵の右方向を、敵の前方軸まわりに回した角度。負で右下がり。</summary>
    [Range(-180f, 180f), Tooltip("一閃の向き。敵の右方向を、敵の前方軸まわりに回した角度。負で右下がり。")]
    public float WarpSlashRoll = -35f;
    /// <summary>一閃の大きさ（基準長2m）。</summary>
    [Range(.3f, 2f), Tooltip("一閃の大きさ（基準長2m）。")]
    public float WarpSlashScale = .9f;
    /// <summary>Art製の消失・出現VFXのいずれかが設定され、旧ワープ演出の代わりに使われるか。</summary>
    public bool UsesArtWarpVFX => WarpVanishVFX != null || WarpAppearVFX != null;
    /// <summary>WarpAttack：刀を挙げ切る瞬間（元Clip秒）。消える前はここで止めて溜め、出現後はこの姿勢から振り下ろす。WindupEndTime より前にする。</summary>
    [Range(0f, 1f), Tooltip("WarpAttack：刀を挙げ切る瞬間（元Clip秒）。消える前はここで止めて溜め、出現後はこの姿勢から振り下ろす。WindupEndTime より前にする。")]
    public float WarpRaiseClipTime = .21f;   // EnemyWarpAttack: right hand reaches its peak (~2.18 m) at ~0.21 s
    /// <summary>WarpAttack：刀を挙げ切ってから消えるまでの「間」（秒）。今から攻撃するという予兆。</summary>
    [Min(0f), Tooltip("WarpAttack：刀を挙げ切ってから消えるまでの「間」（秒）。今から攻撃するという予兆。")]
    public float WarpHoldDuration = .45f;
    /// <summary>刀を挙げ切る元Clip秒（WindupEndTime より必ず前）。</summary>
    public float WarpRaiseTime => Mathf.Clamp(WarpRaiseClipTime, 0f, Mathf.Max(0f, WindupEndTime - .01f));
    /// <summary>消える前の予備動作の実時間：刀を挙げる（通常速度）＋「間」。</summary>
    public float WarpDepartureDuration => WarpRaiseTime + WarpHoldDuration;

    /// <summary>ワープ時の本体の揺らぎ・消失・影からの再形成を定義する。</summary>
    [System.Serializable]
    public sealed class WarpGhostSettings
    {
        /// <summary>体そのものを黒い幽霊のように崩して消し、出現先で黒い影から形作る。</summary>
        [Tooltip("体そのものを黒い幽霊のように崩して消し、出現先で黒い影から形作る。")]
        public bool Enabled = true;
        /// <summary>黒い影・揺らぎの色（Alphaが濃さ）。</summary>
        [Tooltip("黒い影・揺らぎの色（Alphaが濃さ）。")]
        public Color Color = new Color(.02f, .02f, .035f, .85f);
        /// <summary>崩れていく縁の色。暗い青灰で輪郭を読ませる。</summary>
        [Tooltip("崩れていく縁の色。暗い青灰で輪郭を読ませる。")]
        public Color EdgeColor = new Color(.18f, .22f, .32f, 1f);
        /// <summary>予兆（消える前の構え）の終わりに体へ乗る黒い揺らぎの強さ。</summary>
        [Range(0f, 1f), Tooltip("予兆（消える前の構え）の終わりに体へ乗る黒い揺らぎの強さ。")]
        public float WarningStrength = .9f;
        /// <summary>予兆中の輪郭の揺れ幅。</summary>
        [Range(0f, .04f), Tooltip("予兆中の輪郭の揺れ幅。")]
        public float WarningSway = .012f;
        /// <summary>消失：黒い影が足元から崩れ切るまでの秒数。</summary>
        [Range(.05f, .6f), Tooltip("消失：黒い影が足元から崩れ切るまでの秒数。")]
        public float VanishDuration = .24f;
        /// <summary>出現：実体が現れる何秒前から、出現先に黒い影が形を取り始めるか。DisappearDuration以下に収まる。</summary>
        [Range(0f, .5f), Tooltip("出現：実体が現れる何秒前から、出現先に黒い影が形を取り始めるか。DisappearDuration以下に収まる。")]
        public float FormLead = .16f;
        /// <summary>出現後、体に残る黒い揺らぎが抜けるまでの秒数。</summary>
        [Range(.05f, .8f), Tooltip("出現後、体に残る黒い揺らぎが抜けるまでの秒数。")]
        public float AppearFade = .28f;
        /// <summary>崩れながら上へ立ち上る量（m）。</summary>
        [Range(0f, 1f), Tooltip("崩れながら上へ立ち上る量（m）。")]
        public float Drift = .25f;
    }
    /// <summary>WarpAttack の黒い幽霊のような消失・出現（体の揺らぎ・影の崩れ・影からの形成）。</summary>
    [Tooltip("WarpAttack の黒い幽霊のような消失・出現（体の揺らぎ・影の崩れ・影からの形成）。")]
    public WarpGhostSettings WarpGhost = new WarpGhostSettings();
    /// <summary>参照する攻撃データがWarpAttackか。</summary>
    public bool IsWarp => Attack != null && Attack.ActionType == EnemyActionType.WarpAttack;

    /// <summary>抽選時と開始時で共有する距離条件。MidのWarpだけはAIの距離帯を優先し、他は既存Min/Maxを使う。</summary>
    public bool IsInSelectionRange(float distance, EnemyDecisionConfig decisionConfig)
    {
        if (!float.IsFinite(distance) || distance < 0f) return false;
        if (IsWarp && decisionConfig != null && decisionConfig.IsMidDistance(distance)) return true;
        return distance >= MinAttackDistance && distance <= MaxAttackDistance;
    }
    /// <summary>現在の構え時間（秒）。Warpでは再出現後、それ以外ではWindupDurationの時間。</summary>
    public float ActiveWindupDuration => IsWarp ? ReappearWindupDuration : WindupDuration;
    // Warp の出現後の構えは、刀を挙げ切った姿勢で完全に止めて待つ（EnemyController.Warp が速度0→通常速度を切り替える）。
    /// <summary>構え区間を指定時間に収める基準再生倍率。Warpの姿勢保持中は制御側で別途速度を止める。</summary>
    public float WindupSpeed => ActiveWindupDuration > 0f && WindupEndTime > 0f
        ? WindupEndTime / ActiveWindupDuration : PlaybackSpeed;
    /// <summary>完全なAnimator Stateパスから算出した識別ハッシュ。</summary>
    public int StateHash => Animator.StringToHash(AnimatorState ?? "");
    /// <summary>実行に必要な参照・State名・時間・距離・倍率が有効な範囲にあるか。</summary>
    public bool IsValid => Attack != null && !string.IsNullOrWhiteSpace(Attack.AnimatorTrigger) &&
        !string.IsNullOrWhiteSpace(AnimatorState) && Valid(WindupDuration) && Valid(WindupEndTime) && Valid(RecoveryDuration) &&
        Valid(Timeout) && Timeout > 0f && Valid(RootMotionScale) && Valid(Cooldown) &&
        Valid(MinAttackDistance) && Valid(MaxAttackDistance) && MaxAttackDistance >= MinAttackDistance && Valid(MaximumTravelDistance) &&
        (!UsesDashTuning || !OverrideWeaponGrip || (Finite(WeaponGripLocalPosition) && Finite(WeaponGripLocalEuler))) &&
        (!UsesDashTuning || (WindupDuration >= .01f && WindupEndTime > 0f && Valid(DashEndTime) && DashEndTime > WindupEndTime &&
            Valid(DashDistance) && DashDistance >= .1f && Valid(DashSpeed) && DashSpeed >= .1f &&
            Valid(MeasuredDashDistance) && MeasuredDashDistance >= .001f && float.IsFinite(DashPlaybackSpeed))) &&
        (!IsWarp || (Valid(TeleportDistance) && TeleportDistance > 0f && float.IsFinite(TeleportAngleOffset) &&
            Mathf.Abs(TeleportAngleOffset) <= 90f && Valid(DisappearDuration) && Valid(ReappearWindupDuration) && ReappearWindupDuration >= .1f)) &&
        float.IsFinite(PlaybackSpeed) && PlaybackSpeed >= .5f && PlaybackSpeed <= 1.5f;
    private static bool Valid(float value) => float.IsFinite(value) && value >= 0f;
    private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
}
