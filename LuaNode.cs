//要使用此节点需要下载 合并 ToLua的项目工程文件:  https://github.com/topameng/tolua
using System.Collections.Generic;
using System.Linq;
using LuaInterface;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
#endif

namespace FlowCanvas.Nodes
{   
    abstract public class CallLuaMethodBase : FlowNode
    {
        protected LuaFunction luaFunc;
        protected LuaState lua ;


        public string functionHead = "";
        public string functionName = "LuaMethod";
	    public string functionBody = "\n  \n   \n ";
        
	    public bool loadScript=false;
	    public bool LoadScript{
	    	get {
	    		return loadScript;
	    	}
	    	set {
	    		if (loadScript!=value)
	    		{
		    		loadScript=value;
		    		GatherPorts();
	    		}

	    	}
	    }

        public virtual string endPart
        {
            get
            {
                return string.Format(" \n end \n this = {{}} this.{0} = {0} \n", functionName);
            }
        }

        [SerializeField] protected string endBody = "GameObject=UnityEngine.GameObject" +
                                                    "\nMathf=UnityEngine.Mathf" +
                                                    "\nTime=UnityEngine.Time" +
                                                    "\nApplication=UnityEngine.Application" +
                                                    "\nScreen=UnityEngine.Screen" +
                                                    "\nResources=UnityEngine.Resources"+ "\nInput=UnityEngine.Input";



        public virtual string UpdateFunctionHead()
        {
            return "";
        }
		

	    public virtual bool Combined { get; set; }
        
	    ValueInput<string> script;
	    protected override void RegisterPorts()
	    {
	    	if (loadScript)
	    	{
	    		script=AddValueInput<string>("LuaScript");
	    	}
	    }

        public override void OnGraphStarted()
        {
            base.OnGraphStarted();

#if UNITY_EDITOR
            if (highLightVariable)
            {   
                if(!string.IsNullOrEmpty(recordFunctionBody))
                    functionBody = recordFunctionBody;
            }
#endif
			
			if (loadScript)
			{
				functionBody=script.value;
			}
			
            if (!Combined)
            {
                string CombineLuaScript = "";
                List<string> functionNameList = new List<string>();
                List<string> usingList = new List<string>();
                var luaNodeList = graph.GetAllNodesOfType<CallLuaMethodBase>();
                luaNodeList.ForEach(x =>
                {   
                    x.Combined = true;
                    CombineLuaScript += string.Format("{0}{1}\nend\n\n", x.UpdateFunctionHead(), x.functionBody);

                    functionNameList.Add(x.functionName);
                    string[] endbodyWithoutBlank = x.endBody.Replace(" ", "").Split('\n');
                    endbodyWithoutBlank.ForEach(y =>{
                        if (!usingList.Contains(y))
                        {
                            usingList.Add(y);
                        }
                    });

                }); //其他luaNode不需要再初始化了

                CombineLuaScript += "\n this = {{}} ";
                functionNameList.ForEach(x => CombineLuaScript += string.Format("\n this.{0}={0}", x));
                usingList.ForEach(x=> CombineLuaScript += "\n"+x);

                new LuaResLoader();

                LuaState lua = new LuaState();

                lua.Start();
                DelegateFactory.Init();
                LuaBinder.Bind(lua); //非常重要,能调用lua加载的类型方法
                //Debug.Log(CombineLuaScript);

                lua.DoString(CombineLuaScript);
                luaNodeList.ForEach(x=>x.GetFunction(lua));
            }           
        }

        public void GetFunction(LuaState _lua)
        {
            lua = _lua;
            luaFunc = _lua.GetFunction("this." + functionName);
        }

        public override void OnGraphStoped()
        {
            base.OnGraphStoped();
            if (luaFunc != null)
            {
                luaFunc.Dispose();
                luaFunc = null;
            }
        }

        /// <summary>
        /// 如果用的是原版插件，请移除bool参数。
        /// </summary>
        /// <param name="isReplace"></param>
        public override void OnDestroy(bool isReplace)
        {
            if (luaFunc != null)
            {
                luaFunc.Dispose();
                luaFunc = null;
            }
            base.OnDestroy(isReplace);
        }

