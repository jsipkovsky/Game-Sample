using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//------------------------------------------
// STRUCTURES

// Attack structure
public struct AttackStruc
{
    public int AttackValue;
    public string AttackMark;
}

// Damage List structure
public struct DamageList
{
    public NewUnit Target;
    public int Damage;
    public string LogMessage;
}

// Structure for storing original Attributes
public struct AttrOrig
{
    public string UnitTag;
    public int PhysicalAttack;
    public int MagicalAttack;
    public int SpecialAttack;
    public int PhysicalDefence;
    public int MagicalDefence;
    public int SpecialDefence;
    public int ActionPoints;
    public int Damage;
}

//------------------------------------------
// Parent class for each Unit in the Game

public abstract class NewUnit : MonoBehaviour
{
    //----------------------------------
    // ATTRIBUTES

    // Unit Parameters
    public AttrOrig OriginalValues; // original values of Attributes

    public string UnitName;       // Unit Name
    public int PlayerNumber;      // Player Number (0 or 1)
    public int FractionNo;        // Fraction ID

    public int PhysicalAttack;    // Physical Attack
    public int MagicalAttack;     // Magical Attack  
    public int SpecialAttack;     // Special Attack  
    public int PhysicalDefence;   // Physical Defence
    public int MagicalDefence;    // Magical Defence
    public int SpecialDefence;    // Special Defence

    public int Health;            // Health Maximum
    public int HealthAct;         // Health Actual

    public int Damage;            // Damage

    public int RangeFrom;         // Minimal distance from where can attack
    public int RangeTo;           // Maximal distance to where can attack

    public int Iniciative;        // Iniciative (to determine sequence when Units play)
    public int ActionPoints;      // Action Poins (Maximum) 
    public int ActionsPointsAct;  // Action Poins (Actual) 
    public int MovementSpeed;     // Unit movement speed on Game grid

    public int AttackPrice;       // Price of Attack in Action Points 
    public int Resource;          // Local Resource (for Fractions with local resources)

    public int ActionMode;        // Action Mode - determines possible Cells and Targets
    public int ActionAtMoveEnd;   // Determine if action at end Turn is needed

    public bool IsImmune;         // True if Unit has immunity on some Attacks
    public bool IsTarget;         // True if Unit is Target of current action
    public bool IsMoving;         // True if Unit is currently moving
    public bool IsImmobile;       // True if movement is disabled for Unit
    public bool DoCounterAct;     // True if attack (action) on Unit triggers CounterAction
    public bool HeadingRight;     // True if Unit is heading right
    public bool PlayedInTurn;     // Check Unit has already played in current Turn

    public bool? IsWaiting = null;   // Mark Unit when Wait Action selected for current Turn
    public bool? IsDefending = null; // Mark Unit when Defend Action selected for current Turn

    // for now removed [public bool hasCombineAttack]

    // Attack List
    public List<AttackStruc> attacks = new List<AttackStruc>();
    public AttackStruc attack = new AttackStruc();

    // Skills
    public List<Skills> UnitSkills;    // List of Unit Skills
    public List<string> skills;        // List of Unit Skills (for in-game instantiation)

    // Game Relations
    public Cell Cell;                  // Cell where Unit currenly stand
    public Vector2 CoordsUnit;         // Current coordinates (Vector2 from 1,1) of Unit
    private Cell AttackFrom;           // Cell from where melee attack is executed

    public List<Impact> UnitImpacts;   // List of all UnitImpacts on current Unit 
    public Impacts ImpactHandle;       // Class for processing Unit UnitImpacts
    public List<Effects> EffectsOwned; // All Effects triggered by current Unit

    public List<NewUnit> unitAllowed;  // Allowed Targets (suppress standard determination)

    // UI
    public List<Arrow> Directions;     // 'Arrow' colliders triggering Melee Attack
    public Text PadText;               // UI Text to show potential Damage value 
    public Image PadIcons;             // UI Image to show potential Damage icon 

    // References to Main Game classes (not capitalized for as not 'Unit' owned)
    public ControllerGame controller;  // Game Controller
    public CellGrid cellGrid;          // Game Grid
    public CombatLog combatLog;        // Game Log

    // TO BE REMOVED
    public string skillMessage; 

    //------------------------------------------
    // EVENTS

