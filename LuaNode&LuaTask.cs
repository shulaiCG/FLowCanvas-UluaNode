
//要使用此节点需要下载 合并 ToLua的项目工程文件:  https://github.com/topameng/tolua
//需要同时导入FlowCanvas和NodeCnavas插件才不会报错。
using System.Collections.Generic;
using System.Linq;
using LuaInterface;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
using NodeCanvas.Tasks.Actions;
using NodeCanvas.Framework;
using FlowCanvas.Nodes;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
#endif


#region LuaStateManager 负责管理lua虚拟机和 所有lua node 和 lua task的function。
public class LuaManager : MonoBehaviour
{
    public static LuaManager _instance;

    public static LuaManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var findResult = FindObjectOfType<LuaManager>();
                if(findResult==null)
                {
                    GameObject luaManagerGO = new GameObject("[LuaManager]");
                    _instance = luaManagerGO.AddComponent<LuaManager>();
                }
                else
                {
                    _instance = findResult;
                }

            }
            return _instance;
        }
    }

    public string luaScript = "";

    public Dictionary<string, string> functionDicts = new Dictionary<string, string>();
    public List<string> endParts = new List<string>();

    private LuaState _luaState;
    public LuaState LuaState
    {
        get
        {
            if (_luaState == null)
            {
                if (LuaResLoader.Instance == null)
                {
                    new LuaResLoader();
                }

                _luaState = new LuaState();

                _luaState.Start();
                DelegateFactory.Init();
                LuaBinder.Bind(_luaState);
                
                if(Application.isPlaying)
                    DontDestroyOnLoad(this.gameObject);
            }

            return _luaState;
        }
    }

    bool isDirty = false;
    public void AddFunction(string functionName, string functionHead, string functionBody)
    {
        if (string.IsNullOrEmpty(functionName) || string.IsNullOrEmpty(functionBody) || string.IsNullOrEmpty(functionHead))
            return;

        string completeFunction = functionHead + "\n" + functionBody;
        if (!functionDicts.ContainsKey(functionName))
        {
            functionDicts.Add(functionName, completeFunction);
        }
        else
        {
            if(functionDicts[functionName]== completeFunction)
            {

            }
            else
            {
                Debug.Log(string.Format("Function Name:{0},重复注册", functionName));
            }
        }
        isDirty = true;
    }

    public void UpdateScript()
    {
        if (!isDirty)
            return;

        isDirty = false;
        luaScript = " Lua ={}\n";
        functionDicts.ForEach(x =>
        {
            luaScript += string.Format("\n{1}\n end\n Lua.{0}={0} \n", x.Key, x.Value);
        });

        endParts.ForEach(x =>
        {
            luaScript += x + "\n";
        });

        LuaState.DoString(luaScript);
    }

    public void SearchCurrentGraphAllLuaMethod(Component nodeAgent)
    {
        List<Graph> allGraphs = new List<Graph>();

        var rootGraph = nodeAgent.GetComponent<GraphOwner>().graph;
        var childGraph = rootGraph.GetAllNestedGraphs<Graph>(true);

        allGraphs.Add(rootGraph);
        allGraphs.AddRange(childGraph);

        List<LuaActionBase> allLuaAction = new List<LuaActionBase>();
        List<LuaCondition> allLuaCondition = new List<LuaCondition>();
        List<CallLuaMethodBase> allLuaNode = new List<CallLuaMethodBase>();
        allGraphs.ForEach(x =>
        {
            allLuaAction.AddRange(x.GetAllTasksOfType<LuaActionBase>());
        });
        allGraphs.ForEach(x =>
        {
            allLuaCondition.AddRange(x.GetAllTasksOfType<LuaCondition>());
        });
        allGraphs.ForEach(x =>
        {
            allLuaNode.AddRange(x.GetAllNodesOfType<CallLuaMethodBase>());
        });

        allLuaAction.ForEach(x =>
        {
            if (!x.Combined)
            {
                x.Combined = true;//其他luaNode不需要再初始化了

                x.UpdateFunctionHead();
                if (!x.InvokeOnly)
                    LuaManager.Instance.AddFunction(x.functionName, x.functionHead, x.functionBody);

                string[] endbodyWithoutBlank = x.endBody.Replace(" ", "").Split('\n');
                endbodyWithoutBlank.ForEach(y =>
                {
                    if (!LuaManager.Instance.endParts.Contains(y))
                    {
                        LuaManager.Instance.endParts.Add(y);
                    }
                });
            }
        });

        allLuaCondition.ForEach(x =>
        {
            if (!x.Combined)
            {
                x.Combined = true;//其他luaNode不需要再初始化了
                x.UpdateFunctionHead();
                if (!x.InvokeOnly)
                    LuaManager.Instance.AddFunction(x.functionName, x.functionHead, x.functionBody);

                string[] endbodyWithoutBlank = x.endBody.Replace(" ", "").Split('\n');
                endbodyWithoutBlank.ForEach(y =>
                {
                    if (!LuaManager.Instance.endParts.Contains(y))
                    {
                        LuaManager.Instance.endParts.Add(y);
                    }
                });
            }
        });

        allLuaNode.ForEach(x =>
        {
            if (!x.Combined)
            {
                x.Combined = true;//其他luaNode不需要再初始化了
                x.UpdateFunctionHead();
                if (!x.InvokeOnly)
                    LuaManager.Instance.AddFunction(x.functionName, x.functionHead, x.functionBody);

                string[] endbodyWithoutBlank = x.endBody.Replace(" ", "").Split('\n');
                endbodyWithoutBlank.ForEach(y =>
                {
                    if (!LuaManager.Instance.endParts.Contains(y))
                    {
                        LuaManager.Instance.endParts.Add(y);
                    }
                });
            }
        });

        LuaManager.Instance.UpdateScript();
    }

    public void ResetCurrentGraphCombinedState(Component nodeAgent)
    {
        List<Graph> allGraphs = new List<Graph>();

        var rootGraph = nodeAgent.GetComponent<GraphOwner>().graph;
        var childGraph = rootGraph.GetAllNestedGraphs<Graph>(true);

        allGraphs.Add(rootGraph);
        allGraphs.AddRange(childGraph);

        List<LuaActionBase> allLuaAction = new List<LuaActionBase>();
        List<LuaCondition> allLuaCondition = new List<LuaCondition>();
        List<CallLuaMethodBase> allLuaNode = new List<CallLuaMethodBase>();
        allGraphs.ForEach(x =>
        {
            allLuaAction.AddRange(x.GetAllTasksOfType<LuaActionBase>());
        });
        allGraphs.ForEach(x =>
        {
            allLuaCondition.AddRange(x.GetAllTasksOfType<LuaCondition>());
        });
        allGraphs.ForEach(x =>
        {
            allLuaNode.AddRange(x.GetAllNodesOfType<CallLuaMethodBase>());
        });

        allLuaAction.ForEach(x =>
        {
            x.Combined = false;
        });

        allLuaCondition.ForEach(x =>
        {
            x.Combined = false;
        });

        allLuaNode.ForEach(x =>
        {
            x.Combined = false;
        });
    }


    private void OnDestroy()
    {
        luaFunctions.ForEach(x => { x.Value.Dispose(); });
        luaFunctions.Clear();
        LuaState.Dispose();
        _luaState = null;
    }