        public List<string> outParaNameList =new List<string>();
#if UNITY_EDITOR
        public bool showExample;
        public bool highLightVariable;
        protected override void OnNodeInspectorGUI()
	    {	
		    LoadScript = GUILayout.Toggle(LoadScript, "LoadLuaScript");
        	
            GUIStyle textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.8f,0.8f,0.8f);
            textAreaStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.active.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.hover.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.focused.textColor = new Color(0.8f, 0.8f, 0.8f);
            textAreaStyle.margin = new RectOffset(5, 5, 5, 5);
            textAreaStyle.richText = true;
            GUILayout.Label("FunctionName 不能重名");
            functionName = EditorGUILayout.TextField("functionName", functionName);

            EditorGUILayout.TextArea(functionHead);

            if (!highLightVariable)
                functionBody = EditorGUILayout.TextArea(functionBody);
            else
            {
                EditorGUILayout.TextArea(functionBody, textAreaStyle);
            }

            GUILayout.Label(endPart);
            GUILayout.Label("在CustomSetting.cs里可以查看和添加class");
            endBody=EditorGUILayout.TextArea(endBody);
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
                GUILayout.TextArea(
                    "示例:使用Type或静态方法 如:使用GameObject.AddComonent(typeof(ParticleSystem)), \n 在最下方 写入 GameObject = UnityEngine.GameObject 和 ParticleSystem = UnityEngine.ParticleSystem \n  \n 调用方法使用冒号 ':' 如 transform: Rotate, 属性和字段'.'点号不变 \n 构造实例不需要 new, 变量无需声明类型, 如: angle = Vector3(45, 0, 0) \n 常用的语句: \n if a > 0 then\n ...\n end " +
                    "\n\n  if a>0 then\n  ... \n elseif a=10 then \n  ...\n  else\n  ... \n end \n\n while a>0 do \n ... end \n \nfor index= 0, 10 ,(从0-10的增值幅度,默认1,可不写) do\n  ...break 或 return 中止跳出循环  \nend \n \nfor index, value in pairs(字典或table) do\n  index 指数 ,value 值... 中止  \nend  \n repeat ...\n  until a>0 \n 遍历数组,列表或字典 local iter = myDiectionary:GetEnumerator() \n while iter:MoveNext() do\n  print(iter.Current.Key..iter.Current.Value)\n end \r \n\n赋值: 字符串: a = 'hello'\n num = num+1 ,无++ 或-- , a,b=5,6 等同" +
                    " a=5 b=6 ,x, y = y, x 可以交换数值(计算好后才赋值) \n 方法外声明的变量 j = 10 是全局变量  \r\n local i = 1 有local 是局部变量 \n nil是空 不是null  \n 获取type:  type(gameobject)  获得字符串: tostring(10) 字符串用双引号和单引号都可以; \n\n 表格 Table : 新建表格 myTable={} ; myTable={ 1,'jame',gameobject} 可以传入任意类型的值, 默认索引以1开始\n" +
                    " 可以使用字符做key myTable['myKey']=myObject,字符串做索引还可以用myTable.myKey形式 \n 也可以用数字做key,如:return myTable[2], \n\n 算数符: +(加) -(减) *(乘) /(除) %(取余) ^(乘幂: 10^2=100) -(负号: -100) \n\n 关系运算符: == 等于 ~=不等于 >大于 <小于 >= <= \n \n逻辑运算符: and(相当于&&) or(相当于||) not(相当于!)+\n\n 其他运算符: .. 字符串相加 # 字符长度(#hello 为5 ,也可用于table的count数值) \n\n 数学库:" +
                    "圆周率: math.pi  \n绝对值math.abs(-15)=15 ; \nmath.ceil(5.8)=6 ;\nmath.floor(5.6)=5 ;\n取模运算:math.mod(14, 5)=4 ;\n最大值: math.max(2.71, 100, -98, 23)=100 ;\n最小值 math.min ; \n得到x的y次方: math.pow(2, 5)=32 ;  \n开平方函数: math.sqrt(16)=4;角度转弧度: 3.14159265358 ;\n 弧度转角度: math.deg(math.pi)=180 ;\n 获取随机数: math.random(1, 100) ;\n设置随机数种子:math.randomseed(os.time()) 在使用math.random函数之前必须使用此函数设置随机数种子  ;\n正弦: math.sin(math.rad(30))=0.5 ; \n反正弦函数:math.asin(0.5)=0.52359877 ;");
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
                inputString= inputString.Replace(x, string.Format("<b><color=#F4A460>{0}</color></b>", x))
            );

