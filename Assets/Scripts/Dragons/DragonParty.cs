using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DragonParty : MonoBehaviour
{
    [SerializeField] List<Dragon> dragons;

    public List<Dragon> Dragons
    {
        get { return dragons; }
    }

    private void Start()
    {
        foreach (var dragon in dragons)
        {
            dragon.Init();
        }
    }

    public Dragon GetHealthyDragon()
    {
        return dragons.Where(x => x.HP > 0).FirstOrDefault();

    }
}
