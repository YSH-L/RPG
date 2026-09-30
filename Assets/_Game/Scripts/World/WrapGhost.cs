using UnityEngine;

/// <summary>
/// 경계 너머에서도 보이도록 스프라이트를 한 바퀴 폭만큼 좌우에 하나씩 더 그린다.
/// 그림만 복사한다 — 판정은 <see cref="Area.OverlapBox"/>가 경계 너머까지 따로 본다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WrapGhost : MonoBehaviour
{
    [SerializeField] private Area area;

    private SpriteRenderer source;
    private readonly SpriteRenderer[] ghosts = new SpriteRenderer[2];

    public Area Area
    {
        get => area;
        set => area = value;
    }

    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        for (int i = 0; i < ghosts.Length; i++)
        {
            var child = new GameObject(i == 0 ? "Ghost+" : "Ghost-");
            child.transform.SetParent(transform, false);
            ghosts[i] = child.AddComponent<SpriteRenderer>();
            ghosts[i].sharedMaterial = source.sharedMaterial;
        }
    }

    private void LateUpdate()
    {
        bool visible = area != null && source.enabled && source.sprite != null;
        for (int i = 0; i < ghosts.Length; i++)
        {
            SpriteRenderer ghost = ghosts[i];
            ghost.enabled = visible;
            if (!visible) continue;

            float offset = i == 0 ? area.LoopWidth : -area.LoopWidth;
            ghost.transform.position = transform.position + new Vector3(offset, 0f, 0f);
            ghost.sprite = source.sprite;
            ghost.flipX = source.flipX;
            ghost.flipY = source.flipY;
            ghost.color = source.color;
            ghost.drawMode = source.drawMode;
            ghost.size = source.size;
            ghost.sortingLayerID = source.sortingLayerID;
            ghost.sortingOrder = source.sortingOrder;
        }
    }
}
