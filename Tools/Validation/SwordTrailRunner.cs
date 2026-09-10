using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SwordTrailRunner
{
    static void Check(bool ok,string name) { if(!ok)throw new Exception(name);Debug.Log("PASS: "+name); }
    static void Call(SwordTrail effect,string method) => typeof(SwordTrail).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(effect,null);
    public static void Run()
    {
        try
        {
            var go=new GameObject("Authored blade",typeof(BoxCollider),typeof(TrailRenderer),typeof(SwordTrail));
            var collider=go.GetComponent<BoxCollider>();var trail=go.GetComponent<TrailRenderer>();var effect=go.GetComponent<SwordTrail>();
            trail.emitting=false;
            SwordTrail.SetActive(collider,true);
            Check(!trail.emitting,"Activation does not implicitly initialize");
            Check(effect.Init(),"Explicit Init succeeds");
            SwordTrail.SetActive(collider,true);
            var material=trail.sharedMaterial;
            Check(trail.emitting&&material!=null,"Activation after Init uses authored renderer");
            effect.Init();
            Check(trail.emitting&&trail.sharedMaterial==material,"Repeated Init preserves activation and material");
            Check(go.GetComponentsInChildren<TrailRenderer>().Length==1&&go.transform.childCount==0,"No extra renderer or object is created");
            SwordTrail.SetActive(collider,false);Check(!trail.emitting,"Attack end stops emission");
            SwordTrail.SetActive(collider,true);Call(effect,"OnDisable");Check(!trail.emitting,"Disable stops emission");
            effect.Init();Check(trail.sharedMaterial==material,"Repeated initialization reuses material");
            UnityEngine.Object.DestroyImmediate(go);
            var other=new GameObject("Unconfigured blade",typeof(BoxCollider));
            SwordTrail.SetActive(other.GetComponent<BoxCollider>(),true);
            Check(other.GetComponent<SwordTrail>()==null,"Missing component is not dynamically added");
            UnityEngine.Object.DestroyImmediate(other);
            Debug.Log("SWORD_TRAIL_RESULT: 9 passed");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
