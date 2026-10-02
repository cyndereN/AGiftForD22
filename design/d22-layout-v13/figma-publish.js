const fileKey = 'LOMa9NEcogELqstS7Fsol0';
const sectionName = 'v14 · D-22 黑砖、同弧度夹层与二层出口';
const color = hex => ({r:parseInt(hex.slice(0,2),16)/255,g:parseInt(hex.slice(2,4),16)/255,b:parseInt(hex.slice(4,6),16)/255});
const family = 'Noto Sans SC';
await figma.loadFontAsync({family,style:'Regular'});
await figma.loadFontAsync({family,style:'Bold'});
const main = await figma.getNodeByIdAsync('3:90');
const detail = await figma.getNodeByIdAsync('32:1070');
if (!main || !detail || main.type !== 'FRAME' || detail.type !== 'FRAME') throw Error('Expected existing D-22 plan frames are missing');
const page = main.parent;
let section = page.children.find(n => n.type === 'SECTION' && n.name === sectionName);
if (!section) {
  section = figma.createSection();
  page.appendChild(section);
  section.name = sectionName;
  section.x = 200;
  section.y = Math.max(...page.children.filter(c => c.id !== section.id).map(c => c.y+c.height)) + 320;
  section.resizeWithoutConstraints(7240, 4600);
}
for (const n of [...section.children]) if (n.name.startsWith('v14 /')) n.remove();
function heading(parent,name,content,x,y,w,size,bold=false,fill='252b28') {
  const t=figma.createText();parent.appendChild(t);t.name=name;t.fontName={family,style:bold?'Bold':'Regular'};
  t.characters=content;t.fontSize=size;t.fills=[{type:'SOLID',color:color(fill)}];t.resize(w,20);t.textAutoResize='HEIGHT';t.x=x;t.y=y;return t;
}
function frame(name,x,y,w,h){const f=figma.createFrame();section.appendChild(f);f.name=name;f.x=x;f.y=y;f.resize(w,h);f.fills=[{type:'SOLID',color:color('ece8dd')}];return f;}
const imageJobs=[];
function picture(parent,name,file,x,y,w,h){const r=figma.createRectangle();parent.appendChild(r);r.name=name;r.x=x;r.y=y;r.resize(w,h);r.fills=[{type:'SOLID',color:color('242624')}];imageJobs.push({file,nodeIds:[r.id]});return r;}

// Preserve both prior versions in the review section before replacing their stable plan IDs.
for(const [node,label,x] of [[main,'main',80],[detail,'detail',2450]]){
  const archived=node.clone();section.appendChild(archived);archived.name='v14 / ARCHIVE previous '+label+' plan';archived.x=x;archived.y=3000;archived.visible=false;
}
for(const [node,name] of [[main,'03 D22 首关平面 / 当前 v14 参考修正'],[detail,'19 D22 · 一层与二层 / v14 黑砖与二层出口']]){
  const imported=figma.createNodeFromSvg(SVG);
  for(const child of [...node.children]) child.remove();
  node.resize(2240,2560);node.appendChild(imported);imported.x=0;imported.y=0;imported.name='v14 / Editable shared plan';node.name=name;
}
const plan=frame('v14 / 33 同步平面与参考校正',80,80,2320,2860);
const planSvg=figma.createNodeFromSvg(SVG);plan.appendChild(planSvg);planSvg.x=40;planSvg.y=40;
heading(plan,'Plan note','已同步原有 03 / 19 平面画板。尺寸为参考照片推定，出口楼层以用户确认的位置为准。',80,2680,2150,28);
const renders=frame('v14 / 34 Blender 黑砖与标志位置',2500,80,4660,2860);
heading(renders,'Eyebrow','D-22 / REFERENCE CORRECTION / BLENDER SCENE',80,70,4500,27,true,'a24031');
heading(renders,'Title','D-22 背景：错缝黑砖与偏左字样',80,145,4500,64,true);
heading(renders,'Summary','文字在舞台中央附近的砖墙凹槽上，略偏鼓手左侧；右侧原有低位洞口与二层出口分开。',80,255,4500,29);
picture(renders,'Stage front / actual Cycles render','design/media/v13/stage-centered.jpg',80,370,2160,1485);
picture(renders,'Stage reference view / actual Cycles render','design/media/v13/reference-elevated.jpg',2380,370,2160,2160);
heading(renders,'Front caption','正面 / 独立砖块错缝砌筑；舞台进深增加，D-22 字样回到偏左的凹槽砖面。',80,1910,2160,28,true);
heading(renders,'Reference caption','抬高视点 / 上方连续站人平台及前后两道右侧承重墙垛。',2380,2580,2160,28,true);
const structure=frame('v14 / 35 同弧度平台、右侧出口与裸钢',80,3060,7080,1440);
heading(structure,'Structure title','舞台端与入口端共用同一弧度',80,70,6800,58,true);
heading(structure,'Structure summary','舞台正上方可站人；右端接 +2.66 m 二层出口；钢柱、工字梁和斜撑保留为独立可编辑结构。',80,150,6800,28);
for(const [i,label,file] of [[0,'二层出口 / 右端','design/media/v13/upper-exit.jpg'],[1,'双墙垛 / 舞台右侧','design/media/v13/two-wall-piers.jpg'],[2,'裸钢 / 剖面结构','design/media/v13/steel-structure.jpg']]){
 const x=80+i*2320;heading(structure,'Image label '+i,label,x,250,2160,32,true);picture(structure,'Structure image '+i,file,x,320,2160,1000);
}
figma.currentPage.selection=[plan];figma.viewport.scrollAndZoomIntoView([plan]);
return {section:section.id,boards:[plan.id,renders.id,structure.id],plans:[main.id,detail.id],imageJobs,url:'https://www.figma.com/design/'+fileKey+'?node-id='+section.id.replace(':','-')};
