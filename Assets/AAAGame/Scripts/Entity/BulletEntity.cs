using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletEntity : ParticleEntity
{
    private float moveSpeed = 50f;
    TrailRenderer trail = null;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        trail = GetComponent<TrailRenderer>();
    }
    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        trail?.Clear();
    }
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        Move();
    }

    private void Move()
    {
        transform.Translate(transform.forward * Time.deltaTime * moveSpeed, Space.World);
    }
}
