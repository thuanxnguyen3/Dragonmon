using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : MonoBehaviour
{
    [SerializeField] Text messageText;

    PartyMemberUI[] memberSlots;
    List<Dragon> dragons;
    public void Init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUI>();
    }

    public void SetPartyData(List<Dragon> dragons)
    {
        this.dragons = dragons;

        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < dragons.Count)
            {
                memberSlots[i].SetData(dragons[i]);
            }
            else
            {
                memberSlots[i].gameObject.SetActive(false);
            }
        }

        messageText.text = "Choose a Dragon";
    }

    public void UpdateMemberSelection(int selectedMember)
    {
        for(int i = 0; i < dragons.Count; i++)
        {
            if (i == selectedMember)
                memberSlots[i].SetSelected(true);
            else
                memberSlots[i].SetSelected(false);
        }

    }

    public void SetMessageText(string message)
    {
        messageText.text = message;
    }
}
