using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Menus.MainMenu;
using Game.MinigameFramework.Scripts;
using Game.MinigameFramework.Scripts.Framework.Input;
using Game.MinigameFramework.Scripts.Framework.PlayerInfo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Game.MinigameFramework.Menus.PromptWindow;

public class MenuScript : MonoBehaviour {
    [SerializeField] Button startButton;
    [SerializeField] private Button packSelectButton;
    [SerializeField] RectTransform packageIcons;
    public Image packageIconPrefab;
    public SceneField packSelectScene;

    private PromptWindow _promptWindow;

    // Enforce Player Count replaced by context from MinigamePacks listed under MinigameManager
    // [SerializeField] bool enforcePlayerCount = true;
    
    public void NextMinigame() {
        MinigameManager.instance.PopulateMinigameList(); // populate list right before the rounds start
        MinigameManager.instance.GoToMinigameSelectScene();
    }
    
    public void ChangePacks() {
        SceneManager.LoadScene(packSelectScene.SceneName);
    }

    private void OnEnable() {
        PlayerManager.onPlayerConnected.AddListener(OnPlayerConnected);
        PlayerManager.onPlayerDisconnected.AddListener(OnPlayerDisconnected);
    }
    private void OnDisable() {
        PlayerManager.onPlayerConnected.RemoveListener(OnPlayerConnected);
        PlayerManager.onPlayerDisconnected.RemoveListener(OnPlayerDisconnected);
    }

    private void Start() {
        PlayerManager.SetMenuActionMap();
        PlayerJoinManager.onAllPlayersJoined.AddListener(OnAllPlayersConnected);
        
        foreach(Player player in PlayerManager.players) {
            if(player.playerInput!=null) {
                OnPlayerConnected(player.playerIndex);
            }
        }

        if (PlayerManager.AreAllPlayersConnected()) {
            OnAllPlayersConnected();
        }

        UpdateActivePackIcons();
    }

    public void CreatePromptWindow() {
        if(_promptWindow!=null)return;
        _promptWindow = Instantiate(PromptWindow.GetPrefab());
        _promptWindow.SetText("START GAME?");
        _promptWindow.onConfirm.AddListener(NextMinigame);
        _promptWindow.onCancel.AddListener(OnAllPlayersConnected);
    }
    public void CreateExitWindow() {
        if(_promptWindow!=null)return;
        _promptWindow = Instantiate(PromptWindow.GetPrefab());
        _promptWindow.SetText("QUIT GAME?");
        _promptWindow.onConfirm.AddListener(Application.Quit);
        _promptWindow.onCancel.AddListener(OnAllPlayersConnected);
    }


    // unlocks button as needed, called when player connects or disconnects
    public void CheckUnlockButton() {
        startButton.interactable = PlayerManager.AreAllPlayersConnected();
        if (startButton.interactable) {
            PlayerManager.SetSelectedGameObject(startButton.gameObject);
        }
        else {
            PlayerManager.SetSelectedGameObject(packSelectButton.gameObject);
        }
    }
    
    [SerializeField] List<MenuPlayerSlot> playerSlots = new();
    
    public void OnPlayerConnected(int playerIndex) {
        playerSlots[playerIndex].SetStatus(true);
        playerSlots[playerIndex].BindToPlayer(playerIndex);
        CheckUnlockButton();
    }
    public void OnPlayerDisconnected(int playerIndex) {
        playerSlots[playerIndex].SetStatus(false);
        playerSlots[playerIndex].UnBindFromPlayer();
        CheckUnlockButton();
        if (_promptWindow != null) {
            Destroy(_promptWindow.gameObject);
            PlayerManager.SetSelectedGameObject(startButton.gameObject);
        }
    }

    private void OnAllPlayersConnected() {
        PlayerManager.SetSelectedGameObject(startButton.gameObject);
    }

    private void UpdateActivePackIcons() {
        foreach (Sprite spr in MinigameManager.instance.GetPackageSprites()) {
            Image icon = Instantiate(packageIconPrefab, packageIcons);
            icon.sprite = spr;
        }
    }
}
