using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] DragonBase _base;
    [SerializeField] int level;
    [SerializeField] bool isPlayerUnit;

    public Dragon Dragon { get; set; }

    public void Setup()
    {
        Dragon = new Dragon(_base, level);
        if (isPlayerUnit) {
            GetComponent<Image>().sprite = Dragon.Base.BackSprite; 
        } else
        {
            GetComponent<Image>().sprite = Dragon.Base.FrontSprite;

        }
    }
}
