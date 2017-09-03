using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keyctl : MonoBehaviour {
  
  public Terrain terrain;
  
  public GameObject player;
  public GameObject boombox;
  
  public GameObject playerCam;
  public GameObject mapCam;
  public GameObject menuCam;
  public GameObject ui;
  
  public GameObject gameMenu;
  
  TerrainInit ti;
  BasicRadioCtl brctl;
  
  void InitDescent(){
    float x = player.transform.position.x;
    float y = player.transform.position.z;
    
    
    // save current position before descent
    GlobalData.descentHistory_x.Add(x);
    GlobalData.descentHistory_y.Add(y);
    
    int chunk_x = ((int)x) >> TerrainInit.CHUNK_LEVEL;
    int chunk_y = ((int)y) >> TerrainInit.CHUNK_LEVEL;
    
    //we explore a 5x5 area, unless on the edge
    int xmin, xmax, ymin, ymax;
    
    xmin = SS.Max(chunk_x - 2, 0);
    xmax = SS.Min(chunk_x + 2, TerrainInit.NUMBER_OF_CHUNKS);
    ymin = SS.Max(chunk_y - 2, 0);
    ymax = SS.Min(chunk_y + 2, TerrainInit.NUMBER_OF_CHUNKS);
    
    List<MusicPoint> musicPoints = new List<MusicPoint>();
    
    for(int i = xmin; i < xmax; i++)
      for(int j = ymin; j < ymax; j++)
        musicPoints.AddRange(GlobalData.chunks[i,j].allSongs);
        
    if(musicPoints.Count < TerrainInit.MINIMAL_NUMBER_OF_SONGS_FOR_TERRAIN)
      GlobalData.bottomLevel = true;        // deny all future descents
    
    ti.Descend(musicPoints);
  }

  
  void Update () {
    if(! GlobalData.playerHasControl)
      return;
    
    
    if(Input.GetKeyDown(KeyCode.W)){
      
      if(GlobalData.bottomLevel){   // show song list
        
        player.SetActive(false);
        menuCam.SetActive(true);
        
        menuCam.transform.position = player.transform.position;
        menuCam.transform.rotation = player.transform.rotation;
        
        Cursor.visible = true;
        Screen.lockCursor = false;
        
        brctl.ShowSongList(ti.terrainTree);
      }
      else
        InitDescent();
    }
    
    if(Input.GetKeyDown(KeyCode.A)){
      GlobalData.bottomLevel = false;   
      // once we ascend, we'll no lonber be on the bottom level (and if we can't ascend, we aren't on
      // the bottom level either)
      
      if(GlobalData.terrainMagnificationLevel > 0)
        ti.Ascend();
    }
    
    if(Input.GetKeyDown(KeyCode.M)){
      // good evening my lord, helps to have a map
      // Helps to have a map.
      // 
      // https://www.youtube.com/watch?v=cZ0atMpyq9Y
      
      player.SetActive(false);
      ui.SetActive(false);
      mapCam.SetActive(true);
      
      
      Cursor.visible = true;
      Screen.lockCursor = false;
    }
    
    if(Input.GetKeyDown(KeyCode.Escape)){
      if(player.activeSelf){
        player.SetActive(false);
        menuCam.SetActive(true);
        
        menuCam.transform.position = player.transform.position;
        menuCam.transform.rotation = player.transform.rotation;
        
        Cursor.visible = true;
        Screen.lockCursor = false;
        
        ui.SetActive(false);
        gameMenu.SetActive(true);
      }
    }
    
  }
  
  void Start () {
    ti = (TerrainInit) terrain.GetComponent(typeof(TerrainInit));
    brctl = (BasicRadioCtl) boombox.GetComponent(typeof(BasicRadioCtl));
    
    
    GlobalData.descentHistory_x = new List<float>();
    GlobalData.descentHistory_y = new List<float>();
  }  
}
