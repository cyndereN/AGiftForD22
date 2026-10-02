using System;
namespace D22
{
    [Serializable] public class D22Line { public string zh; public string en; }
    [Serializable] public class D22Choice : D22Line { public string note; }
    [Serializable] public class D22Question : D22Line { public D22Choice[] choices; }
    [Serializable] public class D22Story
    {
        public D22Line[] intro, recordShop, hutong, boss, wine;
        public D22Question choice;
    }
}
