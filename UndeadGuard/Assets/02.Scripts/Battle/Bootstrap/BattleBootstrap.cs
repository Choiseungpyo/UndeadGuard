using System.Collections.Generic;
using UnityEngine;

public sealed class BattleBootstrap : MonoBehaviour
{
    [SerializeField] private GridCoordinateMapper coordinateMapper;
    [SerializeField] private BattlePresenter presenter;
    [SerializeField] private BattleInputController inputController;
    [SerializeField] private BattleTurnFlow battleTurnFlow;

    [SerializeField] private List<UnitSpawnEntry> unitSpawns = new List<UnitSpawnEntry>();

    [SerializeField] private MapDefinition mapDefinition;
    [SerializeField] private PlayerResourceSettings playerResourceSettings;

    [SerializeField] private StructureDefinition coreStructureDefinition;
    [SerializeField] private StructureDefinition wallStructureDefinition;
    [SerializeField] private StructureDefinition revivalAltarStructureDefinition;

    private PlayerCommandController playerCommandController;
    private EnemyTurnController enemyTurnController;

    private void Awake()
    {
        if (mapDefinition == null)
        {
            Debug.LogError("MapDefinition이 비어 있습니다.");
            enabled = false;
            return;
        }

        if (playerResourceSettings == null)
        {
            Debug.LogError("PlayerResourceSettings가 비어 있습니다.");
            enabled = false;
            return;
        }

        if (coreStructureDefinition == null)
        {
            Debug.LogError("coreStructureDefinition이 비어 있습니다.");
            enabled = false;
            return;
        }

        if (!mapDefinition.TryGetCorePosition(out Vector2Int corePosition))
        {
            Debug.LogError("맵에 Core가 배치되어 있지 않습니다.");
            enabled = false;
            return;
        }

        BattleGrid grid = new BattleGrid(mapDefinition);
        GridPosition coreGridPosition = new GridPosition(corePosition.x, corePosition.y);

        BattleStructure core = new BattleStructure(coreStructureDefinition, coreGridPosition);
        PlayerResourceState playerResources = new PlayerResourceState(playerResourceSettings.StartDarkEnergy);
        BattleState state = new BattleState(grid, core, playerResources);

        AddStructuresFromMap(state);

        TurnManager turnManager = new TurnManager();
        GridSearchService gridSearchService = new GridSearchService();
        BattleActionService actionService = new BattleActionService(turnManager);
        BattleCommandExecutor commandExecutor = new BattleCommandExecutor();

        EnemyDecisionService enemyDecisionService = new EnemyDecisionService(state, gridSearchService);
        enemyTurnController = new EnemyTurnController(
            state,
            enemyDecisionService,
            actionService,
            commandExecutor);

        playerCommandController = new PlayerCommandController(
            state,
            gridSearchService,
            actionService,
            commandExecutor);

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

        presenter.Initialize(playerCommandController, actors);
        inputController.Initialize(playerCommandController);

        if (battleTurnFlow == null)
        {
            Debug.LogError("BattleTurnFlow가 비어 있습니다.");
            enabled = false;
            return;
        }

        battleTurnFlow.Initialize(
            state,
            turnManager,
            playerCommandController,
            inputController,
            commandExecutor,
            enemyTurnController,
            presenter);

        battleTurnFlow.StartBattle();
    }

    private void AddStructuresFromMap(BattleState state)
    {
        for (int i = 0; i < mapDefinition.Cells.Count; i++)
        {
            MapCellData cell = mapDefinition.Cells[i];
            GridPosition position = new GridPosition(cell.position.x, cell.position.y);

            switch (cell.objectType)
            {
                case StructureType.Wall:
                    state.AddStructure(new BattleStructure(wallStructureDefinition, position));
                    break;

                case StructureType.RevivalAltar:
                    state.AddStructure(new BattleStructure(revivalAltarStructureDefinition, position));
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerCommandController != null)
        {
            playerCommandController.Dispose();
        }
    }
}