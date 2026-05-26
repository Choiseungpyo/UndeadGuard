using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour, IUnit, IDamageable
{
    [SerializeField] private TeamType team;
    [SerializeField] private UnitStats stats;
    [SerializeField] private float tileMoveDuration = 0.2f;

    private UnitAnimator unitAnimator;
    private Vector2Int gridPosition;
    private bool hasMoved;
    private bool hasActed;
    private bool isDead;
    private bool spawnInitialized;

    public TeamType Team => team;
    public Vector2Int GridPosition => gridPosition;
    public bool HasMoved => hasMoved;
    public bool HasActed => hasActed;
    public bool IsDead => isDead;
    public UnitStats Stats => stats;
    public UnitAnimator UnitAnimator => unitAnimator;

    protected virtual void Awake()
    {
        stats?.Initialize();
        unitAnimator = GetComponent<UnitAnimator>();
    }

    protected virtual void Start()
    {
        if (spawnInitialized) return;
        if (GridManager.Instance == null) return;

        var pos = GridManager.Instance.WorldToGrid(transform.position);
        gridPosition = pos;

        var center = GridManager.Instance.GridToWorld(pos);
        transform.position = new Vector3(center.x, transform.position.y, center.z);
    }

    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        int previousHp = stats.CurrentHp;
        stats.ApplyDamage(amount);
        int actualDamage = Mathf.Max(0, previousHp - stats.CurrentHp);

        EventBus.Instance.Publish(new DamageTakenEvent
        {
            Target = this,
            TargetBehaviour = this,
            Damage = actualDamage,
            CurrentHp = stats.CurrentHp,
            MaxHp = stats.MaxHp
        });

        if (stats.IsEmpty)
            Die();
    }

    public virtual void PerformAttack(UnitBase target)
    {
        if (isDead) return;
        if (target == null || target.IsDead) return;

        if (team == TeamType.Undead)
            FaceTowardCardinal(target.GridPosition);
        else
            FaceToward(target.GridPosition);
        unitAnimator?.TriggerAttack();
        AttackEffectService.Play(this, target.GridPosition, UnitActionIds.DefaultAction);

        int damage = stats.PhysicalAttack;
        target.TakeDamage(damage);

        EventBus.Instance.Publish(new UnitAttackedEvent
        {
            Attacker = this,
            Target = target,
            Damage = damage
        });
    }

    public virtual IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        if (isDead) yield break;
        if (path == null || path.Count <= 1) yield break;
        if (GridManager.Instance == null) yield break;

        EventBus.Instance.Publish(new UnitMoveStartedEvent { Unit = this });
        unitAnimator?.SetWalking(true);

        float duration = Mathf.Max(0.01f, tileMoveDuration);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int to = path[i];
            Vector2Int from = gridPosition;

            FaceToward(to);

            Vector3 startWorld = transform.position;
            Vector3 center = GridManager.Instance.GridToWorld(to);
            Vector3 endWorld = new Vector3(center.x, transform.position.y, center.z);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startWorld, endWorld, t);
                yield return null;
            }

            transform.position = endWorld;
            gridPosition = to;

            EventBus.Instance.Publish(new UnitMovedEvent
            {
                Unit = this,
                From = from,
                To = to
            });
        }

        unitAnimator?.SetWalking(false);
        MarkAsMoved();

        EventBus.Instance.Publish(new UnitMoveFinishedEvent { Unit = this });
    }

    public virtual void FaceToward(Vector2Int targetGrid)
    {
        if (GridManager.Instance == null) return;

        Vector3 targetWorld = GridManager.Instance.GridToWorld(targetGrid);
        Vector3 direction = targetWorld - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void FaceTowardCardinal(Vector2Int targetGrid)
    {
        Vector2Int delta = targetGrid - gridPosition;
        if (delta == Vector2Int.zero)
            return;

        int absX = Mathf.Abs(delta.x);
        int absY = Mathf.Abs(delta.y);

        Vector2Int cardinalStep;
        if (absY >= absX)
            cardinalStep = new Vector2Int(0, delta.y > 0 ? 1 : -1);
        else
            cardinalStep = new Vector2Int(delta.x > 0 ? 1 : -1, 0);

        FaceToward(gridPosition + cardinalStep);
    }

    public virtual void Die()
    {
        isDead = true;
        unitAnimator?.TriggerDie();

        EventBus.Instance.Publish(new UnitDiedEvent
        {
            Unit = this,
            Position = gridPosition
        });
    }

    public void MarkAsMoved()
    {
        hasMoved = true;
    }

    public void MarkAsActed()
    {
        hasActed = true;
    }

    public void ResetTurnState()
    {
        hasMoved = false;
        hasActed = false;
    }

    public virtual void Revive(Vector2Int position)
    {
        isDead = false;
        gridPosition = position;
        stats.Initialize();
        gameObject.SetActive(true);
        unitAnimator?.TriggerRevive();

        EventBus.Instance.Publish(new UnitRevivedEvent
        {
            Unit = this,
            Position = position
        });
    }

    public void PrepareForSpawn(Vector2Int position, Vector3 worldPos)
    {
        spawnInitialized = true;
        isDead = false;
        gridPosition = position;
        hasMoved = false;
        hasActed = false;
        stats.Initialize();
        transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);
    }
}

