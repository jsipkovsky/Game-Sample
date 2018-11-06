using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// MAIN CONTROLLER CLASS

public class ControllerGame : MonoBehaviour
{
    //----------------------------------
    // ATTRIBUTES

    // Game State
    private bool IsStateConsistent = false; // No unfinished action
    public bool HighUnitLocked;             // If true, set lock on High.Unit Bar
    public int Turn;                        // Game Turn
    private bool NewTurnAction;             // If true, new Turn Action is required

    public int GlobalResource0;             // Global resource Player 0
    public int GlobalResource1;             // Global resource Player 1

    // Transforms
    public Transform Player0;         // Player 0 Units Parent
    public Transform Player1;         // Player 1 Units Parent
    public Transform CellsParent;     // Cells Parent

    // Units & Cells
    public NewUnit PlayingUnit;        // Unit marked as 'Playing'
    public NewUnit HighlightedUnit;    // Unit marked as 'Highlighted'
    public NewUnit TargetUnit;         // Unit which is targed for current Attack Action 

    public List<NewUnit> UnitsAll;     // All Units
    public List<NewUnit> WaitingUnits; // Units marked as 'IsWaiting' in current Turn
    public List<NewUnit> UnitsPlayer0; // Player 0 Units
    public List<NewUnit> UnitsPlayer1; // Player 1 Units
    private List<NewUnit> Enemies;     // List for storing current living Enemies
    private List<NewUnit> Friends;     // List for storing current living Friends
    public List<NewUnit> DamageList;   // All Unit's to be possibly damaged

    private Cell CellCheked;           // Attack directions
    public List<Cell> Cells;           // All Cells on Game Grid

    // UI
    public Text PlayerName0;           // Player 0 Name
    public Text PlayerName1;           // Player 1 Name
    public Text SkillText;             // Information about concrete Skill // TBR
    public Text SkillMessage;          // Message for current action, cleared after reading (3) // TBR
    public Text TurnText;              // UI for Turn
    public Text FractionProgText;      // Showing Unit's Fraction Progress (value)

    public Image TurnSlider;           // UI for Timer
    public Image FractionProgress;     // Showing Unit's Fraction Progress (fill %)

    public Button ShiftUnit;           // End Turn (for current unit)
    public List<GameObject> SkillButtons; // Skill Buttons

    // Game parameters
    float Timer = 0.0f;                // Counts real Game time
    int gameSeconds;                   // Actual Timer value in seconds

    public int CurrentAPMax;           // TBU
    private int layerMask = 1 << 0;    // Mask for Cells (used reverted)
    private int layerMaskArr = 1 << 1; // Mask for Arrows (used reverted)

    public int SkillRange;             // Range where Skill can be applied
    public float ProgressGrowth;       // Current increase of Fraction progress

    // Game Relations
    public CellGrid CellParent;           // Game Grid class
    public CombatLog combatLog;           // Combat Log class
    public List<AttrOrig> AttributesOrig; // Original Attribute List class

    //----------------------------------
    // METHODS

    // Prepare Unit Lists, Set Unit's to default states, start Game

    void Start()
    {
        UnitsPlayer0 = new List<NewUnit>();          // Player 0 Unit List
        UnitsPlayer1 = new List<NewUnit>();          // Player 1 Unit List
        UnitsAll = new List<NewUnit>();              // All Unit List
        WaitingUnits = new List<NewUnit>();          // Units marked as 'IsWaiting' in current Turn
        Enemies = new List<NewUnit>();               // All living Enemies
        Friends = new List<NewUnit>();               // All living Friends
        DamageList = new List<NewUnit>();            //

        AttributesOrig = new List<AttrOrig>();       // Store all original Unit's attributes

        // Get all Cells
        for (int i = 0; i < CellsParent.childCount; i++)
        {
            Cell cell = CellsParent.GetChild(i).gameObject.GetComponent<Cell>();
            cell.IsHighlightable = false;
            Cells.Add(cell);
        }

        // Prepare Unit's for Game
        PrepareUnitList();

        // Create Cell Effects for relevant Units & set switch Skills
        foreach (var unit in UnitsAll)
        {
            if (unit.ActionAtMoveEnd != 0)
            {
                unit.UpdateAtEndMove(); // Set Cell Effects caused by Unit
            }
            var switchNotDef = unit.UnitSkills.Find(n => n.IsSwitch == true && n.IsDefault == false);
            if (switchNotDef != null)
            {
                // set second form TBU
            }
        }

        // Set Turn    
        Turn = 1;
        TurnText.text = Turn.ToString();
        IsStateConsistent = true;
        Timer = 0.0f;
        ShiftToNextChecked();
    }

