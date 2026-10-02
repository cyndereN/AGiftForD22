using System;
using UnityEngine;

namespace D22
{
    // A resolution-independent prototype UI. All story text and art come from project assets.
    public sealed class D22GameUI : MonoBehaviour
    {
        enum ScreenMode { Menu, Puzzle, Dialogue, Choices, Drink, Pause, Ending, HUD, Loading }
        ScreenMode mode=ScreenMode.Menu;
        D22GameFlow flow;
        Font font;
        Texture2D poster,cover;
        GUIStyle title,body,small,button;
        Texture2D buttonNormal,buttonHover;
        D22Line[] lines;
        D22Question question;
        Action done,back,pourAction;
        Action<int> choose;
        string heading,hint,note;
        int line,placed,sips,drag=-1;
        float pour,cooldown;
        bool hasDrink,pouring;
        readonly Rect[] pieces=new Rect[6];
        readonly bool[] locked=new bool[6];
        Vector2 dragOffset;
        const float Width=1440,Height=900;
        public string CurrentScreen=>mode.ToString();
        public int PlacedPieces=>placed;
        public void Initialize(D22GameFlow f,Font uiFont,Texture2D p,Texture2D c)
        {flow=f;font=uiFont;poster=p;cover=c;}
        void OnDestroy(){if(buttonNormal)Destroy(buttonNormal);if(buttonHover)Destroy(buttonHover);}
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){font=font,fontSize=48,alignment=TextAnchor.MiddleLeft,wordWrap=true};
            body=new GUIStyle(title){fontSize=30};small=new GUIStyle(title){fontSize=19};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=22,wordWrap=true,alignment=TextAnchor.MiddleCenter};
            buttonNormal=new Texture2D(1,1);buttonNormal.SetPixel(0,0,new Color(.28f,.1f,.06f));buttonNormal.Apply();
            buttonHover=new Texture2D(1,1);buttonHover.SetPixel(0,0,new Color(.43f,.16f,.09f));buttonHover.Apply();
            button.normal.background=buttonNormal;button.hover.background=buttonHover;button.active.background=buttonHover;
            button.normal.textColor=button.hover.textColor=button.active.textColor=Color.white;
            button.border=new RectOffset();
            title.normal.textColor=body.normal.textColor=small.normal.textColor=new Color(.95f,.91f,.83f);
        }
        void OnGUI()
        {
            Styles();float scale=Mathf.Min(Screen.width/Width,Screen.height/Height);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-Width*scale)/2,(Screen.height-Height*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;
            if(mode!=ScreenMode.HUD)Fill(new Rect(-Width,-Height,Width*3,Height*3),new Color(.035f,.03f,.025f,.97f));
            switch(mode)
            {
                case ScreenMode.Menu:Menu();break;
                case ScreenMode.Puzzle:Puzzle();break;
                case ScreenMode.Dialogue:Dialogue();break;
                case ScreenMode.Choices:Choices();break;
                case ScreenMode.Drink:Drink();break;
                case ScreenMode.Pause:Pause();break;
                case ScreenMode.Ending:Ending();break;
                case ScreenMode.HUD:HUD();break;
                default:GUI.Label(new Rect(100,360,1200,120),"灯快亮了。 / Loading…",title);break;
            }
            GUI.matrix=Matrix4x4.identity;
        }
        static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        bool Button(Rect rect,string text)=>GUI.Button(rect,text,button);
        void Menu()
        {
            if(cover){GUI.color=new Color(.6f,.5f,.4f,1);GUI.DrawTexture(new Rect(760,70,600,760),cover,ScaleMode.ScaleAndCrop);GUI.color=Color.white;}
            GUI.Label(new Rect(90,115,650,210),"A Gift\nfor D-22",new GUIStyle(title){fontSize=72});
            Fill(new Rect(95,325,70,4),new Color(.75f,.18f,.1f));
            GUI.Label(new Rect(95,365,560,160),"一间房间，\n一段还没有散掉的声音。",body);
            if(Button(new Rect(95,590,420,68),"开始游戏 / START"))flow.Begin();
            if(Button(new Rect(95,678,420,55),"空间漫游 / EXPLORE"))ShowGallery();
            GUI.Label(new Rect(95,795,600,45),"WASD 移动 · 鼠标右键转向 · Esc 菜单",small);
        }
        void ShowGallery()
        {
            question=new D22Question{zh="空间漫游 / EXPLORE",en="",choices=new[]{new D22Choice{zh="唱片店",en="Record Shop"},new D22Choice{zh="胡同",en="Hutong"},new D22Choice{zh="D-22 · 重建空间",en="Livehouse"},new D22Choice{zh="原始现场扫描",en="Live Scan"},new D22Choice{zh="演出",en="Performance"}}};
            choose=i=>flow.LoadSpace(new[]{"D22_RecordShop","D22_Hutong","D22_Bootstrap","D22_LiveScan","D22_Performance"}[i]);mode=ScreenMode.Choices;
        }
        void Dialogue()
        {
            GUI.Label(new Rect(100,75,1240,85),heading,title);
            GUI.Label(new Rect(100,250,560,360),lines[line].zh,body);
            GUI.Label(new Rect(780,250,560,360),lines[line].en,new GUIStyle(body){fontSize=24});
            Fill(new Rect(720,260,1,280),new Color(.6f,.5f,.4f,.3f));
            GUI.Label(new Rect(100,755,300,45),$"{line+1:D2} / {lines.Length:D2}",small);
            if(Button(new Rect(1030,745,310,65),"继续 / CONTINUE"))AdvanceDialogue();
            if(Button(new Rect(690,745,290,65),"跳过 / SKIP"))done?.Invoke();
        }
        public void AdvanceDialogue(){if(mode!=ScreenMode.Dialogue)return;line++;if(line>=lines.Length){line=lines.Length-1;done?.Invoke();}}
        Rect Target(int i)=>new Rect(510+(i%2)*210,155+(i/2)*195,210,195);
        void Puzzle()
        {
            GUI.Label(new Rect(80,40,1260,70),"把撕开的海报拼回去。",body);
            for(int i=0;i<6;i++)Fill(Target(i),new Color(.15f,.13f,.11f));
            // Reverse hit order matches drawing: the last unlocked piece is on top.
            var ev=Event.current;
            if(ev.type==EventType.MouseDown&&ev.button==0)
                for(int i=5;i>=0;i--)if(!locked[i]&&pieces[i].Contains(ev.mousePosition)){drag=i;dragOffset=ev.mousePosition-pieces[i].position;ev.Use();break;}
            if(drag>=0&&ev.type==EventType.MouseDrag){pieces[drag].position=ev.mousePosition-dragOffset;ev.Use();}
            if(drag>=0&&ev.type==EventType.MouseUp)
            {int i=drag;drag=-1;if(Vector2.Distance(pieces[i].center,Target(i).center)<75)PlacePiece(i);ev.Use();}
            for(int i=0;i<6;i++)
            {
                GUI.DrawTextureWithTexCoords(pieces[i],poster,new Rect((i%2)*.5f,1-(i/2+1)/3f,.5f,1f/3));
                if(!locked[i])GUI.Label(new Rect(pieces[i].x+8,pieces[i].y+8,40,28),(i+1).ToString(),small);
            }
            GUI.Label(new Rect(80,790,900,65),$"{placed} / 6     拖动碎片到中间的海报 / Drag the pieces into place",small);
            if(placed==6&&Button(new Rect(1060,780,300,65),"唱片店 / RECORD SHOP"))done?.Invoke();
        }
        // Shared completion path for drag-and-drop and integration verification.
        public void PlacePiece(int i){if(mode!=ScreenMode.Puzzle||i<0||i>=6||locked[i])return;pieces[i]=Target(i);locked[i]=true;placed++;}
        void Choices()
        {
            GUI.Label(new Rect(100,65,1220,100),question.zh,body);
            GUI.Label(new Rect(100,160,1220,90),question.en,small);
            for(int i=0;i<question.choices.Length;i++)
                if(Button(new Rect(180,280+i*92,1080,72),question.choices[i].zh+" / "+question.choices[i].en))Choose(i);
        }
        public void Choose(int i){if(mode==ScreenMode.Choices&&i>=0&&i<question.choices.Length)choose?.Invoke(i);}
        void Drink()
        {
            GUI.Label(new Rect(100,75,1220,90),"酒 / THE DRINK",title);
            GUI.Label(new Rect(100,230,1100,140),"情感放大器。莫贪杯哦。\nEmotion amplifier. Try not to overdose.",body);
            Fill(new Rect(100,440,1240,20),new Color(.18f,.14f,.1f));Fill(new Rect(100,440,1240*pour,20),new Color(.75f,.3f,.1f));
            GUI.enabled=!pouring;
            if(Button(new Rect(100,560,560,75),"倾倒 / POUR"))pourAction?.Invoke();
            if(Button(new Rect(780,560,560,75),"放下 / PUT IT DOWN"))back?.Invoke();GUI.enabled=true;
        }
        void Pause()
        {
            GUI.Label(new Rect(120,90,1200,100),"让耳朵歇一会儿。",title);
            flow.ReducedMotion=GUI.Toggle(new Rect(120,300,800,50),flow.ReducedMotion," 减少镜头晃动 / REDUCED MOTION",button);
            GUI.Label(new Rect(120,400,900,50),"音乐音量 / MUSIC",small);flow.Volume=GUI.HorizontalSlider(new Rect(120,475,800,35),flow.Volume,0,1);
            if(Button(new Rect(120,610,550,75),"继续 / RESUME"))done?.Invoke();
            if(Button(new Rect(780,610,550,75),"主菜单 / MENU"))flow.LoadSpace("D22_Menu");
        }
        void Ending(){GUI.Label(new Rect(130,200,1180,230),note,title);GUI.Label(new Rect(130,480,1180,100),"A Gift for D-22",body);if(Button(new Rect(130,660,420,70),"回到主菜单 / MENU"))done?.Invoke();}
        void HUD()
        {
            Fill(new Rect(35,770,1370,95),new Color(0,0,0,.65f));GUI.Label(new Rect(65,785,1300,60),hint,small);
            if(Button(new Rect(1210,30,195,50),"菜单 / ESC"))flow.Pause();
            if(hasDrink&&Button(new Rect(35,35,330,55),cooldown>0?$"酒 / DRINK  {cooldown:F0}s":"1 · 喝酒 / DRINK"))flow.RequestDrink();
            if(hint!=null && hint.StartsWith("E ·") && Button(new Rect(480,690,480,55),hint))flow.TryInteract();
            if(sips>0)GUI.Label(new Rect(400,35,500,55),$"今晚第 {sips} 口",small);
            Fill(new Rect(718,448,4,4),new Color(1,1,1,.6f));
        }
        public void ShowMenu()=>mode=ScreenMode.Menu;
        public void ShowLoading()=>mode=ScreenMode.Loading;
        public void ShowHUD()=>mode=ScreenMode.HUD;
        public void UpdateHUD(string h,bool has,int s,float cd){hint=h;hasDrink=has;sips=s;cooldown=cd;}
        public void ShowDialogue(string titleText,D22Line[] l,Action a){heading=titleText;lines=l;line=0;done=a;mode=ScreenMode.Dialogue;if(l.Length==0)a();}
        public void ShowPuzzle(Action a)
        {
            done=a;placed=0;drag=-1;mode=ScreenMode.Puzzle;
            for(int i=0;i<6;i++){locked[i]=false;pieces[i]=new Rect(i%2==0?110:1110,165+(i/2)*200,210,195);}
        }
        public void ShowChoices(D22Question q,Action<int> a){question=q;choose=a;mode=ScreenMode.Choices;}
        public void ShowDrink(Action a,Action b){pourAction=a;back=b;pour=0;pouring=false;mode=ScreenMode.Drink;}
        public void DisableDrink()=>pouring=true;
        public void SetPour(float p)=>pour=p;
        public void ShowPause(Action a){done=a;mode=ScreenMode.Pause;}
        public void ShowEnding(string n,Action a){note=n;done=a;mode=ScreenMode.Ending;}
    }
}
