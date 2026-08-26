using UnityEngine;

namespace BatteryGame
{
    /// <summary>플레이어가 조준해서 상호작용할 수 있는 모든 것의 베이스.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] float highlightBoost = 1.45f;

        Renderer[] cachedRenderers;
        Color[] cachedColors;
        MaterialPropertyBlock block;
        bool highlighted;

        /// <summary>조준했을 때 크로스헤어 옆에 뜨는 이름.</summary>
        public abstract string HoverTitle { get; }

        /// <summary>이름 아래 한 줄로 뜨는 부가 설명. 없으면 null.</summary>
        public virtual string HoverDetail { get { return null; } }

        /// <summary>좌클릭. 손에 든 것에 따라 동작이 달라지므로 player를 통째로 받는다.</summary>
        public abstract void Interact(PlayerInteractor player);

        /// <summary>우클릭. 기본은 아무 동작 없음.</summary>
        public virtual void AltInteract(PlayerInteractor player) { }

        protected virtual void Awake()
        {
            CacheRenderers();
        }

        protected void CacheRenderers()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            cachedColors = new Color[cachedRenderers.Length];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                var mat = cachedRenderers[i].sharedMaterial;
                if (mat == null) { cachedColors[i] = Color.white; continue; }
                if (mat.HasProperty(BaseColorId)) cachedColors[i] = mat.GetColor(BaseColorId);
                else if (mat.HasProperty(ColorId)) cachedColors[i] = mat.GetColor(ColorId);
                else cachedColors[i] = Color.white;
            }
            block = new MaterialPropertyBlock();
        }

        public void SetHighlighted(bool on)
        {
            if (highlighted == on) return;
            highlighted = on;
            if (cachedRenderers == null) return;

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                var r = cachedRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);
                Color c = on
                    ? Color.Lerp(cachedColors[i], Color.white, 0.25f) * highlightBoost
                    : cachedColors[i];
                c.a = cachedColors[i].a;
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>런타임에 자식 렌더러가 바뀌었을 때(건전지 삽입 등) 하이라이트 캐시를 다시 잡는다.</summary>
        public void RefreshHighlightCache()
        {
            bool was = highlighted;
            SetHighlighted(false);
            CacheRenderers();
            if (was) SetHighlighted(true);
        }
    }
}
