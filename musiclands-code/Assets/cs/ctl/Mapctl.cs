using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class Mapctl : MonoBehaviour {
  
  public GameObject infoPanel;
  public Text peakTitle;
  public Text songList;
  
  public GameObject player;
  public GameObject mapCam;
  public GameObject ui;
  public Terrain terrain;
  
  public float PEAK_INFO_PANEL_MARGIN; // must be this far away from both the peak and the edge.
  public float PEAK_INFO_HEIGHT;
  
  public float MAP_CANVAS_WIDTH = 1280.0f;
  public float MAP_CANVAS_HEIGHT = 1024f;
  public float ANCHOR_OFFSET_X = 512f;
  public float ANCHOR_OFFSET_Y = 512f;
  
  void OnMouseEnter(){
    
    // set PeakInfoPanel next to the dot. Prefer the right side, go on the left if there's not enough
    // space between the point + offset + edge.
    
    float peak_x, peak_y, panel_w, panel_h, new_x, new_y, panel_x;
    panel_w = infoPanel.GetComponent<RectTransform>().rect.width;
    panel_h = infoPanel.GetComponent<RectTransform>().rect.height;
    
    peak_x = gameObject.transform.position.x;
    peak_y = gameObject.transform.position.z;
    
    print("panel is this wide: " + panel_w + " and this tall " + panel_h);
    
    // set X position
    if ( (panel_w + (2*PEAK_INFO_PANEL_MARGIN) + peak_x) < MAP_CANVAS_WIDTH)
      new_x = peak_x + PEAK_INFO_PANEL_MARGIN + (panel_w / 2);
    else
      new_x = peak_x - PEAK_INFO_PANEL_MARGIN - (panel_w / 2); 
    
    // set Y (Z) position somewhere acceptable (no closer to top and bottom edge than PEAK_INFO_PANEL_MARGIN)
    new_y = Mathf.Min( (MAP_CANVAS_HEIGHT - (panel_h / 2) - PEAK_INFO_PANEL_MARGIN),
                       Mathf.Max( ((panel_h / 2) + PEAK_INFO_PANEL_MARGIN),
                                  peak_y
                             )
                  );
    
    infoPanel.transform.position = new Vector3( new_x,
                                                PEAK_INFO_HEIGHT,
                                                new_y);
    
    // position has been set.
    // time to update data on the popup. We'll cheat and just use the chunk under the peak as our song source
    // as we want to display the songs that user is going to hear.
    
    int chunk_x = ((int)(peak_x))>>TerrainInit.CHUNK_LEVEL;
    int chunk_y = ((int)(peak_y))>>TerrainInit.CHUNK_LEVEL;
    
    List<MusicPoint> peakSongs = GlobalData.chunks[chunk_x,chunk_y].allSongs;
    
    // extract artist's name from the first song:
    string artist = peakSongs[0].meta.Split(new string[] {" - "}, 2, System.StringSplitOptions.None)[0];
    
    peakTitle.text = artist + "'s peak";
    
    // list up to 9 songs on the list:
    string representatives = "";
    int c9 = SS.Min(peakSongs.Count, 9);
    for(int i = 0; i < c9; i++)
      representatives += peakSongs[i].meta + "\n";
    
    songList.text = representatives;
    
    // show the panel
    infoPanel.SetActive(true);
  }
  
  void OnMouseExit(){
    infoPanel.SetActive(false);
  }

  void OnMouseDown(){
    // Find where to deposit our player
    float player_h, player_x, player_y;
    
    player_x = gameObject.transform.position.x;
    player_y = gameObject.transform.position.z;    
    player_h = terrain.SampleHeight(new Vector3(player_x, 0f, player_y)) + 2f;
    
    // Deposit player to location
    player.transform.position = new Vector3(player_x, player_h, player_y);
    
    // Re-enable player
    mapCam.SetActive(false);
    player.SetActive(true);
    ui.SetActive(true);
    Cursor.visible = false;
    Screen.lockCursor = true;
  }
  
  
}
