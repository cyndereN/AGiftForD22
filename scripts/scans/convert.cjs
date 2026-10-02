// Run from any working directory after npm ci --prefix scripts/scans.
const {spawnSync}=require('node:child_process');
const {mkdirSync}=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'../..');
mkdirSync(path.join(root,'work/scans'),{recursive:true});
for(const name of ['recordshop','hutong','live','performance']) {
  const result=spawnSync(process.execPath,[path.join(__dirname,'node_modules/@playcanvas/splat-transform/bin/cli.mjs'),'-w',path.join(root,`assets/scans/scene_${name}.sog`),path.join(root,`work/scans/scene_${name}.ply`)],{stdio:'inherit'});
  if(result.status!==0)process.exit(result.status||1);
}