    // Game state update

    void Update()
    {
        if (IsStateConsistent == true)
        {
            // Turn Timer
            Timer += Time.deltaTime;
            gameSeconds = 30 - Mathf.RoundToInt(Timer);
            TurnSlider.fillAmount = 1 - (Timer * .033f);
            if (gameSeconds <= 10) 
            {
                TurnSlider.color = Color.red; // mark last seconds red
            }
            else
            {
                TurnSlider.color = Color.blue;
            }

            if (gameSeconds == 0) // force Unit shift
            {
                ShiftToNext();             // Switch next Unit to Playing Unit
                Timer = 0;                 // Reset Timer
                TurnSlider.fillAmount = 1; // Reset Time slider
            }
        }

        if (Input.GetKeyDown("s")) // 'Shift' shortcut
            ShiftToNext();

        if (Input.GetKeyDown("d")) // 'Kill' shortcut (TEST)
        {
            PlayingUnit.HealthAct = 0;
            PlayingUnit.IsAliveCheck(true);
        }
    }

    //--------------------------------------------------------
    // GAME FLOW (Unit & Turn shifting)

    // Wait for finished movement & switch to next Unit

    public void ShiftToNext()
    {
        ShiftUnit.interactable = false;       // deactivate Button for 'Unit Shift'
        StartCoroutine(WaitAllActionsDone()); // wait until all actions are finished
        Timer = 0;                            // reset Timer
    }

    // Wait with further Actions until unfinished movement is done

    IEnumerator WaitAllActionsDone()
    {
        IsStateConsistent = false;
        yield return new WaitUntil(() => PlayingUnit.IsMoving == false);
        IsStateConsistent = true;
        ShiftToNextChecked(); // switch on next Unit
    }

    // Handle Unit shifting (Game Flow) , see *1)

    public void ShiftToNextChecked()
    {
        if (PlayingUnit != null) // Process Turn end Actions for current Playing Unit
        {
            PlayingUnit.UpdateAtEndTurn();  // updates at the end of each Turn
            PlayingUnit.Cell.MarkAsTaken(); // 'un-highlight' previous Unit
        }

        // Check for if unprocessed Unit exists
        var nextUnit = UnitsAll.Find(n => n.IsWaiting == null && n.PlayedInTurn == false);
        if (nextUnit != null)
        {
            SetPlayingUnit(nextUnit, false); // Shift Unit
        }
        else // Check for if unprocessed waiting Unit exists
        {
            var waitingUnit = WaitingUnits.Find(n => n.PlayedInTurn == false);
            if (waitingUnit != null)
            {
                SetPlayingUnit(waitingUnit, true); // Shift (waiting) Unit
            }
            else
            {
                SetNextTurn();                      // Shift Turn
                SetPlayingUnit(UnitsAll[0], false); // Shift Unit
            }
        }       
    }

    // Method to set Unit as Playing Unit

