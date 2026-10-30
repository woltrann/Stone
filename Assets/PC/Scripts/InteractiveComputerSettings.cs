using UnityEngine;
using UnityEngine.InputSystem;

namespace StoneGame.Computers
{
    [CreateAssetMenu(fileName="PCSettings",menuName="Stone Game/Interactive PC Settings")]
    public sealed class InteractiveComputerSettings : ScriptableObject
    {
        [Header("Interaction")]
        [Min(.1f)] public float interactionDistance=2.5f;
        public Key interactionKey=Key.E;
        public Key exitKey=Key.Escape;
        [TextArea] public string prompt="İnternete erişmek için [{key}] tuşuna bas";
        public bool requireFrontSide=true;
        [Header("Screen outline")]
        public Shader outlineShader;
        public Color outlineColor=new Color(.32f,.94f,.82f,1f);
        [Range(1f,8f)] public float outlineWidth=3f;
        [Header("Camera")]
        [Min(.05f)] public float enterDuration=.75f;
        [Min(.05f)] public float exitDuration=.65f;
        public bool automaticallyFrameScreen=true;
        [Range(1.01f,1.4f)] public float screenPadding=1.12f;
        [Min(.02f)] public float nearClipClearance=.08f;
        [Header("Screen emission")]
        [ColorUsage(false,true)] public Color emissionColor=new Color(.12f,.6f,.5f);
        [Range(0f,3f)] public float emissionIntensity=.65f;
        public string PromptText=>(prompt??string.Empty).Replace("{key}",interactionKey.ToString());
    }
}
