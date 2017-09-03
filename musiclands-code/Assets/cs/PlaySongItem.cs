using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySongItem : MonoBehaviour {
  public GameObject boombox;
  
  BasicRadioCtl brctl;
  
  public void PlaySong(){
    SongItemData sid;
    
    GameObject root =  transform.parent.parent.gameObject;
    
    sid = root.GetComponent<SongItemData>();
    
    MusicPoint mp = new MusicPoint();
    mp.id = sid.id;
    mp.meta = sid.meta;
    mp.gpmId = sid.gpmId;
    
    
    StartCoroutine(brctl.PlaySong(mp, root));
  }
  
  void Start(){
    brctl = (BasicRadioCtl) boombox.GetComponent(typeof(BasicRadioCtl));
  }
  
}
