using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class DeletCache : MonoBehaviour {

  public Text clearCacheLabel;
  
  public void ClearCache(){
    
    if(GlobalData.cachedir == null)
      CacheOptions.Init();
    
    print("trying to delete cache");
    bool succ = CacheOptions.ClearCache();
    print("success?" + succ);
    
    if(succ){
      clearCacheLabel.text = "<color=\"#4bffab\">Cache cleared</color>";
      StartCoroutine(ResetCCacheButton());
    }
  }
  
  IEnumerator ResetCCacheButton(){
    Stopwatch sw = Stopwatch.StartNew();
    
    while(sw.ElapsedMilliseconds < 5000)
      yield return null;
    
    clearCacheLabel.text = "Clear cache";
  }
}
