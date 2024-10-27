using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityGameFramework.Runtime;

public class ParticleEntity : EntityBase
{
    public const string LIFE_TIME = "LifeTime";
    public const string SORT_LAYER = "SortLayer";
    bool autoHide;
    public float LifeTime { get; private set; }
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        autoHide = true;

        LifeTime = Params.Get<VarFloat>(LIFE_TIME, 2f);

        autoHide = LifeTime > 0;

        if (Params.TryGet<VarInt32>(SORT_LAYER, out var pSortLayer))
        {
            SetParticlesSortLayer(pSortLayer);
        }

        if (autoHide)
        {
            UniTask.Delay(TimeSpan.FromSeconds(LifeTime)).ContinueWith(() =>
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
