using System.Collections.Generic;
using UnityEngine;

public abstract class UndeadUnit : UnitBase
{
    [SerializeField] private UndeadType undeadType;

    private readonly List<IUnitAction> actions = new List<IUnitAction>();
    private DefaultUnitAction defaultAction;

    public UndeadType UndeadType => undeadType;

    protected override void Awake()
    {
        base.Awake();
        defaultAction = new DefaultUnitAction(this);
    }

    public IReadOnlyList<IUnitAction> GetActions()
    {
        actions.Clear();

        if (defaultAction == null)
            defaultAction = new DefaultUnitAction(this);

        actions.Add(defaultAction);

        UnitActionBase[] componentActions = GetComponents<UnitActionBase>();
        for (int i = 0; i < componentActions.Length; i++)
        {
            if (componentActions[i] != null)
                actions.Add(componentActions[i]);
        }

        return actions;
    }
}
