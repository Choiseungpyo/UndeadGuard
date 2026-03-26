using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 시작 시 필요한 객체와 데이터를 초기화하고 연결하는 진입점 클래스.
/// 그리드, 전투 상태, 서비스, 컨트롤러, 프레젠터를 생성하고 씬과 연결한다.
/// </summary>
public sealed class BattleBootstrap : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;

    [SerializeField] private Vector2Int corePosition = new Vector2Int(5, 5);
    [SerializeField] private int coreMaxHp = 20;
    [SerializeField] private int startDarkEnergy = 5;

    [SerializeField] private GridCoordinateMapper coordinateMapper;
    [SerializeField] private BattlePresenter presenter;
    [SerializeField] private BattleInputController inputController;

    [SerializeField] private List<UnitSpawnEntry> unitSpawns = new List<UnitSpawnEntry>();

    private BattleController controller;

    private void Awake()
    {
        BattleGrid grid = new BattleGrid(width, height);

        GridPosition coreGridPosition = new GridPosition(corePosition.x, corePosition.y);
        grid.SetObjectType(coreGridPosition, CellObjectType.DefensePoint);

        BattleCore core = new BattleCore(coreGridPosition, coreMaxHp);
        PlayerResourceState playerResources = new PlayerResourceState(startDarkEnergy);
        BattleState state = new BattleState(grid, core, playerResources);

        TurnSystem turnSystem = new TurnSystem();
        GridRangeService rangeService = new GridRangeService();
        BattleActionService actionService = new BattleActionService(turnSystem);
        BattleCommandExecutor commandExecutor = new BattleCommandExecutor();
        PathFinder pathFinder = new PathFinder();

        controller = new BattleController(
            state,
            rangeService,
            actionService,
            turnSystem,
            commandExecutor,
            pathFinder);

        List<UnitActor> actors = new List<UnitActor>();

        for (int i = 0; i < unitSpawns.Count; i++)
        {
            UnitSpawnEntry spawn = unitSpawns[i];

            BattleUnit unit = new BattleUnit(
                spawn.UnitId,
                spawn.Team,
                spawn.StartPosition,
                spawn.Facing,
                spawn.MaxHp,
                spawn.PhysicalAttack,
                spawn.MagicAttack,
                spawn.DefensePower,
                spawn.AttackRange,
                spawn.MoveRange);

            bool added = state.TryAddUnit(unit);

            if (!added)
            {
                Debug.LogError($"유닛 배치 실패: {spawn.UnitId}");
                continue;
            }

            if (spawn.Actor != null)
            {
                spawn.Actor.Bind(spawn.UnitId);
                spawn.Actor.SetWorldPosition(coordinateMapper.GetWorldPosition(spawn.StartPosition));
                actors.Add(spawn.Actor);
            }
        }

        presenter.Initialize(controller, actors);
        inputController.Initialize(controller);
        controller.StartBattle();
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.Dispose();
        }
    }
}