    public event EventHandler UnitDown;    // triggered when Unit is clicked
    public event EventHandler UnitEnter;   // triggered when Unit is highlighted
    public event EventHandler UnitExit;    // triggered when Unit is de-highlighted

    //------------------------------------------
    // METHODS - MOUSE ACTIONS

    //------------------------------------------
    // OnMouseDown - Unit

    protected void OnMouseDown()
    {
        if (IsTarget == true) // Must be target of currently selected action
        {
            UnitDown.Invoke(this, new EventArgs());
        }
    }

    //------------------------------------------
    // OnMousEnter - Unit

    protected void OnMouseEnter() 
    {
        UnitEnter.Invoke(this, new EventArgs()); // Get & show Unit data
    }

    //------------------------------------------
    // OnMouseExit - Unit

    protected void OnMouseExit()
    {
        UnitExit.Invoke(this, new EventArgs()); // Hide Unit data (switch to Cell/Team data)
    }

    //------------------------------------------
    // OnMousEnter - Attack Arrow

    public void ShowPathImg(string direction)
    {
        var dest = CoordsUnit;

        switch (direction)
        {
            case "Up":
                dest += new Vector2(0, 1);
                break;
            case "Right":
                dest += new Vector2(1, 0);
                break;
            case "Left":
                dest += new Vector2(-1, 0);
                break;
            case "Down":
                dest += new Vector2(0, -1);
                break;
        }

        // Mark path to Cell from where possible Attack will be executed
        AttackFrom = cellGrid.Cells.FindLast(n => n.CellCoords == dest);
        cellGrid.MarkPathCurr(AttackFrom);     
    }

    //------------------------------------------
    // OnMousExit - Attack Arrow

    public void RemovePath()
    {
        RemovePathSP();     
    }

    // Attack Arrow leave - singleplayer
    public void RemovePathSP()
    {
        cellGrid.CellsPossible = cellGrid.Cells.FindAll(n => n.IsHighlightable == true);
        foreach (var cell in cellGrid.CellsPossible)
        {
            cell.MarkAsReachable();
        }
    }

    //------------------------------------------
    // OnMouseDown - Attack Arrow

    public void MoveAndAttack()
    {  
        // Process movement to 'AttackFrom' Cell & Attack
        if (cellGrid.Path.Count > 0)
        {
            controller.targetUnit = this;
            cellGrid.Move(AttackFrom, cellGrid.Path, controller.PlayingUnit);

        } else
        {
            controller.ProcessAttack(controller.PlayingUnit, this); // only Attack
        }
       
        // Deactivate 'Attack Arrows'
        for (int i = 0; i < 4; i++)
        {
            Directions[i].gameObject.SetActive(false);
        }
    }

    //------------------------------------------
    // METHODS - GAME ACTIONS

    //------------------------------------------
    // Game Flow mechanics

    // Method for processing action at the end of each move

    public virtual void UpdateAtEndMove() {} // never used as base call 

    // Method for update Unit at start of each Turn (base)

    public virtual void UpdateAtStartTurn()
    {
        ActionsPointsAct = ActionPoints;   // Restore Action Points
        ImpactHandle.ProcessUnitImpacts(); // Process UnitImpacts
        if (IsDefending == true)           // Unit has defended last Turn (set Damage penalty)
        {
            Damage -= (int)(OriginalValues.Damage * 0.2f); // Set Damage penalization
            IsDefending = false;                           // => set Defend Mode to 0 at end of Turn 
        }
    }

    // Method for update waiting Unit at start of Turn

    public virtual void UpdateAtStartWait()
    {
        IsWaiting = false;                             // Set 'IsWaiting' to be removed at end of Turn
        ActionsPointsAct -= 2;                         // Set Action Points penalization
        Damage -= (int)(OriginalValues.Damage * 0.1f); // Set Damage penalization
        ImpactHandle.ProcessWaitingUnit();             // Process UnitImpacts (exceptions only)
    }

    // Method for update Unit at end of each Turn (base)

