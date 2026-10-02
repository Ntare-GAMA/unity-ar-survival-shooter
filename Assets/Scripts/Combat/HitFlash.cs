using UnityEngine;

namespace ARSurvival.Combat
{
    /// <summary>
    /// Hit feedback: briefly tints every mesh under this object red. Uses a MaterialPropertyBlock
    /// so shared materials are never modified or duplicated. The tint multiplies textured
    /// materials, so textured characters read as "hurt" rather than turning flat white.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Color flashColor = new(1f, 0.3f, 0.25f);
        [SerializeField] float duration = 0.1f;

        Renderer[] renderers;
        MaterialPropertyBlock block;
        float remaining;

        void Awake()
        {
            // Meshes and skinned characters; not trails, lines or the muzzle flash.
            renderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true),
                r => (r is MeshRenderer || r is SkinnedMeshRenderer) && r.GetComponentInParent<MuzzleFlash>() == null);
            block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, flashColor);
        }

        public void Flash()
        {
            remaining = duration;
            foreach (var r in renderers)
                r.SetPropertyBlock(block);
        }

        public void Clear()
        {
            remaining = 0f;
            if (renderers == null)
                return;
            foreach (var r in renderers)
                r.SetPropertyBlock(null);
        }

        void Update()
        {
            if (remaining <= 0f)
                return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
                Clear();
        }

        void OnDisable() => Clear();
    }
}
