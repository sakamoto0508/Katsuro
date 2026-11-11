using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class SetupGameViewV2
{
    public static string Main()
    {
        var asm=typeof(Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");
        var sizes=sizesType.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var group=sizesType.GetProperty("currentGroup").GetValue(sizes);var gt=group.GetType();
        int count=(int)gt.GetMethod("GetTotalCount").Invoke(group,null),index=-1;
        for(int i=0;i<count;i++)
        {
            var size=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});var st=size.GetType();
            if((int)st.GetProperty("width").GetValue(size)==1920&&(int)st.GetProperty("height").GetValue(size)==1080&&st.GetProperty("sizeType").GetValue(size).ToString()=="FixedResolution")index=i;
        }
        if(index<0)
        {
            var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1920,1080,"Katsuro UI 1080p"});
            gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});index=count;
        }
        var viewType=asm.GetType("UnityEditor.GameView");var window=EditorWindow.GetWindow(viewType);
        SessionState.SetInt("KatsuroUI.PreviousSize",(int)viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(window));
        viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(window,index);window.Show();window.Focus();window.Repaint();
        return "Game view fixed to 1920 x 1080; selected index "+index;
    }
}
