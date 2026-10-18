using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class PrepareUALHumanoid
{
 public const string Source="Assets/InportAssets/Universal Animation Library 2[Standard]/Universal Animation Library 2[Standard]/Unity/";
 public const string Folder="Assets/Mock/Animation/Enemy/UAL2Combos";
 public const string Working="Assets/InportAssets/UAL2EnemyImportWork";
 public static void Run()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode only");
  if(!AssetDatabase.IsValidFolder(Working))AssetDatabase.CreateFolder("Assets/InportAssets","UAL2EnemyImportWork");
  string path=Working+"/UAL2_Enemy_Humanoid_RM.fbx";
  if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null && !AssetDatabase.CopyAsset(Source+"UAL2_Standard_RM.fbx",path))throw new Exception("copy failed");
  var source=(ModelImporter)AssetImporter.GetAtPath(Source+"UAL2_Standard.fbx");
  var dest=(ModelImporter)AssetImporter.GetAtPath(path);
  dest.animationType=ModelImporterAnimationType.Human;dest.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  dest.humanDescription=source.humanDescription;dest.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
  File.WriteAllText("Artifacts/UAL2Combo/RMImport.txt","Avatar valid="+avatar?.isValid+" human="+avatar?.isHuman+"\n"+string.Join("\n",AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")&&c.name.Contains("Combo")).Select(c=>c.name+" human="+c.humanMotion+" length="+c.length+" root="+c.hasRootCurves+" speed="+c.averageSpeed)));
  if(avatar==null||!avatar.isValid||!avatar.isHuman)throw new Exception("RM Humanoid Avatar invalid");
  if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Mock/Animation/Enemy","UAL2Combos");
  foreach(string kind in new[]{"Regular","Heavy"})
  {
   var original=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>c.name=="Armature|Sword_"+kind+"_Combo");
   var clip=UnityEngine.Object.Instantiate(original);clip.name="EnemyUAL2"+kind+"Combo";
   var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlendPositionY=true;settings.loopBlendPositionXZ=false;settings.keepOriginalPositionY=true;
   AnimationUtility.SetAnimationClipSettings(clip,settings);
   string output=Folder+"/"+clip.name+".anim";
   if(AssetDatabase.LoadAssetAtPath<AnimationClip>(output)!=null)throw new Exception("Output exists: "+output);
   AssetDatabase.CreateAsset(clip,output);AssetDatabase.SaveAssetIfDirty(clip);
  }
  File.Copy(path+".meta","Artifacts/UAL2Combo/HumanoidRMImporter.meta.txt",true);
 }
}