#if UNITY_EDITOR

    public string luaGamma = "示例:使用Type或静态方法 如:使用GameObject.AddComonent(typeof(ParticleSystem)), \n 在最下方 写入 GameObject = UnityEngine.GameObject 和 ParticleSystem = UnityEngine.ParticleSystem \n  \n 调用方法使用冒号 ':' 如 transform: Rotate, 属性和字段'.'点号不变 \n 构造实例不需要 new, 变量无需声明类型, 如: angle = Vector3(45, 0, 0) \n 常用的语句: \n if a > 0 then\n ...\n end " +
                    "\n\n  if a>0 then\n  ... \n elseif a=10 then \n  ...\n  else\n  ... \n end \n\n while a>0 do \n ... end \n \nfor index= 0, 10 ,(从0-10的增值幅度,默认1,可不写) do\n  ...break 或 return 中止跳出循环  \nend \n \nfor index, value in pairs(字典或table) do\n  index 指数 ,value 值... 中止  \nend  \n repeat ...\n  until a>0 \n 遍历数组,列表或字典 local iter = myDiectionary:GetEnumerator() \n while iter:MoveNext() do\n  print(iter.Current.Key..iter.Current.Value)\n end \r \n\n赋值: 字符串: a = 'hello'\n num = num+1 ,无++ 或-- , a,b=5,6 等同" +
                    " a=5 b=6 ,x, y = y, x 可以交换数值(计算好后才赋值) \n 方法外声明的变量 j = 10 是全局变量  \r\n local i = 1 有local 是局部变量 \n nil是空 不是null  \n 获取type:  type(gameobject)  获得字符串: tostring(10) 字符串用双引号和单引号都可以; \n\n 表格 Table : 新建表格 myTable={} ; myTable={ 1,'jame',gameobject} 可以传入任意类型的值, 默认索引以1开始\n" +
                    " 可以使用字符做key myTable['myKey']=myObject,字符串做索引还可以用myTable.myKey形式 \n 也可以用数字做key,如:return myTable[2], \n\n 算数符: +(加) -(减) *(乘) /(除) %(取余) ^(乘幂: 10^2=100) -(负号: -100) \n\n 关系运算符: == 等于 ~=不等于 >大于 <小于 >= <= \n \n逻辑运算符: and(相当于&&) or(相当于||) not(相当于!)+\n\n 其他运算符: .. 字符串相加 # 字符长度(#hello 为5 ,也可用于table的count数值) \n\n 数学库:" +
                    "圆周率: math.pi  \n绝对值math.abs(-15)=15 ; \nmath.ceil(5.8)=6 ;\nmath.floor(5.6)=5 ;\n取模运算:math.mod(14, 5)=4 ;\n最大值: math.max(2.71, 100, -98, 23)=100 ;\n最小值 math.min ; \n得到x的y次方: math.pow(2, 5)=32 ;  \n开平方函数: math.sqrt(16)=4;角度转弧度: 3.14159265358 ;\n 弧度转角度: math.deg(math.pi)=180 ;\n 获取随机数: math.random(1, 100) ;\n设置随机数种子:math.randomseed(os.time()) 在使用math.random函数之前必须使用此函数设置随机数种子  ;\n正弦: math.sin(math.rad(30))=0.5 ; \n反正弦函数:math.asin(0.5)=0.52359877 ;";
#endif
    #region Dispose LuaFunction
    //public float disposeLuaFunctionInnerTime = 10f;
    //public int everyDisposeFunctionCount = 10;

    //float counter = 0;
    //void Update()
    //{
    //    counter += Time.deltaTime;
    //    if (counter > disposeLuaFunctionInnerTime)
    //    {
    //        counter = 0;
    //        int totalLuaFunctionCount = luaFunctions.Count;
    //        while (luaFunctions.Count > totalLuaFunctionCount - everyDisposeFunctionCount)
    //        {
    //            var keyPair = luaFunctions.First();
    //            LuaFunction func;
    //            luaFunctions.TryGetValue(keyPair.Key, out func);
    //            if (func != null)
    //            {
    //                func.Dispose();
    //            }
    //            Debug.Log("Dispose lua function:" + keyPair.Key);
    //        }
    //    }
    //}
    #endregion

    public void RemoveLuaFunction(string functionName)
    {
        LuaFunction func;
        luaFunctions.TryGetValue(functionName, out func);
        if (func != null)
        {
            func.Dispose();
            func = null;
        }
        luaFunctions.Remove(functionName);
        isDirty = true;
    }
    Dictionary<string, LuaFunction> luaFunctions = new Dictionary<string, LuaFunction>();


    public LuaFunction GetLuaFunction(string functionName)
    {
        if (!luaFunctions.ContainsKey(functionName))
        {
            var func = LuaState.GetFunction("Lua." + functionName);
            if (func != null)
            {
                luaFunctions.Add(functionName, func);
                return func;
            }
            else
            {
                Debug.LogError("Can't Find this lua function:" + functionName);
            }
        }
        else
        {
            return luaFunctions[functionName];
        }
        return null;
    }


    public void DisposeLuaFunction(LuaFunction func)
    {
        if (luaFunctions.ContainsValue(func))
        {
            var t = luaFunctions.FirstOrDefault(x => x.Value == func);
            //luaFunctions.Remove(t.Key);
            func.Dispose();
            func = null;
        }
    }
}
#endregion

#region Lua Task 应用于NodeCanvas
namespace NodeCanvas.Tasks.Actions
{

    [Category("✫Lua")]
    [Description("Execute Lua Action")]
    public abstract class LuaActionBase : ActionTask
    {
        protected LuaFunction luaFunc;
        public bool InvokeOnly = false;

        [HideInInspector]
        public string functionHead = "";
        [HideInInspector]
        public string functionName = "LuaMethod";
        [HideInInspector]
        public string functionBody = "\n  \n   \n ";

        public BBParameter<object> para1;
        public BBParameter<object> para2;
        public BBParameter<object> para3;
        public BBParameter<object> para4;
        public BBParameter<object> para5;

        public virtual string endPart
        {
            get
            {
                return string.Format(" \n end \n this = {{}} this.{0} = {0} \n", functionName);
            }
        }

        [HideInInspector]
        [SerializeField]
        public string endBody = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}", "GameObject=UnityEngine.GameObject", "Mathf=UnityEngine.Mathf", "Time=UnityEngine.Time", "Application=UnityEngine.Application", "Screen=UnityEngine.Screen", "Resources=UnityEngine.Resources", "Input=UnityEngine.Input");
                                                     
        public virtual string UpdateFunctionHead()
        {
            string parameters = "";

            ParaNameList.Clear();
            if (para1.varRef != null)
            {
                ParaNameList.Add(para1.varRef.name);
            }
            if (para2.varRef != null)
            {
                ParaNameList.Add(para2.varRef.name);
            }
            if (para3.varRef != null)
            {
                ParaNameList.Add(para3.varRef.name);
            }
            if (para4.varRef != null)
            {
                ParaNameList.Add(para4.varRef.name);
            }
            if (para5.varRef != null)
            {
                ParaNameList.Add(para5.varRef.name);
            }

            if (ParaNameList != null && ParaNameList.Count > 0)
            {
                ParaNameList.ForEach(x => parameters += x + ",");
                parameters = parameters.Remove(parameters.Length - 1);
            }
            functionHead = string.Format(" function {0} ({1})\n", functionName, parameters);
            return functionHead;
        }

        bool combined = false;
        public virtual bool Combined
        {
            get { return combined; }
            set
            {
                combined = value;
            }
        }
        protected override string OnInit()
        {
#if UNITY_EDITOR
            if (highLightVariable)
            {
                if (!string.IsNullOrEmpty(recordFunctionBody))
                    functionBody = recordFunctionBody;
            }
#endif
            if (InvokeOnly)
            {
                return base.OnInit();
            }

            if (!Combined)
            {
                LuaManager.Instance.SearchCurrentGraphAllLuaMethod(agent);
                Combined = true;
            }
            return base.OnInit();
        }


        public void GetFunction()
        {
            luaFunc = LuaManager.Instance.GetLuaFunction(functionName);
        }
        [HideInInspector]
        public List<string> ParaNameList = new List<string>();

#if UNITY_EDITOR
        [HideInInspector]
        public bool showExample;
        [HideInInspector]
        public bool highLightVariable;

