using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.EditorCoroutines.Editor;

public class BakingEditor : EditorWindow
{
    [MenuItem("Noble/BakeAnimation")]
    public static void ShowExample()
    {
        BakingEditor wnd = GetWindow<BakingEditor>();
        wnd.titleContent = new GUIContent("BakeAnimation");
    }

    Unity.EditorCoroutines.Editor.EditorCoroutine ExportingProcess;

    Text TitleLabel;
    Material material;
    GameObject Model;
    AnimationClip clip;
    float AnimationTime = 0.0f;
    Editor PreviewViewer;
    bool IsPlaying;

    string SaveName;
    int NumberOfFrame;
    float SamplingLatency;
    string SaveDirectory;


    bool isGenerateTirggerOn;
    bool isGenerated;
    int GenProgress;
    int AmountSoFar;
    float generateDealy;

    Matrix4x4 bindMat;
    bool isCollectMesh;


    private void OnEnable()
    {
        NumberOfFrame = 60;
        
        SaveDirectory = "Assets/BakingEditor/Result/";
        if (UnityEditor.AssetDatabase.AssetPathExists("Assets/BakingEditor/Result/") == false)
        {
            Directory.CreateDirectory("Assets/BakingEditor/Result/");
        }
        GenProgress = 0;

        bindMat.m01 = 0;
        bindMat.m11 = 1;
        bindMat.m21 = 0;
        bindMat.m02 = 0;
        bindMat.m12 = 0;
        bindMat.m22 = -1;
    }


