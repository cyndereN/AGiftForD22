using UnityEngine;

namespace D22
{
    public sealed class D22HutongLighting : MonoBehaviour
    {
        public Light sun;
        public Light[] practicals;

        public void Apply(bool dusk)
        {
            if(sun)
            {
                sun.intensity=dusk?.28f:1.1f;
                sun.color=dusk?new Color(1f,.52f,.32f):new Color(1f,.86f,.67f);
            }
            foreach(var light in practicals)
                if(light)light.intensity=dusk?2.8f:1.2f;
            RenderSettings.ambientSkyColor=dusk?new Color(.12f,.18f,.28f):new Color(.46f,.52f,.60f);
            RenderSettings.ambientEquatorColor=dusk?new Color(.13f,.13f,.17f):new Color(.32f,.34f,.36f);
            RenderSettings.ambientGroundColor=dusk?new Color(.06f,.06f,.08f):new Color(.17f,.17f,.16f);
            DynamicGI.UpdateEnvironment();
        }
    }
}
