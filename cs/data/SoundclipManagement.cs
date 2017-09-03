using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundclipManagement : MonoBehaviour {
  
  public void LoadWhenReady(int id, string path, AudioSource musicPlayer){
//     StartCoroutine(LoadFileWhenReady(id, path, musicPlayer));
    this.StartCoroutine(test());
  }
  
  IEnumerator test(){
    print("test?");
    
    yield return null;
  }
  
  IEnumerator LoadFileWhenReady(int id, string path, AudioSource musicPlayer) {
    
    print("coroutine started!");
    
    while( (CacheOptions.FindItemStatus(id) & CacheOptions.PLAYABLE | CacheOptions.DOWNLOADED) == 0)
      yield return null;
    
    // download finished
    
    print("download finished");
    
    WWW www = new WWW("file://" + path);
    print("loading " + path);
    
    AudioClip clip = www.GetAudioClip(false);
    while(!clip.isReadyToPlay)
      yield return www;
    
    print("done loading");
    //     clip.name = Path.GetFileName(path);
    
    musicPlayer.clip = clip;
    musicPlayer.Play();
    
  }

}
