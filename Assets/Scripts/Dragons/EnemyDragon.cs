using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDragon : MonoBehaviour
{
    [SerializeField] List<Dragon> enemyDragons;

    public Dragon GetRandomEnemyDragon()
    {
        var enemyDragon = enemyDragons[Random.Range(0, enemyDragons.Count)];
        enemyDragon.Init();
        return enemyDragon;
    }
}
