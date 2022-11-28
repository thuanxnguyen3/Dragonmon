using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, BattleOver}

public enum BattleAction { Move, SwitchDragon, UseItem, Run}
public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;

    //public event Action<bool> OnBattleOver;

    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;

    DragonParty playerParty;
    Dragon enemyDragon;

    public void StartBattle(DragonParty playerParty, Dragon enemyDragon)
    {
        this.playerParty = playerParty;
        this.enemyDragon = enemyDragon;
        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Setup(playerParty.GetHealthyDragon());
        enemyUnit.Setup(enemyDragon);

        partyScreen.Init();

        dialogBox.SetMoveNames(playerUnit.Dragon.Moves);

        yield return dialogBox.TypeDialog($"A wild {enemyUnit.Dragon.Base.Name} appeared.");

        ActionSelection();
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogBox.SetDialog("Choose an action");
        dialogBox.EnableActionSelector(true);
    }

    void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Dragons);
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if(playerAction == BattleAction.Move)
        {
            playerUnit.Dragon.CurrentMove  = playerUnit.Dragon.Moves[currentMove];
            enemyUnit.Dragon.CurrentMove = enemyUnit.Dragon.GetRandomMove();

            //Check who goes first but this does not matter right now

            var firstUnit = playerUnit;
            var secondUnit = enemyUnit;

            //First turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Dragon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver) yield break;

            //Second turn
            yield return RunMove(secondUnit, firstUnit, secondUnit.Dragon.CurrentMove);
            yield return RunAfterTurn(secondUnit);
            if (state == BattleState.BattleOver) yield break;

        } 
        else
        {
            if(playerAction == BattleAction.SwitchDragon)
            {
                var selectedDragon = playerParty.Dragons[currentMember];
                state = BattleState.Busy;
                yield return SwitchDragon(selectedDragon);
            }

            //Enemy Turn
            var enemyMove = enemyUnit.Dragon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }
    }


    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Dragon.OnBeforeMove();

        if(!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Dragon);
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Dragon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Dragon.Base.Name} used {move.Base.Name}");

        sourceUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(1f);
        targetUnit.PlayHitAnimation();

        if (move.Base.Category == MoveCategory.Status)
        {
            yield return RunMoveEffects(move, sourceUnit.Dragon, targetUnit.Dragon);
        }
        else
        {
            var damageDetails = targetUnit.Dragon.TakeDamage(move, sourceUnit.Dragon);
            yield return targetUnit.Hud.UpdateHP();
            yield return ShowDamageDetails(damageDetails);
        }


        if (targetUnit.Dragon.HP <= 0)
        {
            yield return dialogBox.TypeDialog($"{targetUnit.Dragon.Base.Name} Fainted");
            targetUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(targetUnit);
            //Game over
            state = BattleState.BattleOver;
        }

        
    }

    IEnumerator RunMoveEffects(Move move, Dragon source, Dragon target)
    {
        var effects = move.Base.Effects;

        //Stat Boosting
        if (effects.Boosts != null)
        {
            if (move.Base.Target == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }

        //Status Condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;

        // Statues like burn or psn will hurt the dragon after the turn
        sourceUnit.Dragon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Dragon);
        yield return sourceUnit.Hud.UpdateHP();

        if (sourceUnit.Dragon.HP <= 0)
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Dragon.Base.Name} Fainted");
            sourceUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(sourceUnit);
        }
    }

    IEnumerator ShowStatusChanges(Dragon dragon)
    {
        while (dragon.StatusChanges.Count > 0)
        {
            var message = dragon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if(faintedUnit.IsPlayerUnit)
        {
            var nextDragon = playerParty.GetHealthyDragon();
            if (nextDragon != null)
            {
                OpenPartyScreen();
            }
            else
            {
                //game over
                state = BattleState.BattleOver;

            }
        } 
        else
        {
            // game over
            state = BattleState.BattleOver;

        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit!");

        if(damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's super effective!");
        else if (damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's not very effective!");


    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if(state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
    }

    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogBox.UpdateActionSelection(currentAction);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                //Fight
                MoveSelection();
            } 
            else if (currentAction == 1)
            {
                //Bag
            }
            else if (currentAction == 2)
            {
                //Dragon
                prevState = state;
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                //Run
            }
        }
    }

    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMove;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMove;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMove -= 2;

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Dragon.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove, playerUnit.Dragon.Moves[currentMove]);

        if(Input.GetKeyDown(KeyCode.Z))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));

        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMember;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMember;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMember += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMember -= 2;

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Dragons.Count - 1);

        partyScreen.UpdateMemberSelection(currentMember);

        if(Input.GetKeyDown(KeyCode.Z))
        {
            var selectedMember = playerParty.Dragons[currentMember];
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted dragon");
                return;
            }
            if (selectedMember == playerUnit.Dragon)
            {
                partyScreen.SetMessageText("You can't switch with the same dragon");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.ActionSelection)
            {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchDragon));
            } 
            else
            {
                state = BattleState.MoveSelection;

                dialogBox.EnableActionSelector(false);

                StartCoroutine(SwitchDragon(selectedMember));
            }
            /*

            state = BattleState.MoveSelection;

            dialogBox.EnableActionSelector(false);

            StartCoroutine(SwitchDragon(selectedMember));*/
        }
        else if(Input.GetKeyDown(KeyCode.X))
        {
            partyScreen.gameObject.SetActive(false);
            ActionSelection();
        }
    }

    IEnumerator SwitchDragon(Dragon newDragon)
    {

        if (playerUnit.Dragon.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Dragon.Base.Name}");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

        }

        playerUnit.Setup(newDragon);
        dialogBox.SetMoveNames(newDragon.Moves);

        yield return dialogBox.TypeDialog($"Go {newDragon.Base.Name}!");

        state = BattleState.RunningTurn;
        //StartCoroutine(EnemyMove());
    }
}
