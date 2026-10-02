const family='Noto Sans SC';await figma.loadFontAsync({family,style:'Regular'});await figma.loadFontAsync({family,style:'Bold'});
const color=h=>({r:parseInt(h.slice(0,2),16)/255,g:parseInt(h.slice(2,4),16)/255,b:parseInt(h.slice(4,6),16)/255});
const main=await figma.getNodeByIdAsync('3:90');const page=main.parent;
const makeText=(p,name,content,x,y,w,size=28,bold=false)=>{const t=figma.createText();p.appendChild(t);t.name=name;t.fontName={family,style:bold?'Bold':'Regular'};t.characters=content;t.fontSize=size;t.lineHeight={value:145,unit:'PERCENT'};t.fills=[{type:'SOLID',color:color('252b28')}];t.resize(w,20);t.textAutoResize='HEIGHT';t.x=x;t.y=y;return t;};
let section=page.children.find(n=>n.name==='v12 · 出口、柱位、红木板与实灯验证');
if(!section){section=figma.createSection();page.appendChild(section);section.name='v12 · 出口、柱位、红木板与实灯验证';section.x=200;section.y=Math.max(...page.children.filter(c=>c.id!==section.id).map(c=>c.y+c.height))+300;section.resizeWithoutConstraints(7320,4940);}
let archived=section.children.find(c=>c.name==='ARCHIVE / v11 plan before correction');
if(!archived){archived=main.clone();section.appendChild(archived);archived.name='ARCHIVE / v11 plan before correction';archived.x=80;archived.y=2990;archived.visible=false;}
// Existing current-plan IDs remain stable. Only these two boards' previous children are replaced.
for(const id of ['3:90','32:1070']){const board=await figma.getNodeByIdAsync(id);if(!board)continue;const imported=figma.createNodeFromSvg(SVG);for(const n of [...board.children])n.remove();board.appendChild(imported);imported.x=0;imported.y=0;imported.name='v12 / Shared editable floor plan';board.resize(2240,2560);board.name=id==='3:90'?'03 D22 首关平面 / 当前 v12 出口与柱位':'19 D22 · 一层与二层 / v12 出口与柱位';}
for(const c of [...section.children])if(c.name.startsWith('v12 /'))c.remove();
function board(name,x,y,w=2320,h=2840){const b=figma.createFrame();section.appendChild(b);b.name=name;b.x=x;b.y=y;b.resize(w,h);b.fills=[{type:'SOLID',color:color('ece8dd')}];return b;}
const imageJobs=[];
function picture(p,name,file,x,y,w,h){const r=figma.createRectangle();p.appendChild(r);r.name=name;r.x=x;r.y=y;r.resize(w,h);r.fills=[{type:'SOLID',color:color('1b1e1b')}];imageJobs.push({file,nodeIds:[r.id]});return r;}
const plan=board('v12 / 29 平面与出口关系',80,80,2320,2840);const imported=figma.createNodeFromSvg(SVG);plan.appendChild(imported);imported.x=40;imported.y=40;
makeText(plan,'Plan source','当前平面同步 3:90 与 32:1070；出口存在明确，尺寸与二层衔接为待核工作方案。',80,2680,2160,28);
const refs=board('v12 / 30 原图与场景修正',2500,80);
makeText(refs,'Eyebrow','V12 / REFERENCE → EDITABLE GEOMETRY',80,70,2160,28,true);
makeText(refs,'Title','柱在出口前方，墙后必须留通',80,145,2160,64,true);
makeText(refs,'Subtitle','红木板连续使用同一基色；承重构件、圆牌与功放向观众方向一起前移。',80,250,2160,30);
picture(refs,'Reference IMG_5275','design/media/v12/reference-stage.jpg',80,390,1020,1520);
picture(refs,'v12 actual Blender','design/media/v12/stage-right-final.jpg',1220,390,1020,1275);
makeText(refs,'Photo caption','原始参考 / IMG_5275\n洞口在 D-22 右侧，柱在其前方。',80,1970,1020,30,true);
makeText(refs,'Render caption','Blender v12 / 实际 Cycles 渲染\n前移 1.27 m 是工作尺寸，非实测。',1220,1740,1020,30,true);
makeText(refs,'Change list','01 全部木板改用已确认的红漆基色\n02 去掉视口里重复的旧功放\n03 开口后保留通道，并检查横向净空\n04 栏板、黑砖墙与上层楼板分开处理',80,2180,2160,34);
makeText(refs,'Uncertainty','后侧弧线与 128 件廊道物件原位置保留。洞口去向、上层衔接带与所有尺寸仍待进一步确认。',80,2640,2160,28);
const lights=board('v12 / 31 灯光隔离验证',4920,80);
makeText(lights,'Eyebrow','V12 / 49 LIGHT OBJECTS / MATERIAL AUDIT',80,70,2160,28,true);
makeText(lights,'Title','光来自可编辑的灯光对象',80,145,2160,64,true);
makeText(lights,'Subtitle','相同机位与曝光。红木板不使用灯光贴图；只有小灯灯罩自发光。',80,250,2160,30);
const tests=[['正常照明','lighting-on.jpg'],['仅关四盏舞台灯','stage-spots-off.jpg'],['统一灰材质 + 实灯','lighting-only-neutral.jpg'],['所有光源能量归零','all-light-energy-off.jpg']];
for(let i=0;i<4;i++){const x=80+i%2*1140,y=390+Math.floor(i/2)*840;makeText(lights,'Test label '+i,tests[i][0],x,y,1020,32,true);picture(lights,'Test image '+i,'design/media/v12/'+tests[i][1],x,y+70,1020,714);}
makeText(lights,'Light groups','00 LIGHTS / 五个可开关灯组\n舞台 Spot 4 盏；画框、吧台、反射补光及通道灯单独分组。\n视口打开 Scene Lights / Scene World，并选中 L01 显示光锥。',80,2180,2160,31);
makeText(lights,'Lighting limits','灰材质仍产生明暗；所有光源、环境光和灯罩发光关闭后画面近乎全黑。\nMeshy 器材颜色贴图可能含生成的明暗，未宣称逐件完成去光照处理。',80,2480,2160,28);
const wide=board('v12 / 32 通道与二层结构检查',80,3000,7160,1860);
makeText(wide,'Title','出口进深、连续红木板与二层衔接',80,70,7000,64,true);
const shots=[['出口与前移柱位','exit-final.jpg'],['后侧连续红色','rear-final.jpg'],['结构剖开检查','stage-cutaway.jpg']];
for(let i=0;i<3;i++){const x=80+i*2340;makeText(wide,'View label '+i,shots[i][0],x,215,2160,38,true);picture(wide,'View image '+i,'design/media/v12/'+shots[i][1],x,305,2160,1350);}
makeText(wide,'Footer','剖开图临时隐藏外墙与顶面，并加中性补光以展示结构；正式场景保留完整外墙。几何净空检查不等于游戏碰撞验收。',80,1740,6970,27);
figma.currentPage.selection=[plan];figma.viewport.scrollAndZoomIntoView([plan]);
return {section:section.id,boards:[plan.id,refs.id,lights.id,wide.id],plans:['3:90','32:1070'],imageJobs,url:'https://www.figma.com/design/LOMa9NEcogELqstS7Fsol0?node-id='+section.id.replace(':','-')};