    float curFlow;
    void Update()
    {
        if (IsPlaying == true && clip != null && isGenerateTirggerOn == false)
        {
            

            AnimationTime += Time.fixedDeltaTime;
            if (AnimationTime >= clip.length)
            {
                AnimationTime = 0.0f;
            }
        }

        if (isGenerateTirggerOn)
        {
            //generateDealy += Time.deltaTime;
            //ExportMeshes();
            //curFlow += Time.fixedDeltaTime;
            //if (curFlow > 60f)
            //{
            //    isGenerateTirggerOn = false;
            //}
        }

    }
    private void OnGUI()
    {
        if (isGenerateTirggerOn)
        {
            GUI.enabled = false;
        }

        GUILayout.BeginVertical();
        // TitleLabel.text = "Baking Animation System For GPU Instancing Animator";
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUIStyle expStyle = new GUIStyle();
        expStyle.fontSize = 10;
        expStyle.normal.textColor = Color.white;

        GUIStyle SubStyle = new GUIStyle();
        SubStyle.fontSize = 16;
        SubStyle.normal.textColor = Color.white;

        GUILayout.Label("Baking Animation System For GPU Instancing Animator", style);
        GUILayout.Label("Skinned MeshRenderer의 GPU Instancing Animating 동작을 위한 시스템 \nMeshRenderer는 제대로 동작 안 할 수 있음 \n게임 오브젝트와 애니메이션 클립을 넣어야 합니다. Model/Animation Clip must be uploaded.", expStyle);

        EditorGUI.BeginChangeCheck();
        Model = ((GameObject)EditorGUILayout.ObjectField("Model", Model, typeof(GameObject), true));
        clip = ((AnimationClip)EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), true));

        material = ((Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), true));
        EditorGUILayout.HelpBox("Material :: dynamical Shader can be overworked to GPU Workload. not Recommended", MessageType.Warning);


        bool InputChanged = EditorGUI.EndChangeCheck();
        if (InputChanged)
        {
            if (Model != null)
            {
                SaveName = Model.name;
            }
            PreviewViewer = null;
        }

        if (clip == null)
        {
            if (PreviewViewer != null)
            {
                DestroyImmediate(PreviewViewer);
            }
            GUILayout.EndVertical();
            return;
        }

        if (Model == null)
        {
            if (PreviewViewer != null)
            {
                DestroyImmediate(PreviewViewer);
            }
            GUILayout.EndVertical();
            return;
        }

        if (material != null)
        {
            if (Model.GetComponentInChildren<MeshRenderer>() != null)
            {
                Model.GetComponentInChildren<MeshRenderer>().material = material;
            }
            else if (Model.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                Model.GetComponentInChildren<SkinnedMeshRenderer>().material = material;
            }
            else
            {
                if (Model.GetComponentInChildren<SpriteRenderer>() == null)
                {
                    GUILayout.EndVertical();
                    return;
                }
                Model.GetComponentInChildren<SpriteRenderer>().material = material;
            }

        }

        GUILayout.Space(10);

        GUILayout.Label("Preview", SubStyle);
        

        if (PreviewViewer == null && Model != null)
        {
            PreviewViewer = Editor.CreateEditor(Model);
        }

        if (PreviewViewer == null)
        {
            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label($"Model Name : {Model.name}", expStyle);

        //에디터에 추가
        PreviewViewer.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(256, 256), style);

        EditorGUI.BeginChangeCheck();
        bool ButtonPressed = false;
        if (GUILayout.Button(IsPlaying == true ? "Stop" : "Play", EditorStyles.miniButton))
        {
            ButtonPressed = true;
            if (IsPlaying == true)
            {
                IsPlaying = false;
            }
            else
            {
                IsPlaying = true;
            }
        }

        AnimationTime = EditorGUILayout.Slider("Animation Time", AnimationTime, 0.0f, clip.length);

        bool change = EditorGUI.EndChangeCheck();

        GUI.enabled = true;
        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        if (change || IsPlaying)
        {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(Model, clip, AnimationTime);
            AnimationMode.EndSampling();
            PreviewViewer.ReloadPreviewInstances();
        }
        else if (ButtonPressed == false && change)
        {
            IsPlaying = false;
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(Model, clip, AnimationTime);
            AnimationMode.EndSampling();
            PreviewViewer.ReloadPreviewInstances();
        }

        //AnimationMode 종료
        if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();

        if (Model.GetComponentInChildren<SkinnedMeshRenderer>() == null)
        {
            EditorGUILayout.HelpBox("Before Generate, Strolgly Recommended to Using Skinned MeshRenderer.", MessageType.Warning);
        }
        if (isGenerateTirggerOn)
        {
            GUI.enabled = false;
        }

        GUILayout.Space(10);
        GUILayout.Label("Bake Setup", SubStyle);
        GUILayout.Label("1. Save Name (string) : 저장할 파일 이름", expStyle);
        SaveName = EditorGUILayout.TextField("Save Name", SaveName);
        if (SaveName == string.Empty)
        { 
            SaveName = Model.name;
        }
        GUILayout.Space(3);
        GUILayout.Label("2. Number of Frame (int) : 저장할 Mesh 샘플링 갯수", expStyle);
        EditorGUILayout.HelpBox("Frame must be higher than 0.01 sampling latency.", MessageType.Info);
        NumberOfFrame = EditorGUILayout.IntField("Number of Frame", NumberOfFrame);

        if (NumberOfFrame < 1f)
        {
            NumberOfFrame = 1;
        }
        else if (clip.length / NumberOfFrame < 0.01f)
        {
            NumberOfFrame = clip.length * 100 > 1 ? (int)(clip.length * 100) : 1;
        }
        GUILayout.Space(3);
        GUILayout.Label("3. Directory Path (Directory) : 저장할 폴더 위치", expStyle);
        GUILayout.Label($"Save Directory : {SaveDirectory}", expStyle);
        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Open", EditorStyles.miniButton))
        {
            SaveDirectory = EditorUtility.OpenFolderPanel("Select Save Directory", SaveDirectory, "");
            if (SaveDirectory.Equals(string.Empty))
            {
                SaveDirectory = "Assets/BakingEditor/Result/";
            }

        }
        #region Bind Pose Setup
        GUILayout.Space(3);
        GUILayout.Label("BindPose", expStyle);
        GUILayout.BeginHorizontal();

        GUILayout.Label("Matrix4x4", expStyle);
        GUILayout.Label("    0    ", expStyle);
        GUILayout.Label("    1    ", expStyle);
        GUILayout.Label("    2    ", expStyle);
        GUILayout.Label("    3    ", expStyle);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("        0", expStyle);
        bindMat.m00 = float.Parse(EditorGUILayout.TextField(bindMat.m00.ToString()));
        bindMat.m01 = float.Parse(EditorGUILayout.TextField(bindMat.m01.ToString()));
        bindMat.m02 = float.Parse(EditorGUILayout.TextField(bindMat.m02.ToString()));
        bindMat.m03 = float.Parse(EditorGUILayout.TextField(bindMat.m03.ToString()));
        


        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("        1", expStyle);
        bindMat.m10 = float.Parse(EditorGUILayout.TextField(bindMat.m10.ToString()));
        bindMat.m11 = float.Parse(EditorGUILayout.TextField(bindMat.m11.ToString()));
        bindMat.m12 = float.Parse(EditorGUILayout.TextField(bindMat.m12.ToString()));
        bindMat.m13 = float.Parse(EditorGUILayout.TextField(bindMat.m13.ToString()));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("        2", expStyle);
        bindMat.m20 = float.Parse(EditorGUILayout.TextField( bindMat.m20.ToString()));
        bindMat.m21 = float.Parse(EditorGUILayout.TextField( bindMat.m21.ToString()));
        bindMat.m22 = float.Parse(EditorGUILayout.TextField( bindMat.m22.ToString()));
        bindMat.m23 = float.Parse(EditorGUILayout.TextField( bindMat.m23.ToString()));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("        3", expStyle);
        bindMat.m30 = float.Parse(EditorGUILayout.TextField(bindMat.m30.ToString()));
        bindMat.m31 = float.Parse(EditorGUILayout.TextField(bindMat.m31.ToString()));
        bindMat.m32 = float.Parse(EditorGUILayout.TextField(bindMat.m32.ToString()));
        bindMat.m33 = float.Parse(EditorGUILayout.TextField(bindMat.m33.ToString()));

        GUILayout.EndHorizontal();
        #endregion

        GUILayout.Space(3);

        isCollectMesh = GUILayout.Toggle(isCollectMesh, "Donwload as Scriptable Object");

        GUILayout.Space(10);
        string texturePath = System.IO.Path.Combine("Assets/BakingEditor/Texture/", "BakingInfoTexture.png");
        Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
        GUIStyle InfoStyle = new GUIStyle();
        InfoStyle.fontSize = 12;
        InfoStyle.normal.textColor = Color.black;
        InfoStyle.normal.background = texture;
        GUILayout.Label("Information Display", SubStyle);
        GUILayout.Label($"Clip Length : {AnimationTime} / {clip.length}", InfoStyle);
        GUILayout.Label($"Number of Sampling : {NumberOfFrame}", InfoStyle);
        GUILayout.Label($"SamplingLatency : {clip.length/NumberOfFrame}", InfoStyle);
        GUILayout.Label($"Save Directory : {SaveName}", InfoStyle);
        GUILayout.Label($"Expectated File Name: {SaveDirectory}{SaveName}.asset", InfoStyle);

        GUILayout.Space(10);
        if (GUILayout.Button("Generate", EditorStyles.miniButton))
        {
            if (isGenerateTirggerOn == false && isGenerated == false)
            {

                try
                {
                    GenProgress = 0;
                    AmountSoFar = 0;
                    AnimationTime = 0f;
                    isGenerateTirggerOn = true;
                    //StartCoroutine
                    ExportingProcess = EditorCoroutineUtility.StartCoroutine(Export(), this);
                }
                catch
                {
                    EditorCoroutineUtility.StopCoroutine(ExportingProcess);
                    isGenerateTirggerOn = false;
                }

            }

        }
        GUILayout.EndVertical();

        if (SaveName == string.Empty || NumberOfFrame > 1 || NumberOfFrame > (int)(clip.length * 100) || SaveDirectory == string.Empty)
        {
            GUI.enabled = false;
        }
        else
        {
            GUI.enabled = true;
        }

        EditorGUI.ProgressBar(new Rect(0f, GUILayoutUtility.GetLastRect().height, Screen.width / 2, 40f), (float)GenProgress / (float)NumberOfFrame, "Progress");
        

    }

    IEnumerator Export()
    {
        List<Mesh> Meshes = new List<Mesh>();

        string FileName = SaveName;
        if (SaveName == string.Empty)
        {
            FileName = Model.name;
        }
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log("Export Complete");

        int SamplingOrder = 0;
        float SamplingTime = 0f;



        while (SamplingOrder < NumberOfFrame)
        {
            Debug.Log(SamplingTime);

            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(Model, clip, SamplingTime);


            Mesh TargetMesh = new Mesh();
            SkinnedMeshRenderer skinnedMesh = Model.GetComponentInChildren<SkinnedMeshRenderer>();
            skinnedMesh.BakeMesh(TargetMesh);

            Mesh sharedMesh = skinnedMesh.sharedMesh;
            List<Matrix4x4> bindposeList = new List<Matrix4x4>();

            for (int i = 0; i < skinnedMesh.bones.Length; i++)
            {
                //Matrix4x4 matrix = skinnedMesh.rootBone.parent.localToWorldMatrix;
                //matrix.m01 = 0;
                //matrix.m11 = 1;
                //matrix.m21 = 0;
                //matrix.m02 = 0;
                //matrix.m12 = 0;
                //matrix.m22 = -1;
                //Matrix4x4 item = skinnedMesh.bones[i].worldToLocalMatrix * skinnedMesh.rootBone.localToWorldMatrix;
                Matrix4x4 item = skinnedMesh.bones[i].localToWorldMatrix * bindMat;
                bindposeList.Add(item);
            }
            TargetMesh.bindposes = bindposeList.ToArray();
            TargetMesh.boneWeights = sharedMesh.boneWeights;

            TargetMesh.bounds = sharedMesh.bounds;

            AssetDatabase.CreateAsset(TargetMesh, SaveDirectory + FileName + $" ({SamplingOrder})" + ".asset");
            if (isCollectMesh == true)
            {
                Meshes.Add(AssetDatabase.LoadAssetAtPath<Mesh>(SaveDirectory + FileName + $" ({SamplingOrder})" + ".asset"));
            }


            SamplingTime += (float)(clip.length / NumberOfFrame);
            AnimationMode.EndSampling();
            PreviewViewer.ReloadPreviewInstances();

            yield return new WaitForSecondsRealtime(0.02f);
            SamplingOrder++;
            GenProgress = SamplingOrder;
        }

        if (isCollectMesh)
        {
            BakeScript bo = Instantiate<BakeScript>(CreateInstance<BakeScript>());
            bo.material = material;
            bo.name = FileName;
            bo.meshes = Meshes.ToArray();
            AssetDatabase.CreateAsset(bo, SaveDirectory + FileName + ".asset");
        }


        isGenerateTirggerOn = false;
        GenProgress = NumberOfFrame;
    }

    public void ExportMeshes()
    {
        //if(clip.length / NumberOfFrame > )


        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(Model, clip, AnimationTime);
        AnimationMode.EndSampling();
        PreviewViewer.ReloadPreviewInstances();
    }

    //IEnumerator WaitForNextMesh()
    //{
    //    yield return new WaitForSeconds(WaitAmount);
    //    AmountSoFar++;
    //    //wait done! Let's make the static mesh!


    //    // mesh setup
    //    mesh = new Mesh();
    //    SkinMeshRender.BakeMesh(mesh);
    //    // GraphicsBuffer g = SkinMeshRender.GetVertexBuffer();

    //    Mesh sharedMesh = SkinMeshRender.sharedMesh;


    //    List<Matrix4x4> bindposeList = new List<Matrix4x4>();

    //    //for (int i = 0; i < sharedMesh.bindposes.Length; i++)
    //    //{
    //    //    Matrix4x4 item = sharedMesh.bindposes[i];
    //    //    bindposeList.Add(item);
    //    //}

    //    for (int i = 0; i < SkinMeshRender.bones.Length; i++)
    //    {
    //        Matrix4x4 matrix = SkinMeshRender.rootBone.parent.localToWorldMatrix;
    //        matrix.m01 = 0;
    //        matrix.m11 = 1;
    //        matrix.m21 = 0;
    //        matrix.m02 = 0;
    //        matrix.m12 = 0;
    //        matrix.m22 = -1;
    //        Matrix4x4 item = SkinMeshRender.bones[i].localToWorldMatrix * matrix;
    //        bindposeList.Add(item);
    //    }


    //    mesh.bindposes = bindposeList.ToArray();
    //    mesh.boneWeights = sharedMesh.boneWeights;

    //    mesh.bounds = sharedMesh.bounds;


    //    //now that's it's made, add it to the que
    //    AddMeshToQ(mesh);
    //    if (AmountSoFar < numberOfFrames)
    //    {
    //        //do it again, we have more meshes to make!
    //        StartCoroutine(WaitForNextMesh());
    //    }

    //    else
    //    {
    //        int c = 0;
    //        //created all meshes, we're done!
    //        foreach (Mesh staticmesh in MeshQ)
    //        {
    //            AmountSavedSoFar++;
    //            //try to save to specified location
    //            try
    //            {
    //                //AssetDatabase.CreateAsset(staticmesh, SaveLocation + AmountSavedSoFar.ToString() + c++.ToString() + "_StaticFromSkinned" + ".asset");
    //            }
    //            //if the location is invalid, throw an error
    //            catch
    //            {
    //                Debug.Log("<color=red><b>Invalid save location! Make sure you've spelled the path to a folder correctly. For example: Assets/_@MYSTUFF/StaticAnimations/ </b></color>");
    //                yield break;
    //            }
    //        }
    //        //spam the console in fancy ways
    //        Debug.Log("<color=green><b>All meshes created! You'll find them here: </b></color>" + SaveLocation);
    //        Debug.Log("<color=red><i>For some reason I can't explain, delete the first frame and the last 2 frames so they loop properly.</i></color>");
    //        Debug.Log("<color=red><i>Don't forget to disable/change this script, or you'll do what you just did again!</i></color>");
    //    }
    //}
}

