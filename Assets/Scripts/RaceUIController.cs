using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class RaceUIController : MonoBehaviour
{
    // reference to the race game manager
    [SerializeField] private RaceGameManager raceGameManager;

    // fields used for hide horse pick buttons + next race until connected
    private readonly System.Collections.Generic.List<Button> horseButtons = new System.Collections.Generic.List<Button>();
    private Button nextRaceButton;

    // fields for UI Toolkit elements
    private VisualElement root;
    private Button hostButton;
    private Button clientButton;
    private Button serverButton;
    private Label statusLabel;
    private Label scoreLabel;
    private Label roundSummaryLabel;
    private Label scoreSummaryLabel;
    // keep local horse picks visible for a short delay after selecting
    private float localPickHideDelaySeconds = 5f;
    private float localPickTimestamp = -1f;

    // called when the component is enabled
    void OnEnable()
    {
        // get the UI document component
        var uiDocument = GetComponent<UIDocument>();
        // get the root visual element from the UI document
        root = uiDocument.rootVisualElement;
        // clear the root visual element
        root.Clear();
        // clear horse button references if ui gets rebuilt
        horseButtons.Clear();

        // create elements (labels)
        statusLabel = new Label("Race status here");
        scoreLabel = new Label("Score here");

        // add elements (labels) to the root visual element
        root.Add(statusLabel);
        root.Add(scoreLabel);

        // create button for hosting a game
        hostButton = CreateButton("HostButton", "Host");
        // create button for connecting as a client
        clientButton = CreateButton("ClientButton", "Client");
        // create button for starting a server
        serverButton = CreateButton("ServerButton", "Server");

        hostButton.clicked += OnHostButtonClicked;
        clientButton.clicked += OnClientButtonClicked;
        serverButton.clicked += OnServerButtonClicked;

        // add buttons to the root visual element
        root.Add(hostButton);
        root.Add(clientButton);
        root.Add(serverButton);

        // create a compact grid container for horse buttons
        var horseGrid = new VisualElement();
        horseGrid.style.flexDirection = FlexDirection.Row;
        horseGrid.style.flexWrap = Wrap.Wrap;
        horseGrid.style.width = 520;
        root.Add(horseGrid);

        // create buttons for each horse
        for (int i = 0; i < 8; i++)
        {
            // add button for picking a horse
            int horseIndex = i; // local copy of the index
            var pickButton = new Button(() =>
            {
                // horse pick should only work AFTER a network role is started (Host, Client, Server)
                // needs a gate for pick click so nothing happens when not connected

                // submit pick to the server
                if (raceGameManager == null || !IsConnected()) return;
                raceGameManager.SubmitPickServerRpc(horseIndex);

                // NOTE: gate added above, replacing below
                //if (raceGameManager != null) raceGameManager.SubmitPickServerRpc(horseIndex);
            })
            {
                // set the text of the button
                text = $"🏇 {horseIndex + 1}"
            };

            // compact per-button tile sizing
            pickButton.style.width = 120;
            pickButton.style.height = 70;
            pickButton.style.marginBottom = 6;
            pickButton.style.marginRight = 6;

            // make it look like clicking just the emoji
            pickButton.style.backgroundColor = new Color(0, 0, 0, 0);
            pickButton.style.borderTopWidth = 0;
            pickButton.style.borderBottomWidth = 0;
            pickButton.style.borderLeftWidth = 0;
            pickButton.style.borderRightWidth = 0;
            pickButton.style.fontSize = 28;

            // add button to the horse grid visual element
            horseGrid.Add(pickButton);

            // NOTE: replaced with horse grid above
            // add button to the root visual element
            //root.Add(pickButton);

            horseButtons.Add(pickButton);
        }

        // NOTE: for reset button, changed from local variable to field
        // create button for next race
        nextRaceButton = new Button(() =>
        {
            // reset the game
            if (raceGameManager != null) raceGameManager.ResetRaceServerRpc();
        })
        {
            // set the text of the button
            text = "Next Race"
        };

        // add button to the root visual element
        root.Add(nextRaceButton);

        // NOTE: reset race button replaced with next race button
        /*
        // create button for next race
        var resetButton = new Button(() =>
        {
            // reset the game
            if (raceGameManager != null) raceGameManager.ResetRaceServerRpc();//ResetGameServerRpc(); oops wrong method name
        })
        {
            // set the text of the button
            text = "Next Race"
        };

        // add button to the root visual element
        root.Add(resetButton);
        */

        // NOTE: replaced single button (hardcoded pick button) with 8 buttons loop above
        /*
        // add button for picking first horse
        var pickHorse0Button = new Button(() =>
        {
            // submit pick to the server
            if (raceGameManager != null) raceGameManager.SubmitPickServerRpc(0);
        })
        {
            // set the text of the button
            text = "Pick Horse 1"
        };

        // add button to the root visual element
        root.Add(pickHorse0Button);
        */
    }

    void UpdateHorseEmojiState()
    {
        if (raceGameManager == null || !IsConnected()) return;

        int selected = -1;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            selected = raceGameManager.player1Pick.Value;
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            selected = raceGameManager.player2Pick.Value;
        }

        for (int i = 0; i < horseButtons.Count; i++)
        {
            horseButtons[i].text = (i == selected) ? $"🐴 {i + 1}" : $"🏇 {i + 1}";
            horseButtons[i].style.fontSize = (i == selected) ? 36 : 28;
        }
    }

    // set the state of the race buttons (horse pick buttons + next race button)
    // hide pick buttons + next race until connected
    // pick buttons only show during picking, next race button only shows after finishing
    void UpdateRaceActionButtons()
    {
        if (raceGameManager == null || !IsConnected())
        {
            foreach (var horseButton in horseButtons) horseButton.style.display = DisplayStyle.None;
            if (nextRaceButton != null) nextRaceButton.style.display = DisplayStyle.None;
            return;
        }

        int state = raceGameManager.raceState.Value;
        bool isPicking = state == (int)RaceGameManager.RaceState.WaitingForPicks;
        bool isFinished = state == (int)RaceGameManager.RaceState.Finished;

        // check if the local player has picked a horse
        // hide horse buttons when local player already picked
        bool localPicked = false;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            localPicked = raceGameManager.player1Pick.Value != -1;
        }
        else if (NetworkManager.Singleton.IsClient) 
        {
            localPicked = raceGameManager.player2Pick.Value != -1;
        }

        // start local pick timer after player selects a horse
        if (isPicking && localPicked && localPickTimestamp < 0f)
        {
            localPickTimestamp = Time.time;
        }
        // reset local pick timer when local player has no pick
        if (!localPicked)
        {
            localPickTimestamp = -1f;
        }
        // reset local pick timer when race is not in picking state
        if (!isPicking)
        {
            localPickTimestamp = -1f;
        }

        // keep horse buttons visible for 5 seconds after local pick
        bool localDelayActive = localPicked &&
                                localPickTimestamp >= 0f &&
                                (Time.time - localPickTimestamp) < localPickHideDelaySeconds;

        foreach (var horseButton in horseButtons)
        {
            // show all horse buttons when picking
            // hide all horse buttons when not picking
            horseButton.style.display = (isPicking && (!localPicked || localDelayActive)) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (nextRaceButton != null)
        {
            // show next race button when finished
            // hide next race button when not finished
            nextRaceButton.style.display = isFinished ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    
    // NOTE: replaced with UpdateRaceActionButtons() above
    /*
    void SetRaceButtons(bool state)
    {
        var display = state ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (var horseButton in horseButtons) horseButton.style.display = display;
        if (nextRaceButton != null) nextRaceButton.style.display = display;
    }
    */

    void OnDisable()
    {
        if (hostButton != null) hostButton.clicked -= OnHostButtonClicked;
        if (clientButton != null) clientButton.clicked -= OnClientButtonClicked;
        if (serverButton != null) serverButton.clicked -= OnServerButtonClicked;
    }

    void OnHostButtonClicked() => NetworkManager.Singleton.StartHost();

    void OnClientButtonClicked() => NetworkManager.Singleton.StartClient();

    void OnServerButtonClicked() => NetworkManager.Singleton.StartServer();

    private void Update()
    {
        UpdateUI();
        UpdateHorseEmojiState();

        // if the race game manager is not set or not connected, return
        if (raceGameManager == null || !IsConnected())
        {
            return;
        }

        UpdateStatusLabels();
        // update the status label
        statusLabel.text += $"\n{raceGameManager.GetRoundSummary()}";
        // update the score label
        scoreLabel.text = raceGameManager.GetScoreSummary();
    }

    private void UpdateUI()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStartButtons(false);
            SetStatusText("NetworkManager not found");
            return;
        }

        if (!IsConnected())
        {
            SetStartButtons(true);
            SetStatusText("Not connected");
            UpdateRaceActionButtons(); // hide race buttons when not connected
        }
        else
        {
            SetStartButtons(false);
            UpdateStatusLabels();
            UpdateRaceActionButtons(); // show race buttons when connected
        }
    }

    private bool IsConnected()
    {
        return NetworkManager.Singleton != null &&
               (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);
    }

    void SetStartButtons(bool state)
    {
        var display = state ? DisplayStyle.Flex : DisplayStyle.None;
        if (hostButton != null) hostButton.style.display = display;
        if (clientButton != null) clientButton.style.display = display;
        if (serverButton != null) serverButton.style.display = display;
    }

    void SetStatusText(string text) => statusLabel.text = text;

    void UpdateStatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";
        string transport = "Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name;
        string modeText = "Mode: " + mode;
        SetStatusText($"{transport}\n{modeText}");
    }

    private Button CreateButton(string name, string text)
    {
        var button = new Button();
        button.name = name;
        button.text = text;
        button.style.width = 240;
        button.style.backgroundColor = Color.white;
        button.style.color = Color.black;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        return button;
    }
}