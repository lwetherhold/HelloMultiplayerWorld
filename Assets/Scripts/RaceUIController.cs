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

    // fields for UI Toolkit elements (labels, buttons, etc.)
    private VisualElement root; // root visual element
    private Button hostButton;
    private Button clientButton;
    private Button serverButton;
    private Label statusLabel;
    private Label scoreLabel;
    //private Label roundSummaryLabel; // UNUSED
    //private Label scoreSummaryLabel; // UNUSED

    // keep local horse picks visible for a short delay after selecting
    private float localPickHideDelaySeconds = 5f; // delay duration in seconds
    private float localPickTimestamp = -1f; // timestamp of the local pick (used to track delay)

    // called when the component is enabled
    void OnEnable()
    {
        // get the UI document component
        var uiDocument = GetComponent<UIDocument>();
        // get the root visual element from the UI document
        root = uiDocument.rootVisualElement;
        // clear the root visual element
        root.Clear();
        // clear horse button references if UI gets rebuilt
        horseButtons.Clear();

        // create elements (labels) for status and score
        statusLabel = new Label("Race status here");
        scoreLabel = new Label("Score here");

        // add elements (labels) for status and score to the root visual element
        root.Add(statusLabel);
        root.Add(scoreLabel);

        // create button for hosting a game
        hostButton = CreateButton("HostButton", "Host");
        // create button for connecting as a client
        clientButton = CreateButton("ClientButton", "Client");
        // create button for starting a server
        serverButton = CreateButton("ServerButton", "Server");

        // add event listeners to the buttons
        hostButton.clicked += OnHostButtonClicked; // on host button clicked, start hosting
        clientButton.clicked += OnClientButtonClicked; // on client button clicked, start client
        serverButton.clicked += OnServerButtonClicked; // on server button clicked, start server

        // add buttons for hosting, client, and server to the root visual element
        root.Add(hostButton);
        root.Add(clientButton);
        root.Add(serverButton);

        // create a compact grid container for horse buttons
        var horseGrid = new VisualElement();
        // set the style of the horse grid
        horseGrid.style.flexDirection = FlexDirection.Row;
        horseGrid.style.flexWrap = Wrap.Wrap;
        horseGrid.style.width = 520;
        // add the horse grid to the root visual element
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
                if (raceGameManager == null || !IsConnected()) return; // if the race game manager is not set or not connected, return
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

            // make it look like clicking just the emoji (transparent background, no borders)
            pickButton.style.backgroundColor = new Color(0, 0, 0, 0); // transparent background
            pickButton.style.borderTopWidth = 0;
            pickButton.style.borderBottomWidth = 0;
            pickButton.style.borderLeftWidth = 0;
            pickButton.style.borderRightWidth = 0;
            pickButton.style.fontSize = 28; // default font size

            // add button to the horse grid visual element
            horseGrid.Add(pickButton);

            // NOTE: replaced with horse grid above
            // add button to the root visual element
            //root.Add(pickButton);

            // add the pick button to the list of horse buttons
            // this is used to update the horse emoji state and race action buttons
            horseButtons.Add(pickButton);
        }

        // NOTE: for reset button, changed from local variable to field
        // create button for next race
        nextRaceButton = new Button(() =>
        {
            // reset the game if the race game manager is set
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

    // helper method to update the horse emoji state and race action buttons
    void UpdateHorseEmojiState()
    {
        // check if the race game manager is set and connected
        if (raceGameManager == null || !IsConnected()) return; // if the race game manager is not set or not connected, return

        // initialize the selected horse index to -1 (no horse selected)
        int selected = -1;
        // check if the local player is the host or server
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            // get the player 1 pick
            selected = raceGameManager.player1Pick.Value;
        }
        // check if the local player is the client
        else if (NetworkManager.Singleton.IsClient)
        {
            // get the player 2 pick
            selected = raceGameManager.player2Pick.Value;
        }

        // update the horse emoji state for each horse button
        for (int i = 0; i < horseButtons.Count; i++)
        {
            // update the horse emoji state for the current horse button
            // if the current horse index is the selected horse index, display the horse head emoji
            // otherwise, display the horse running with jockey emoji
            // the horse head emoji (🐴) is for the selected horse
            // and the horse running with jockey emoji (🏇) is for the other horses
            horseButtons[i].text = (i == selected) ? $"🐴 {i + 1}" : $"🏇 {i + 1}";
            // the font size is increased to make the emoji larger for the selected horse as feedback
            horseButtons[i].style.fontSize = (i == selected) ? 36 : 28;
        }
    }

    // helper method to set the state of the race buttons (horse pick buttons + next race button)
    // hide pick buttons + next race until connected
    // pick buttons only show during picking, next race button only shows after finishing
    void UpdateRaceActionButtons()
    {
        // check if the race game manager is set and connected
        if (raceGameManager == null || !IsConnected()) // if the race game manager is not set or not connected, return
        {
            // hide all horse buttons
            foreach (var horseButton in horseButtons) horseButton.style.display = DisplayStyle.None;
            // hide the next race button
            if (nextRaceButton != null) nextRaceButton.style.display = DisplayStyle.None;
            return;
        }

        // get the current race state       
        int state = raceGameManager.raceState.Value;

        // check if the race is in the waiting for picks state
        bool isPicking = state == (int)RaceGameManager.RaceState.WaitingForPicks;
        // check if the race is finished
        bool isFinished = state == (int)RaceGameManager.RaceState.Finished;

        // check if the local player has picked a horse
        // hide horse buttons when local player already picked
        bool localPicked = false;
        // check if the local player is the host or server
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            // check if player 1 has picked a horse
            localPicked = raceGameManager.player1Pick.Value != -1;
        }
        // check if the local player is the client
        else if (NetworkManager.Singleton.IsClient) 
        {
            // check if player 2 has picked a horse
            localPicked = raceGameManager.player2Pick.Value != -1;
        }

        // start local pick timer after player selects a horse
        if (isPicking && localPicked && localPickTimestamp < 0f) // if the race is in picking state and the local player has picked a horse and the local pick timer is not started, start the local pick timer
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

        // update the display state of the horse buttons
        foreach (var horseButton in horseButtons)
        {
            // show all horse buttons when picking and the local player has not picked a horse or the local pick timer is active
            // hide all horse buttons when not picking or the local player has picked a horse and the local pick timer is not active
            horseButton.style.display = (isPicking && (!localPicked || localDelayActive)) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // update the display state of the next race button
        if (nextRaceButton != null) // if the next race button is set
        {
            // show next race button when the race is finished
            // hide next race button when the race is not finished
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

    // called when the component is disabled
    void OnDisable()
    {
        // remove event listeners from the buttons
        if (hostButton != null) hostButton.clicked -= OnHostButtonClicked; // on host button clicked, stop hosting
        if (clientButton != null) clientButton.clicked -= OnClientButtonClicked; // on client button clicked, stop client
        if (serverButton != null) serverButton.clicked -= OnServerButtonClicked; // on server button clicked, stop server
    }

    // called when the host button is clicked
    void OnHostButtonClicked() => NetworkManager.Singleton.StartHost();

    // called when the client button is clicked
    void OnClientButtonClicked() => NetworkManager.Singleton.StartClient();

    // called when the server button is clicked
    void OnServerButtonClicked() => NetworkManager.Singleton.StartServer();

    // called when the component is updated
    private void Update()
    {
        UpdateUI(); // update the UI
        UpdateHorseEmojiState(); // update the horse emoji state

        // if the race game manager is not set or not connected, return
        if (raceGameManager == null || !IsConnected())
        {
            return;
        }

        UpdateStatusLabels(); // update the status labels

        // update the round summary label
        statusLabel.text += $"\n{raceGameManager.GetRoundSummary()}";
        // update the score summary label
        scoreLabel.text = raceGameManager.GetScoreSummary();
    }

    // helper method to update the UI
    private void UpdateUI()
    {
        // check if the network manager is set
        if (NetworkManager.Singleton == null) // if the network manager is not set, return
        {
            // hide the start buttons
            SetStartButtons(false);
            SetStatusText("NetworkManager not found");
            return;
        }

        // check if the network manager is connected
        if (!IsConnected())
        {
            // show the start buttons
            SetStartButtons(true);
            SetStatusText("Not connected");
            UpdateRaceActionButtons(); // hide race buttons when not connected
        }
        else
        {
            // hide the start buttons
            SetStartButtons(false);
            UpdateStatusLabels(); // update the status labels
            UpdateRaceActionButtons(); // show race buttons when connected
        }
    }

    // check if the network manager is connected
    private bool IsConnected()
    {
        // check if the network manager is set and the network manager is connected
        return NetworkManager.Singleton != null &&
               (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer); // if the network manager is set and the network manager is connected, return true
    }

    // helper method to set the state of the start buttons
    void SetStartButtons(bool state)
    {
        // get the display state of the start buttons
        var display = state ? DisplayStyle.Flex : DisplayStyle.None;

        // set the display state of the start buttons if they are set
        if (hostButton != null) hostButton.style.display = display;
        if (clientButton != null) clientButton.style.display = display;
        if (serverButton != null) serverButton.style.display = display;
    }

    // set the text of the status label
    void SetStatusText(string text) => statusLabel.text = text;

    // update the status labels
    void UpdateStatusLabels()
    {
        // get the mode of the network manager
        var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        // get the transport of the network manager
        string transport = "Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name;

        // get the mode text
        string modeText = "Mode: " + mode;

        // set the text of the status label
        SetStatusText($"{transport}\n{modeText}");
    }

    // create a button with a name and text
    private Button CreateButton(string name, string text)
    {
        // create a new button
        var button = new Button();

        // set the attributes of the button
        button.name = name;
        button.text = text;
        button.style.width = 240;
        button.style.backgroundColor = Color.white;
        button.style.color = Color.black;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;

        // return the button
        return button;
    }
}