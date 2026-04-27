using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PooledAttackEffect : MonoBehaviour
{
    private ParticleSystem[] particleSystems = Array.Empty<ParticleSystem>();
    private AttackEffectService owner;
    private Coroutine returnCoroutine;
    private int lifecycleTicket;
    private Action<PooledAttackEffect> releaseAction;

    public void Initialize(AttackEffectService ownerInstance)
    {
        owner = ownerInstance;
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Spawn(Vector3 position, Action<PooledAttackEffect> onRelease)
    {
        lifecycleTicket++;
        StopReturnCoroutine();

        releaseAction = onRelease;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        gameObject.SetActive(true);
        RestartParticles();
    }

    public void ScheduleReturn(float delaySeconds)
    {
        if (releaseAction == null)
            return;

        if (delaySeconds <= 0f)
        {
            releaseAction(this);
            return;
        }

        int ticket = lifecycleTicket;
        returnCoroutine = owner.StartCoroutine(ReturnAfterDelay(delaySeconds, ticket));
    }

    public void Despawn()
    {
        lifecycleTicket++;
        StopReturnCoroutine();
        StopParticles();
        releaseAction = null;
        gameObject.SetActive(false);
    }

    public float EstimateLifetime(float fallbackSeconds)
    {
        float longest = 0f;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particle = particleSystems[i];
            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;
            if (main.loop)
                continue;

            float startDelay = GetCurveMax(main.startDelay);
            float startLifetime = GetCurveMax(main.startLifetime);
            float total = startDelay + main.duration + startLifetime;
            longest = Mathf.Max(longest, total);
        }

        if (longest > 0f)
            return longest;

        return Mathf.Max(0.05f, fallbackSeconds);
    }

    private IEnumerator ReturnAfterDelay(float delaySeconds, int expectedTicket)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (expectedTicket != lifecycleTicket)
            yield break;

        releaseAction?.Invoke(this);
    }

    private void RestartParticles()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particle = particleSystems[i];
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }
    }

    private void StopParticles()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particle = particleSystems[i];
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void StopReturnCoroutine()
    {
        if (returnCoroutine == null || owner == null)
            return;

        owner.StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }

    private static float GetCurveMax(ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return curve.constantMax;
            case ParticleSystemCurveMode.Curve:
                return curve.curve != null ? curve.curve.Evaluate(1f) * curve.curveMultiplier : 0f;
            case ParticleSystemCurveMode.TwoCurves:
                {
                    float max = 0f;
                    if (curve.curveMax != null)
                        max = Mathf.Max(max, curve.curveMax.Evaluate(1f) * curve.curveMultiplier);
                    if (curve.curveMin != null)
                        max = Mathf.Max(max, curve.curveMin.Evaluate(1f) * curve.curveMultiplier);
                    return max;
                }
            default:
                return 0f;
        }
    }
}
