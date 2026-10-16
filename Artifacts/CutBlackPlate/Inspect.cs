using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class CutBlackPlateInspect
{
    public static object Main()
    {
        var paths = new[] { "Assets/Mock/Effects/JustAvoidCounterFirstCutCFX.prefab", "Assets/Mock/Effects/JustAvoidCounterFinisherCFX.prefab" };
        var prefabs = paths.Select(path => {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var so = new SerializedObject(root.GetComponent<JustAvoidCounterCutVFX>());
            var renderer = (MeshRenderer)so.FindProperty("_cutRenderer").objectReferenceValue;
            var material = renderer.sharedMaterial;
            ShaderUtil.CompilePass(material, 0, true);
            var properties = Enumerable.Range(0, ShaderUtil.GetPropertyCount(material.shader)).ToDictionary(
                n => ShaderUtil.GetPropertyName(material.shader, n), n => {
                    string name = ShaderUtil.GetPropertyName(material.shader, n);
                    var type = ShaderUtil.GetPropertyType(material.shader, n);
                    if (type == ShaderUtil.ShaderPropertyType.Color || type == ShaderUtil.ShaderPropertyType.Vector)
                        return (object)material.GetVector(name).ToString("R");
                    return (object)material.GetFloat(name);
                });
            return new { path, renderer = renderer.name, renderer.enabled,
                material = AssetDatabase.GetAssetPath(material), shader = AssetDatabase.GetAssetPath(material.shader),
                material.renderQueue, material.passCount, material.shader.isSupported,
                pass = material.GetPassName(0), renderer.sortingOrder, renderer.sortingLayerID,
                pipelineTag = material.GetTag("RenderPipeline", false), queueTag = material.GetTag("Queue", false),
                mesh = AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh),
                cutSize = so.FindProperty("_cutSize").vector2Value.ToString("R"), properties,
                compiled = ShaderUtil.IsPassCompiled(material, 0),
                messages = ShaderUtil.GetShaderMessages(material.shader).Select(m => new { m.message, m.line, severity = m.severity.ToString() }).ToArray() };
        }).ToArray();
        var result = new { playing = EditorApplication.isPlaying, graphicsAPI = SystemInfo.graphicsDeviceType.ToString(), prefabs };
        File.WriteAllText("Artifacts/CutBlackPlate/AssetInspection.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
