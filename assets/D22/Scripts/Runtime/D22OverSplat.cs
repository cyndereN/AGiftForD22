using UnityEngine;

namespace D22
{
    public sealed class D22OverSplat : MonoBehaviour
    {
        public Shader shader;

        void Awake()
        {
            var sh = shader != null ? shader : Shader.Find("D22/Over Splat");
            if (!sh) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var src = renderer.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    var mat = new Material(sh);
                    var tex = TextureOf(src[i]);
                    if (tex) mat.mainTexture = tex;
                    dst[i] = mat;
                }
                renderer.materials = dst;
            }
        }

        static Texture TextureOf(Material src)
        {
            if (!src) return null;
            if (src.HasProperty("_BaseMap"))
            {
                var tex = src.GetTexture("_BaseMap");
                if (tex) return tex;
            }
            if (src.HasProperty("_MainTex"))
            {
                var tex = src.GetTexture("_MainTex");
                if (tex) return tex;
            }
            return src.mainTexture;
        }
    }
}
