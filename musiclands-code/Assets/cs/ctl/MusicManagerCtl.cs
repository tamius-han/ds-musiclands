using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManagerCtl : MonoBehaviour {

  public GameObject player;
  public GameObject menuCam;
  

  void Update () {
    
    // close music preview
    if(Input.GetKeyDown(KeyCode.Escape)){
      menuCam.SetActive(false);
      player.SetActive(true);
      
      Cursor.visible = false;
      Screen.lockCursor = true;
      
      gameObject.SetActive(false);
    }
  }
}
