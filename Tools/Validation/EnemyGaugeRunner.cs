using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mock.UI;

public static class EnemyGaugeRunner
{
    static int count;
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static void Call(object o,string m,params object[] args)=>o.GetType().GetMethod(m,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Debug.Log("PASS: "+name);}
    static void Move(EventSystem es,MoveDirection direction)
    {
        ExecuteEvents.Execute(es.currentSelectedGameObject,new AxisEventData(es){moveDir=direction},ExecuteEvents.moveHandler);
    }
    public static void Run()
    {
        try {
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/BattleHUD.prefab"));
            var gauge=root.GetComponentInChildren<DamageTrailGauge>();var fill=Get<Image>(gauge,"_currentFill");var trail=Get<Image>(gauge,"_trailFill");
            Check(Get<DamageTrailGauge>(root.GetComponent<RunHUD>(),"_enemyGauge")==gauge,"HUD binds authored enemy gauge");
            Check(trail.transform.GetSiblingIndex()<fill.transform.GetSiblingIndex(),"Trail renders behind current HP");
            gauge.SetNormalized(1);gauge.SetNormalized(.5f);float until=Get<float>(gauge,"_catchupAt");
            Check(fill.fillAmount==.5f&&trail.fillAmount==1,"Enemy damage immediately reduces current layer");
            gauge.SetNormalized(.5f);Check(Get<float>(gauge,"_catchupAt")==until,"Per-frame unchanged HP does not restart delay");
            Call(gauge,"Tick",until-.01f,1f);Check(trail.fillAmount==1,"Trail waits");
            Call(gauge,"Tick",until+.2f,.2f);Check(trail.fillAmount<1&&trail.fillAmount>.5f,"Trail gradually catches up");
            Call(gauge,"Tick",until+2,2f);Check(trail.fillAmount==.5f,"Trail reveals background");
            gauge.SetNormalized(.8f);Check(trail.fillAmount==.8f,"Healing synchronizes layers");
            var color=fill.color;var sprite=fill.sprite;fill.color=Color.magenta;fill.sprite=trail.sprite;
            var replacement=fill.sprite;gauge.SetNormalized(.4f);Call(gauge,"Tick",999f,1f);
            Check(fill.color==Color.magenta&&fill.sprite==replacement,"Authored color and sprite remain unchanged");fill.color=color;fill.sprite=sprite;
            var view=root.GetComponent<PlayerHUDView>();view.SetSkillNormalized(.3f);
            var skill=Get<Image>(view,"_skillFill");Check(Mathf.Approximately(skill.fillAmount,.3f)&&skill.transform.parent.parent.name=="SkillGauge","Original skill gauge receives resource updates");
            view.SetHpNormalized(1);view.SetHpNormalized(.7f);gauge.SetNormalized(1);gauge.SetNormalized(.6f);Call(gauge,"Tick",Get<float>(gauge,"_catchupAt")+.3f,.3f);
            root.GetComponent<CanvasGroup>().alpha=1;
            typeof(HealthTrailRunner).GetMethod("Preview",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"enemy-gauge-preview.png"});
            UnityEngine.Object.DestroyImmediate(root);

            var es=new GameObject("EventSystem",typeof(EventSystem)).GetComponent<EventSystem>();
            Call(es,"OnEnable");EventSystem.current=es; root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mock/UI/RunSetupCanvas.prefab"));
            var setup=root.GetComponent<RunSetupUI>();setup.Init();RunSession.Begin("Navigation test",0,0);
            setup.Open(new GameObject("Title").AddComponent<TitleManager>());
            var attack=Get<Toggle[]>(setup,"_attackOptions");var defense=Get<Toggle[]>(setup,"_defenseOptions");
            Move(es,MoveDirection.Right);Check(attack[1].isOn&&!attack[0].isOn&&Get<int>(setup,"_attack")==1,"Right navigation selects equipment and model");
            Move(es,MoveDirection.Down);Check(defense[1].isOn&&Get<int>(setup,"_defense")==1,"Down navigation selects defense");
            Move(es,MoveDirection.Left);Check(defense[0].isOn&&!defense[1].isOn,"Left navigation updates selected graphic state");
            Move(es,MoveDirection.Up);Check(attack[0].isOn,"Up navigation returns to attack");
            attack[2].OnPointerClick(new PointerEventData(es){button=PointerEventData.InputButton.Left});
            Check(attack[2].isOn&&Get<int>(setup,"_attack")==2,"Pointer selection still works");
            Debug.Log("ENEMY_GAUGE_RESULT: "+count+" passed");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
