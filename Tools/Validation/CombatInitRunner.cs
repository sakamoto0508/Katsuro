using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CombatInitRunner
{
    static void Check(bool ok,string name) { if(!ok)throw new Exception(name);Debug.Log("PASS: "+name); }
    public static void Run()
    {
        try
        {
            var root=new GameObject("Authored character",typeof(SkinnedMeshRenderer),typeof(CombatFeedback));
            var renderer=root.GetComponent<SkinnedMeshRenderer>();
            var original=new Material(Resources.Load<Shader>("CombatGlow"));
            renderer.sharedMaterials=new[]{original};
            var feedback=root.GetComponent<CombatFeedback>();
            feedback.Init();feedback.SetGhost(true);
            var ghost=renderer.sharedMaterial;
            Check(ghost!=original,"Init prepares ghost material before use");
            feedback.Init();Check(renderer.sharedMaterial==ghost,"Repeated Init preserves active ghost and material");
            feedback.SetGhost(false);Check(renderer.sharedMaterial==original,"Ghost exit restores original material after repeated Init");
            feedback.SetGhost(true);
            typeof(CombatFeedback).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(feedback,null);
            Check(renderer.sharedMaterial==original,"Disable restores original material");
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(original);
            Debug.Log("COMBAT_INIT_RESULT: 4 passed");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
