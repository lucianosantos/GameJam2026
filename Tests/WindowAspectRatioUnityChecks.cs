using System;
using GameJam2026;
using UnityEditor;
using UnityEngine;
public static class WindowAspectRatioUnityChecks
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static object Main()
    {
        Check(PlayerSettings.resizableWindow,"Resizable window enabled");
        var previous=new Vector2Int(1280,720);
        var maximum=new Vector2Int(1888,976);
        Check(WindowAspectRatio.ConstrainSize(new Vector2Int(1600,720),previous,maximum)==new Vector2Int(1600,900),"Width resize");
        Check(WindowAspectRatio.ConstrainSize(new Vector2Int(1280,540),previous,maximum)==new Vector2Int(960,540),"Height resize");
        Check(WindowAspectRatio.ConstrainSize(new Vector2Int(1280,720),previous,maximum)==previous,"Stable resolution");
        for(int w=200;w<=4000;w+=37) for(int h=100;h<=2200;h+=31)
        {
            var size=WindowAspectRatio.ConstrainSize(new Vector2Int(w,h),previous,maximum);
            Check(size.x*9==size.y*16,"16:9 ratio");
            Check(size.x<=maximum.x && size.y<=maximum.y && size.x>0 && size.y>0,"Display bounds");
        }
        return new {resizable=PlayerSettings.resizableWindow,defaultWidth=PlayerSettings.defaultScreenWidth,defaultHeight=PlayerSettings.defaultScreenHeight,checks="Width, height, stable size, aspect and bounds across 7000+ sizes passed"};
    }
}
