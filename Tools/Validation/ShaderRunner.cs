using System;
using UnityEditor;
using UnityEngine;
public static class ShaderRunner
{
    public static void Run()
    {
        try
        {
            var shader = Resources.Load<Shader>("CombatGlow");
            if (shader == null) throw new Exception("Shader missing");
            var material = new Material(shader);
            ShaderUtil.CompilePass(material, 0, true);
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
            {
                Debug.Log(message.severity + ": " + message.message);
                if (message.severity.ToString() == "Error") throw new Exception(message.message);
            }
            Debug.Log("SHADER_RESULT: passed; supported=" + shader.isSupported);
            UnityEngine.Object.DestroyImmediate(material);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