        private bool hasInvokeTarget;
        protected override void OnTaskInspectorGUI()
        {
            base.OnTaskInspectorGUI();

            GUIStyle textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.active.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.hover.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.focused.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.margin = new RectOffset(5, 5, 5, 5);
            textAreaStyle.richText = true;
            if (!InvokeOnly)
                GUILayout.Label(" FunctionName 不能重名");
            UpdateFunctionHead();
            functionName = EditorGUILayout.TextField("functionName", functionName);
            EditorGUILayout.TextArea(functionHead, textAreaStyle);

            if (InvokeOnly && GUILayout.Button("ShowInvokeLuaMethod"))
            {
                if (!Application.isPlaying)
                {
                    LuaManager.Instance.functionDicts.Clear();
                    LuaManager.Instance.ResetCurrentGraphCombinedState(agent);
                    LuaManager.Instance.SearchCurrentGraphAllLuaMethod(agent);
                }

                string functionBody = "";
                LuaManager.Instance.functionDicts.TryGetValue(functionName, out functionBody);
                hasInvokeTarget = !string.IsNullOrEmpty(functionBody);
            }
            GUILayout.Label(hasInvokeTarget? LuaManager.Instance.functionDicts[functionName]:"    Null");

            if (InvokeOnly)
                return;

            if (!highLightVariable)
                functionBody = EditorGUILayout.TextArea(functionBody);
            else
            {
                EditorGUILayout.TextArea(functionBody, textAreaStyle);
            }

            GUILayout.Label(endPart);
            GUILayout.Label("在CustomSetting.cs里可以查看和添加class");
            endBody = EditorGUILayout.TextArea(endBody);
            if (GUILayout.Button(!highLightVariable ? "高亮检查参数" : "恢复编辑"))
            {
                if (highLightVariable)
                {
                    functionBody = recordFunctionBody;
                }
                else
                {
                    functionBody = CheckVariable(functionBody);
                }
                highLightVariable = !highLightVariable;
            }


            showExample = GUILayout.Toggle(showExample, "ShowLuaExample");
            if (showExample)
                GUILayout.TextArea(LuaManager.Instance.luaGamma);
        }
        [HideInInspector]
        public string recordFunctionBody;
        public string CheckVariable(string inputString)
        {
            recordFunctionBody = inputString;

            List<string> paraList = new List<string>();

            for (var i = 0; i < ParaNameList.Count; i++)
            {   //Debug.Log(outParaNameList[i]);
                paraList.Add(ParaNameList[i]);
            } //记录所有参数

            //字体变白
            //inputString = "<color=EEEEEE>" +inputString + "</color>";

            string p = @"(local\s+?\w+\s*?=)";
            var t = Regex.Matches(inputString, p);
            t.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                var para = y.Replace("local", "").Replace("=", "").Replace(" ", "");

                if (!paraList.Contains(para))
                {
                    paraList.Add(para);
                    //Debug.Log(para);
                }
            });

            paraList.ForEach(x =>
                inputString = inputString.Replace(x, string.Format("<b><color=#F4A460>{0}</color></b>", x))
            );

            //改变方法和属性的颜色
            string method = @"\:[A-Z]\w*\W";
            var methodResult = Regex.Matches(inputString, method);
            methodResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                inputString = Regex.Replace(inputString, y.Replace("(", ""), y.Replace(y, string.Format(":<b><color=#00CD00>{0}</color></b>", y.Replace(":", "").Replace("(", ""))));
            });

            List<string> availAbleType = new List<string>();
            string type = @"\s*?\w+=";
            var typeResult = Regex.Matches(endBody, type);
            typeResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                y = y.Replace("=", "").Replace(" ", "").Replace("\n", "");
                //Debug.Log(y);
                if (!availAbleType.Contains(y))
                    availAbleType.Add(y);
            });

            availAbleType.ForEach(x => inputString = inputString.Replace(x, string.Format("<b><color=#5CACEE>{0}</color></b>", x)));

            //改变单引号颜色
            inputString = inputString.Replace("\'", string.Format("<b><color=#EE5C42>\'</color></b>"));
            //Debug.Log(inputString);
            return inputString;
        }



