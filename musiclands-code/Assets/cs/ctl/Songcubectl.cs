using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Songcubectl : MonoBehaviour {

  public GameObject pressToInteract;
  
  
  void OnTriggerEnter(){
    pressToInteract.SetActive(true);
    print("triggered!");
  }
  
  void OnTriggerExit(){
    pressToInteract.SetActive(false);
    print("untriggered!");
  }
  
}
