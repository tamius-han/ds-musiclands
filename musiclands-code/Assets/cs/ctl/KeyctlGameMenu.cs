using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyctlGameMenu : MonoBehaviour {

  public GameObject player;
  public GameObject ui;
  public GameObject menuCam;
  
  public void HideMenu(){
    player.SetActive(true);
    menuCam.SetActive(false);
    
    Cursor.visible = false;
    Screen.lockCursor = true;
    
    ui.SetActive(! GlobalData.bottomLevel);
    gameObject.SetActive(false);
  }
  
  void Update(){
    if(Input.GetKeyDown(KeyCode.Escape)){
      HideMenu();
    }
  }
}
