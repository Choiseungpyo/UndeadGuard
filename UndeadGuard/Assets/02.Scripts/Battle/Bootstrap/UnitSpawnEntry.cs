using UnityEngine;

/// <summary>
/// 전투 시작 시 배치할 유닛 정보를 Inspector에서 설정하기 위한 데이터 클래스.
/// 유닛 ID, 진영, 시작 위치, 기본 스탯, 연결할 Actor를 보관한다.
/// </summary>
[System.Serializable]
public sealed class UnitSpawnEntry
{
    [SerializeField] private int unitId;
    [SerializeField] private TeamType team = TeamType.Player;
    [SerializeField] private Vector2Int startPosition;
    [SerializeField] private Direction facing = Direction.North;

    [SerializeField] private int maxHp = 10;
    [SerializeField] private int physicalAttack = 3;
    [SerializeField] private int magicAttack = 0;
    [SerializeField] private int defensePower = 1;
    [SerializeField] private int attackRange = 1;
    [SerializeField] private int moveRange = 4;

    [SerializeField] private UnitActor actor;

    public int UnitId => unitId;
    public TeamType Team => team;
    public GridPosition StartPosition => new GridPosition(startPosition.x, startPosition.y);
    public Direction Facing => facing;
    public int MaxHp => maxHp;
    public int PhysicalAttack => physicalAttack;
    public int MagicAttack => magicAttack;
    public int DefensePower => defensePower;
    public int AttackRange => attackRange;
    public int MoveRange => moveRange;
    public UnitActor Actor => actor;
}