            //改变方法和属性的颜色
            string method = @"\:[A-Z]\w*\W";
            var methodResult = Regex.Matches(inputString, method);
            methodResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                inputString = Regex.Replace(inputString, y.Replace("(", ""),y.Replace(y, string.Format(":<b><color=#00CD00>{0}</color></b>", y.Replace(":", "").Replace("(", ""))));
            });

            List<string>availAbleType=new List<string>();            
            string type = @"\s*?\w+=";
            var typeResult = Regex.Matches(endBody, type);
            typeResult.Cast<Match>().Select(x => x.Value).ForEach(y =>
            {
                y = y.Replace("=", "").Replace(" ","").Replace("\n", "");
                //Debug.Log(y);
                if (!availAbleType.Contains(y))
                    availAbleType.Add(y);
            });

            availAbleType.ForEach(x=>inputString=inputString.Replace(x, string.Format("<b><color=#5CACEE>{0}</color></b>", x)));

            //改变单引号颜色
            inputString = inputString.Replace("\'", string.Format("<b><color=#EE5C42>\'</color></b>"));
            //Debug.Log(inputString);
            return inputString;
        }

        public void ConvertCSharpScript()
        {
            string p = @"^\s*"; //消除开头空格
            var result = Regex.Matches(functionBody, p);
            result.Cast<Match>().Select(x=>x.Value).ForEach(y=> { functionBody = Regex.Replace(functionBody,y, ""); });

            //string p2 = @"\.[A-Z]+?"; //将方法前的点好换成冒号
            //var result2 = Regex.Matches(functionBody, p2);
            //result2.Cast<Match>().Select(x => x.Value).ForEach(y =>
            //{
            //    //Debug.Log(y);
            //    functionBody = Regex.Replace(functionBody, y, y.Replace(".",":")); });

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
#endif
    }

    [Name("CallLuaAction")]
    [Category("UnityEngine/Lua")]
    [Description("调用Lua方法 (无返回值)")]
    [ContextDefinedInputs(typeof(Flow))]
    public class CallLuaActionScript: CallLuaMethodBase
    {
        public override string name
        {
            get { return string.Format("{0}", string.IsNullOrEmpty(functionName) ? "LuaAction:" : functionName); }
        }

        public  override string endPart
        {
            get { return string.Format(" end \n this = {{}} this.{0} = {0} \n", functionName); ; }
        }
        FlowOutput outPut;
        protected override void RegisterPorts()
	    {	
			
		    base.RegisterPorts();
			
            List<ValueInput> ins=new List<ValueInput>();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                var def = inputDefinitions[i];

                ins.Add(AddValueInput(def.name, def.type,def.ID));
            }

            outPut = AddFlowOutput("Out");
            AddFlowInput("Call", f =>
            {
                switch (ins.Count)
                {
                    case 0:
                        luaFunc.Call();
                        break;
                    case 1:
                         luaFunc.Call(ins[0].value);
                        break;
                    case 2:
                        luaFunc.Call(ins[0].value, ins[1].value);
                        break;
                    case 3:
                        luaFunc.Call(ins[0].value, ins[1].value, ins[2].value);
                        break;
                    case 4:
                        luaFunc.Call(ins[0].value, ins[1].value, ins[2].value, ins[3].value);
                        break;
                    case 5:
                        luaFunc.Call(ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value);
                        break;
                    case 6:
                        luaFunc.Call(ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value);
                        break;
                    case 7:
                        luaFunc.Call(ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value, ins[6].value);
                        break;
                    case 8:
                        luaFunc.Call(ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value, ins[6].value, ins[7].value);
                        break;
                    case 9:
                        luaFunc.Call(ins[0].value, ins[1].value, ins[2].value, ins[3].value, ins[4].value, ins[5].value, ins[6].value, ins[7].value, ins[8].value);
                        break;
                }
                lua.CheckTop();

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
            functionHead= string.Format(" function {0} ( {1} )\n", functionName,paras);
            return functionHead;
        }

        #region port Define
        [HideInInspector]
        [SerializeField]
        public List<DynamicPortDefinition> inputDefinitions =
            new List<DynamicPortDefinition>();

        public bool AddInputDefinition(DynamicPortDefinition def)
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
                    var def = new DynamicPortDefinition(port.name, port.type);
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


        private Vector2 nodeView;
        public bool showAllNode=true;
        protected override void OnNodeGUI()
        {   
            UpdateFunctionHead();
            base.OnNodeGUI();


            if (showAllNode)
            {
                GUILayout.Label("传入参数:");

                EditorUtils.ReorderableList(inputDefinitions, (i, j) =>
                {
                    var def = inputDefinitions[i];
                    GUILayout.BeginHorizontal();
                    def.name = EditorGUILayout.TextField(def.name, GUILayout.Width(0),
                        GUILayout.ExpandWidth(true));
                    GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(0), GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        inputDefinitions.RemoveAt(i);
                        GatherPorts();
                        UpdateFunctionHead();
                    }
                    GUILayout.EndHorizontal();
                });
                functionName = EditorGUILayout.TextField("functionName", functionName);
                EditorGUILayout.LabelField(functionHead);
                nodeView = GUILayout.BeginScrollView(nodeView, true, false, GUILayout.Width(280), GUILayout.Height(150));

                if (!highLightVariable)
                    functionBody = EditorGUILayout.TextArea(functionBody, GUILayout.Width(260));
                else
                {
                    //GUIStyle s= new GUIStyle();
                    //s.richText = true;
                    EditorGUILayout.TextArea(functionBody, GUILayout.Width(260));
                }
                EditorGUILayout.LabelField(endPart);
                //endBody = EditorGUILayout.TextArea(endBody);
                GUILayout.EndScrollView();
            }

            if (GUILayout.Button("增加输入参数"))
            {
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), t =>
                {
                    AddInputDefinition(new DynamicPortDefinition("value", t));
                    GatherPorts();
                    UpdateFunctionHead();
                });
            }
            if (GUILayout.Button("刷新参数Port"))
            {
                GatherPorts();
                UpdateFunctionHead();
            }
            if (GUILayout.Button(!showAllNode? "显示代码": "折叠"))
            {
                showAllNode = !showAllNode;
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
            get { return string.Format("{0}",  string.IsNullOrEmpty(functionName)? "LuaFunction:" : functionName); }
        }

        public override string endPart
        {
            get { return string.Format(" end \n this = {{}} this.{0} = {0} \n", functionName); ; }
        }

        private T result;

        protected override void RegisterPorts()
	    {	
		    base.RegisterPorts();


            List<ValueInput> ins = new List<ValueInput>();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                var def = inputDefinitions[i];
                ins.Add(AddValueInput(def.name, def.type, def.ID));
            }
            AddValueOutput("result", () =>
            {
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
                lua.CheckTop();
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
            functionHead = string.Format(" function {0} ( {1} )\n", functionName, paras);
            return functionHead;
        }


        #region port Define
        [HideInInspector]
        [SerializeField]
        public List<DynamicPortDefinition> inputDefinitions =
            new List<DynamicPortDefinition>();

        public bool AddInputDefinition(DynamicPortDefinition def)
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
                    var def = new DynamicPortDefinition(port.name, port.type);
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

        private Vector2 nodeView;
        public bool showAllNode = true;
        protected override void OnNodeGUI()
        {
            UpdateFunctionHead();
            base.OnNodeGUI();


            if (showAllNode)
            {
                GUILayout.Label("传入参数:");

                EditorUtils.ReorderableList(inputDefinitions, (i, j) =>
                {
                    var def = inputDefinitions[i];
                    GUILayout.BeginHorizontal();
                    def.name = EditorGUILayout.TextField(def.name, GUILayout.Width(0),
                        GUILayout.ExpandWidth(true));
                    GUILayout.Label(def.type.FriendlyName(), GUILayout.Width(0), GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        inputDefinitions.RemoveAt(i);
                        GatherPorts();
                        UpdateFunctionHead();
                    }
                    GUILayout.EndHorizontal();
                });
                functionName = EditorGUILayout.TextField("functionName", functionName);
                EditorGUILayout.LabelField(functionHead);
                nodeView = GUILayout.BeginScrollView(nodeView, true, false, GUILayout.Width(280), GUILayout.Height(150));

                if (!highLightVariable)
                    functionBody = EditorGUILayout.TextArea(functionBody, GUILayout.Width(260));
                else
                {
                    //GUIStyle s= new GUIStyle();
                    //s.richText = true;
                    EditorGUILayout.TextArea(functionBody, GUILayout.Width(260));
                }
                EditorGUILayout.LabelField(endPart);
                //endBody = EditorGUILayout.TextArea(endBody);
                GUILayout.EndScrollView();
            }

            if (GUILayout.Button("增加输入参数"))
            {
                EditorUtils.ShowPreferedTypesSelectionMenu(typeof(object), t =>
                {
                    AddInputDefinition(new DynamicPortDefinition("value", t));
                    GatherPorts();
                    UpdateFunctionHead();
                });
            }
            if (GUILayout.Button("刷新参数Port"))
            {
                GatherPorts();
                UpdateFunctionHead();
            }
            if (GUILayout.Button(!showAllNode ? "显示代码" : "折叠"))
            {
                showAllNode = !showAllNode;
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
		    base.RegisterPorts();

            List<ValueInput<T>> ins = new List<ValueInput<T>>();
            for (var i = 0; i < inputDefinitions.Count; i++)
            {
                var def = inputDefinitions[i];
                ins.Add(AddValueInput<T>(def));
            }
            AddValueOutput("result", () => {

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
                lua.CheckTop();
                return result; });

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
            functionHead = string.Format(" function {0} ( {1} )\n", functionName, paras);
            return functionHead;
        }


        #region port Define
        [HideInInspector]
        [SerializeField]
        public List<string> inputDefinitions =
            new List<string> { "value1", "value2"};

        public void AddInputDefinition(string portName)
        {
            inputDefinitions.Add(portName);
        }


        #endregion
#if UNITY_EDITOR

        private Vector2 nodeView;
        public bool showAllNode = true;
        protected override void OnNodeGUI()
        {   
            UpdateFunctionHead();
            base.OnNodeGUI();
            if (showAllNode)
            {
                GUILayout.Label("传入参数:");

                EditorUtils.ReorderableList(inputDefinitions, (i, j) =>
                {
                    var def = inputDefinitions[i];
                    GUILayout.BeginHorizontal();
                    inputDefinitions[i] = EditorGUILayout.TextField(def, GUILayout.Width(0),
                    GUILayout.ExpandWidth(true));
                    GUILayout.Label(typeof(T).FriendlyName(), GUILayout.Width(0), GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        inputDefinitions.RemoveAt(i);
                        GatherPorts();
                        UpdateFunctionHead();
                    }
                    GUILayout.EndHorizontal();
                });
                functionName = EditorGUILayout.TextField("functionName", functionName);
                EditorGUILayout.LabelField(functionHead);
                nodeView = GUILayout.BeginScrollView(nodeView, true, false, GUILayout.Width(280), GUILayout.Height(100));
                if (!highLightVariable)
                    functionBody = EditorGUILayout.TextArea(functionBody, GUILayout.Width(260));
                else
                {
                    //GUIStyle s= new GUIStyle();
                    //s.richText = true;
                    EditorGUILayout.TextArea(functionBody, GUILayout.Width(260));
                }
                EditorGUILayout.LabelField(endPart);
                //endBody = EditorGUILayout.TextArea(endBody);
                GUILayout.EndScrollView();
            }

            if (GUILayout.Button("增加输入参数"))
            {
                AddInputDefinition("value"+(inputDefinitions.Count+1));
                GatherPorts();
                UpdateFunctionHead();
            }
            if (GUILayout.Button("刷新参数Port"))
            {
                GatherPorts();
                UpdateFunctionHead();
            }
            if (GUILayout.Button(!showAllNode ? "显示代码" : "折叠"))
            {
                showAllNode = !showAllNode;
            }
        }

#endif

    }
}