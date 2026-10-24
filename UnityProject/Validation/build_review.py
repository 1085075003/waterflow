"""Build a dependency-free local viewer for actual Unity validation captures."""
import json
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parent
report_path = root / "FullContent" / "all-runtime-validation.json"
report = json.loads(report_path.read_text(encoding="utf-8-sig"))
payload = json.dumps(report, ensure_ascii=False).replace("</", "<\\/")
html = r'''<!doctype html>
<html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Flow Water · Unity 实测画面</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#13212b;color:#e3eef5;font:15px/1.6 system-ui,sans-serif}
main{max-width:1440px;margin:auto;padding:28px}h1{font-size:25px;margin:0 0 8px}p{margin:5px 0 15px;color:#bacbd6}
a{color:#8edbff}nav{display:flex;gap:12px;flex-wrap:wrap;align-items:center;margin:18px 0;position:sticky;top:0;background:#13212bf5;padding:12px 0;z-index:2}
input,select,button{font:inherit;background:#223846;border:1px solid #557183;color:inherit;padding:7px 12px;border-radius:5px}input{width:180px}button{cursor:pointer}button:disabled{opacity:.35;cursor:default}
#cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,672px),1fr));gap:18px}article{background:#1b303f;border:1px solid #345062;border-radius:7px;padding:14px;overflow:hidden}
.line{display:flex;justify-content:space-between;gap:12px}.pass{color:#83dfb0}.fail{color:#ffb09e}.frames{display:flex;flex-wrap:wrap;gap:12px;margin-top:10px}
figure{margin:0}figcaption{font-size:12px;color:#bacbd6}.tile{display:block;width:320px;height:180px;background-repeat:no-repeat;background-size:1920px 720px;border:1px solid #49606c}
pre{white-space:pre-wrap;margin:8px 0;color:#ffb09e;font:13px/1.6 system-ui,sans-serif}.summary{font-variant-numeric:tabular-nums}footer{color:#8ca8b9;margin-top:30px;font-size:13px}
</style><main>
<h1>Flow Water · Unity 实测画面</h1>
<p>每关包含原始布局与应用原解答后的实际渲染。画面取自本机 Unity 2022.3.28f1；点击缩略图打开对应原始拼图。</p>
<p class="summary" id="summary"></p><p><a href="all-runtime-validation.json">全部布局 JSON</a> · <a href="../VisualReview/final-frame-review.json">2700帧画面复核</a> · <a href="../Save1Gameplay/native-gameplay-coverage.json">800关真实拖放汇总</a> · <a href="../FinalGameplay/native-recipe-regression-coverage.json">169关修复回归</a> · <a href="../Interaction/interaction-validation.json">真实拖动</a> · <a href="../WaterPropagation/water-propagation-validation.json">逐段水流</a> · <a href="../CategoryPreview/category-preview-validation.json">分类演示</a> · <a href="../UIFlows/ui-flow-validation.json">菜单交互</a> · <a href="../Springs/spring-validation.json">机关测试</a> · <a href="../Aspect/aspect-validation.json">窗口比例</a></p>
<nav><input id="search" placeholder="输入 gm 编号"><select id="filter"><option value="all">全部已测关卡</option><option value="failed">仅失败关卡</option><option value="passed">仅通过关卡</option></select><button id="prev">上一页</button><span id="page"></span><button id="next">下一页</button></nav>
<div id="cards"></div><footer id="footer"></footer></main>
<script>
const report=__PAYLOAD__;
const perPage=24;let page=0;
const el=id=>document.getElementById(id);
const original=report.levels.map((r,i)=>({...r,captureIndex:i}));
const escapeText=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
function frame(index,solution){const tile=index*2+(solution?1:0),sheet=Math.floor(tile/24)+1,slot=tile%24;
 const file='all-contact-'+String(sheet).padStart(3,'0')+'.png';
 return '<figure><a class="tile" href="'+file+'" target="_blank" style="background-image:url(\''+file+'\');background-position:-'+(slot%6*320)+'px -'+(Math.floor(slot/6)*180)+'px"></a><figcaption>'+(solution?'应用原解答后':'原始布局')+'</figcaption></figure>';}
function draw(){const query=el('search').value.trim().toLowerCase().replace(/^gm/,'');const mode=el('filter').value;
 const rows=original.filter(r=>(!query||String(r.assetId).includes(query))&&(mode==='all'||(mode==='failed'?r.failures.length>0:r.failures.length===0)));
 const pages=Math.max(1,Math.ceil(rows.length/perPage));page=Math.min(page,pages-1);
 el('page').textContent=(page+1)+' / '+pages+' · '+rows.length+' 关';el('prev').disabled=page===0;el('next').disabled=page===pages-1;
 el('cards').innerHTML=rows.slice(page*perPage,(page+1)*perPage).map(r=>'<article><div class="line"><strong>gm'+r.assetId+' · '+escapeText(r.scene)+'</strong><span class="'+(r.failures.length?'fail':'pass')+'">'+(r.failures.length?'待修复':'通过')+' · '+(r.previewHasNoCountedTargets?'预览无计数目标；正常游玩'+(r.normalGameplayVerified?'另验通过':'待验证'):'喷泉 '+r.activeFountains+'/'+r.requiredFountains)+'</span></div>'+(r.failures.length?'<pre>'+escapeText(r.failures.join('\n'))+'</pre>':'')+'<div class="frames">'+frame(r.captureIndex,false)+frame(r.captureIndex,true)+'</div></article>').join('');}
el('summary').textContent='Unity '+report.unityVersion+' · 已测 '+report.completed+'/'+report.requested+' · 失败检查 '+report.failureCount+' · 运行异常 '+report.runtimeErrors.length+(report.finishedUtc?' · 本次验证已结束':' · 本次验证仍在进行');
el('footer').textContent='截图缩略图为 320×180。含计数目标的解答要求全部目标喷泉收到匹配颜色的水并实际触发原 51 帧轮询通关；无计数目标的原版预览另要求正常游玩证据。测试按原延迟逐段传播，并等待传播结束；不等同于已证明与原二进制完全等价。报告开始时间 '+report.startedUtc+'。';
el('search').oninput=el('filter').onchange=()=>{page=0;draw()};el('prev').onclick=()=>{page--;draw();window.scrollTo(0,0)};el('next').onclick=()=>{page++;draw();window.scrollTo(0,0)};draw();
</script></html>'''
destination = report_path.parent / "index.html"
destination.write_text(html.replace("__PAYLOAD__", payload), encoding="utf-8")
(report_path.parent / "review.html").write_text(html.replace("__PAYLOAD__", payload), encoding="utf-8")
print(json.dumps({"review": str(destination), "completed": report["completed"], "generatedUtc": datetime.now(timezone.utc).isoformat()}))
