using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancingWithAnimate : MonoBehaviour
{
    [SerializeField] BakeScript ScriptableAsset;
    [SerializeField] Mesh CurrentMesh;
    [SerializeField] Mesh[] meshes;
    [SerializeField] Material material;
    [SerializeField] int SpawnCount = 20000;
    [SerializeField] int batchSize = 1000;
    [SerializeField] int batchCounter;
    public List<List<Matrix4x4>> batchPosition;
    [SerializeField] bool Running;
    [SerializeField] float moveSpeed = 2f; // 이동 속도
    public RenderParams _rp;
    int count = 0;

    // 각 인스턴스의 이동 데이터 저장
    private List<InstanceData> instanceDataList;

    private struct InstanceData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 forward; // 이동 방향
    }

    void Start()
    {
        batchPosition = new List<List<Matrix4x4>>();
        instanceDataList = new List<InstanceData>();
        Running = true;

        if (ScriptableAsset != null)
        {
            meshes = ScriptableAsset.meshes;
        }

        CurrentMesh = meshes[0];

        if (material != null)
        {
            _rp = new RenderParams(material);
        }

        int stack = 0;
        batchCounter = 0;
        batchPosition.Add(new List<Matrix4x4>());

        for (int idx = 0; idx < SpawnCount; idx++)
        {
            float x = Random.Range(-50f, 50f);
            float z = Random.Range(-50f, 50f);
            float y = 1f;
            Vector3 trsVec = new Vector3(x, y, z);

            float yaw = Random.Range(0f, 360f);
            Quaternion rotation = Quaternion.Euler(new Vector3(0f, yaw, 0f));

            // 인스턴스 데이터 저장
            InstanceData data = new InstanceData
            {
                position = trsVec,
                rotation = rotation,
                forward = rotation * Vector3.forward // 회전 방향으로 전진
            };
            instanceDataList.Add(data);

            if (batchCounter < 1000)
            {
                batchPosition[stack].Add(Matrix4x4.TRS(trsVec, rotation, new Vector3(1f, 1f, 1f)));
                batchCounter++;
            }
            else
            {
                batchPosition.Add(new List<Matrix4x4>());
                batchCounter = 0;
                stack++;
            }
        }
    }

    void Update()
    {
        if (Running)
        {
            // 모든 인스턴스 이동 처리
            UpdatePositions();

            // 렌더링
            for (int idx = 0; idx < batchPosition.Count; idx++)
            {
                Graphics.RenderMeshInstanced(_rp, CurrentMesh, 0, batchPosition[idx]);
            }
        }
    }

    private void UpdatePositions()
    {
        float deltaMove = moveSpeed * Time.deltaTime;
        int globalIdx = 0;

        for (int batchIdx = 0; batchIdx < batchPosition.Count; batchIdx++)
        {
            for (int i = 0; i < batchPosition[batchIdx].Count; i++)
            {
                // 인스턴스 데이터 가져오기
                InstanceData data = instanceDataList[globalIdx];

                // 위치 업데이트
                data.position += data.forward * deltaMove;

                // 경계 체크 (범위를 벗어나면 반대편으로 이동)
                if (data.position.x > 50f) data.position.x = -50f;
                if (data.position.x < -50f) data.position.x = 50f;
                if (data.position.z > 50f) data.position.z = -50f;
                if (data.position.z < -50f) data.position.z = 50f;

                // 업데이트된 데이터 저장
                instanceDataList[globalIdx] = data;

                // Matrix4x4 업데이트
                batchPosition[batchIdx][i] = Matrix4x4.TRS(
                    data.position,
                    data.rotation,
                    new Vector3(1f, 1f, 1f)
                );

                globalIdx++;
            }
        }
    }

    private void FixedUpdate()
    {
        if (Running)
        {
            CurrentMesh = meshes[count++];
            if (count > meshes.Length - 1) count = 0;
        }
    }
}