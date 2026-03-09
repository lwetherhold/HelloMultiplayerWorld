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

    // called when the component is enabled
    void OnEnable()
    {
        // get the UI document component
        var uiDocument = GetComponent<UIDocument>();
        // get the root visual element from the UI document
        root = uiDocument.rootVisualElement;
        // clear the root visual element
        root.Clear();

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
                if (raceGameManager != null || !IsConnected()) return;
                raceGameManager.SubmitPickServerRpc(horseIndex)

                // NOTE: gate added above, replacing below
                //if (raceGameManager != null) raceGameManager.SubmitPickServerRpc(horseIndex);
            })
            {
                // set the text of the button
                text = $"Pick Horse {horseIndex + 1}"
            };

            // add button to the root visual element
            root.Add(pickButton);

            horseButtons.Add(pickButton);
        }

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
        }
        else
        {
            SetStartButtons(false);
            UpdateStatusLabels();
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