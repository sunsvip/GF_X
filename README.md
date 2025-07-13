# GF_X
点击链接加入群聊【GF_X自动化游戏框架】：[QQ交流群:1035236947](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=sA2mRXcNn1vQb5dz8pe1wndL9jr8gOKA&authKey=hk7wZDWjniHi2kJexJxSMZsgmXgf%2B3JDRQWCaYih9mF7V%2ByZ%2F%2BzMG4fThy2vF2Ze&noverify=0&group_code=1035236947)
### 介绍
[GameFramework](https://github.com/EllanJiang/GameFramework) + [HybridCLR](https://github.com/focus-creative-games/hybridclr)，通过静态扩展方式，极度简化或扩展框架接口，并编写了大量自动化工具，主打一个工业化生产工作流，追求极致性能和开发效率，使GF对新手友好，开箱即用。
作为极其懒惰、从不加班的设计开发工程师，我的宗旨是拒绝一切高重复度工作内耗，框架层零投入，用户只需专注业务逻辑。
### [GF_X DeepWiki](https://deepwiki.com/sunsvip/GF_X)

### 框架功能介绍：[【Unity自动化游戏框架】通用自动化游戏框架 爽到起飞的工作流 巨幅提升效率 质量 产能 功能展示](https://blog.csdn.net/final5788/article/details/138164034)

### 视频教程(用爱发电,免费持续更新)
0. [GF_X半小时极速入门](https://www.bilibili.com/video/BV1AT2rYVE3V/?share_source=copy_web&vd_source=47daa1bb9519dea051e24cd30d7be9be)
1. [自动化工具集用法 降本增效](https://www.bilibili.com/video/BV1AiyeYpEQF?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
2. [DataTable数据表用法 类型扩展 智能自动导表](https://www.bilibili.com/video/BV1enS2YSEVB?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
3. [Entity用法 实体创建 自动对象池管理](https://www.bilibili.com/video/BV11BS2YMEUN/?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
4. [UI系统用法 代码生成工具 二级界面](https://www.bilibili.com/video/BV1VsSUYREz7?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
5. [UI Item用法 对象池复用 代码生成](https://www.bilibili.com/video/BV1gAmkYZEg4?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
6. [打包工具 一键打包 打热更 自动化工作流](https://www.bilibili.com/video/BV18AmkYZEBt?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
7. [jenkins远程打包 打热更 远程打包部署](https://www.bilibili.com/video/BV1DAmkYZEus?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
8. [代码加固混淆 Obfuz加密 代码安全 AOT加密](https://www.bilibili.com/video/BV1RQjgzLEii?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
9. [WebGL打包 热更](https://www.bilibili.com/video/BV1UP3czfEr3?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
10. [WebGL转微信小游戏和热更](https://www.bilibili.com/video/BV1p13BzxEHf?vd_source=4d9d1930ecd35c4ed49ec0cdae412285)
    
(持续更新中...)

### GF_X功能说明：
1. 简化和扩展GameFramework接口，适配WebGL(支持热更)小游戏，新手友好，开箱即用。并支持GF所有异步加载的方法通过UniTask"可等待"加载。
2. 高效自动化工作流

   ①打包工具：一键打包/打热更，一键切换单机/热更，支持Obfuz代码加固。支持Jenkins远程打包/打热更。自动处理AB包资源重复依赖。

   ②包体优化工具集：批量压缩图片文件、修改压缩格式，批量创建图集、图集变体。压缩动画文件。

   ③数据表/常量表/多语言表：excel表修改后自动导表、生成表结构代码。excel表自动生成数据类型下拉列表。

   ④多语言工具：一键扫描生成多语言表，一键新增语言，自动翻译。告别冗余，一键精准控制保留行。

   ⑤实用工具集/批处理工具：代码裁剪link.xml配置工具, 艺术字生成，图集生成，一键生成TMP_SpriteAsset, SpriteAtlas转Sprite图集。批量替换Prefab字体。

   ⑥UI工具：零代码添加UI变量，自动隔离式生成变量代码，零侵入。一键添加按钮事件、批量设置Rasycast。可视化UI动效。
4. 支持A/B Test; 使用GF.Setting.SetABTestGroup("GroupName")对用户分配测试组，不同测试组会读取对应组的配置表/数据表。
5. 扩展模块以应对极致的性能要求，如实现万人同屏。
6. 其它N多扩展功能自行探索。
### 安装教程
1.  首次使用需安装HybridCLR环境，点击Unity顶部菜单栏 【HybridCLR->Installer】安装HybridCLR环境;
2.  Unity工具栏 【Build App/Hotfix】按钮打开一键打包界面; 首次打热更App需点击【Build App】按钮右侧的下拉菜单，点击【Full Build】构建；
3.  Build App出包，Build Resource打热更;

### 使用说明
1.  开发目录为Assets/AAAGame, Script为热更新脚本目录，ScriptBuiltin为内置程序集；
2.  Assets/AAAGame/Scene/Launch场景为游戏的启动场景


### 工作流
前言:
首先要理清游戏核心元素，游戏中可见的元素一共就两种，UI界面和GameObject物体(模型、Sprite、粒子等)

可见元素：GF简单得分为两种，即UIForm和Entity(UI和实体)，分别通过UIComponent和EntityComponent管理。（实体Entity可以理解为任何GameObject）
不可见元素: 即游戏的业务逻辑，GF提供了流程(Procedure)，通过有限状态机来管理游戏流程，使游戏逻辑结构清晰。

此框架对GF又进一步封装,使用更加简单便捷:

UIForm: 使用 GF.UI.OpenUIForm() 打开UI, GF.UI.CloseUIForm()关闭UI； 开发者只管打开/关闭，GF内部会自动通过对象池管理复用或销毁释放。

Entity：使用 GF.Entity.ShowEntity()显示实体， GF.Entity.HideEntity()隐藏实体； 同样,开发者只需管显示/隐藏，GF会自动对象池复用或销毁释放。

Procedure：它拥有类似MonoBehaviour的生命周期，如OnEnter(进入流程)、OnUpdate(每帧刷新)、OnLeave(离开流程)，在流程内调用ChangeState()即可切换到其它流程

GF_HybridCLR通过Procedure(流程)来走游戏逻辑，游戏入口流程为LaunchProcedure
通过流程可以清晰地管理游戏逻辑，例如：本框架的基本流程为：

1. LaunchProcedure(启动流程,主要处理初始化游戏设置,如语言，音效，震动等) => 完成后切换至CheckAndUpdateProcedure
2. CheckAndUpdateProcedure(此流程处理单机/热更模式的资源初始化逻辑，处理App升级和资源更新以及初始化资源，只需要打AB资源时配置热更地址等即可实现完整热更) => 资源准备就绪，切换至LoadHotfixDllProcedure
3. LoadHotfixDllProcedure(主要为HybridCLR做准备，如加载热更dll，AOT泛型补充。 反射调用热更类HotfixEntry.StartHotfixLogic创建新的流程状态机) => 开始进入热更代码的Procedure

以上为Builtin(内置)代码，逻辑上比较靠前，不可以热更新。至此之后，逻辑被热更新代码接管，进入PreloadProcedure

4. PreloadProcedure(预加载流程，会添加GF框架扩展功能、设置AB测试组， 加载数据表/配置表/多语言) => 切换至ChangeSceneProcedure
5. ChangeSceneProcedure(此流程专门用于切换场景，不同的场景对应切换至不同的流程) => 切换至Game场景，同时流程切换为MenuProcedure
6. MenuProcedure(即游戏的主菜单流程, 流程OnEnter时显示主菜单UI，通常有开始游戏入口、商店入口、设置入口等，流程OnLeave时关闭主菜单等) => 若玩家点击开始，切换至GameProcedure
7. GameProcedure(游戏玩法流程，例如: 加载游戏关卡地图，创建玩家、敌人等) => 游戏胜利/失败，切换到GameOverProcedure，显示游戏胜利/失败界面，进行游戏结算等
8. GameOverProcedure(游戏结束流程, 通常会显示游戏胜利/失败界面, 进行游戏奖励结算, 并显示下一关、重玩按钮或返回主页等按钮) => 如点击下一关, 把关卡Id改为下一关然后切换到GameProcedure就开始下一关游戏了。点击返回主页按钮切换到MenuProcedure就回到了游戏主菜单。

使用有限状态机Procedure管理游戏逻辑，结构清晰，逻辑简单。无论是维护还是debug都非常容易切入，使用Procedure可以规范开发

### 引用库 致谢
[UnityGameFramework](https://github.com/EllanJiang/UnityGameFramework)

[HybridCLR](https://github.com/focus-creative-games/hybridclr)

[UGFExtensions](https://github.com/FingerCaster/UGFExtensions)

[UniTask(零分配Task,gc优化)](https://github.com/Cysharp/UniTask)

[ZString(零分配StringBuilder,字符串连接、格式化gc优化)](https://github.com/Cysharp/ZString)
