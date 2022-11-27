using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum GameState { Battle }

public class GameController : MonoBehaviour
{
    GameState state;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] GameObject playerController;

    private void Awake()
    {
        ConditionDB.Init();
    }

    private void Update()
    {
        if (state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
    }



    private void Start()
    {
        state = GameState.Battle;
        
        var playerParty = playerController.GetComponent<DragonParty>();
        var enemyDragon = FindObjectOfType<EnemyDragon>().GetComponent<EnemyDragon>().GetRandomEnemyDragon();

        battleSystem.StartBattle(playerParty, enemyDragon);

    }
}
