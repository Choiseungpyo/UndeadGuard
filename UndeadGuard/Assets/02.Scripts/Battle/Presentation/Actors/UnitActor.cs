using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 유닛의 실제 월드 연출을 담당한다.
/// 이동, 애니메이션, 선택 표시 등을 처리한다.
/// </summary>
public sealed class UnitActor : MonoBehaviour
{
    [SerializeField] private int unitId;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotateSpeed = 12f;
    [SerializeField] private float actorYOffset = 1f;
    private Animator animator;
    private UnitOutlineToggle outlineToggle;

    private Coroutine moveRoutine;

    public int UnitId => unitId;

    public event Action<int> MoveFinished;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        outlineToggle = GetComponent<UnitOutlineToggle>();

        SetSelected(false);
        SetMoveAnimation(false);
    }

    public void Bind(int unitId)
    {
        this.unitId = unitId;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        worldPosition.y += actorYOffset;
        transform.position = worldPosition;
    }

    public void SetSelected(bool selected)
    {
        if (outlineToggle != null)
        {
            outlineToggle.SetSelected(selected);
        }
    }

    public void PlayMove(IReadOnlyList<GridPosition> path, GridCoordinateMapper coordinateMapper)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(path, coordinateMapper));
    }

    public void MoveAlongPath(IReadOnlyList<GridPosition> path, GridCoordinateMapper coordinateMapper)
    {
        PlayMove(path, coordinateMapper);
    }

    private IEnumerator MoveRoutine(IReadOnlyList<GridPosition> path, GridCoordinateMapper coordinateMapper)
    {
        if (path == null || path.Count <= 1 || coordinateMapper == null)
        {
            SetMoveAnimation(false);
            moveRoutine = null;
            MoveFinished?.Invoke(unitId);
            yield break;
        }

        SetMoveAnimation(true);

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 targetPosition = coordinateMapper.GetWorldPosition(path[i]);
            targetPosition.y += actorYOffset;

            while (Vector3.Distance(transform.position, targetPosition) > 0.02f)
            {
                Vector3 direction = targetPosition - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotateSpeed * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime);

                yield return null;
            }

            transform.position = targetPosition;
        }

        SetMoveAnimation(false);
        moveRoutine = null;
        MoveFinished?.Invoke(unitId);
    }

    private void SetMoveAnimation(bool moving)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool("IsMoving", moving);
    }
}