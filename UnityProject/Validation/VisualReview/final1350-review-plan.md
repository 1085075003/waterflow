# 最终1350关/2700帧视觉审阅

此文件是审阅流程，不是通过报告。仅最终同一版本 Unity2022.3.28f1 实际产物可登记，旧 FirstPass/旧2700帧不继承 reviewed 标志。

## 输入与冻结

根代理先将通过的 Motion/Jets/连续入水/原喷泉与颜色批次统一到最终版本，记录全部 Assets/meta SHA 与原数据catalog hash。正常游玩由两个各400关工程跑；第三视觉工程只运行同一版本 FullValidation.All，1350原关卡各初始/原解答2帧。800正常游玩与1350最终预览是不同证据，不互相顶替。

以最终 all-runtime-validation.json 的 levels 顺序和 contactSheets 列表登记：须1350唯一ID、2700有效tile、113张原contact sheet；绝不 glob 整个输出目录混入旧图。每张原图及报告记SHA、startedUtc/finishedUtc、实际工程路径与版本manifest。新版本重渲染的相同文件名不沿用旧人工审阅结论。

## 分工（按实际ID，不按连续1..1350）

- original_presentation：1..200、251..300、351..550，共450关/900帧。重点原模型/地面/相机/HUD/教程遮挡；451..550新增tipo3/4/6原喷泉也逐帧确认。
- native_mechanics：551..800、1001..1250，共500关/1000帧。重点机关层高/压板/桥柱/洞门/携带物/初始与最终代理布局差异，原静态几何不能与装饰混淆。
- original_water：1651..2050，共400关/800帧。重点四路水色、Jet方向与高低落点、原池/喷泉/发光/Night梯度、地下地面和原保留的竖直入水A片。
- root：汇总所有异常、复核跨组边界与代表关卡，并单独审最终完整UI/分类/窗口比例/动画fixtures，不为接手代理自动标通过。

1350关分组总数=450+500+400；两帧均须看，混组sheet只登记实际本人负责tile。可先逐sheet原分辨率查看全图，再对疑点打开现有全分辨率PNG；如果无成功关全分辨率图，安排独立同版该关重渲染，不能据放大缩略图证明粒子/文字细节。

## 每帧核验

1. 截图新鲜且内容非空，原场景、地面、全部模型和层级完整；相机不裁剪真实结构，HUD不压住棋盘，残留弹窗/教程按原实际操作消除。
2. 初始帧允许未连通，不要求盆全湿；最终帧对应报告的匹配水色目标与原51帧计数。mode1 st9代理不证明normal模式赢关，零目标预览需同版真实800报告旁证。
3. 通道内的原水、瀑布/上喷/喷泉、粒子与发光没有明显缺块、错误方向、重复大型面或错误颜色。原q*竖直A片、原w* B池面等已有native生命周期证据的对象不得只凭超边图隐藏。
4. 透明/发光/粒子材质无错误shader粉红/黑片；实际报告的runtime error、渲染bounds与原mesh diagnostics一起看。像素自动标记只引导人工审阅，不等同视觉fail，也不自动pass。
5. 两张静态帧不能证明动态等价：另保留已实际验证的Front/门余时/Snapshot、Animator1/2/3/5/6/7、PS原速度/Force/原RGB曲线、Menu demo等专项报告及原资源/调用依据。

## 异常台账与结案

每条记录：frameKey=gmID:start或solution、sheet与tile、sourceSHA、reviewer、observedIssue、原renderer/controller路径、native字段/方法依据、复现步骤、实际图与graph/meshdiagnostic、归因分类、重测证据。分类采用：实际渲染bug、截图/fixture故障、原设计行为、已知未证等价。unknown保留unknown，不用目标计数掩盖视觉差距。

只有看过的实际帧可以写 reviewed；所有2700条均被逐帧人工登记后才能称“2700帧已审”。修复后必须用新SHA重新审受影响帧及相关原机制代表，不修改旧失败报告。全通过仅说明明确列出的功能/视觉检查范围，不称C#源码恢复或二进制完全等价。

## Fixture入口阻断复核

FountainDispatch与DecorativePit入口直接IEnumerator，无Begin反射；真实456/549同时含tipo3/4，518含tipo6。1058/2001为Save1，Solve只作原最终预览，正常玩法另验。两fixture都等待UI再StartGame，再等Game；独立deadline与prefs/scene恢复已有。Jet/Arrival wrapper统一按实际ParameterInfo填可选参数默认值，保nativeInput=true；ArrivalFixtureBatch2处理batch干净未命名scene并写新鲜geometry时间戳。Root按最终实际结果结案，不把编译/静态审计当PASS。