#endif

    }

    [Category("✫Lua")]
    [Description("Execute Lua Action")]
    public class LuaAction : LuaActionBase
    {

        protected override string info
        {
            get
            {
                return string.Format("{0} : {1}", "LuaAction", functionName);
            }
        }
        protected override void OnExecute()
        {

            GetFunction();

            switch (ParaNameList.Count)
            {
                case 0:
                    luaFunc.Call();
                    break;
                case 1:
                    luaFunc.Call<object>(para1.value);
                    break;
                case 2:
                    luaFunc.Call<object, object>(para1.value, para2.value);
                    break;
                case 3:
                    luaFunc.Call<object, object, object>(para1.value, para3.value, para3.value);
                    break;
                case 4:
                    luaFunc.Call<object, object, object, object>(para1.value, para3.value, para3.value, para4.value);
                    break;
                case 5:
                    luaFunc.Call<object, object, object, object, object>(para1.value, para3.value, para3.value, para4.value, para5.value);
                    break;
            }
            LuaManager.Instance.LuaState.CheckTop();

            EndAction();
        }
    }

    [Category("✫Lua")]
    [Description("Execute Lua Function")]
    public class LuaFunction<T> : LuaActionBase
    {
        protected override string info
        {
            get
            {
                return string.Format("{0} : {1}<{2}> ", "LuaFunction", functionName, typeof(T).FriendlyName());
            }
        }

        public BBParameter<T> Result;
        protected override void OnExecute()
        {
            GetFunction();
            switch (ParaNameList.Count)
            {
                case 0:
                    Result.value = luaFunc.Invoke<T>();
                    break;
                case 1:
                    Result.value = luaFunc.Invoke<object, T>(para1.value);
                    break;
                case 2:
                    Result.value = luaFunc.Invoke<object, object, T>(para1.value, para2.value);
                    break;
                case 3:
                    Result.value = luaFunc.Invoke<object, object, object, T>(para1.value, para2.value, para3.value);
                    break;
                case 4:
                    Result.value = luaFunc.Invoke<object, object, object, object, T>(para1.value, para2.value, para3.value, para4.value);
                    break;
                case 5:
                    Result.value = luaFunc.Invoke<object, object, object, object, object, T>(para1.value, para2.value, para3.value, para4.value, para5.value);
                    break;
            }
            LuaManager.Instance.LuaState.CheckTop();

            EndAction();
        }
    }


    [Category("✫Lua")]
    [Description("Checj Lua Condition")]
    public class LuaCondition : ConditionTask
    {
        protected LuaFunction luaFunc;
        public bool InvokeOnly = false;

        [HideInInspector]
        public string functionHead = "";
        [HideInInspector]
        public string functionName = "LuaCondition";
        [HideInInspector]
        public string functionBody = "\n  \n   \n ";

        public BBParameter<object> para1;
        public BBParameter<object> para2;
        public BBParameter<object> para3;
        public BBParameter<object> para4;
        public BBParameter<object> para5;

        protected override string info
        {
            get
            {
                return string.Format("{0} : {1} ", "Check LuaCondition", functionName);
            }
        }

        public bool Result;
        protected override bool OnCheck()
        {
            GetFunction();
            switch (ParaNameList.Count)
            {
                case 0:
                    Result = luaFunc.Invoke<bool>();
                    break;
                case 1:
                    Result = luaFunc.Invoke<object, bool>(para1.value);
                    break;
                case 2:
                    Result = luaFunc.Invoke<object, object, bool>(para1.value, para2.value);
                    break;
                case 3:
                    Result = luaFunc.Invoke<object, object, object, bool>(para1.value, para2.value, para3.value);
                    break;
                case 4:
                    Result = luaFunc.Invoke<object, object, object, object, bool>(para1.value, para2.value, para3.value, para4.value);
                    break;
                case 5:
                    Result = luaFunc.Invoke<object, object, object, object, object, bool>(para1.value, para2.value, para3.value, para4.value, para5.value);
                    break;
            }
            LuaManager.Instance.LuaState.CheckTop();

            //if (luaFunc != null)
            //{
            //    luaFunc.Dispose();
            //    luaFunc = null;
            //}
            return invert ? !Result : Result;
        }



        public virtual string endPart
        {
            get
            {
                return string.Format(" \n end \n this = {{}} this.{0} = {0} \n", functionName);
            }
        }

        [SerializeField]
        [HideInInspector]
        public string endBody = "GameObject=UnityEngine.GameObject" +
                                                    "\nMathf=UnityEngine.Mathf" +
                                                    "\nTime=UnityEngine.Time" +
                                                    "\nApplication=UnityEngine.Application" +
                                                    "\nScreen=UnityEngine.Screen" +
                                                    "\nResources=UnityEngine.Resources" + "\nInput=UnityEngine.Input";



        public virtual string UpdateFunctionHead()
        {
            string parameters = "";

            ParaNameList.Clear();
            if (para1.varRef != null)
            {
                ParaNameList.Add(para1.varRef.name);
            }
            if (para2.varRef != null)
            {
                ParaNameList.Add(para2.varRef.name);
            }
            if (para3.varRef != null)
            {
                ParaNameList.Add(para3.varRef.name);
            }
            if (para4.varRef != null)
            {
                ParaNameList.Add(para4.varRef.name);
            }
            if (para5.varRef != null)
            {
                ParaNameList.Add(para5.varRef.name);
            }

            if (ParaNameList != null && ParaNameList.Count > 0)
            {
                ParaNameList.ForEach(x => parameters += x + ",");
                parameters = parameters.Remove(parameters.Length - 1);
            }
            functionHead = string.Format(" function {0} ({1})\n", functionName, parameters);
            return functionHead;
        }

        bool combined = false;
        public virtual bool Combined
        {
            get { return combined; }
            set
            {
                combined = value;
            }
        }
        protected override string OnInit()
        {
#if UNITY_EDITOR
            if (highLightVariable)
            {
                if (!string.IsNullOrEmpty(recordFunctionBody))
                    functionBody = recordFunctionBody;
            }
#endif
            if (InvokeOnly)
                return base.OnInit();

            if (!Combined)
            {
                Combined = true;
                LuaManager.Instance.SearchCurrentGraphAllLuaMethod(agent);

            }

            //string CombineLuaScript = UpdateFunctionHead() + "\n" + functionBody + "\n" + endPart + "\n" + endBody;
            return base.OnInit();
        }

        public void GetFunction()
        {
            luaFunc = LuaManager.Instance.GetLuaFunction(functionName);
        }

        //protected override void OnStop()
        //{
        //    //if (luaFunc != null)
        //    //{
        //    //    luaFunc.Dispose();
        //    //    luaFunc = null;
        //    //}
        //    Debug.Log("stop");
        //    base.OnStop();
        //}
        [HideInInspector]
        public List<string> ParaNameList = new List<string>();

#if UNITY_EDITOR
        [HideInInspector]
        public bool showExample;
        [HideInInspector]
        public bool highLightVariable;
        bool hasInvokeTarget;
        protected override void OnTaskInspectorGUI()
        {
            base.OnTaskInspectorGUI();

            GUIStyle textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.active.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.hover.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.focused.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.margin = new RectOffset(5, 5, 5, 5);
            textAreaStyle.richText = true;
            if (!InvokeOnly)
                GUILayout.Label("FunctionName 不能重名");
            UpdateFunctionHead();
            functionName = EditorGUILayout.TextField("functionName", functionName);

            EditorGUILayout.TextArea(functionHead, textAreaStyle);

            if (InvokeOnly && GUILayout.Button("ShowInvokeLuaMethod"))
            {
                if (!Application.isPlaying)
                {
                    LuaManager.Instance.functionDicts.Clear();
                    LuaManager.Instance.ResetCurrentGraphCombinedState(agent);
                    LuaManager.Instance.SearchCurrentGraphAllLuaMethod(agent);
                }

                string functionBody = "";
                LuaManager.Instance.functionDicts.TryGetValue(functionName, out functionBody);
                hasInvokeTarget = !string.IsNullOrEmpty(functionBody);
            }
            GUILayout.Label(hasInvokeTarget ? LuaManager.Instance.functionDicts[functionName] +"\nend": "    Null");
            if (InvokeOnly)
                return;
            if (!highLightVariable)
                functionBody = EditorGUILayout.TextArea(functionBody);
            else
            {
                EditorGUILayout.TextArea(functionBody, textAreaStyle);
            }

            GUILayout.Label(endPart);
            GUILayout.Label("在CustomSetting.cs里可以查看和添加class");
            endBody = EditorGUILayout.TextArea(endBody);
            if (GUILayout.Button(!highLightVariable ? "高亮检查参数" : "恢复编辑"))
            {
                if (highLightVariable)
                {
                    functionBody = recordFunctionBody;
                }
                else
                {
                    functionBody = CheckVariable(functionBody);
                }
                highLightVariable = !highLightVariable;
            }

            showExample = GUILayout.Toggle(showExample, "ShowLuaExample");
            if (showExample)
                GUILayout.TextArea(LuaManager.Instance.luaGamma);
        }
        [HideInInspector]
        public string recordFunctionBody;
        public string CheckVariable(string inputString)
        {
            recordFunctionBody = inputString;

            List<string> paraList = new List<string>();

            for (var i = 0; i < ParaNameList.Count; i++)
            {   //Debug.Log(outParaNameList[i]);
                paraList.Add(ParaNameList[i]);
            } //记录所有参数

            //字体变白
            //inputString = "<color=EEEEEE>" +inputString + "</color>";

            string p = @"(local\s+?\w+\s*?=)";
            var t = Regex.Matches(inputString, p);
            t.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                var para = y.Replace("local", "").Replace("=", "").Replace(" ", "");

                if (!paraList.Contains(para))
                {
                    paraList.Add(para);
                    //Debug.Log(para);
                }
            });

            paraList.ForEach(x =>
                inputString = inputString.Replace(x, string.Format("<b><color=#F4A460>{0}</color></b>", x))
            );

            //改变方法和属性的颜色
            string method = @"\:[A-Z]\w*\W";
            var methodResult = Regex.Matches(inputString, method);
            methodResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                inputString = Regex.Replace(inputString, y.Replace("(", ""), y.Replace(y, string.Format(":<b><color=#00CD00>{0}</color></b>", y.Replace(":", "").Replace("(", ""))));
            });

            List<string> availAbleType = new List<string>();
            string type = @"\s*?\w+=";
            var typeResult = Regex.Matches(endBody, type);
            typeResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                y = y.Replace("=", "").Replace(" ", "").Replace("\n", "");
                //Debug.Log(y);
                if (!availAbleType.Contains(y))
                    availAbleType.Add(y);
            });

            availAbleType.ForEach(x => inputString = inputString.Replace(x, string.Format("<b><color=#5CACEE>{0}</color></b>", x)));

            //改变单引号颜色
            inputString = inputString.Replace("\'", string.Format("<b><color=#EE5C42>\'</color></b>"));
            //Debug.Log(inputString);
            return inputString;
        }

#endif

    }

}
#endregion

#region Lua Node  应用于FlowCanvas
namespace FlowCanvas.Nodes
{
    [Color("103BFF")]
    public class EditorCallLuaMethod : FlowNode
    {
        protected LuaFunction luaFunc;


        public string functionHead = "";
        public string functionName = "EditorModeExecute";
        public string functionBody = "\n  \n   \n ";

        public virtual string endPart
        {
            get
            {
                return string.Format(" \n end \n this = {{}} this.{0} = {0} \n", functionName);
            }
        }

        [SerializeField]
        public string endBody = "GameObject=UnityEngine.GameObject" +
                                                    "\nMathf=UnityEngine.Mathf" +
                                                    "\nTime=UnityEngine.Time" +
                                                    "\nApplication=UnityEngine.Application" +
                                                    "\nScreen=UnityEngine.Screen" +
                                                    "\nResources=UnityEngine.Resources" + "\nInput=UnityEngine.Input";

        string setCurrentSelectionFunction = "\n function SetCurrentSelection(cs)\n  currentSelection = cs \n end \n this.SetCurrentSelection = SetCurrentSelection \n";
        //string setCurrentSelectionListFunction = "\n function SetCurrentSelectionList(cs)\n   for index,value in cs do \n  currentSelectionList[index] = value \n  end \n end \n this.SetCurrentSelectionList = SetCurrentSelectionList \n";

