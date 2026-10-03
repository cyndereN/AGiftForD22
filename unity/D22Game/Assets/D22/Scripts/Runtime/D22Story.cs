using System;
namespace D22
{
    [Serializable] public class D22Line { public string zh; public string en; }
    [Serializable] public class D22Choice : D22Line { public string note; }
    [Serializable] public class D22Question : D22Line { public D22Choice[] choices; }
    [Serializable] public class D22AbilityCopy : D22Line { public string id; public string title; public string caption; }
    [Serializable] public class D22Story
    {
        public D22Line[] intro, recordShop, hutong, door, boss, wine, cricketTalk, pigeonTalk, grindTalk, livehouse;
        public D22Question choice, hutongAsk;
        public D22AbilityCopy[] abilities;
    }
}
