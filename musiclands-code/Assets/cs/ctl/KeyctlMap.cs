using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyctlMap : MonoBehaviour {

  public GameObject player;
  public GameObject mapCam;
  public GameObject ui;
  
  // Update is called once per frame
  
  void Update () {
    // close the map
    if(Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape)){
      
      Cursor.visible = false;
      Screen.lockCursor = true;
      
      mapCam.SetActive(false);
      player.SetActive(true);
      
      if(! GlobalData.bottomLevel)
        ui.SetActive(true);
    }
  }
}
