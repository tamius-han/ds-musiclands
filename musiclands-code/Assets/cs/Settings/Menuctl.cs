using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(GameObject))]
[RequireComponent(typeof(GameObject))]

public class Menuctl : MonoBehaviour {
  
  public GameObject mainMenuPanel;
  public GameObject settingsMenuPanel;
  
  
  public void HideAllMenus(){
    mainMenuPanel.SetActive(false);
    settingsMenuPanel.SetActive(false);
  }
  
  
  public void ShowMainMenuCanvas(){
    GameObject mainMenuCanvas = GameObject.Find("MainMenuCanvas");
    mainMenuCanvas.SetActive(true);
  }
  
  public void ShowMainMenu(){
    HideAllMenus();
    mainMenuPanel.SetActive(true);
  }
  
  public void ShowSettingsMenu(){
    HideAllMenus();
    settingsMenuPanel.SetActive(true);
  }
  
  
  public void DeleteCache(){
    
  }
  
  
  // Use this for initialization
  void Start () {
    
  }
  
  // Update is called once per frame
  void Update () {
    
  }
}
