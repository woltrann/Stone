using UnityEngine;

namespace StoneGame.Computers
{
    [ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(MeshRenderer))]
    public sealed class ComputerScreenEmission : MonoBehaviour
    {
        [SerializeField] InteractiveComputerSettings settings;
        MeshRenderer screen;
        MaterialPropertyBlock block, original;
        Color lastColor;
        float lastIntensity=-1;
        void OnEnable()
        {
            screen=GetComponent<MeshRenderer>();
            block=new MaterialPropertyBlock();original=new MaterialPropertyBlock();screen.GetPropertyBlock(original);
            Refresh();
        }
        void OnValidate() { lastIntensity=-1; if(isActiveAndEnabled) Refresh(); }
        void Update() { if(settings!=null && (lastColor!=settings.emissionColor || lastIntensity!=settings.emissionIntensity)) Refresh(); }
        public void Refresh()
        {
            if(settings==null) return;
            if(screen==null) screen=GetComponent<MeshRenderer>();
            block??=new MaterialPropertyBlock();screen.GetPropertyBlock(block);
            block.SetColor("_EmissionColor",settings.emissionColor*settings.emissionIntensity);
            screen.SetPropertyBlock(block);lastColor=settings.emissionColor;lastIntensity=settings.emissionIntensity;
        }
        void OnDisable() { if(screen!=null && original!=null) screen.SetPropertyBlock(original); }
    }
}
