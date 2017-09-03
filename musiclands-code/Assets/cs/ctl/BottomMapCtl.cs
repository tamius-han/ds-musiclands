using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomMapCtl : Mapctl {

  public Text andN;
  
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
    // time to update data on the popup.
    
    SongItemData sid = (SongItemData) gameObject.GetComponent(typeof(SongItemData));
    
    // not only we're repurposing sid, we'll also repurpose text items .
    // peak title - artist
    // song list - title
    // 
    // And n is the new one, for how many songs more.
    
    string[] metasplit = SS.SplitMeta(sid.meta);
    
    peakTitle.text = metasplit[0];
    songList.text = metasplit[1];
    andN.text = sid.gpmId;
   
    
    // show the panel
    infoPanel.SetActive(true);
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
    Cursor.visible = false;
    Screen.lockCursor = true;
  }
  
}
