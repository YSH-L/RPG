using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트 재사용 풀. <b>싱글톤이 아니다.</b>
/// 프리팹 종류마다 하나씩 씬에 배치하고, 쓰는 쪽에서 [SerializeField]로 참조한다.
/// <code>
/// [SerializeField] private ObjectPool pipePool;
/// GameObject pipe = pipePool.Get(spawnPos, Quaternion.identity);
/// pipePool.Release(pipe);
/// </code>
/// </summary>
/// <remarks>
/// GameObject 단위로만 다룬다. Rigidbody2D 같은 물리 타입은 시그니처에 등장하지 않는다
/// (3D 주차에서 같은 파일을 그대로 쓰기 위해서다).
/// 반납 조건(화면 밖 판정 등)은 이 클래스가 정하지 않는다 — 쓰는 쪽 책임이다.
/// </remarks>
public class ObjectPool : MonoBehaviour
{
    [Header("풀 설정")]
    [Tooltip("이 풀이 복제할 프리팹.")]
    [SerializeField] private GameObject prefab;

    [Tooltip("시작할 때 미리 만들어 둘 개수.")]
    [SerializeField, Min(0)] private int initialSize = 10;

    [Tooltip("풀이 비었을 때 더 만들지 여부. 끄면 Get()이 null을 반환한다.")]
    [SerializeField] private bool expandable = true;

    [Tooltip("복제된 오브젝트를 담을 부모. 비우면 이 오브젝트 아래로 들어간다.")]
    [SerializeField] private Transform container;

    /// <summary>대기 중인 오브젝트.</summary>
    private readonly Queue<GameObject> available = new Queue<GameObject>();

    /// <summary>대여 중인 오브젝트. 중복 반납을 막기 위해 Set으로 들고 있다.</summary>
    private readonly HashSet<GameObject> inUse = new HashSet<GameObject>();

    private void Awake()
    {
        if (container == null) container = transform;

        if (prefab == null)
        {
            Debug.LogError($"[ObjectPool] '{name}'에 prefab이 비어 있습니다. 인스펙터에서 지정하세요.", this);
            return;
        }

        for (int i = 0; i < initialSize; i++)
        {
            available.Enqueue(CreateInstance());
        }
    }

    public GameObject Get()
    {
        return Get(container.position, Quaternion.identity);
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        GameObject instance = null;

        // 큐에 남아 있던 것이 외부에서 Destroy된 경우가 있으므로 걸러낸다.
        while (available.Count > 0 && instance == null)
        {
            instance = available.Dequeue();
        }

        if (instance == null)
        {
            if (!expandable)
            {
                Debug.LogWarning($"[ObjectPool] '{name}'이 비었고 expandable이 꺼져 있습니다. null을 반환합니다.", this);
                return null;
            }

            instance = CreateInstance();
        }

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        inUse.Add(instance);

        return instance;
    }

    /// <summary>
    /// 오브젝트를 풀로 되돌린다. 같은 오브젝트를 두 번 반납해도, 이 풀이 만들지 않은
    /// 오브젝트를 넘겨도 조용히 무시된다.
    /// </summary>
    public void Release(GameObject obj)
    {
        if (obj == null) return;

        // 대여 목록에 없으면 중복 반납이거나 남의 오브젝트다.
        if (!inUse.Remove(obj)) return;

        obj.SetActive(false);
        obj.transform.SetParent(container, worldPositionStays: false);
        available.Enqueue(obj);
    }

    /// <summary>대여 중인 오브젝트를 모두 회수한다. 재시작 처리에 쓴다.</summary>
    public void ReleaseAll()
    {
        // Release가 inUse를 수정하므로 복사해서 돈다.
        var borrowed = new List<GameObject>(inUse);
        foreach (GameObject obj in borrowed)
        {
            Release(obj);
        }

        inUse.Clear();
    }

    private GameObject CreateInstance()
    {
        GameObject instance = Instantiate(prefab, container);
        instance.name = $"{prefab.name} (Pooled)";
        instance.SetActive(false);
        return instance;
    }
}
