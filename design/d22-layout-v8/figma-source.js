await figma.loadFontAsync({family:'Noto Sans SC',style:'Regular'});
await figma.loadFontAsync({family:'Noto Sans SC',style:'Bold'});
await figma.loadFontAsync({family:'Inter',style:'Medium'});
const C={paper:'#EAE5D9',white:'#F8F5EE',ink:'#232724',muted:'#60665F',red:'#8B3C35',tile:'#D0BFA1',wood:'#5B5148',teal:'#3E7164',blue:'#416980',amber:'#A47735',line:'#ABAFA6',paleBlue:'#DCE6E8',paleAmber:'#EEE0C5'};
const rgb=h=>({r:parseInt(h.slice(1,3),16)/255,g:parseInt(h.slice(3,5),16)/255,b:parseInt(h.slice(5,7),16)/255});
const paint=h=>[{type:'SOLID',color:rgb(h)}];
function rect(p,name,x,y,w,h,fill,stroke=null,sw=1,dash){const n=figma.createRectangle();p.appendChild(n);n.name=name;n.x=x;n.y=y;n.resize(w,h);n.fills=fill?paint(fill):[];n.strokes=stroke?paint(stroke):[];n.strokeWeight=sw;if(dash)n.dashPattern=dash;return n;}
function text(p,name,str,x,y,w,size=24,color=C.ink,bold=false){const n=figma.createText();p.appendChild(n);n.name=name;n.fontName={family:'Noto Sans SC',style:bold?'Bold':'Regular'};n.fontSize=size;n.lineHeight={unit:'PERCENT',value:145};n.characters=str;n.fills=paint(color);n.x=x;n.y=y;n.resize(w,10);n.textAutoResize='HEIGHT';return n;}
function path(p,name,pts,color,weight=3,dash){const n=figma.createVector();p.appendChild(n);n.name=name;n.vectorPaths=[{windingRule:'NONZERO',data:pts}];n.fills=[];n.strokes=paint(color);n.strokeWeight=weight;n.strokeCap='ROUND';if(dash)n.dashPattern=dash;return n;}
function line(p,name,x1,y1,x2,y2,color=C.ink,w=2,dash){return path(p,name,`M ${x1} ${y1} L ${x2} ${y2}`,color,w,dash);}
function circle(p,name,x,y,r,fill,stroke=C.ink,w=2){const n=figma.createEllipse();p.appendChild(n);n.name=name;n.x=x-r;n.y=y-r;n.resize(r*2,r*2);n.fills=fill?paint(fill):[];n.strokes=stroke?paint(stroke):[];n.strokeWeight=w;return n;}
function tag(p,label,x,y,color){rect(p,'Legend swatch',x,y+5,18,18,color);text(p,label,label,x+30,y,510,23,color,true);}
function frame(p,name,x,y,w,h){const n=figma.createFrame();p.appendChild(n);n.name=name;n.x=x;n.y=y;n.resize(w,h);n.fills=paint(C.paper);n.clipsContent=false;return n;}
function header(p,kicker,title,sub){text(p,'Revision / eyebrow',kicker,80,58,2080,22,C.red,true);text(p,'Board title',title,80,114,2080,61,C.ink,true);text(p,'Board subtitle',sub,80,212,2080,27,C.muted);}
function evidence(n,refs,confidence,note){n.setPluginData('evidence',JSON.stringify({refs,confidence,note}));}
function callout(p,num,title,body,x,y,w=510,color=C.teal){circle(p,'Evidence number',x+17,y+20,17,color,null);text(p,'Number '+num,String(num),x+7,y-1,30,22,C.white,true);text(p,title,title,x+48,y-1,w-48,27,C.ink,true);text(p,'Notes / '+title,body,x+48,y+49,w-48,23,C.muted);}
const page=figma.currentPage;
if(page.children.some(n=>n.name==='v8 · D22 现场照片校正 / 平面方案'))throw new Error('v8 section already exists; revise the existing section instead.');
const section=figma.createSection();section.name='v8 · D22 现场照片校正 / 平面方案';section.x=200;section.y=13800;section.resizeWithoutConstraints(9900,2720);section.fills=paint('#DADDD7');
const plan=frame(section,'19 D22 · 一层与夹层平面（照片校正）',80,80,2240,2560);
header(plan,'D22 / FLOOR PLAN · PHOTO STUDY 08 · 2026.09.25','D22，先把空间关系对上','面向舞台为统一方向。照片支持结构关系；落位与尺寸仍是可修改的制作方案。');
tag(plan,'照片可见',80,290,C.teal);tag(plan,'跨视角推断',430,290,C.blue);tag(plan,'设计暂定 / 待证',830,290,C.amber);
text(plan,'Ground plan title','01 / 一层',80,367,700,38,C.ink,true);
text(plan,'Gallery plan title','02 / 夹层',850,367,700,38,C.ink,true);
text(plan,'Working scale','试摆净宽 6.4 m × 进深 13.0 m；非实测，不作为历史建筑尺寸。',80,430,1430,22,C.amber);
const S=66, X=205,Y=530,W=6.4*S,H=13*S,X2=977;
function plRect(p,x,y,w,h,fill,stroke=C.ink,sw=2,dash,xx=X){return rect(plan,p,xx+x*S,Y+y*S,w*S,h*S,fill,stroke,sw,dash);}
function plText(str,x,y,w=5,size=20,color=C.ink,xx=X,bold=false){return text(plan,str,str,xx+x*S,Y+y*S,w*S,size,color,bold);}
function dim(xx){line(plan,'width dimension',xx,Y-41,xx+W,Y-41,C.amber,1.5);line(plan,'width tick',xx,Y-51,xx,Y-31,C.amber);line(plan,'width tick',xx+W,Y-51,xx+W,Y-31,C.amber);text(plan,'width assumed','6.4 m · 试摆',xx+100,Y-78,270,20,C.amber);line(plan,'length dimension',xx-35,Y,xx-35,Y+H,C.amber,1.5);text(plan,'length assumed','13.0 m\n试摆',xx-100,Y+H/2-15,80,20,C.amber);}
dim(X);dim(X2);
const room=plRect('Ground / tiled floor',0,0,6.4,13,C.white,C.ink,4);evidence(room,['R04','R06','R07'],'observed_material_proposed_extent','方砖地面可见，房间尺寸未实测');
for(let x=.4;x<6.4;x+=.4)line(plan,'Floor tile grid',X+x*S,Y,X+x*S,Y+H,C.tile,.55);
for(let y=.4;y<13;y+=.4)line(plan,'Floor tile grid',X,Y+y*S,X+W,Y+y*S,C.tile,.55);
plRect('Ground / low worn timber stage',.1,.1,6.2,2.9,'#615A50',C.teal,3);plText('低舞台 / 磨损木地板',1.5,2.54,4.5,19,C.white,X,true);
for(let x=.45;x<6.1;x+=.4)line(plan,'Stage boards',X+x*S,Y+.1*S,X+x*S,Y+2.58*S,'#867A64',.6);
plRect('Black brick stage backdrop',0,0,6.4,.16,C.ink);plText('D-22 · 黑砖背景墙',1.12,.20,4.8,20,C.white,X,true);
const door=plRect('Rear opening / destination unknown',5.08,.01,.82,.15,C.white,C.teal,2);evidence(door,['R02'],'observed_opening','照片可见洞口；门后功能及位置精度未确定');
line(plan,'Backstage opening pointer',X+5.5*S,Y-10,X+5.5*S,Y-18,C.teal);
text(plan,'Rear opening note','洞口 → 去向未知',X+4.4*S,Y-38,245,18,C.teal);
for(const [x,y,r] of [[3.1,1.18,.38],[2.57,.85,.22],[3.53,.85,.24],[3.81,1.55,.25]])circle(plan,'Drum footprint',X+x*S,Y+y*S,r*S,C.white,C.ink,2);
for(const [x,y] of [[2.17,1.45],[4.1,.95]])circle(plan,'Cymbal footprint',X+x*S,Y+y*S,.27*S,C.tile,C.ink,2);
plText('鼓组',2.8,1.64,1.8,18,C.white);
for(const [x,y] of [[.2,2.26],[5.55,2.26]]){plRect('Stage PA / monitor',x,y,.58,.43,C.ink,C.white,1);}
plRect('Hero amp / GDD',5.05,1.16,.86,.52,'#E5CAB1',C.amber,3);plText('功放',5.03,1.70,1.22,18,C.white,X,true);
for(const [x,y] of [[1.7,2.05],[3.15,2.1],[4.7,2.08]]){circle(plan,'Mic stand',X+x*S,Y+y*S,7,C.white,C.ink);line(plan,'Mic boom',X+x*S,Y+y*S,X+(x+.25)*S,Y+(y-.16)*S,C.ink,2);}
const bar=plRect('Long bar / inferred left from stage reverse view',.87,4.15,.66,6.1,'#B2A186',C.blue,3);evidence(bar,['R01','R03','R06','R07','R08'],'inference_medium','以面向舞台为基准，反向视角支持长吧台位于左侧；长度为暂定');
plRect('Back bar shelves',.08,4.15,.28,6.1,C.wood,C.teal,2);plRect('Bartender working strip',.38,4.15,.46,6.1,'#E2D8C3',C.blue,1);
for(let y=4.55;y<10;y+=.85)circle(plan,'Bar stool',X+1.83*S,Y+y*S,10,C.wood,null);
const lbl=plText('长 吧 台',.96,5.8,1,21,C.ink,X,true);lbl.characters='长\n吧\n台';
plText('观众区 / 方砖',2.65,5.55,3.5,22,C.ink,X,true);
plText('中央开放区域\n靠近舞台，不设座椅排',2.35,6.2,3.8,19,C.muted);
const mezz=plRect('Overhead gallery projection',.03,2.95,2.15,7.9,null,C.blue,2,[10,7]);evidence(mezz,['R01','R03','R06'],'inference_medium','夹层在吧台上方，边线和长度按照片关系试摆');
plText('上方夹层边线',.15,3.13,2.6,17,C.blue);
// Entrance and stair are explicitly assumptions, not asserted facts.
plRect('Unconfirmed access zone',.04,10.85,6.32,2.1,C.paleAmber,C.amber,2,[10,7]);
plRect('Candidate stair',4.9,10.85,1.34,2.0,C.white,C.amber,2,[8,6]);
for(let y=11.03;y<12.8;y+=.19)line(plan,'Candidate stair treads',X+4.99*S,Y+y*S,X+6.17*S,Y+y*S,C.amber,1);
plText('楼梯候选\n位置未证',4.91,11.6,1.45,16,C.amber);
plText('入口 / 后部服务区：待证',.24,11.15,4.4,19,C.amber,X,true);
plRect('Entrance assumed opening',2.6,12.92,1.16,.12,C.white,C.amber,2,[7,4]);
const route=path(plan,'Proposed player route',`M ${X+3.18*S} ${Y+13.5*S} L ${X+3.18*S} ${Y+10.75*S} L ${X+3.9*S} ${Y+7.6*S} L ${X+4.65*S} ${Y+3.6*S}`,C.amber,4,[12,8]);evidence(route,['GDD §2.2'],'design','功放交互动线为游戏设计');
circle(plan,'Recording position proposal',X+4.65*S,Y+3.6*S,14,C.amber,C.white,2);plText('录音位',4.6,3.97,1.7,18,C.amber,X,true);
plRect('PA boundary left',.1,3.03,.48,.5,C.ink);plRect('PA boundary right',5.82,3.03,.48,.5,C.ink);
text(plan,'Direction','↑ 面向舞台 / 本页统一方向',X,Y+H+75,540,24,C.ink,true);
// Upper floor: L-shaped working hypothesis, not a claim of complete survey.
plRect('Gallery outer footprint',0,0,6.4,13,C.white,C.ink,4,null,X2);
plRect('Open void over audience',2.12,.2,4.13,10.48,C.paleBlue,C.blue,2,[8,7],X2);
plRect('Stage below',.1,.1,6.2,2.9,'#D9D5C8',C.line,1,[7,6],X2);plText('下方舞台',2,1.22,4,22,C.muted,X2,true);
const g1=plRect('Side gallery above bar',.03,2.95,2.09,7.9,'#CBC7B8',C.blue,3,null,X2);
const g2=plRect('Rear cross connection / inferred',.03,10.85,6.34,2.1,'#CBC7B8',C.blue,3,[9,7],X2);
evidence(g1,['R01','R03','R06'],'inference_medium','照片可见吧台上方夹层；宽度及边界为推断');evidence(g2,['R01'],'inference_medium','反向照片支持后端连接；具体范围未测量');
plRect('Solid red gallery front',2.0,2.95,.13,7.9,C.red,C.red,1,null,X2);
plRect('Rear red gallery front',2.12,10.72,4.22,.13,C.red,C.red,1,null,X2);
for(let y=3.4;y<10.7;y+=.65){rect(plan,'Band photograph fascia marker',X2+2.0*S-10,Y+y*S,9,25,C.ink);}
plText('侧廊\n吧台上方',.39,6.2,1.6,22,C.ink,X2,true);
plText('挑空 / 下看观众与舞台',2.53,6.0,3.7,21,C.blue,X2,true);
plText('后端连通 · 暂定范围',1.2,11.65,5,20,C.blue,X2,true);
plRect('Candidate stair arrival',4.9,10.85,1.34,2.0,null,C.amber,2,[8,6],X2);
// sightline / reference camera pointers
function marker(xx,x,y,code,dx,dy,color=C.teal){const px=xx+x*S,py=Y+y*S;circle(plan,'Photo camera '+code,px,py,16,color,C.white,2);text(plan,'Photo code '+code,code,px-12,py-13,60,15,C.white,true);line(plan,'Photo viewing direction',px,py,px+dx,py+dy,color,3);}
marker(X,3.4,2.37,'01',0,65);marker(X2,1.65,4.1,'04',54,-74);marker(X,1.94,9.7,'07',0,-68);
text(plan,'Upper uncertainty','另一侧是否也有连续回廊：照片不足，本案暂不补实体。',X2,Y+H+75,650,23,C.amber);
// Right notes / material facts and uncertainty.
callout(plan,'1','舞台不是普通红木背景','黑砖矮背景墙、D-22 字样、右侧洞口、低木台面。R02 / R04 / R05。',1570,415,560);
callout(plan,'2','长吧台在夹层下','连续台面、杯架、酒架、柱与低顶构成一个区域。左侧落位是反向照片推断。',1570,650,560,C.blue);
callout(plan,'3','红色实心栏板','相框与小暖灯沿夹层边连续排列；替换上一版通透金属栏杆。R01 / R03。',1570,900,560);
callout(plan,'4','入口与楼梯待确认','当前照片不支持精确入口、楼梯、卫生间或后勤房间定位。虚线只表示试摆。',1570,1135,560,C.amber);
rect(plan,'Section divider',80,1530,2080,2,C.line);
text(plan,'Section title','03 / 横剖关系 · 吧台上方是夹层，观众区挑空',80,1585,2080,37,C.ink,true);
text(plan,'Section caveat','用于检验空间关系；楼板标高、净高、栏板高度均待实测。',80,1650,2000,23,C.amber);
const sx=180,sy=2230,ss=117;
rect(plan,'Section floor',sx,sy,6.4*ss,12,C.ink);
rect(plan,'Section wall left',sx-10,sy-4.65*ss,10,4.65*ss,C.red);
rect(plan,'Section wall right',sx+6.4*ss,sy-4.65*ss,10,4.65*ss,C.red);
rect(plan,'Section ceiling',sx,sy-4.65*ss,6.4*ss,12,C.ink);
rect(plan,'Gallery slab section',sx,sy-2.5*ss,2.12*ss,15,C.ink);
rect(plan,'Solid balustrade section',sx+2.0*ss,sy-3.5*ss,.13*ss,ss,C.red);
rect(plan,'Gallery column section',sx+2.03*ss,sy-2.5*ss,.06*ss,2.5*ss,C.ink);
rect(plan,'Counter section',sx+.85*ss,sy-1.05*ss,.75*ss,1.05*ss,C.wood);
rect(plan,'Backbar section',sx+.04*ss,sy-2.05*ss,.36*ss,2.05*ss,'#98856F');
for(let z=.5;z<2.1;z+=.45)line(plan,'Backbar shelf',sx+.04*ss,sy-z*ss,sx+.4*ss,sy-z*ss,C.white,3);
text(plan,'Section labels','杯架 / 酒架 / 工作位',sx,sy+32,420,22,C.ink);
text(plan,'Section void label','观众区挑空',sx+3.0*ss,sy-3.1*ss,470,30,C.blue,true);
text(plan,'Section gallery label','夹层',sx+.35*ss,sy-3.9*ss,280,27,C.ink,true);
callout(plan,'5','这轮先修平面，再做高精度','现有 Blender 模型作为布局代理。黑砖墙、方砖地面、连续吧台、杯架和栏板按新证据拆成独立资产再制作。',1110,1740,1020,C.red);
callout(plan,'6','名称回到 GDD','项目用 D22；舞台墙按参考写 D-22。ROOM 22 是上一轮错误命名，后续源文件已列入纠正。',1110,1990,1020,C.red);
text(plan,'Plan footer','照片编号可在相邻「20 现场证据」看板对照。主案是照片约束下的工作平面，不是已完成的历史建筑测绘。',80,2430,2080,23,C.muted);
// Evidence board with individual editable image nodes, images supplied separately by the image-fill tool.
const refs=frame(section,'20 D22 · 现场证据与视角对应',2500,80,2240,2560);
header(refs,'D22 / REFERENCE EVIDENCE · USER PHOTOGRAPHS','从照片里确认什么','9 个文件归为 7 个视角组；保留原图与水印。人物仅作比例和空间遮挡参考。');
const cards=[
 ['R01','舞台 → 观众区','上层观众、红色栏板和转角；吧台与夹层的关系。反向观察用于推断平面左 / 右。','IMG_5274 2.JPG'],
 ['R02','舞台侧面','黑砖背景、D-22、洞口、功放支架与红色竖板。几何关系优先于强暖色偏。','IMG_5275.JPG'],
 ['R08','吧台横向视角','长台面、杯架、酒架、柱网和夹层相框。R03 / R08 / R09 为同一视角组。','IMG_5282 2.JPG'],
 ['R04','夹层 → 舞台','低舞台、观众区方砖、舞台两侧 PA 与较近观看距离。','IMG_5277.JPG'],
 ['R06','吧台低顶与梁柱','吧台上方楼板底、密集梁格、黑色柱、小暖灯及服务通道。','IMG_5279.JPG'],
 ['R07','顺着长吧台看','连续台面和服务面，与右侧观众 / 通行空间并行。','IMG_5280.JPG'],
 ['R05','观众区 → 舞台','木台磨损、近距离设备与集中顶光；辅助 R02 / R04，不据此估精确米数。','IMG_5278.JPG']
];
const photoNodes=[];
for(let i=0;i<cards.length;i++){
 const [id,title,body,file]=cards[i],x=80+(i%3)*705,y=354+Math.floor(i/3)*620;
 const card=frame(refs,id+' / '+title,x,y,665,588);card.fills=paint(C.white);
 const im=rect(card,id+' / Source photograph',16,16,633,374,'#D4D1C8');photoNodes.push({id:im.id,ref:id,file});evidence(im,[id],'source_image',file);
 text(card,'Photo title',id+'  '+title,20,413,623,26,C.ink,true);text(card,'Photo interpretation',body,20,464,623,21,C.muted);
}
text(refs,'Deduplication note','重复归并：IMG_5282 2.JPG = IMG_5282.JPG（文件哈希相同）；IMG_5276.JPG 为该画面的另一版本。\n拍摄时间与来源帖未提供；不把不同照片中的临时布置强行视为同一天的固定状态。',790,1680,1320,26,C.muted);
rect(refs,'Evidence caution box',790,1900,1320,230,C.paleAmber);text(refs,'Evidence caution','暂时无法确认\n入口和楼梯准确位置、后勤房间用途、完整二层轮廓、净高与全部尺寸。\n这些位置在平面用虚线，不补出未经证实的卫生间或后台。',820,1928,1260,27,C.amber,true);
text(refs,'Evidence footer','保留位置：references/d22-user-photos/ · 每张原图、预览图、SHA-256 和文件名已归档。',80,2425,2080,24,C.muted);
const prod=frame(section,'21 D22 · 资产重做与 Prompt 档案',4920,80,2240,2200);
header(prod,'D22 / ASSET REBUILD · ARCHIVE FIRST','当前模型是布局代理','保存原始生成链；高精度重做按单件和模块拆分。此轮只更新平面与档案，不提交新一轮生成。');
const rows=[
 ['01','舞台建筑','黑砖墙 + D-22 字样独立贴花；洞口、低木台分开。依据 R02 / R04。'],
 ['02','吧台系统','长台面、后吧酒架、悬挂杯架、吧凳分别制作；不再把吧台和凳子生为一块。'],
 ['03','夹层结构','红木实心栏板、相框、小灯、楼板底和柱分模块；先锁平面关系。'],
 ['04','鼓组与细杆','鼓壳、鼓皮、金属架、镲片分组；检查圆度、独立支撑与无粘连。'],
 ['05','交互设备','功放、录音机、麦架、吉他保留独立文件；旋钮、琴弦与细线不能仅靠模糊贴图。'],
 ['06','高精度复做依据','每件保留原始 prompt / 输入图 / task ID / 模型与参数 / 尺寸 / 已知问题 / 修订 prompt。']
];
for(let i=0;i<rows.length;i++){
 const [n,title,body]=rows[i],y=370+i*195;rect(prod,'Asset row '+n,80,y,2080,170,i%2?C.paper:C.white);text(prod,'Asset index',n,110,y+26,100,36,C.red,true);text(prod,title,title,240,y+20,1730,31,C.ink,true);text(prod,'Rebuild detail',body,240,y+77,1730,27,C.muted);
}
rect(prod,'Archive manifest panel',80,1590,2080,360,C.ink);
text(prod,'Archive location','可追溯文件',120,1623,2000,34,C.paper,true);
text(prod,'Archive paths','assets/d22/prompts/prompt-archive.json\nassets/prompts/original/  ·  原始文本，不覆盖\nassets/prompts/rebuild/  ·  对应新平面的高精度重做提示\nassets/generation-manifest.json  ·  现有 10 件 Meshy 资产来源',120,1690,1970,27,C.paper);
text(prod,'Quality note','精度升级同时检查几何、贴图和拆件。仅增加纹理像素不会修复镲片变形、杆件粘连或缺少的结构。',80,2010,2070,28,C.red,true);
const old=await figma.getNodeByIdAsync('3:90');const archive=old.clone();section.appendChild(archive);archive.x=7340;archive.y=80;archive.name='历史 / 首版平面（已被 v8 替代）';
for(const n of archive.findAll(n=>n.type==='TEXT'&&n.characters==='先搭一个听得见的房间')){await figma.loadFontAsync(n.fontName);n.characters='历史首版 / 不再作为布局依据';}
section.setPluginData('revision','D22 photo-grounded floor plan v8');plan.setPluginData('role','current floor plan');
figma.currentPage.selection=[plan];figma.viewport.scrollAndZoomIntoView([plan]);
return {section:section.id,plan:plan.id,evidence:refs.id,production:prod.id,archive:archive.id,photoNodes};