        public string UpdateFunctionHead()
        {
            string paras = "";
            outParaNameList.Clear();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                if (!outParaNameList.Contains(inputDefinitions[i].name))
                {
                    outParaNameList.Add(inputDefinitions[i].name);
                }
                if (i != inputDefinitions.Count - 1)
                    paras += inputDefinitions[i].name + ",";
                else
                    paras += inputDefinitions[i].name;

            }
            functionHead = string.Format(" function {0} ( {1} )", functionName, paras);
            return functionHead;
        }
        LuaState lua;
        List<ValueInput> ins;
        LuaResLoader lrl;
        protected override void RegisterPorts()
        {

            ins = new List<ValueInput>();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                var def = inputDefinitions[i];

                ins.Add(AddValueInput(def.name, def.type, def.ID));
            }
        }

        void Invoke()
        {
            if (highLightVariable)
            {
                if (!string.IsNullOrEmpty(recordFunctionBody))
                    functionBody = recordFunctionBody;
            }
            //string cs = "  currentSelection ={} \n  currentSelectionList ={} \n";
            string cs = "  currentSelection ={} \n";
            string luacode = string.Format("{0}\n{1}\n{2}\n{3}\n {4}", UpdateFunctionHead(), functionBody, endPart, endBody, cs + setCurrentSelectionFunction);
            if (lrl == null)
            {
                lrl = new LuaResLoader();
            }

            lua = new LuaState();
            lua.Start();
            DelegateFactory.Init();
            LuaBinder.Bind(lua);

            lua.DoString(luacode);

            luaFunc = lua.GetFunction("this." + functionName);

#if UNITY_EDITOR

            var setCS = lua.GetFunction("this.SetCurrentSelection");
            setCS.Call<GameObject>(UnityEditor.Selection.activeGameObject);

#endif
            switch (ins.Count)
            {
                case 0:
                    luaFunc.Call();
                    break;
                case 1:
                    luaFunc.Call<object>(ins[0].value);
                    break;
                case 2:
                    luaFunc.Call<object, object>(ins[0].value, ins[1].value);
                    break;
                case 3:
                    luaFunc.Call<object, object, object>(ins[0].value, ins[1].value, ins[2].value);
                    break;
                case 4:
                    luaFunc.Call<object, object, object, object>(ins[0].value, ins[1].value,
                        ins[2].value, ins[3].value);
                    break;
                case 5:
                    luaFunc.Call<object, object, object, object, object>(ins[0].value, ins[1].value,
                        ins[2].value, ins[3].value, ins[4].value);
                    break;
                case 6:
                    luaFunc.Call<object, object, object, object, object, object>(ins[0].value,
                        ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value);
                    break;
                case 7:
                    luaFunc.Call<object, object, object, object, object, object, object>(
                        ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                        ins[6].value);
                    break;
                case 8:

                    luaFunc.Call<object, object, object, object, object, object, object, object>(
                        ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                        ins[6].value, ins[7].value);
                    break;
                case 9:

                    luaFunc.Call<object, object, object, object, object, object, object, object, object>(
                        ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                        ins[6].value, ins[7].value, ins[8].value);
                    break;
            }
            lua.CheckTop();
            if (luaFunc != null)
            {
                luaFunc.Dispose();
                luaFunc = null;
            }

            lua.Dispose();
            lua = null;
        }
        public List<string> outParaNameList = new List<string>();
        void ButtonClick()
        {
            //graph.agent.GetComponent<FlowScriptController>().StartBehaviour();
            ////for (var i = 0; i < graph.allNodes.Count; i++)
            ////{
            ////    if (graph.allNodes[i] is FlowNode)
            ////    {
            ////        var flowNode = (FlowNode)graph.allNodes[i];
            ////        flowNode.BindPorts();
            ////        flowNode.AssignSelfInstancePort();
            ////    }
            ////}

            //Invoke();

            //graph.agent.GetComponent<FlowScriptController>().StopBehaviour();
            for (var i = 0; i < graph.allNodes.Count; i++)
            {
                if (graph.allNodes[i] is FlowNode)
                {
                    var flowNode = (FlowNode)graph.allNodes[i];
                    flowNode.AssignSelfInstancePort();
                    flowNode.BindPorts();
                }
            }
            Invoke();
            for (var i = 0; i < graph.allNodes.Count; i++)
            {
                if (graph.allNodes[i] is FlowNode)
                {
                    var flowNode = (FlowNode)graph.allNodes[i];
                    flowNode.UnBindPorts();
                }
            }
        }
#if UNITY_EDITOR


        public bool showExample;
        public bool highLightVariable;
        protected override void OnNodeInspectorGUI()
        {
            if (GUILayout.Button("Execute"))
            {
                ButtonClick();
            }
            GUIStyle textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.active.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.hover.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.focused.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.margin = new RectOffset(5, 5, 5, 5);
            textAreaStyle.richText = true;

            functionName = EditorGUILayout.TextField("functionName", functionName);
            UpdateFunctionHead();
            GUILayout.Label(functionHead);

            if (!highLightVariable)
                functionBody = EditorGUILayout.TextArea(functionBody);
            else
            {
                EditorGUILayout.TextArea(functionBody, textAreaStyle);
            }

            GUILayout.Label(endPart);
            GUILayout.Label("在CustomSetting.cs里可以查看和添加class");
            endBody = EditorGUILayout.TextArea(endBody);
            if (GUILayout.Button(!highLightVariable ? "高亮检查参数" : "恢复编辑"))
            {

                if (highLightVariable)
                {
                    functionBody = recordFunctionBody;
                }
                else
                {
                    functionBody = CheckVariable(functionBody);
                }
                highLightVariable = !highLightVariable;
            }

            if (GUILayout.Button("ConvertC#ToLua"))
            {
                ConvertCSharpScript();
            }
            showExample = GUILayout.Toggle(showExample, "ShowLuaExample");
            if (showExample)
                GUILayout.TextArea(LuaManager.Instance.luaGamma);
        }

        public string recordFunctionBody;
        public string CheckVariable(string inputString)
        {
            recordFunctionBody = inputString;

            List<string> paraList = new List<string>();

            for (var i = 0; i < outParaNameList.Count; i++)
            {   //Debug.Log(outParaNameList[i]);
                paraList.Add(outParaNameList[i]);
            } //记录所有参数

            //字体变白
            //inputString = "<color=EEEEEE>" +inputString + "</color>";

            string p = @"(local\s+?\w+\s*?=)";
            var t = Regex.Matches(inputString, p);
            t.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                var para = y.Replace("local", "").Replace("=", "").Replace(" ", "");

                if (!paraList.Contains(para))
                {
                    paraList.Add(para);
                    //Debug.Log(para);
                }
            });

            paraList.ForEach(x =>
                inputString = inputString.Replace(x, string.Format("<b><color=#F4A460>{0}</color></b>", x))
            );

            //改变方法和属性的颜色
            string method = @"\:[A-Z]\w*\W";
            var methodResult = Regex.Matches(inputString, method);
            methodResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                inputString = Regex.Replace(inputString, y.Replace("(", ""), y.Replace(y, string.Format(":<b><color=#00CD00>{0}</color></b>", y.Replace(":", "").Replace("(", ""))));
            });

            List<string> availAbleType = new List<string>();
            string type = @"\s*?\w+=";
            var typeResult = Regex.Matches(endBody, type);
            typeResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                y = y.Replace("=", "").Replace(" ", "").Replace("\n", "");
                //Debug.Log(y);
                if (!availAbleType.Contains(y))
                    availAbleType.Add(y);
            });

            availAbleType.ForEach(x => inputString = inputString.Replace(x, string.Format("<b><color=#5CACEE>{0}</color></b>", x)));

            //改变单引号颜色
            inputString = inputString.Replace("\'", string.Format("<b><color=#EE5C42>\'</color></b>"));
            //Debug.Log(inputString);
            return inputString;
        }

        public void ConvertCSharpScript()
        {
            string p = @"^\s*"; //消除开头空格
            var result = Regex.Matches(functionBody, p);
            result.Cast<Match>().Select(x => x.Value).ForEach(y => { functionBody = Regex.Replace(functionBody, y, ""); });

            string p3 = @"\s*?var\s+?"; //将var 改成local
            var result3 = Regex.Matches(functionBody, p3);
            result3.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                //Debug.Log(y);
                functionBody = Regex.Replace(functionBody, y, y.Replace("var", "local"));
            });
            string p4 = @"\df"; //将数值后的f 改成空
            var fresult = Regex.Matches(functionBody, p4);
            fresult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                //Debug.Log(y);
                functionBody = Regex.Replace(functionBody, y, y.Replace("f", ""));
            });

            functionBody = functionBody.Replace(";", ""); //移除封号
            functionBody = functionBody.Replace("new ", ""); //移除new 
            functionBody = functionBody.Replace("\"", "\'"); //双引号改单引号
        }

        #region port Define
        [HideInInspector]
        [SerializeField]
        public List<DynamicParameterDefinition> inputDefinitions =
            new List<DynamicParameterDefinition>();

        public bool AddInputDefinition(DynamicParameterDefinition def)
        {
            if (inputDefinitions.Find(d => d.ID == def.ID) == null)
            {
                inputDefinitions.Add(def);
                return true;
            }
            return false;
        }
        #endregion
