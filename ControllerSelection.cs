using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Selection mode controller class

public class ControllerSelection : MonoBehaviour
{
    //-----------------------------
    // ATTRIBUTES
 
    public bool SelectionDone = false;       // If true, stop processing Update 
    public bool IsReadyPlr0;                 // If true, switch on Player 1
    public bool IsReadyPlr1;                 // If true, start Game

    private float Timer = 0.0f;              // Selection Timer
    private int TimeLeft;                    // If equals to 0, trigger 'Switch Player'
    private int PrevIndex;                   // Previously 'Gripped Unit' index

    // Game Objects
    public Transform Player0;                // Player 0 Unit's parent Transform
    public Transform Player1;                // Player 1 Unit's parent Transform

    public Transform TokenParent;            // Tokens parent Transform
    public GameObject UnitToken;             // Unit Token
    public UnitTokenHS GripedUnit;           // Currenly focused Token
    private UnitGameData UnitData;           // Unit Game data

    private List<UnitGameData> UnitsPlayer0; // Player 0 Unit's init. data (position, default Skill)
    private List<UnitGameData> UnitsPlayer1; // Player 1 Unit's init. data (position, default Skill)

    public List<UnitTokenHS> UnitTokens0;    // Player 0 Tokens
    public List<UnitTokenHS> UnitTokens1;    // Player 1 Tokens
    public List<UnitTokenHS> UnitTokensAll;  // All Tokens List

    // UI
    public Text Counter;                     // Shows remaining time for selection (Text)
    public Image TimerImage;                 // Shows remaining time for selection (Image)
    public List<Button> ButtonsUnit0;        // Player 0 character selection Button's 
    public List<Button> ButtonsUnit1;        // Player 1 character selection Button's
    public List<Button> SwitchSkills0;       // Player 0 switch Skills selection Button's 
    public List<Button> SwitchSkills1;       // Player 1 switch Skills selection Button's 

    public Transform SelBarPlayer0;          // Player 0 Selection Bar
    public Transform SelBarPlayer1;          // Player 1 Selection Bar

    public Transform StartPanel;             // Selection counter panel
    public Transform MainPanel;              // Main Game panel

    public List<Text> UnitName;              // Unit names

    // Game Controllers
    public ControllerGame ControllerGame;    // Game Controller class
    public CellGrid GameGrid;                // Cell Grid class - Game mode

    // Selection Controllers
    public CellGridSelection SelectionGrid;  // Cell Grid class - selection mode

    //-----------------------------
    // METHODS

    // Selection mode initialization

    void Start()
    {
        var mask = 1 << 9;
        Camera.main.cullingMask = ~mask; // show only Pl.0 Tokens

        UnitTokens0 = new List<UnitTokenHS>();
        UnitTokens1 = new List<UnitTokenHS>();

        ButtonsUnit0[0].GetComponent<Image>().color = new Color(255, 255, 0); // mark fisrt Unit from list

        for (int i = 0; i < 5; i++) // Instantiate Player 0 Tokens
        {
            var tokenPath = "Prefabs/Tokens/" + StaticGameData.UnitsPlayer0[i].Name + "TK";
            UnitToken = Resources.Load(tokenPath) as GameObject;
            GameObject Token = Instantiate(UnitToken);
            Token.transform.position = new Vector3(0, (i * 2), -0.1f);
            Token.layer = 8;
            Token.transform.parent = TokenParent;
            var unitToken = Token.GetComponent<UnitTokenHS>();
            Token.GetComponent<SpriteRenderer>().sprite = unitToken.Figures[0];
            unitToken.index = i;
            unitToken.PlayerNumber = 0;
            unitToken.Coords = new Vector2(1, (i * 2) + 1);
            unitToken.CurrentCell = SelectionGrid.Cells.Find(n => n.CellCoords == unitToken.Coords);
            UnitTokens0.Add(unitToken);
            UnitTokensAll.Add(unitToken);

            ButtonsUnit0[i].GetComponent<Image>().sprite = unitToken.Icons[0];

            if (i == 0) { GripUnit(0); } // Set defauled Griped Unit             
        }

        for (int i = 0; i < 5; i++) // Instantiate Player 1 Tokens
        {
            var tokenPath = "Prefabs/Tokens/" + StaticGameData.UnitsPlayer1[i].Name + "TK";
            UnitToken = Resources.Load(tokenPath) as GameObject;
            GameObject Token = Instantiate(UnitToken);
            Token.transform.position = new Vector3(12.06f, (i * 2), -0.1f);
            Token.layer = 9;
            Token.transform.parent = TokenParent;
            var unitToken = Token.GetComponent<UnitTokenHS>();
            Token.GetComponent<SpriteRenderer>().sprite = unitToken.Figures[1];
            unitToken.index = i;
            unitToken.PlayerNumber = 1;
            unitToken.Coords = new Vector2(13, (i * 2) + 1);
            unitToken.CurrentCell = SelectionGrid.Cells.Find(n => n.CellCoords == unitToken.Coords);
            UnitTokens1.Add(unitToken);
            UnitTokensAll.Add(unitToken);

            ButtonsUnit1[i].GetComponent<Image>().sprite = unitToken.Icons[1];
        }
    }

