using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
[RequireComponent(typeof(Camera))]

public class PlayerController : MonoBehaviour {

  public static float MOUSE_SENS;
  
  private static float lastMousePosX = 0.0f;
  private static float lastMousePosY = 0.0f;
  private static float curMousePosX = 0.0f;
  private static float curMousePosY = 0.0f;
  
  private static float viewx = 0.0f;
  private static float viewy = 0.0f;
  
  private static bool flying = false;
  private static float speed = 3.0f;
  private static float flySpeed = 10.0f;
  private static float walkSpeed = 3.0f;
  
  static Camera mainCam;
  
//   public static void SetPosition(float x, float z){
//     mainCam.transform.position = new Vector3(x, 100.0f, z);
//   }
//   
//   public static void SetPosition(float x, float y, float z){
//     mainCam.transform.position = new Vector3(x, y, z);
//   }
  
  public static void SetFly(bool wannafly){
    flying = wannafly;
    speed = flying ? flySpeed : walkSpeed;
  }
  
  // Use this for initialization
  void Start () {
    MOUSE_SENS = 1.0f;
    
//     mainCam = Camera.mainCamera;
  }
  
  // Update is called once per frame
  void Update () {
    var x = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
    var z = Input.GetAxis("Vertical") * Time.deltaTime * speed;
    
    curMousePosX = Input.GetAxis("Mouse X");
    curMousePosY = Input.GetAxis("Mouse Y");
    
    viewy -= curMousePosX * MOUSE_SENS;
    viewx += curMousePosY * MOUSE_SENS;
    
    transform.eulerAngles = new Vector3(viewx, viewy, 0.0f);
    if(flying){
      transform.Translate(x, 0, z);
    }
    else{
      transform.position += new Vector3(x, 0.0f, z);
    }
  }
}
