using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

public class ParticleEntity : EntityBase
{
    public const string LIFE_TIME = "LifeTime";
    public const string SORT_LAYER = "SortLayer";
    bool autoHide;
    float lifeTime;
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        autoHide = true;

        lifeTime = Params.Get<VarFloat>(LIFE_TIME, 2f);

        autoHide = lifeTime > 0;

        if (Params.TryGet<VarInt32>(SORT_LAYER, out var pSortLayer))
        {
            SetParticlesSortLayer(pSortLayer);
        }

        if (autoHide)
        {
            UniTask.Delay((int)(lifeTime * 1000)).ContinueWith(() =>
            {
                GF.Entity.HideEntitySafe(this);
            }).Forget();
        }
    }
    private void SetParticlesSortLayer(int layer)
    {
        var particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem item in particles)
        {
            var render = item.GetComponent<Renderer>();
            render.sortingOrder = layer;
        }
    }
}