    // Check for selection state & update Timer

    void Update()
    {
        if (SelectionDone == false)
        {
            Timer += Time.deltaTime;
            TimeLeft = 30 - Mathf.RoundToInt(Timer);
            TimerImage.fillAmount = 1 - (Timer * 0.0333f);
            if (TimeLeft == 0) // shift selection state
            {
                if (IsReadyPlr0 == false)
                {
                    SwitchPlayer(); // switch on Player 1
                }
                else
                {
                    SelectionDone = true;
                    StartGame();
                }
            }
            if (IsReadyPlr0 == true && IsReadyPlr1 == true) // start Game
            {
                SelectionDone = true;
                StartGame();
            }
        }
    }

    // Mark Token as 'current one' for placing & Skill picking

    public void GripUnit(int index)
    {
        if (IsReadyPlr0 == false)
        {
            GripedUnit = UnitTokens0.Find(n => n.index == index);
            ButtonsUnit0[PrevIndex].GetComponent<Image>().color = new Color(255, 255, 255);
            PrevIndex = index;
            ButtonsUnit0[index].GetComponent<Image>().color = new Color(255, 255, 0);
            UnitName[0].text = GripedUnit.UnitName;

            for (int a = 0; a < 2; a++) // handle switch Skill selection, if relevant
            {
                if (GripedUnit.Skills.Count > 0)
                {
                    SwitchSkills0[a].gameObject.SetActive(true);
                    SwitchSkills0[a].GetComponent<Image>().sprite = GripedUnit.Skills[a];
                    if (GripedUnit.DefaultSwitch == true) 
                    {
                        ResetColors(0);
                    }
                    else
                    {
                        ResetColors(1);
                    }
                }
                else
                {
                    SwitchSkills0[a].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            GripedUnit = UnitTokens0.Find(n => n.index == index);
            ButtonsUnit1[PrevIndex].GetComponent<Image>().color = new Color(255, 255, 255);
            PrevIndex = index;
            ButtonsUnit1[index].GetComponent<Image>().color = new Color(255, 255, 0);
            UnitName[0].text = GripedUnit.UnitName;

            for (int a = 0; a < 2; a++)
            {
                if (GripedUnit.Skills.Count > 0)
                {
                    SwitchSkills1[a].gameObject.SetActive(true);
                    SwitchSkills1[a].GetComponent<Image>().sprite = GripedUnit.Skills[a];
                    if (GripedUnit.DefaultSwitch == true)
                    {
                        ResetColors(0);
                    }
                    else
                    {
                        ResetColors(1);
                    }
                }
                else
                {
                    SwitchSkills1[a].gameObject.SetActive(false);
                }
            }
        }
    }

    // For 'switch' Skill, mark selected form

    public void SetSwitch(int index)
    {
        if (index == 0)
        {
            GripedUnit.DefaultSwitch = true;
            ResetColors(0);
        }
        else
        {
            GripedUnit.DefaultSwitch = false;
            ResetColors(1);
        }
    }

    // Set color mark for Skill switch

    private void ResetColors(int index)
    {
        SwitchSkills0[index].GetComponent<Image>().color = new Color(0, 255, 0);
        SwitchSkills0[Mathf.Abs(index - 1)].GetComponent<Image>().color = new Color(255, 255, 255);
    }

    // Player confirmed 'ready' state manually

    public void PlayerReady()
    {
        SwitchPlayer();
    }

    // Switch to Player 1

    private void SwitchPlayer()
    {
        var mask = 1 << 8; // show only Pl.1 Tokens
        Camera.main.cullingMask = ~mask;

        TimeLeft = 30;
        if (IsReadyPlr0 == false)
        {
            SelBarPlayer0.gameObject.SetActive(false); // De-activate Player 0 sel. Bar
            SelBarPlayer1.gameObject.SetActive(true);  // Activate Player 1 sel. Bar
            IsReadyPlr0 = true;                        // Set Player 0 ready
            GripedUnit = UnitTokens1[0];               // Set Gripped Unit
            GripedUnit.IsGripped = true;

            // mark fisrt Unit from list
            ButtonsUnit0[0].GetComponent<Image>().color = new Color(255, 255, 0); 
        }
        else
        {
            IsReadyPlr1 = true;
        }
    }

    // Prepare Unit Lists and start Game

    private void StartGame()
    {
        PrepareUnitData(); // Create Unit's data from Tokens
        DestroyObjects();  // Destroy Tokens
        SetGameUI();       // Set UI to 'Game View'
        CreateGameUnits(); // Create Playing Units
        RunTurnOne();      // Run Game
    }

    // Prepare Game data for Unit's instantiation

    private void PrepareUnitData()
    {
        UnitsPlayer0 = new List<UnitGameData>();
        UnitsPlayer1 = new List<UnitGameData>();

        for (int i = 0; i < 5; i++)
        {
            UnitData.Name = UnitTokens0[i].UnitName;
            UnitData.Position = UnitTokens0[i].Position;
            UnitData.DefaultSwitch = UnitTokens0[i].DefaultSwitch;
            UnitsPlayer0.Add(UnitData);

            UnitData.Name = UnitTokens1[i].UnitName;
            UnitData.Position = UnitTokens1[i].Position;
            UnitData.DefaultSwitch = UnitTokens1[i].DefaultSwitch;
            UnitsPlayer1.Add(UnitData);
        }
    }

    // Destroy Tokens

    private void DestroyObjects()
    {
        for (int i = 9; i >= 0; i--)
        {
            var token = UnitTokensAll[i];
            UnitTokensAll.Remove(token);
            Destroy(token.gameObject);
        }
    }

    // Set Game UI

    private void SetGameUI()
    {
        StartPanel.gameObject.SetActive(false);
        MainPanel.gameObject.SetActive(true);
    }

    // Game Unit's preparation

    private void CreateGameUnits()
    {
        for (int i = 0; i < 5; i++) // Create Units Player 0
        {
            // Create path to get Unit Prefab & load it
            var path = "Prefabs/Units/" + StaticGameData.FractionPlayer0 +
                "/" + UnitsPlayer0[i].Name + "";
            var prefab = Resources.Load(path) as GameObject;
            GameObject unit = Instantiate(prefab);

            unit.layer = 8; // Player 0 Unit Layer
            unit.transform.position = UnitsPlayer0[i].Position; // Unit position

            var newUnit = unit.GetComponent<NewUnit>();
            newUnit.CoordsUnit = new Vector2(Mathf.RoundToInt(UnitsPlayer0[i].Position.x - 1),
                Mathf.RoundToInt(UnitsPlayer0[i].Position.y - 1)); // Unit 'Coordinates'

            newUnit.PlayerNumber = 0;                 // Unit Player number
            newUnit.HeadingRight = true;              // Where Unit is heading to
            newUnit.UnitImpacts = new List<Impact>(); // Initialize UnitImpacts
            newUnit.ImpactHandle = new Impacts        // Initialize Impact Class
            {
                unit = newUnit,
            };

            // probably must stay - reactivate Object to make collider working
            unit.SetActive(false);
            unit.SetActive(true);

            // Skills (load and assign Prefab)
            for (int x = 0; x < newUnit.skills.Count; x++)
            {
                var skillPath = "Prefabs/Skills/" + StaticGameData.FractionPlayer0 + "/" + newUnit.skills[x];
                var skillPfb = Resources.Load(skillPath) as GameObject;
                GameObject Skill = Instantiate(skillPfb);

                var skill = Skill.GetComponentInChildren<Skills>();
                if (UnitsPlayer0[i].DefaultSwitch == false)
                    skill.IsDefault = false;

                newUnit.UnitSkills[x] = skill;
            }

            // Animator - TBU
            var animator = unit.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetBool("headRight", true);
                animator.SetFloat("moveX", 1.0f);
            }
            unit.transform.parent = Player0; // assign Parent             
        }
    
        for (int i = 0; i < 5; i++) // Create Units Player 1
        {
            // Create path to get Unit Prefab & load it
            var path = "Prefabs/Units/" + StaticGameData.FractionPlayer1 +
                "/" + UnitsPlayer1[i].Name + "";
            var prefab = Resources.Load(path) as GameObject;
            GameObject unit = Instantiate(prefab);

            unit.layer = 9; // Player 0 Unit Layer
            unit.transform.position = UnitsPlayer1[i].Position; // Unit position

            var newUnit = unit.GetComponent<NewUnit>();
            newUnit.CoordsUnit = new Vector2(Mathf.RoundToInt(UnitsPlayer1[i].Position.x - 1),
                Mathf.RoundToInt(UnitsPlayer1[i].Position.y - 1)); // Unit 'Coordinates'

            newUnit.PlayerNumber = 1;                 // Unit Player number
            newUnit.HeadingRight = false;             // Where Unit is heading to
            newUnit.UnitImpacts = new List<Impact>(); // Initialize UnitImpacts
            newUnit.ImpactHandle = new Impacts        // Initialize Impact Class
            {
                unit = newUnit,
            };

            // probably must stay - reactivate Object to make collider working
            unit.SetActive(false);
            unit.SetActive(true);

            // Skills (load and assign Prefab)
            for (int x = 0; x < newUnit.skills.Count; x++)
            {
                var skillPath = "Prefabs/Skills/" + StaticGameData.FractionPlayer1 + "/" + newUnit.skills[x];
                var skillPfb = Resources.Load(skillPath) as GameObject;
                GameObject Skill = Instantiate(skillPfb);

                var skill = Skill.GetComponentInChildren<Skills>();
                if (UnitsPlayer1[i].DefaultSwitch == false)
                    skill.IsDefault = false;

                newUnit.UnitSkills[x] = skill;
            }

            // Animator 
            var animator = unit.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetBool("headRight", false);
                animator.SetFloat("moveX", -1.0f);
            }
            unit.transform.parent = Player1; // assign Parent             
        }
    }

    // Deactivate 'selection mode' objects and start Game Controller

    private void RunTurnOne()
    {
        ControllerGame.gameObject.SetActive(true);
        GameGrid.gameObject.SetActive(true);
        Destroy(SelectionGrid.gameObject);
        Destroy(this);
    }

    //-----------------------------
    // UI methods

    // Set Start Button 'still' Image

    public void SetStill(Image button)
    {
        button.GetComponent<Image>().sprite = ImagePicker.univ_still;
    }

    // Set Start Button 'ready' Image

    public void SetReady(Image button)
    {
        button.GetComponent<Image>().sprite = ImagePicker.univ_ready;
    }
}
