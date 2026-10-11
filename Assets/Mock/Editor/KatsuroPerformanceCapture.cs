using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Profiler = UnityEngine.Profiling.Profiler;

/// <summary>製品Buildに含まれない、実時間のフレーム計測とProfiler保存用のEditor診断機能。</summary>
[InitializeOnLoad]
public static class KatsuroPerformanceCapture
{
    private struct Sample { public double time, ms; public int frame, scene, combat, collections, hits, justs, counters; public float scale, hp; public long[] values; public double gpuMs; }
    private static readonly string[] CounterNames = { "Main Thread", "GPU Frame Time", "GC Allocated In Frame", "Total Used Memory", "Total Reserved Memory", "GC Used Memory", "GC Reserved Memory", "Texture Memory", "Mesh Memory", "Draw Calls Count", "Batches Count", "SetPass Calls Count", "Triangles Count", "PlayerLoop", "Update.ScriptRunBehaviourUpdate", "Physics.Simulate", "WaitForTargetFPS" };
    private static ProfilerRecorder[] recorders;
    private static Sample[] samples;
    private static readonly FrameTiming[] timing = new FrameTiming[1];
    private static readonly List<string> scenes = new List<string>();
    private static int count, lastFrame, hits, justs, counters;
    private static double start, previous, warmup, seconds, nextInput;
    private static bool active, previousProfiler, previousGPU, previousEditor, previousBackground;
    private static string label, mode, output;
    private static PlayerController player;
    private static EnemyController enemy;
    private static InputBuffer input;
    private static bool pulse;
    /// <summary>Editorの更新とPlay終了時の後処理を登録する。Playしていない間は計測しない。</summary>
    static KatsuroPerformanceCapture() { EditorApplication.update += Tick; EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingPlayMode && active) Stop(); }; AssemblyReloadEvents.beforeAssemblyReload += () => { if(active) Stop(); }; }
    /// <summary>現在のPlay画面を10秒安定させてから30秒間記録する。</summary>
    [MenuItem("Katsuro/Profiling/Capture current play - warm 10s, sample 30s")]
    public static void CaptureCurrent() { Begin("Manual", "Manual", 10, 30); }
    /// <summary>進行中の計測を終了し、CSVとProfiler履歴を保存して診断設定を復元する。</summary>
    [MenuItem("Katsuro/Profiling/Stop and save")]
    public static void StopMenu() { if(active) Stop(); }
    /// <summary>利用可能なカウンターだけを開始する。品質、AI、HP、時間倍率、戦闘仕様は変更しない。</summary>
    public static string Begin(string captureLabel, string inputMode = "Manual", double warmSeconds = 10, double duration = 30)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Modeで実行してください。");
        if (active) throw new InvalidOperationException("計測中です。");
        label = captureLabel; mode = inputMode; warmup = warmSeconds; seconds = duration;
        output = Path.GetFullPath("ProfilingResults/" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + label);
        Directory.CreateDirectory(output);
        previousProfiler = ProfilerDriver.enabled; previousGPU = ProfilerDriver.profileGPU; previousEditor = ProfilerDriver.profileEditor; previousBackground = Application.runInBackground;
        if (ProfilerDriver.deepProfiling) throw new InvalidOperationException("Deep Profileを解除してから計測してください。");
        ProfilerDriver.profileEditor = false; ProfilerDriver.profileGPU = SystemInfo.supportsGpuRecorder; ProfilerDriver.enabled = true;
        Application.runInBackground = true;
        var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
        recorders = new ProfilerRecorder[CounterNames.Length];
        var available = new StringBuilder("Counter,Available,Unit\n");
        for (int i = 0; i < CounterNames.Length; i++) {
            foreach(var h in handles) { var d = ProfilerRecorderHandle.GetDescription(h); if(d.Name != CounterNames[i]) continue; recorders[i] = ProfilerRecorder.StartNew(d.Category, d.Name, 1); available.AppendLine(d.Name + ",true," + d.UnitType); break; }
            if(!recorders[i].Valid) available.AppendLine(CounterNames[i] + ",false,");
        }
        File.WriteAllText(output + "/Counters.csv", available.ToString());
        samples = new Sample[30000]; for(int i=0;i<samples.Length;i++) samples[i].values = new long[CounterNames.Length];
        scenes.Clear(); count = hits = justs = counters = 0; lastFrame = -1; pulse = false;
        player = GameManager.Instance ? GameManager.Instance.Player : null;
        enemy = UnityEngine.Object.FindFirstObjectByType<EnemyController>(); input = UnityEngine.Object.FindFirstObjectByType<InputBuffer>();
        if(player) player.JustAvoidSucceeded += OnJust;
        if(enemy) enemy.HitReceived += OnHit;
        var environment = "UTC=" + DateTime.UtcNow.ToString("O") + "\nUnity=" + Application.unityVersion + "\nCPU=" + SystemInfo.processorType + "\nGPU=" + SystemInfo.graphicsDeviceName + "\nRAM_MB=" + SystemInfo.systemMemorySize + "\nAPI=" + SystemInfo.graphicsDeviceType + "\nResolution=" + Screen.width + "x" + Screen.height + "\nQuality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + "\nVSync=" + QualitySettings.vSyncCount + "\nTargetFPS=" + Application.targetFrameRate + "\nEditor=true; Standalone=false; DeepProfile=" + ProfilerDriver.deepProfiling + "\nGPU_Support=" + ProfilerDriver.isGPUProfilerSupported + "\nWarmup=" + warmup + " SampleSeconds=" + seconds + " Input=" + mode + "\nEditor CPU / GC / render counters include Editor overhead; missing counter=-1. GPU timing availability must be checked.\n";
        File.WriteAllText(output + "/Environment.txt", environment);
        start = previous = EditorApplication.timeSinceStartup; nextInput = start + 1; active = true;
        return output;
    }
    /// <summary>実際に通知されたジャスト回避成功数を数える。判定や成功状態は注入しない。</summary>
    private static void OnJust(DamageInfo info) { justs++; }
    /// <summary>実際の命中通知と追撃属性を記録する。ダメージや判定を変更しない。</summary>
    private static void OnHit(DamageInfo info) { hits++; if(info.IsJustAvoidCounter) counters++; }
    /// <summary>実時間とフレーム番号で重複を除き、固定容量へ記録する。CSV化は計測終了後に行う。</summary>
    private static void Tick()
    {
        if(!active || !EditorApplication.isPlaying || EditorApplication.isPaused || Time.frameCount == lastFrame) return;
        double now = EditorApplication.timeSinceStartup; lastFrame = Time.frameCount;
        if(now - start >= warmup && count < samples.Length) {
            ref var s = ref samples[count++]; s.time = now - start; s.ms = (now - previous) * 1000; s.frame = Time.frameCount;
            var name = SceneManager.GetActiveScene().name; s.scene = scenes.IndexOf(name); if(s.scene<0){s.scene=scenes.Count; scenes.Add(name);}
            var game = GameManager.Instance; s.combat = game && game.IsCombatActive ? 1 : 0; s.hp = game && game.Player ? game.Player.Hp : -1;
            s.scale = Time.timeScale; s.collections = GC.CollectionCount(0); s.hits = hits; s.justs = justs; s.counters = counters;
            for(int i=0;i<recorders.Length;i++) s.values[i] = recorders[i].Valid && recorders[i].Count > 0 ? recorders[i].LastValue : -1;
            FrameTimingManager.CaptureFrameTimings(); s.gpuMs = FrameTimingManager.GetLatestTimings(1,timing)>0 && timing[0].gpuFrameTime>0 ? timing[0].gpuFrameTime : -1;
        }
        previous = now;
        if (mode != "Manual" && mode != "Idle" && now >= nextInput && input && input.enabled) {
            nextInput = now + (pulse ? 0.08 : 0.65); pulse = !pulse;
            if(Mouse.current != null){var mouse=new MouseState();if(pulse && (mode=="Attack" || justs>counters)){mouse=mouse.WithButton(((int)((now-start)/4)%2)==0?MouseButton.Left:MouseButton.Right);}InputSystem.QueueStateEvent(Mouse.current,mouse);}
            if(Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, mode=="Avoid" && pulse ? new KeyboardState(Key.Space) : new KeyboardState());
        }
        if(now - start >= warmup + seconds || count == samples.Length) Stop();
    }
    /// <summary>計測後にProfiler履歴・代表フレームのTimeline/HierarchyとフレームCSVを保存する。</summary>
    public static string Stop()
    {
        if(!active) return "Not capturing";
        active=false; ProfilerDriver.enabled=false;
        try {
            if(player) player.JustAvoidSucceeded -= OnJust; if(enemy) enemy.HitReceived -= OnHit;
            if(mode!="Manual" && mode!="Idle"){if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState());if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());}
            foreach(var r in recorders) if(r.Valid) r.Dispose();
            var b=new StringBuilder("frame,elapsed_real_s,frame_real_ms,scene,combat,timeScale,hp,gcCollections,hits,justSuccess,counterHits,frameTimingGpuMs,"+string.Join(",",CounterNames)+"\n");
            for(int i=0;i<count;i++){var s=samples[i];b.Append(s.frame).Append(',').Append(s.time.ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append(s.ms.ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append(scenes[s.scene]).Append(',').Append(s.combat).Append(',').Append(s.scale.ToString(CultureInfo.InvariantCulture)).Append(',').Append(s.hp.ToString(CultureInfo.InvariantCulture)).Append(',').Append(s.collections).Append(',').Append(s.hits).Append(',').Append(s.justs).Append(',').Append(s.counters).Append(',').Append(s.gpuMs.ToString(CultureInfo.InvariantCulture));foreach(var v in s.values)b.Append(',').Append(v);b.AppendLine();}
            File.WriteAllText(output+"/Frames.csv",b.ToString());
            if(ProfilerDriver.lastFrameIndex>=0){ProfilerDriver.SaveProfile(output+"/Timeline.raw");ExportTimeline(output);}
            File.WriteAllText(output+"/Complete.txt","samples="+count+" hits="+hits+" justSuccess="+justs+" counterHits="+counters+"\nProfiler history is a rolling window (not necessarily the full CSV period). Timeline export after capture is excluded from measurements.");
            File.WriteAllText("ProfilingResults/Latest.txt",output);
        } finally { ProfilerDriver.profileGPU=previousGPU;ProfilerDriver.profileEditor=previousEditor;ProfilerDriver.enabled=previousProfiler;Application.runInBackground=previousBackground;samples=null;recorders=null; }
        return output;
    }
    /// <summary>履歴末尾と最大時間のフレームを、スレッド別の開始時刻・Total・Self付きで書き出す。</summary>
    private static void ExportTimeline(string path)
    {
        int last=ProfilerDriver.lastFrameIndex, worst=last;float worstMs=0;
        for(int f=Math.Max(ProfilerDriver.firstFrameIndex,last-60);f<=last;f++){using(var v=ProfilerDriver.GetRawFrameDataView(f,0)){if(v.valid && v.frameTimeMs>worstMs){worstMs=v.frameTimeMs;worst=f;}}}
        var b=new StringBuilder("frame,thread,sample,parent,start_ms,total_ms,self_ms,name\n");
        foreach(int f in new[]{Math.Max(ProfilerDriver.firstFrameIndex,last-2),worst}.Distinct()){
            for(int t=0;t<128;t++){using(var v=ProfilerDriver.GetRawFrameDataView(f,t)){if(!v.valid)break;var parent=new Stack<int>();var end=new Stack<int>();for(int i=0;i<v.sampleCount;i++){while(end.Count>0&&i>=end.Peek()){end.Pop();parent.Pop();}int p=parent.Count>0?parent.Peek():-1;float total=v.GetSampleTimeMs(i),self=total;int children=v.GetSampleChildrenCount(i);for(int c=i+1,k=0;k<children;k++,c+=1+v.GetSampleChildrenCountRecursive(c))self-=v.GetSampleTimeMs(c);b.Append(f).Append(',').Append('"').Append((v.threadName ?? "Unnamed").Replace("\"","\"\"")).Append('"').Append(',').Append(i).Append(',').Append(p).Append(',').Append(v.GetSampleStartTimeMs(i).ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append(total.ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append(Math.Max(0,self).ToString("F6",CultureInfo.InvariantCulture)).Append(',').Append('"').Append((v.GetSampleName(i) ?? "Counter").Replace("\"","\"\"")).Append('"').AppendLine();if(children>0){parent.Push(i);end.Push(i+1+v.GetSampleChildrenCountRecursive(i));}}}}
        }
        File.WriteAllText(path+"/TimelineSamples.csv",b.ToString());
    }
    /// <summary>現在の診断状態を返す。外部ツールから進行確認に利用する。</summary>
    public static string Status() { return "active="+active+" label="+label+" elapsed="+(EditorApplication.timeSinceStartup-start).ToString("F1")+" samples="+count+" hits="+hits+" just="+justs+" counter="+counters+" scene="+SceneManager.GetActiveScene().name+" resolution="+Screen.width+"x"+Screen.height+" combat="+(GameManager.Instance&&GameManager.Instance.IsCombatActive); }
}

