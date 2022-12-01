using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DragonParty : MonoBehaviour
{
    [SerializeField] List<Dragon> dragons;

    public event Action OnUpdated;
    public List<Dragon> Dragons
    {
        get { 
            return dragons; 
        }
        set
        {
            dragons = value;
            OnUpdated?.Invoke();
        }
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

    public static DragonParty GetPlayerParty()
    {
        return FindObjectOfType<DragonParty>().GetComponent<DragonParty>();
    }
}
