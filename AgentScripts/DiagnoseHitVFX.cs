using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text;
public static class DiagnoseHitVFX
{
    static void Describe(GameObject go,StringBuilder b)
    {
        foreach(var p in go.GetComponentsInChildren<PlayerController>(true))
        {
            var cfg=p.FeedbackConfig;
            var f=p.GetComponent<CombatFeedback>();
            b.AppendLine("PLAYER "+p.name+" active="+p.gameObject.activeInHierarchy+" feedback="+(f!=null)+" enabled="+(f!=null&&f.enabled)+" config="+AssetDatabase.GetAssetPath(cfg)+" hit="+(cfg!=null?AssetDatabase.GetAssetPath(cfg.HitVFX):"NULL"));
        }
        foreach(var e in go.GetComponentsInChildren<EnemyController>(true))
        {var f=e.GetComponent<CombatFeedback>();b.AppendLine("ENEMY "+e.name+" feedback="+(f!=null)+" enabled="+(f!=null&&f.enabled));}
        foreach(var g in go.GetComponentsInChildren<GameManager>(true))
        {var so=new SerializedObject(g);b.AppendLine("GAMEMANAGER "+g.name+" enabled="+g.enabled+" active="+g.gameObject.activeInHierarchy+" player="+so.FindProperty("_playerController").objectReferenceValue+" enemy="+so.FindProperty("_enemyController").objectReferenceValue+" camera="+so.FindProperty("_camera").objectReferenceValue);}
        foreach(var c in go.GetComponentsInChildren<Camera>(true))b.AppendLine("CAMERA "+c.name+" mask="+c.cullingMask+" enabled="+c.enabled+" near="+c.nearClipPlane+" far="+c.farClipPlane);
    }
    public static string Main()
    {
        var b=new StringBuilder();b.AppendLine("PlayMode="+EditorApplication.isPlaying);
        for(int i=0;i<SceneManager.sceneCount;i++)
        {var s=SceneManager.GetSceneAt(i);b.AppendLine("LOADED "+s.path);foreach(var go in s.GetRootGameObjects())Describe(go,b);}
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Mock/Scenes"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try{b.AppendLine("DISK SCENE "+path);foreach(var go in scene.GetRootGameObjects())Describe(go,b);}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/Effects/SwordHitVFX.prefab");
        var preview=EditorSceneManager.NewPreviewScene();
        var clone=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);
        try
        {
            clone.SetActive(true);
            b.AppendLine("PREFAB root active="+prefab.activeSelf+" layer="+prefab.layer);
            foreach(var p in clone.GetComponentsInChildren<ParticleSystem>(true))
            {
                var r=p.GetComponent<ParticleSystemRenderer>();
                var streams=new System.Collections.Generic.List<ParticleSystemVertexStream>();r.GetActiveVertexStreams(streams);
                b.AppendLine(p.name+" active="+p.gameObject.activeSelf+" renderer="+r.enabled+" layer="+p.gameObject.layer+" streams="+string.Join(",",streams)+" culling="+p.main.cullingMode+" alpha="+p.main.startColor.colorMax.a+" queue="+r.sharedMaterial.renderQueue+" passes="+r.sharedMaterial.passCount);
                p.useAutoRandomSeed=false;p.randomSeed=42;
                p.Simulate(.05f,false,true,true);
                var particles=new ParticleSystem.Particle[p.main.maxParticles];int count=p.GetParticles(particles);
                b.AppendLine("SIMULATE t=.05 "+p.name+" alive="+count+" emission="+p.emission.enabled+" burstProbability="+p.emission.GetBurst(0).probability);
                if(count>0)b.AppendLine("particle position="+particles[0].position+" size="+particles[0].GetCurrentSize(p)+" color="+particles[0].GetCurrentColor(p));
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        return b.ToString();
    }
}
