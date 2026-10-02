// Extract only literal story declarations; never evaluate the archived page runtime.
const fs=require('fs'),vm=require('vm'),path=require('path');
const root=path.resolve(__dirname,'../..');
const player=fs.readFileSync(path.join(root,'legacy/web/player.js'),'utf8');
const menu=fs.readFileSync(path.join(root,'legacy/web/index.html'),'utf8');
const literal=(source,name,end)=>vm.runInNewContext(source.slice(source.indexOf('const '+name+' ='),source.indexOf(end,source.indexOf('const '+name+' =')))+';'+name,{}, {timeout:1000});
const result={intro:literal(menu,'BEATS','const COLS'),recordShop:literal(player,'BRIEFS','const BOSS').scene_recordshop,hutong:literal(player,'BRIEFS','const BOSS').scene_hutong,boss:literal(player,'BOSS','const WINE'),wine:literal(player,'WINE','const ABILITY_INTRO'),choice:literal(player,'WINE_ASK','const PREVIEW')};
const dest=path.join(root,'unity/D22Game/Assets/D22/Story/story.json');fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,JSON.stringify(result,null,2)+'\n');