    private void SetPlayingUnit(NewUnit playingUnit, bool isWaiting)
    {
        PlayingUnit = playingUnit;
        combatLog.LogStandard(PlayingUnit.UnitName + " is on the move."); // Log Turn start
        if (isWaiting == false)
        {
            PlayingUnit.UpdateAtStartTurn(); // update UnitImpacts at start Turn
        } else
        {
            PlayingUnit.UpdateAtStartWait(); // update filtred UnitImpacts
        }

        CurrentAPMax = PlayingUnit.ActionsPointsAct;
        PlayingUnit.Cell.MarkAsHighlighted();
        CellParent.PlayingUnit = PlayingUnit;
        CellParent.GetPathsAll(PlayingUnit);
        GetPossibleTargets(PlayingUnit);

        //UI
        LoadSkillButtons(PlayingUnit); // Load Skill Buttons according to number of Playing Unit Skills
        UpdateSkillBar(PlayingUnit, 0, "", "");
        ShiftUnit.interactable = true; // activate Button for 'Unit Shift'
    }

    // Wait Action

    public void Wait()
    {
        PlayingUnit.IsWaiting = true;
        WaitingUnits.Insert(0, PlayingUnit);
        ShiftToNext();
    }

    // Defend Action

    public void Defend()
    {
        PlayingUnit.IsDefending = true;
        ShiftToNext();
    }

    // Shift Turn & process Turn start updates

    private void SetNextTurn()
    {
        ReSort();                       // Resort Units by Iniciative
        RefreshMarks();                 // Uncheck 'Wait' & 'Defend' indicators
        Turn = Turn + 1;                // Shift Turn
        TurnText.text = Turn.ToString();
    }

    // Re-sort by Iniciative

    private void ReSort()
    {
        List<NewUnit> unsorted = UnitsAll;
        UnitsAll = unsorted.OrderByDescending(n => n.Iniciative).ToList();
    }

    // Method for un-marking Unit indicators (HasPlayed, IsWaiting)

    private void RefreshMarks()
    {
        for (int i = 0; i < UnitsAll.Count; i++)
        {
            UnitsAll[i].PlayedInTurn = false;
            UnitsAll[i].IsWaiting = null;
        }

        WaitingUnits.Clear(); // clear waiting Units List
    }

    //--------------------------------------------------------
    // GAME PLAY

    // Update possible targets for Playing Unit

