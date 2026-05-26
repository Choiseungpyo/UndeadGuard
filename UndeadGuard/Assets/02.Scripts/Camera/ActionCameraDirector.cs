using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ActionCameraDirector : Singleton<ActionCameraDirector>
{
    [SerializeField] private Camera targetCamera;

    [SerializeField] private bool useActionCamera = true;
    [SerializeField] private bool usePlayerAttackCamera = true;
    [SerializeField] private bool useEnemyGroupAttackCamera = true;

    [SerializeField] private float playerAttackDistance = 6f;
    [SerializeField] private float playerAttackHeight = 3f;
    [SerializeField] private float enemyGroupDistance = 8f;
    [SerializeField] private float enemyGroupHeight = 5f;
    [SerializeField] private float lookAtHeight = 1.2f;

    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float returnDuration = 0.35f;
    [FormerlySerializedAs("holdDuration")]
    [SerializeField] private float preAttackHoldDuration = 0.15f;
    [FormerlySerializedAs("postHoldDuration")]
    [SerializeField] private float postAttackHoldDuration = 0.15f;

    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Dynamic Framing")]
    [SerializeField] private bool useDynamicDistance = true;
    [SerializeField] private float dynamicPadding = 1.15f;
    [SerializeField] private float minDynamicDistance = 4f;
    [SerializeField] private float maxDynamicDistance = 22f;

    public bool IsPlaying { get; private set; }

    private Camera savedCamera;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float savedFieldOfView;
    private float savedOrthographicSize;
    private bool hasSavedState;

    public IEnumerator PlayPlayerAttackCamera(Transform attacker, Transform target)
    {
        return PlayPlayerAttackCamera(attacker, target, null);
    }

    public IEnumerator PlayPlayerAttackCamera(
        Transform attacker,
        Transform target,
        IReadOnlyList<Transform> affectedTargets)
    {
        if (IsPlaying || !useActionCamera || !usePlayerAttackCamera)
            yield break;

        if (attacker == null || target == null)
            yield break;

        if (!TryGetCamera(out Camera camera))
            yield break;

        if (!TryCalculatePlayerAttackShot(
                camera,
                attacker,
                target,
                affectedTargets,
                out Vector3 cameraPosition,
                out Quaternion cameraRotation))
            yield break;

        IsPlaying = true;
        try
        {
            SaveCurrentCameraState();
            if (!hasSavedState)
                yield break;

            yield return MoveCamera(camera, cameraPosition, cameraRotation, moveDuration, moveCurve);

            float hold = Mathf.Max(0f, preAttackHoldDuration);
            if (hold > 0f)
                yield return new WaitForSeconds(hold);
        }
        finally
        {
            IsPlaying = false;
        }
    }

    public IEnumerator PlayEnemyGroupAttackCamera(IReadOnlyList<Transform> attackers, Transform target)
    {
        if (IsPlaying || !useActionCamera || !useEnemyGroupAttackCamera)
            yield break;

        if (attackers == null || attackers.Count == 0 || target == null)
            yield break;

        if (!TryGetCamera(out Camera camera))
            yield break;

        if (!TryCalculateEnemyGroupShot(attackers, target, out Vector3 cameraPosition, out Quaternion cameraRotation))
            yield break;

        IsPlaying = true;
        try
        {
            SaveCurrentCameraState();
            if (!hasSavedState)
                yield break;

            yield return MoveCamera(camera, cameraPosition, cameraRotation, moveDuration, moveCurve);

            float hold = Mathf.Max(0f, preAttackHoldDuration);
            if (hold > 0f)
                yield return new WaitForSeconds(hold);
        }
        finally
        {
            IsPlaying = false;
        }
    }

    public IEnumerator HoldBeforeReturn()
    {
        float hold = Mathf.Max(0f, postAttackHoldDuration);
        if (hold > 0f)
            yield return new WaitForSeconds(hold);
    }

    public IEnumerator ReturnToSavedCamera()
    {
        if (IsPlaying)
            yield break;

        IsPlaying = true;
        try
        {
            if (!hasSavedState)
                yield break;

            Camera camera = savedCamera;
            if (camera == null && !TryGetCamera(out camera))
                yield break;

            yield return MoveCamera(camera, savedPosition, savedRotation, returnDuration, returnCurve);

            camera.fieldOfView = savedFieldOfView;
            camera.orthographicSize = savedOrthographicSize;

            hasSavedState = false;
        }
        finally
        {
            IsPlaying = false;
        }
    }

    private void SaveCurrentCameraState()
    {
        if (!TryGetCamera(out Camera camera))
        {
            hasSavedState = false;
            return;
        }

        savedCamera = camera;
        savedPosition = camera.transform.position;
        savedRotation = camera.transform.rotation;
        savedFieldOfView = camera.fieldOfView;
        savedOrthographicSize = camera.orthographicSize;
        hasSavedState = true;
    }

    private bool TryGetCamera(out Camera camera)
    {
        camera = targetCamera != null ? targetCamera : Camera.main;
        return camera != null;
    }

    private bool TryCalculatePlayerAttackShot(
        Camera camera,
        Transform attacker,
        Transform target,
        IReadOnlyList<Transform> affectedTargets,
        out Vector3 cameraPosition,
        out Quaternion cameraRotation)
    {
        cameraPosition = Vector3.zero;
        cameraRotation = Quaternion.identity;

        if (attacker == null || target == null)
            return false;

        Vector3 attackerPosition = attacker.position;
        Vector3 targetPosition = target.position;

        Vector3 attackDirection = targetPosition - attackerPosition;
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 normalizedDirection = attackDirection.normalized;
        Vector3 lookPoint = (attackerPosition + targetPosition) * 0.5f + Vector3.up * lookAtHeight;
        cameraPosition = attackerPosition
            - normalizedDirection * playerAttackDistance
            + Vector3.up * playerAttackHeight;

        Vector3 lookVector = lookPoint - cameraPosition;
        if (lookVector.sqrMagnitude <= 0.0001f)
            return false;

        cameraRotation = Quaternion.LookRotation(lookVector.normalized, Vector3.up);

        if (useDynamicDistance && camera != null)
        {
            float baseDistance = Vector3.Distance(cameraPosition, lookPoint);
            if (TryComputeDynamicDistance(
                    camera,
                    attacker,
                    target,
                    affectedTargets,
                    lookPoint,
                    cameraRotation,
                    baseDistance,
                    out float adjustedDistance))
            {
                Vector3 forward = cameraRotation * Vector3.forward;
                cameraPosition = lookPoint - forward * adjustedDistance;
            }
        }

        return true;
    }

    private bool TryCalculateEnemyGroupShot(
        IReadOnlyList<Transform> attackers,
        Transform target,
        out Vector3 cameraPosition,
        out Quaternion cameraRotation)
    {
        cameraPosition = Vector3.zero;
        cameraRotation = Quaternion.identity;

        if (attackers == null || attackers.Count == 0 || target == null)
            return false;

        Vector3 sum = Vector3.zero;
        int validCount = 0;
        for (int i = 0; i < attackers.Count; i++)
        {
            Transform attacker = attackers[i];
            if (attacker == null)
                continue;

            sum += attacker.position;
            validCount++;
        }

        if (validCount == 0)
            return false;

        Vector3 attackerAveragePosition = sum / validCount;
        Vector3 targetPosition = target.position;

        Vector3 groupDirection = targetPosition - attackerAveragePosition;
        groupDirection.y = 0f;

        if (groupDirection.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 normalizedDirection = groupDirection.normalized;
        cameraPosition = attackerAveragePosition
            - normalizedDirection * enemyGroupDistance
            + Vector3.up * enemyGroupHeight;

        Vector3 lookPoint = (attackerAveragePosition + targetPosition) * 0.5f + Vector3.up * lookAtHeight;
        Vector3 lookVector = lookPoint - cameraPosition;
        if (lookVector.sqrMagnitude <= 0.0001f)
            return false;

        cameraRotation = Quaternion.LookRotation(lookVector.normalized, Vector3.up);
        return true;
    }

    private bool TryComputeDynamicDistance(
        Camera camera,
        Transform attacker,
        Transform primaryTarget,
        IReadOnlyList<Transform> affectedTargets,
        Vector3 lookPoint,
        Quaternion cameraRotation,
        float baseDistance,
        out float adjustedDistance)
    {
        adjustedDistance = baseDistance;
        if (camera == null || camera.orthographic)
            return false;

        Vector3 right = cameraRotation * Vector3.right;
        Vector3 up = cameraRotation * Vector3.up;
        Vector3 forward = cameraRotation * Vector3.forward;

        float verticalHalfFov = camera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        if (verticalHalfFov <= 0.001f)
            return false;

        float tanVertical = Mathf.Tan(verticalHalfFov);
        float horizontalHalfFov = Mathf.Atan(tanVertical * Mathf.Max(0.0001f, camera.aspect));
        float tanHorizontal = Mathf.Tan(horizontalHalfFov);

        float requiredDistance = baseDistance;
        bool hasAnyPoint = false;

        AddRequiredDistanceForTransform(attacker);
        AddRequiredDistanceForTransform(primaryTarget);
        if (affectedTargets != null)
        {
            for (int i = 0; i < affectedTargets.Count; i++)
                AddRequiredDistanceForTransform(affectedTargets[i]);
        }

        if (!hasAnyPoint)
            return false;

        float padded = requiredDistance * Mathf.Max(1f, dynamicPadding);
        padded = Mathf.Clamp(padded, minDynamicDistance, maxDynamicDistance);
        adjustedDistance = Mathf.Max(baseDistance, padded);
        return true;

        void AddRequiredDistanceForTransform(Transform pointTransform)
        {
            if (pointTransform == null)
                return;

            hasAnyPoint = true;
            Vector3 relative = pointTransform.position - lookPoint;
            float x = Mathf.Abs(Vector3.Dot(relative, right));
            float y = Mathf.Abs(Vector3.Dot(relative, up));
            float z = Vector3.Dot(relative, forward);

            float distX = x / Mathf.Max(0.0001f, tanHorizontal) - z;
            float distY = y / Mathf.Max(0.0001f, tanVertical) - z;
            float required = Mathf.Max(distX, distY, 0f);
            if (required > requiredDistance)
                requiredDistance = required;
        }
    }

    private IEnumerator MoveCamera(
        Camera camera,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        AnimationCurve curve)
    {
        if (camera == null)
            yield break;

        float clampedDuration = Mathf.Max(0f, duration);
        if (clampedDuration <= 0f)
        {
            camera.transform.position = targetPosition;
            camera.transform.rotation = targetRotation;
            yield break;
        }

        Vector3 startPosition = camera.transform.position;
        Quaternion startRotation = camera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < clampedDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / clampedDuration);
            float eased = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;

            camera.transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            camera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            yield return null;
        }

        camera.transform.position = targetPosition;
        camera.transform.rotation = targetRotation;
    }
}