#if UNITY_EDITOR
        protected override GenericMenu OnDragAndDropPortContextMenu(GenericMenu menu, Port port)
        {
            if (port is ValueOutput)
            {
                menu.AddItem(new GUIContent(string.Format("增加该输入类型的port '{0}'", port.name)), false, () =>
                {
                    var def = new DynamicParameterDefinition(port.name, port.type);
                    if (AddInputDefinition(def))
                    {
                        UpdateFunctionHead();
                        GatherPorts();
                        BinderConnection.Create(port, GetInputPort(def.ID));
                    }
                });
            }
            return menu;
        }


        protected override void OnNodeGUI()
        {
            //UpdateFunctionHead();
            base.OnNodeGUI();
            GUILayout.Space(10);
            if (GUILayout.Button("Execute"))
            {
                ButtonClick();
            }

            GUILayout.Label("传入参数:");

            EditorUtils.ReorderableList(inputDefinitions, (i, j) =>
            {
                var def = inputDefinitions[i];
                GUILayout.BeginHorizontal();
                def.name = EditorGUILayout.TextField(def.name, GUILayout.Width(100),
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(100), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    inputDefinitions.RemoveAt(i);
                    GatherPorts();
                    UpdateFunctionHead();
                }
                GUILayout.EndHorizontal();
            });     
                    

            if (GUILayout.Button("增加输入参数"))
            {
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), t =>
                {
                    AddInputDefinition(new DynamicParameterDefinition("value", t));
                    GatherPorts();
                    UpdateFunctionHead();
                });
            }
            if (GUILayout.Button("刷新参数Port"))
            {
                GatherPorts();
                UpdateFunctionHead();
            }

        }


#endif
#endif
    }

    [Color("2a5caa")]
    abstract public class CallLuaMethodBase : FlowNode
    {
        protected LuaFunction luaFunc;


        public string functionHead = "";
        public string functionName = "LuaMethod";
        public string functionBody = "\n  \n   \n ";

        public bool InvokeOnly = false;

        public virtual string endPart
        {
            get
            {
                return string.Format(" \n end \n this = {{}} this.{0} = {0} \n", functionName);
            }
        }

        [SerializeField]
        public string endBody = "GameObject=UnityEngine.GameObject" +
                                                    "\nMathf=UnityEngine.Mathf" +
                                                    "\nTime=UnityEngine.Time" +
                                                    "\nApplication=UnityEngine.Application" +
                                                    "\nScreen=UnityEngine.Screen" +
                                                    "\nResources=UnityEngine.Resources" + "\nInput=UnityEngine.Input";



        public virtual string UpdateFunctionHead()
        {
            return "";
        }


        bool combined = false;
        public virtual bool Combined
        {
            get { return combined; }
            set
            {
                combined = value;
            }
        }

        public override void OnGraphStarted()
        {
            base.OnGraphStarted();

            if (InvokeOnly)
                return;

#if UNITY_EDITOR
            if (highLightVariable)
            {
                if (!string.IsNullOrEmpty(recordFunctionBody))
                    functionBody = recordFunctionBody;
            }
#endif


            if (!Combined)
            {
                LuaManager.Instance.SearchCurrentGraphAllLuaMethod(graphAgent);
                Combined = true;
            }
        }

        public void GetFunction()
        {
            luaFunc = LuaManager.Instance.GetLuaFunction(functionName);
        }


        public List<string> outParaNameList = new List<string>();
#if UNITY_EDITOR
        public bool showExample;
        public bool highLightVariable;
        bool hasInvokeTarget;
        protected override void OnNodeInspectorGUI()
        {
            GUIStyle textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.active.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.hover.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.focused.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.margin = new RectOffset(5, 5, 5, 5);
            textAreaStyle.richText = true;

            if (!InvokeOnly)
                GUILayout.Label("FunctionName 不能重名");
            functionName = EditorGUILayout.TextField("functionName", functionName);


            if (InvokeOnly && GUILayout.Button("ShowInvokeLuaMethod"))
            {   
                if(!Application.isPlaying)
                {
                    LuaManager.Instance.functionDicts.Clear();
                    LuaManager.Instance.ResetCurrentGraphCombinedState(graphAgent);
                }
                LuaManager.Instance.SearchCurrentGraphAllLuaMethod(graphAgent);
                string functionBody = "";
                LuaManager.Instance.functionDicts.TryGetValue( functionName,out functionBody);
                hasInvokeTarget = !string.IsNullOrEmpty(functionBody);
            }
            if (InvokeOnly)
                GUILayout.Label(hasInvokeTarget ? LuaManager.Instance.functionDicts[functionName]+"\nend" : "    Null");
            else
            {
                UpdateFunctionHead();
                GUILayout.Label(functionHead);
            }

         

            if (InvokeOnly)
                return;

            if (!highLightVariable)
                functionBody = EditorGUILayout.TextArea(functionBody);
            else
            {
                EditorGUILayout.TextArea(functionBody, textAreaStyle);
            }

            GUILayout.Label(endPart);
            GUILayout.Label("在CustomSetting.cs里可以查看和添加class");
            endBody = EditorGUILayout.TextArea(endBody);
            if (GUILayout.Button(!highLightVariable ? "高亮检查参数" : "恢复编辑"))
            {

                if (highLightVariable)
                {
                    functionBody = recordFunctionBody;
                }
                else
                {
                    functionBody = CheckVariable(functionBody);
                }
                highLightVariable = !highLightVariable;
            }

            showExample = GUILayout.Toggle(showExample, "ShowLuaExample");
            if (showExample)
                GUILayout.TextArea(LuaManager.Instance.luaGamma);
        }

        public string recordFunctionBody;
        public string CheckVariable(string inputString)
        {
            recordFunctionBody = inputString;

            List<string> paraList = new List<string>();

            for (var i = 0; i < outParaNameList.Count; i++)
            {   //Debug.Log(outParaNameList[i]);
                paraList.Add(outParaNameList[i]);
            } //记录所有参数

            //字体变白
            //inputString = "<color=EEEEEE>" +inputString + "</color>";

            string p = @"(local\s+?\w+\s*?=)";
            var t = Regex.Matches(inputString, p);
            t.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                var para = y.Replace("local", "").Replace("=", "").Replace(" ", "");

                if (!paraList.Contains(para))
                {
                    paraList.Add(para);
                    //Debug.Log(para);
                }
            });

            paraList.ForEach(x =>
                inputString = inputString.Replace(x, string.Format("<b><color=#F4A460>{0}</color></b>", x))
            );

            //改变方法和属性的颜色
            string method = @"\:[A-Z]\w*\W";
            var methodResult = Regex.Matches(inputString, method);
            methodResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                inputString = Regex.Replace(inputString, y.Replace("(", ""), y.Replace(y, string.Format(":<b><color=#00CD00>{0}</color></b>", y.Replace(":", "").Replace("(", ""))));
            });

            List<string> availAbleType = new List<string>();
            string type = @"\s*?\w+=";
            var typeResult = Regex.Matches(endBody, type);
            typeResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                y = y.Replace("=", "").Replace(" ", "").Replace("\n", "");
                //Debug.Log(y);
                if (!availAbleType.Contains(y))
                    availAbleType.Add(y);
            });

            availAbleType.ForEach(x => inputString = inputString.Replace(x, string.Format("<b><color=#5CACEE>{0}</color></b>", x)));

            //改变单引号颜色
            inputString = inputString.Replace("\'", string.Format("<b><color=#EE5C42>\'</color></b>"));
            //Debug.Log(inputString);
            return inputString;
        }