    public void GetPossibleTargets(NewUnit playingUnit)
    {
        foreach (var unit in UnitsAll) // reset All Units 
        {
            unit.IsTarget = false;
        }

        // Get 'Enemies' and 'Friends' according to Player Number
        if (playingUnit.unitAllowed.Count == 0)
        {
            if (playingUnit.PlayerNumber.Equals(0))
            {
                Enemies = UnitsPlayer1;
                Friends = UnitsPlayer0;
            }
            else
            {
                Enemies = UnitsPlayer0;
                Friends = UnitsPlayer1;
            }
        }

        // Determine Targets (according to Action Mode)
        switch (playingUnit.ActionMode)
        {
            case 0: // STANDARD

                if (PlayingUnit.ActionsPointsAct >= PlayingUnit.AttackPrice)
                {
                    foreach (var enemy in Enemies)
                    {
                        if (playingUnit.RangeTo > 1) // Ranged Attack 
                        {
                            var playerPos = playingUnit.transform.localPosition; // Playing Unit position
                            var enemyPos = enemy.transform.localPosition;        // Current Enemy position

                            // Calculate distance
                            var pos_current = new Vector2(Mathf.RoundToInt(playerPos.x), Mathf.RoundToInt(playerPos.y));
                            var pos_enemy = new Vector2(Mathf.RoundToInt(enemyPos.x), Mathf.RoundToInt(enemyPos.y));
                            float dist_units = Vector2.Distance(pos_current, pos_enemy);
                            Vector2 dist_vectors = pos_current - pos_enemy;
                            //Debug.DrawRay(pos_current, -(dist_vectors), Color.yellow, 30.0f); 

                            if (dist_units >= playingUnit.RangeFrom && dist_units <= playingUnit.RangeTo) // check if Enemy is in range
                            {
                                // Check if Emeny is covered by another object
                                RaycastHit2D collision2D = Physics2D.Raycast(pos_current, -(dist_vectors), (dist_units - 1), ~layerMask);
                                if (collision2D.collider != null) { enemy.IsTarget = true; }  // mark Enemy as Target                    
                            }
                        }
                        else // Melee Attack
                        {
                            // Get possible Cells
                            var cells_dest = Cells.FindAll(n => n.IsHighlightable == true && n.IsTaken == false);
                            cells_dest.Add(PlayingUnit.Cell);

                            for (int x = 0; x < 2; x++)
                            {
                                for (int y = 0; y < 2; y++)
                                {
                                    // Check in possible Cells for concrete neighbour of Enemy
                                    CellCheked = cells_dest.FindLast(n => n.CellCoords == enemy.CoordsUnit + new Vector2(y - x, 1 - x - y));

                                    enemy.Directions[(2 * x) + y].IsAccess = false; // default: not aceess

                                    if (CellCheked != null)
                                    {
                                        // Check if possible for Playing Unit to get there and Attack
                                        if (CellCheked.PriceActual + playingUnit.AttackPrice <= playingUnit.ActionsPointsAct)
                                        {
                                            enemy.Directions[(2 * x) + y].IsAccess = true;
                                            enemy.IsTarget = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                break;

            case 1: // SKILLS - Offensive

                foreach (var enemy in Enemies)
                {
                    // Calculate Vector distance of Coords
                    float dist_units = Vector2.Distance(enemy.CoordsUnit, PlayingUnit.CoordsUnit);
                    if (dist_units <= SkillRange)
                    {
                        // do some check Skill is already applied ??
                        enemy.IsTarget = true;
                    }
                    else
                    {
                        enemy.IsTarget = false;
                    }
                }
                break;

            case 2: // SKILLS - Defensive 

                foreach (var friend in Friends)
                {
                    // do some check Skill is already applied ??
                    var skill = PlayingUnit.UnitSkills.Find(n => n.IsActivated == true && n.IsPassive == false);
                    var applied = friend.UnitImpacts.Find(n => n.name == skill.skillName);

                    if (applied.name == null) { friend.IsTarget = true; } // mark Friend as Target
                }
                break;

            case 3: // SKILLS - No Tagret

                foreach (var unit in UnitsAll)
                {
                    unit.IsTarget = false;
                }
                break;

            case 5: // SKILLS - Cell Range

                foreach (var enemy in Enemies)
                {
                    if (enemy.Cell.PriceActual <= 999) // TBU
                    {
                        enemy.IsTarget = true;
                    }
                }
                break;

            case 6: // SKILLS - 'Issue a Challenge'

                foreach (var unit in UnitsAll)
                {
                    unit.IsTarget = false;
                }
                PlayingUnit.UnitImpacts.FindLast(n => n.name == "Issue a Challenge").trigger.IsTarget = true;

                break;
        }
    }

    //--------------------------------------------------------
    // MOUSE EVENTS

    //  On Mouse Down (Unit clicked)

    private void OnUnitDown(object sender, EventArgs e)
    {
        NewUnit receiver = sender as NewUnit;
        AnimateDamage(); // before execution, animate Damage info

        if (PlayingUnit.ActionMode == 0 && PlayingUnit.RangeTo > 1) // AM 0, 'ranged'
        {
            PlayingUnit.Attack(PlayingUnit, receiver, true); // Process Attack      
            CellParent.GetPathsAll(PlayingUnit);             // Get possible Paths
            GetPossibleTargets(PlayingUnit);                 // Get possible Targets
            UpdateSkillBar(PlayingUnit, 0, "", "");          // Update Skill Info Bar
        }
        else // AM != 0 (Skills)
        {
            PlayingUnit.ExecuteSkill(receiver);     // Process Attack
            CellParent.GetPathsAll(PlayingUnit);    // Get possible Paths
            GetPossibleTargets(PlayingUnit);        // Get possible Targets
            UpdateSkillBar(PlayingUnit, 0, "", ""); // Update Skill Info Bar
        }
    }

    // Process standard attack (Attacking vs Defending Unit interaction)

    public void ProcessAttack(NewUnit attacker, NewUnit defender)
    {
        PlayingUnit.Attack(attacker, defender, true); // Process Attack
        CellParent.GetPathsAll(PlayingUnit);          // Get possible Paths
        GetPossibleTargets(PlayingUnit);              // Get possible Targets
        UpdateSkillBar(PlayingUnit, 0, "", "");       // Update Skill Info Bar
    }

    //  On Mouse Enter (Unit highlighted)

    public void OnUnitEnter(object sender, EventArgs e)
    {
        HighlightedUnit = sender as NewUnit;
        if (HighlightedUnit.IsTarget == true)
        {
            SetCursor(PlayingUnit.ActionMode); // set cursor

            // Simulate Attack to get possible Damage values
            PlayingUnit.Attack(PlayingUnit, HighlightedUnit, false);

            // For melee Attack, show possible directions
            if (PlayingUnit.ActionMode == 0 && PlayingUnit.IsMoving == false)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (HighlightedUnit.Directions[i].IsAccess == true)
                    {
                        HighlightedUnit.Directions[i].gameObject.SetActive(true);
                    }
                }
            }
        }
        else
        {
            SetCursor(-1); // 'non-target'
        }
    }

    // On Mouse Exit (Unit highlighted)

    public void OnUnitExit(object sender, EventArgs e)
    {
        // Check if mouse now on direction Arrow or not
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Camera.main.ScreenToViewportPoint(Input.mousePosition);
        var hit = Physics2D.GetRayIntersection(ray, 100f, layerMaskArr);

        if (hit == false) // Unit left
        {
            if (DamageList.Count > 0) // clear Damage info UI's
                HideDamage();
  
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Reset cursor
            if (HighlightedUnit.IsTarget == true)                  // Hide direction Arrows
            {
                for (int i = 0; i < 4; i++)
                {
                    HighlightedUnit.Directions[i].gameObject.SetActive(false);
                }
            }
            if (HighUnitLocked == false) // TBU
                HighlightedUnit = null;
        }
    }

    //--------------------------------------------------------
    // SKILLS

    // Skill Bar Update (most of functionality processed within Info Bar class)

    public void UpdateSkillBar(NewUnit playingUnit, int isFirstTime, string info, string message)
    {
        for (int i = 0; i < playingUnit.UnitSkills.Count; i++)
        {
            var skill = playingUnit.UnitSkills[i];

            //SkillButtons[i].GetComponent<Button>().interactable = true;
            //SkillButtons[i].GetComponentInChildren<Image>().sprite = skill.DefaultIcon;

            //if (skill.IsPassive == true) // Passive Skills
            //{
            //    SkillButtons[i].GetComponent<Button>().interactable = false;
            //}   
          
            if (skill.IsPassive == false) // Active Skills
            {
                if (skill.IsActivated == false)
                {
                    SkillButtons[i].GetComponent<Image>().color = new Color(255, 255, 255);
                }
                else if (skill.IsActivated == true)
                {
                    SkillButtons[i].GetComponent<Image>().color = new Color(255, 255, 0);
                }
            }
        }
    }

    // Load Playing Unit Skill Buttons

    public void LoadSkillButtons(NewUnit playingUnit)
    {
        for (int i = 0; i < SkillButtons.Count; i++)
        {
            SkillButtons[i].SetActive(false);
        }

        //var unitSkills = playingUnit.UnitSkills.FindAll(n => n.IsInvariable == false);
        var unitSkills = playingUnit.UnitSkills;
        if (unitSkills.Count > 0)
        {
            for (int i = 0; i < unitSkills.Count; i++)
            {
                SkillButtons[i].SetActive(true);
                if (unitSkills[i].IsSwitch == true && unitSkills[i].IsDefault == false) // 'Switch' in secondary Form
                {
                    SkillButtons[i].GetComponentInChildren<Image>().sprite = unitSkills[i].SwitchIcon;
                }
                else { 
                   SkillButtons[i].GetComponentInChildren<Image>().sprite = unitSkills[i].DefaultIcon;
                }
                if (unitSkills[i].IsPassive == true) // Set Passive Skills not interactible
                {
                    SkillButtons[i].GetComponent<Button>().interactable = false;
                }   
            }
        }
    }

    // Skill Button 1 Handle

    public void HandleSkill1()
    {
        if (PlayingUnit != null)
        {
            PlayingUnit.ActivateSkill1();           // Execute Skill processing
            CellParent.GetPathsAll(PlayingUnit);    // Get possible Paths
            GetPossibleTargets(PlayingUnit);        // Get possible Targets
            UpdateSkillBar(PlayingUnit, 0, "", ""); // Update Skill Bar
        }
    }

    // Skill Button 2 Handle

    public void HandleSkill2()
    {
        if (PlayingUnit != null)
        {
            PlayingUnit.ActivateSkill1();           // Execute Skill processing
            CellParent.GetPathsAll(PlayingUnit);    // Get possible Paths
            GetPossibleTargets(PlayingUnit);        // Get possible Targets
            UpdateSkillBar(PlayingUnit, 0, "", ""); // Update Skill Bar
        }
    }

    // Skill Button 3 Handle

    public void HandleSkill3()
    {
        if (PlayingUnit != null)
        {
            PlayingUnit.ActivateSkill1();           // Execute Skill processing
            CellParent.GetPathsAll(PlayingUnit);    // Get possible Paths
            GetPossibleTargets(PlayingUnit);        // Get possible Targets
            UpdateSkillBar(PlayingUnit, 0, "", ""); // Update Skill Bar
        }
    }

    // Execute Skill selections (for Skills with sub-choices)

    public void SkillSelections(Button selected)
    {
        var butonInfo = selected.GetComponentInChildren<Text>().text;
        PlayingUnit.ExecuteSelection(butonInfo);
    }

    //// hide message after several (3?) seconds // 
    //IEnumerator RemoveAfterRead(string message)
    //{
    //    SkillMessage.text = message;
    //    yield return new WaitForSeconds(3);
    //    SkillMessage.text = "";
    //}


    //--------------------------------------------------------
    // GAME PREPARATION

    // Prepare Units for Game (set references, events, values)

    private void PrepareUnitList()
    {
        for (int i = 0; i < 5; i++) // add Player 0 Units to List
        {
            var unit = Player0.GetChild(i).GetComponent<NewUnit>();

            // Add events
            unit.UnitDown += OnUnitDown;
            unit.UnitEnter += OnUnitEnter;
            unit.UnitExit += OnUnitExit;

            // Create Unit & Cell relation
            unit.Cell = Cells.FindLast(n => n.CellCoords == unit.CoordsUnit);
            unit.Cell.TakenBy = unit;
            unit.Cell.IsTaken = true;

            // Game references
            unit.combatLog = FindObjectOfType<CombatLog>();
            unit.controller = this;
            unit.cellGrid = CellParent;

            // Add to lists
            CreateAttributeList(unit);
            UnitsPlayer0.Add(unit);
            UnitsAll.Add(unit);

            unit = Player1.GetChild(i).GetComponent<NewUnit>();

            // Add events
            unit.UnitDown += OnUnitDown;
            unit.UnitEnter += OnUnitEnter;
            unit.UnitExit += OnUnitExit;

            // Create Unit & Cell relation
            unit.Cell = Cells.FindLast(n => n.CellCoords == unit.CoordsUnit);
            unit.Cell.TakenBy = unit;
            unit.Cell.IsTaken = true;

            // Game references
            unit.combatLog = FindObjectOfType<CombatLog>();
            unit.controller = this;
            unit.cellGrid = CellParent;

            // Add to lists
            CreateAttributeList(unit);
            UnitsPlayer1.Add(unit);
            UnitsAll.Add(unit);
        }

        // Create List with all Units
        UnitsAll = UnitsAll.OrderByDescending(n => n.Iniciative).ToList();
    }

    // Method for adding record to Attributes List

    private void CreateAttributeList(NewUnit unit)
    {
        AttrOrig attrCurr = new AttrOrig()
        {
            PhysicalAttack = unit.PhysicalAttack,
            MagicalAttack = unit.MagicalAttack,
            SpecialAttack = unit.SpecialAttack,
            PhysicalDefence = unit.PhysicalDefence,
            MagicalDefence = unit.MagicalDefence,
            SpecialDefence = unit.SpecialDefence,
            ActionPoints = unit.ActionPoints,
            Damage = unit.Damage,
        };
        AttributesOrig.Add(attrCurr);   // Store into List (currently not used)
        unit.OriginalValues = attrCurr; // Stor values as Unit attribute
    }

    //----------------------------------
    // UI 

    // Set cursor according to Playing Unit Action Mode/Range

    private void SetCursor(int mode)
    {
        switch (mode)
        {
            case 0:
                if (PlayingUnit.RangeTo == 1) // Melee Attack Cursor
                {
                    Cursor.SetCursor(CursorSetter.melee, Vector2.zero, CursorMode.Auto);
                }
                else // Ranged Attack Cursor
                {
                    Cursor.SetCursor(CursorSetter.ranged, Vector2.zero, CursorMode.Auto);
                }
                break;
            case -1: // 'Not a Target' Cursor
                Cursor.SetCursor(CursorSetter.non_target, Vector2.zero, CursorMode.Auto);

                break;
            default: // Skill Cursor
                Cursor.SetCursor(CursorSetter.skill, Vector2.zero, CursorMode.Auto);
                break;
        }
    }

    // Clear UI showing possible Damage information

    public void HideDamage()
    {
        foreach (var line in DamageList)
        {
            line.PadText.gameObject.SetActive(false);  // Hide Damage
            line.PadIcons.gameObject.SetActive(false); // Hide Damge Type Icon
        }
        DamageList.Clear(); // clear current Damage List
    }

    // Animate & clear UI showing Damage information

    public void AnimateDamage()
    {
        foreach (var damageline in DamageList)
        {
            StartCoroutine(ImoveDamage(damageline.PadText)); // Animate 'Damage dealt'
            damageline.PadIcons.gameObject.SetActive(false); // Hide Damage info
        }
        DamageList.Clear(); // clear current damage list
    }

    // Animate Damage (process) TBU

    protected IEnumerator ImoveDamage(Text dmgLine)
    {
        dmgLine.color = Color.red;
        var dest = dmgLine.transform.position + new Vector3(0, 0.5f, 0);
        while (dmgLine.transform.position != dest)
        {
            dmgLine.transform.position = Vector3.MoveTowards(dmgLine.transform.position, dest, Time.deltaTime * 1f);
            yield return 0;
        }

        dmgLine.gameObject.SetActive(false);
        dmgLine.color = Color.black;
        dmgLine.transform.position -= new Vector3(0, 0.5f, 0);
    }

    // Draw Attack

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.1f)
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
        Destroy(myLine, duration);
    }
}

// *1)
// ----- MAIN method to evaluate game state and shift on new Unit -----
// 1. execute 'At End Turn' updates for previously playing Unit
// 2. check if all living Unit have already played this Turn => if yes, start next Turn
// 3. execute 'At Start Turn' updates for currently Playing Unit (override or not?)
// 4. execute common updates for currently Playing Unit (get paths, targets, restore AP)


