using UnityEngine;


[CreateAssetMenu(fileName ="BakedAnimationClip.so", menuName ="BakingEditor/BakeScriptableObject", order =-1)]
public class BakeScript : ScriptableObject
{
    public Mesh[] meshes;
    public Material material;
}