#endif
    }

    [Name("CallLuaAction")]
    [Category("UnityEngine/Lua")]
    [Description("调用Lua方法 (无返回值)")]
    [ContextDefinedInputs(typeof(Flow))]
    public class CallLuaActionScript : CallLuaMethodBase
    {
        public override string name
        {
            get { return string.Format("{0}", string.IsNullOrEmpty(functionName) ? "LuaAction:" : functionName); }
        }

        public override string endPart
        {
            get { return string.Format(" end \n this = {{}} this.{0} = {0} \n", functionName); ; }
        }
        FlowOutput outPut;
        protected override void RegisterPorts()
        {
            List<ValueInput> ins = new List<ValueInput>();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                var def = inputDefinitions[i];

                ins.Add(AddValueInput(def.name, def.type, def.ID));
            }

            outPut = AddFlowOutput("Out");
            AddFlowInput("Call", f =>
            {
                GetFunction();
                switch (ins.Count)
                {
                    case 0:
                        luaFunc.Call();
                        break;
                    case 1:
                        luaFunc.Call<object>(ins[0].value);
                        break;
                    case 2:
                        luaFunc.Call<object, object>(ins[0].value, ins[1].value);
                        break;
                    case 3:
                        luaFunc.Call<object, object, object>(ins[0].value, ins[1].value, ins[2].value);
                        break;
                    case 4:
                        luaFunc.Call<object, object, object, object>(ins[0].value, ins[1].value,
                            ins[2].value, ins[3].value);
                        break;
                    case 5:
                        luaFunc.Call<object, object, object, object, object>(ins[0].value, ins[1].value,
                            ins[2].value, ins[3].value, ins[4].value);
                        break;
                    case 6:
                        luaFunc.Call<object, object, object, object, object, object>(ins[0].value,
                            ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value);
                        break;
                    case 7:
                        luaFunc.Call<object, object, object, object, object, object, object>(
                            ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                            ins[6].value);
                        break;
                    case 8:

                        luaFunc.Call<object, object, object, object, object, object, object, object>(
                            ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                            ins[6].value, ins[7].value);
                        break;
                    case 9:

                        luaFunc.Call<object, object, object, object, object, object, object, object, object>(
                            ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                            ins[6].value, ins[7].value, ins[8].value);
                        break;
                }
                LuaManager.Instance.LuaState.CheckTop();

                // if (luaFunc != null)
                // {
                //     luaFunc.Dispose();
                //     luaFunc = null;
                // }

                outPut.Call(f);
            });
        }

        public override string UpdateFunctionHead()
        {
            string paras = "";
            outParaNameList.Clear();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                if (!outParaNameList.Contains(inputDefinitions[i].name))
                {
                    outParaNameList.Add(inputDefinitions[i].name);
                }
                if (i != inputDefinitions.Count - 1)
                    paras += inputDefinitions[i].name + ",";
                else
                    paras += inputDefinitions[i].name;

            }
            functionHead = string.Format(" function {0} ( {1} )", functionName, paras);
            return functionHead;
        }

        #region port Define
        [HideInInspector]
        [SerializeField]
        public List<DynamicParameterDefinition> inputDefinitions =
            new List<DynamicParameterDefinition>();

        public bool AddInputDefinition(DynamicParameterDefinition def)
        {
            if (inputDefinitions.Find(d => d.ID == def.ID) == null)
            {
                inputDefinitions.Add(def);
                return true;
            }
            return false;
        }
        #endregion
#if UNITY_EDITOR
        protected override GenericMenu OnDragAndDropPortContextMenu(GenericMenu menu, Port port)
        {
            if (port is ValueOutput)
            {
                menu.AddItem(new GUIContent(string.Format("增加该输入类型的port '{0}'", port.name)), false, () =>
                {
                    var def = new DynamicParameterDefinition(port.name, port.type);
                    if (AddInputDefinition(def))
                    {
                        UpdateFunctionHead();
                        GatherPorts();
                        BinderConnection.Create(port, GetInputPort(def.ID));
                    }
                });
            }
            return menu;
        }


        protected override void OnNodeGUI()
        {
            //UpdateFunctionHead();
            base.OnNodeGUI();

            InvokeOnly = GUILayout.Toggle(InvokeOnly, "InvokeOnly");

            GUILayout.Label("传入参数:");

            EditorUtils.ReorderableList(inputDefinitions, (i, j) =>
            {
                var def = inputDefinitions[i];
                GUILayout.BeginHorizontal();
                def.name = EditorGUILayout.TextField(def.name, GUILayout.Width(100),
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(100), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    inputDefinitions.RemoveAt(i);
                    GatherPorts();
                    UpdateFunctionHead();
                }
                GUILayout.EndHorizontal();
            });


            if (GUILayout.Button("增加输入参数"))
            {
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), t =>
                {
                    AddInputDefinition(new DynamicParameterDefinition("value", t));
                    GatherPorts();
                    UpdateFunctionHead();
                });
            }
            if (GUILayout.Button("刷新参数Port"))
            {
                GatherPorts();
                UpdateFunctionHead();
            }

        }


#endif

    }

    [Name("CallLuaFunction")]
    [Category("UnityEngine/Lua")]
    [Description("调用Lua方法 (有返回值)")]
    [ContextDefinedInputs(typeof(Flow))]
    public class CallLuaFunctionScript<T> : CallLuaMethodBase
    {
        public override string name
        {
            get { return string.Format("{0}", string.IsNullOrEmpty(functionName) ? "LuaFunction:" : functionName); }
        }

        public override string endPart
        {
            get { return string.Format(" end \n this = {{}} this.{0} = {0} \n", functionName); ; }
        }

        private T result;
        protected override void RegisterPorts()
        {
            List<ValueInput> ins = new List<ValueInput>();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                var def = inputDefinitions[i];
                ins.Add(AddValueInput(def.name, def.type, def.ID));
            }
            AddValueOutput("result", () =>
            {
                GetFunction();
                switch (ins.Count)
                {
                    case 0:
                        result = luaFunc.Invoke<T>();
                        break;
                    case 1:
                        result = luaFunc.Invoke<object, T>(ins[0].value);
                        break;
                    case 2:
                        result = luaFunc.Invoke<object, object, T>(ins[0].value, ins[1].value);
                        break;
                    case 3:
                        result = luaFunc.Invoke<object, object, object, T>(ins[0].value, ins[1].value, ins[2].value);
                        break;
                    case 4:
                        result = luaFunc.Invoke<object, object, object, object, T>(ins[0].value, ins[1].value,
                            ins[2].value, ins[3].value);
                        break;
                    case 5:
                        result = luaFunc.Invoke<object, object, object, object, object, T>(ins[0].value, ins[1].value,
                            ins[2].value, ins[3].value, ins[4].value);
                        break;
                    case 6:
                        result = luaFunc.Invoke<object, object, object, object, object, object, T>(ins[0].value,
                            ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value);
                        break;
                    case 7:
                        result = luaFunc.Invoke<object, object, object, object, object, object, object, T>(
                            ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                            ins[6].value);
                        break;
                    case 8:
                        result =
                            luaFunc.Invoke<object, object, object, object, object, object, object, object, T>(
                                ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                                ins[6].value, ins[7].value);
                        break;
                    case 9:
                        result =
                            luaFunc.Invoke<object, object, object, object, object, object, object, object, object, T>(
                                ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value,
                                ins[6].value, ins[7].value, ins[8].value);
                        break;
                }
                LuaManager.Instance.LuaState.CheckTop();

                //if (luaFunc != null)
                //{
                //    luaFunc.Dispose();
                //    luaFunc = null;
                //}
                return result;
            });
        }

        public override string UpdateFunctionHead()
        {
            string paras = "";
            outParaNameList.Clear();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                if (!outParaNameList.Contains(inputDefinitions[i].name))
                {
                    outParaNameList.Add(inputDefinitions[i].name);
                }
                if (i != inputDefinitions.Count - 1)
                    paras += inputDefinitions[i].name + ",";
                else
                    paras += inputDefinitions[i].name;

            }
            functionHead = string.Format(" function {0} ( {1} )", functionName, paras);
            return functionHead;
        }


        #region port Define
        [HideInInspector]
        [SerializeField]
        public List<DynamicParameterDefinition> inputDefinitions =
            new List<DynamicParameterDefinition>();

        public bool AddInputDefinition(DynamicParameterDefinition def)
        {
            if (inputDefinitions.Find(d => d.ID == def.ID) == null)
            {
                inputDefinitions.Add(def);
                return true;
            }
            return false;
        }

        #endregion