    public virtual void UpdateAtEndTurn()
    {
        // Process Wait action
        if (IsWaiting == null) 
        {
            PlayedInTurn = true;            // Mark Unit with 'played in Turn' indicator
            ImpactHandle.RemoveOnEndTurn(); // Remove expired UnitImpacts with 'OnEndTurn' processing
        }
        else if (IsWaiting == false)
        {
            PlayedInTurn = true;                           // Mark Unit with 'played in Turn' indicator
            IsWaiting = null;                              // Reset 'IsWaiting' indicator
            Damage += (int)(OriginalValues.Damage * 0.1f); // Cancel Damage penalization
        }

        // Process Defend action
        if (IsDefending == false) 
        {
            Damage += (int)(OriginalValues.Damage * 0.2f); // Cancel Damage penalization
            IsDefending = null;                            // Reset Defend Mode
        }

        // Deactivate Skills (only persistent Skills remains activated)
        foreach (var skill in UnitSkills)
        {
            if (skill.IsPassive == false && skill.IsPersistent == false)
            {
                skill.IsActivated = false;
            }
        }

        ActionMode = 0;      // Reset mode to standard
        unitAllowed.Clear(); // Clear Target restrictions   
        IsImmobile = false;  // Clear movement restrictions 

        // Clear Attack Directions and reset cursor
        foreach (var unit in controller.UnitsAll)
        {
            foreach (var button in unit.Directions)
            {
                button.gameObject.tag = "NotAccess";
            }
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // ---------------------------------------------------------
    // Game Flow mechanics - Death processing

    // Quick check Unit is still alive

    public void IsAliveCheck(bool onTurn)
    {
        if (HealthAct <= 0)
        {
            UpdateAfterDeath(onTurn);
        }
    }

    // Method for update Game when Unit dies

    public virtual void UpdateAfterDeath(bool OnTurn)
    {
        // Handle UnitImpacts
        foreach (var impact in UnitImpacts.FindAll(n => n.OnDeathAction == true))
        {
            impact.trigger.TargetDied(this);
        }

        // Handle Effects 
        if (EffectsOwned != null)
        {
            var nonPersistent = EffectsOwned.FindAll(n => n.isPersistent == false);
            for (int i = 0; i < nonPersistent.Count; i++)
            {
                nonPersistent[i].cell.CellEffects.Remove(nonPersistent[i]);
                var unit = nonPersistent[i].cell.unitCurr;
                if (unit != null)
                {
                    unit.cellGrid.ApplyCellEffects(unit, 0); // remove Effect
                }
            }
        }

        // Handle Game Flow
        combatLog.LogDeath(UnitName + " is dead."); // Log Death
        Cell.IsTaken = false;                       // Cell not Taken any more
        controller.UnitsAll.Remove(this);           // Remove Unit form list of all Units
        controller.WaitingUnits.Remove(this);       // Remove Unit form list of waiting Units
        if (PlayerNumber == 0)                      // Remove Unit form list of current Team
        {
            controller.UnitsPlayer0.Remove(this);
        } else
        {
            controller.UnitsPlayer1.Remove(this);
        }

        // Destroy entire game object (or just send transform away?)
        Destroy(this.gameObject);

        if (OnTurn == true) // If Unit was on Turn, shift to next
        {
            controller.ShiftToNext();
        }
    }

    // Method for processing special action when Target of Impact died

    public virtual void TargetDied(NewUnit unit) { } // never used as base call 

    //------------------------------------------
    // Attack processing

    // Basic method for Attack processing (FALSE: simulate & show, TRUE: real damage & log)

    public virtual void Attack(NewUnit attacker, NewUnit defender, bool doDamage)
    {
        int damage = 0;

        // Calculate basic Damage
        if (defender.IsImmune != true)
        {
            float coeficient = CalculateAttack(attacker, defender);
            damage = Mathf.RoundToInt(coeficient * attacker.Damage);
        } else
        {
            damage = defender.CalculateReduced(attacker, defender, false);
        }

        // Check if Attack is rear Attack
        if (attacker.RangeTo == 1)
        {
            if (RearAttack(attacker, defender) == true)
            {
                Mathf.RoundToInt(damage * 1.3f); // TEST VALUE
            }
        }
        // Check if Target 'is Defending'
        if (defender.IsDefending == true)
        {
            damage -= (int)(damage * 0.2);
        }

        if (doDamage == true) 
        {
            // Substract Health (defender)
            defender.HealthAct -= damage;
            combatLog.LogAttack(attacker.UnitName + " caused  " + damage + " damage to " + defender.UnitName + "."); // Log Attack
            attacker.ActionsPointsAct -= attacker.AttackPrice; // Substract Action Points (attacker)

            // CounterAction (defender) 
            if (defender.DoCounterAct == true)
            {
                defender.CounterAction(defender);
            }

            // Is alive check (defender)
            defender.IsAliveCheck(false);
        }
        else
        {
            // Show Damage done if Unit entered
            defender.ShowDamage(damage, CursorSetter.melee_spr);
        }
    }

    // Method for calculation of coeficient for final Damage (standard Attack)

    public float CalculateAttack(NewUnit attacker, NewUnit defender)
    {
        float koef = 0; // initial coeficient

        attack.AttackMark = "PhysicalDefence";
        attack.AttackValue = attacker.PhysicalAttack;
        attacks.Add(attack);
        attack.AttackMark = "MagicalDefence";
        attack.AttackValue = attacker.MagicalAttack;
        attacks.Add(attack);
        attack.AttackMark = "SpecialDefence";
        attack.AttackValue = attacker.SpecialAttack;
        attacks.Add(attack);

        attacks = attacks.OrderByDescending(n => n.AttackValue).ToList();

        if (attacks[0].AttackValue > 0) // Primary Attack type
        {
            int defenceValue = (int)defender.GetType().GetField(attacks[0].AttackMark).GetValue(defender);
            koef = koef + (attacks[0].AttackValue - defenceValue);
        }

        if (attacks[1].AttackValue > 0) // Secondary Attack type
        {
            int defenceValue = (int)defender.GetType().GetField(attacks[1].AttackMark).GetValue(defender);
            koef = koef + ((attacks[1].AttackValue - defenceValue) / 2);
        }

        koef = 1 + (koef / 100);
        attacks.Clear();
        return koef;
    }

    //------------------------------------------
    // Attack - processing exceptions

    // Method for damage calculation if Target is immune (always run as override)

    public virtual int CalculateReduced(NewUnit attacker, NewUnit defender, bool handle) // never used as base call 
    {
        var reduced = 0; 
        return reduced;
    }

    // Method to check if Attack is rear Attack

    public bool RearAttack(NewUnit attacker, NewUnit defender)
    {
        var rear = false;

        if (defender.HeadingRight) // check if defender on [1,0] Coords
        {
            if (defender.CoordsUnit.x - attacker.CoordsUnit.x == -1 && defender.CoordsUnit.y - attacker.CoordsUnit.y == 0)
            {
                rear = true;
            }
        }
        else // check if defender on [-1,0] Coords
        {
            if (defender.CoordsUnit.x - attacker.CoordsUnit.x == 1 && defender.CoordsUnit.y - attacker.CoordsUnit.y == 0)
            {
                rear = true;
            }
        }
        return rear;
    }

    // Method for processing CounterAction (always run as override)

    public virtual void CounterAction(NewUnit trigger) {}

    // Method for showing Damage on highlighted Unit

    public void ShowDamage(int damage, Sprite image)
    {
        // Show Damage
        PadText.gameObject.SetActive(true);
        PadText.text = damage.ToString();

        // Show Damge Type Icon
        PadIcons.gameObject.SetActive(true);
        PadIcons.sprite = image;

        controller.damageList.Add(this);

    }

    //------------------------------------------
    // Skills processing (all overrides)

    public virtual void ActivateSkill1() {} // Activate Skill 1, if exists
    public virtual void ActivateSkill2() {} // Activate Skill 2, if exists
    public virtual void ActivateSkill3() {} // Activate Skill 3, if exists

    // Skill execution handled separately for each Unit

    public virtual void ExecuteSkill(NewUnit receiver, Cell targetCell = null) {}

    // Skill selection handled separately for each Unit, update same for all Units

    public virtual void ExecuteSelection(string selection) 
    {
        // Call updates
        cellGrid.GetPathsAll(this);                                // Refresh paths
        controller.GetPossibleTargets(this);                       // Refresh targets
        controller.UpdateSkillBar(this, 0, "", this.skillMessage); // Update Skill Bar
    }

    // Method for processing special action when Target Unit Impact expired

    public virtual void TargetImpactExpired(NewUnit unit) { } // never used as base call 

    // Method for direct applying of Effect (always run as override)

    public virtual void DirectEffectApply(NewUnit unit) { } // never used as base call 

    //------------------------------------------

    // Method for 'draw' Attack (USED FOR TESTS)

    public void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.1f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer line = myLine.GetComponent<LineRenderer>();
        //line.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
        line.startColor = color;
        line.endColor = color;
        line.startWidth = 0.2f;
        line.endWidth = 0.2f;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }
}