#if UNITY_EDITOR

        protected override GenericMenu OnDragAndDropPortContextMenu(GenericMenu menu, Port port)
        {
            if (port is ValueOutput)
            {
                menu.AddItem(new GUIContent(string.Format("增加该输入类型的port '{0}'", port.name)), false, () =>
                {
                    var def = new DynamicParameterDefinition(port.name, port.type);
                    if (AddInputDefinition(def))
                    {
                        UpdateFunctionHead();
                        GatherPorts();
                        BinderConnection.Create(port, GetInputPort(def.ID));
                    }
                });
            }
            return menu;
        }


        protected override void OnNodeGUI()
        {
            //UpdateFunctionHead();
            base.OnNodeGUI();
            InvokeOnly = GUILayout.Toggle(InvokeOnly, "InvokeOnly");

            GUILayout.Label("传入参数:");

            EditorUtils.ReorderableList(inputDefinitions, (i, j) =>
            {
                var def = inputDefinitions[i];
                GUILayout.BeginHorizontal();
                def.name = EditorGUILayout.TextField(def.name, GUILayout.Width(100),
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(100), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    inputDefinitions.RemoveAt(i);
                    GatherPorts();
                    UpdateFunctionHead();
                }
                GUILayout.EndHorizontal();
            });


            if (GUILayout.Button("增加输入参数"))
            {
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), t =>
                {
                    AddInputDefinition(new DynamicParameterDefinition("value", t));
                    GatherPorts();
                    UpdateFunctionHead();
                });
            }
            if (GUILayout.Button("刷新参数Port"))
            {
                GatherPorts();
                UpdateFunctionHead();
            }

        }

#endif

    }

    [Name("CallLuaMathFunction")]
    [Category("UnityEngine/Lua")]
    [Description("调用单一类型参数的Lua方法 (有返回值),适合数学计算,性能会更高?")]
    [ContextDefinedInputs(typeof(Flow))]
    public class CallLuaMathFunctionScript<T> : CallLuaMethodBase
    {
        public override string name
        {
            get { return string.Format("{0}", string.IsNullOrEmpty(functionName) ? "LuaMath:" : functionName); }
        }

        public override string endPart
        {
            get { return string.Format(" end \n this = {{}} this.{0} = {0} \n", functionName); ; }
        }

        private T result;
        protected override void RegisterPorts()
        {
            List<ValueInput<T>> ins = new List<ValueInput<T>>();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                var def = inputDefinitions[i];
                ins.Add(AddValueInput<T>(def));
            }
            AddValueOutput("result", () =>
            {
                GetFunction();
                switch (ins.Count)
                {
                    case 0:
                        result = luaFunc.Invoke<T>();
                        break;
                    case 1:
                        result = luaFunc.Invoke<T, T>(ins[0].value);
                        break;
                    case 2:
                        result = luaFunc.Invoke<T, T, T>(ins[0].value, ins[1].value);
                        break;
                    case 3:
                        result = luaFunc.Invoke<T, T, T, T>(ins[0].value, ins[1].value, ins[2].value);
                        break;
                    case 4:
                        result = luaFunc.Invoke<T, T, T, T, T>(ins[0].value, ins[1].value, ins[2].value, ins[3].value);
                        break;
                    case 5:
                        result = luaFunc.Invoke<T, T, T, T, T, T>(ins[0].value, ins[1].value, ins[2].value, ins[3].value,
                            ins[4].value);
                        break;
                    case 6:
                        result = luaFunc.Invoke<T, T, T, T, T, T, T>(ins[0].value, ins[1].value, ins[2].value,
                            ins[3].value, ins[4].value, ins[5].value);
                        break;
                    case 7:
                        result = luaFunc.Invoke<T, T, T, T, T, T, T, T>(ins[0].value, ins[1].value, ins[2].value,
                            ins[3].value, ins[4].value, ins[5].value, ins[6].value);
                        break;
                    case 8:
                        result = luaFunc.Invoke<T, T, T, T, T, T, T, T, T>(ins[0].value, ins[1].value, ins[2].value,
                            ins[3].value, ins[4].value, ins[5].value, ins[6].value, ins[7].value);
                        break;
                    case 9:
                        result = luaFunc.Invoke<T, T, T, T, T, T, T, T, T, T>(ins[0].value, ins[1].value, ins[2].value,
                            ins[3].value, ins[4].value, ins[5].value, ins[6].value, ins[7].value, ins[8].value);
                        break;
                }
                LuaManager.Instance.LuaState.CheckTop();

                //if (luaFunc != null)
                //{
                //    luaFunc.Dispose();
                //    luaFunc = null;
                //}
                return result;
            });

        }

        public override string UpdateFunctionHead()
        {
            string paras = "";
            outParaNameList.Clear();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                if (!outParaNameList.Contains(inputDefinitions[i]))
                {
                    outParaNameList.Add(inputDefinitions[i]);
                }
                if (i != inputDefinitions.Count - 1)
                    paras += inputDefinitions[i] + ",";
                else
                    paras += inputDefinitions[i];

            }
            functionHead = string.Format(" function {0} ( {1} )", functionName, paras);
            return functionHead;
        }


        #region port Define
        [HideInInspector]
        [SerializeField]
        public List<string> inputDefinitions =
            new List<string> { "value1", "value2" };

        public void AddInputDefinition(string portName)
        {
            inputDefinitions.Add(portName);
        }


        #endregion
#if UNITY_EDITOR

        protected override void OnNodeGUI()
        {
            //UpdateFunctionHead();
            base.OnNodeGUI();
            InvokeOnly = GUILayout.Toggle(InvokeOnly, "InvokeOnly");

            GUILayout.Label("传入参数:");

            EditorUtils.ReorderableList(inputDefinitions, (i, j) =>
            {
                var def = inputDefinitions[i];
                GUILayout.BeginHorizontal();
                inputDefinitions[i] = EditorGUILayout.TextField(def, GUILayout.Width(100),
                GUILayout.ExpandWidth(true));
                GUILayout.Label(typeof(T).FriendlyName(), GUILayout.Width(100), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    inputDefinitions.RemoveAt(i);
                    GatherPorts();
                    UpdateFunctionHead();
                }
                GUILayout.EndHorizontal();
            });
            

            if (GUILayout.Button("增加输入参数"))
            {
                AddInputDefinition("value" + (inputDefinitions.Count + 1));
                GatherPorts();
                UpdateFunctionHead();
            }
            if (GUILayout.Button("刷新参数Port"))
            {
                GatherPorts();
                UpdateFunctionHead();
            }

        }

#endif

    }
}
#endregion

